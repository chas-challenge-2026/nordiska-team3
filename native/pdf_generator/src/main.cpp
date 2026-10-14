// main.cpp

/*
    The program inits libharu for PDF generation,
    nlohmann/json for json parsing and prints out
    an input json. Right now uses example .json from NordiskaAPI.
*/

#include "nordiska/error_codes.h"

#include <hpdf.h>

#include <cstdlib>
#include <cerrno>
#include <cstring>
#include <unistd.h>
#include <iostream>
#include <string>
#include <filesystem>
#include <fstream>
#include <nlohmann/json.hpp>
#include <algorithm>
#include <vector>
#include <system_error>
#include <memory>
#include <type_traits>

static void print_haru_error(
    HPDF_STATUS error_number,
    HPDF_STATUS detail_number,
    void*)
{
    std::cerr << "LibHaru error: " << error_number
              << ", detail: " << detail_number << "\n";
}

static std::vector<std::string> wrap_text(
    HPDF_Page page,
    const std::string& text,
    HPDF_REAL max_width)
{
    std::vector<std::string> lines;
    std::string line;
    std::string word;

    const auto add_word = [&]()
    {
        if (word.empty())
            return;

        const std::string candidate = line.empty() ? word : line + " " + word;

        if (HPDF_Page_TextWidth(page, candidate.c_str()) <= max_width)
        {
            line = candidate;
            word.clear();
            return;
        }

        if (!line.empty())
        {
            lines.push_back(line);
            line.clear();
        }

        //Split an oversized word without splitting UTF-8 characters
        for (std::size_t position = 0; position < word.size();)
        {
            std::size_t next = position + 1;

            while (next < word.size() && (static_cast<unsigned char>(word[next]) & 0xC0) == 0x80)
            {
                ++next;
            }

            const std::string character = word.substr(position, next - position);
            const std::string fragment = line + character;

            if (!line.empty() && HPDF_Page_TextWidth(page, fragment.c_str()) > max_width)
            {
                lines.push_back(line);
                line.clear();
            }

            line += character;
            position = next;
        }

        word.clear();
    };

    for (char character : text)
    {
        if 
        (
            character == ' ' || 
            character == '\t' ||
            character == '\r' ||
            character == '\n'
        )
        {
            add_word();

            if (character == '\n')
            {
                lines.push_back(line);
                line.clear();
            }
        }
        else
        {
            word += character;
        }
    }
    
    add_word();
    lines.push_back(line);
    return lines;
}

int main(int argc, char* argv[])
{
    constexpr int expected_argument_count = 3; // program name + input JSON + output PDF, to avoid magic numbers!

    if (argc != expected_argument_count) // runs when argument count is incorrect
    { 
        std::cerr << "Usage: " << argv[0] // for .NET to seperate error messages from normal output (CERR)
        << " <input-json> <output-pdf>\n"; // print expected arguments
        return NORDISKA_EXIT_INVALID_INPUT;   
    }

    const char* input_json_path = argv[1];

    std::ifstream input_file(input_json_path);

    if (!input_file)
    {
        std::cerr << "Error: could not open input JSON: " << input_json_path << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    nlohmann::json report_data;

    try
    {
        input_file >> report_data;
    }
    catch(const nlohmann::json::parse_error& error)
    {
        std::cerr << "Error: invaild JSON: " << error.what() << "\n";
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    if (!report_data.is_object() ||
            !report_data.contains("schemaVersion") ||
            !report_data["schemaVersion"].is_number_integer() ||
              report_data["schemaVersion"] != 1)
    {
        std::cerr << "Error: unsupported tax-report JSON schema\n";
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    const auto has_string_field = [](const nlohmann::json& object, const char* field_name)
    {
        return object.contains(field_name) && object[field_name].is_string();
    };

    if 
    (
        !report_data.contains("reportId") ||
        !report_data["reportId"].is_string() ||

        !report_data.contains("reportYear") ||
        !report_data["reportYear"].is_number_integer() ||

        !has_string_field(report_data, "generatedAt") ||
        !has_string_field(report_data, "currency") ||

        !report_data.contains("bank") ||
        !report_data["bank"].is_object() ||

        !report_data.contains("customer") ||
        !report_data["customer"].is_object() ||

        !report_data.contains("accounts") ||
        !report_data["accounts"].is_array() ||

        !report_data.contains("summary") ||
        !report_data["summary"].is_object()
    )
    {
        std::cerr << "Error: tax-report JSON has an invalid header\n";
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    const auto& bank = report_data["bank"];
    const auto& customer = report_data["customer"];
    const auto& accounts = report_data["accounts"];
    const auto& summary = report_data["summary"];

    if
    (
        !has_string_field(summary, "totalDeposits") ||
        !has_string_field(summary, "totalWithdrawals") ||
        !has_string_field(summary, "interestIncome") ||
        !has_string_field(summary, "capitalTaxRate") ||
        !has_string_field(summary, "capitalTax") 
    )
    {
        std::cerr << "Error: tax-report JSON has an invalid summary\n";
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    if
    (
        !has_string_field(bank, "name") ||
        !has_string_field(bank, "organizationNumber") ||
        !has_string_field(customer, "displayName")
    )
    {
        std::cerr << "Error: tax-report JSON is missing header fields\n";
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    for (const auto& account : accounts)
    {
        if 
        (
            !account.is_object() ||
            !has_string_field(account, "displayNumber") ||
            !has_string_field(account, "accountType") ||
            !has_string_field(account, "openingBalance") ||
            !has_string_field(account, "closingBalance") ||
            !account.contains("transactions") ||
            !account["transactions"].is_array()
        )
        {
            std::cerr << "Error: tax-report JSON has an invalid account\n";
            return NORDISKA_EXIT_INVALID_INPUT;
        }   

            for (const auto& transaction : account["transactions"])
        {
            if 
            (
                !transaction.is_object() ||
                !has_string_field(transaction, "bookedAt") ||
                !has_string_field(transaction, "type") ||
                !has_string_field(transaction, "description") ||
                !has_string_field(transaction, "amount") ||
                transaction["bookedAt"].get<std::string>().size() < 10
            )
            {
                std::cerr << "Error: tax-report JSON has an invalid transaction\n";
                return NORDISKA_EXIT_INVALID_INPUT;
            }
        }
    }

    const std::string bank_name = bank["name"].get<std::string>();
    const std::string organization_number = bank["organizationNumber"].get<std::string>();
    const std::string customer_name = customer["displayName"].get<std::string>();
    const int report_year = report_data["reportYear"].get<int>();
    const std::string generated_at = report_data["generatedAt"].get<std::string>();
    const std::string currency = report_data["currency"].get<std::string>();

    const bool ends_with_utc_offset = 
                            generated_at.size() >= 6 && 
                                generated_at.compare(generated_at.size() - 6, 6, "+00:00") == 0;

    if 
    (
        generated_at.size() < 17 ||
        generated_at[10] != 'T' ||
      (generated_at.back() != 'Z' && !ends_with_utc_offset)
    )
    {
        std::cerr << "Error: invalid generatedAt timestamp\n";
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    const std::string generated_display =
        generated_at.substr(0, 10) + " " +
        generated_at.substr(11, 5) + " UTC";

    const char* output_pdf_path = argv[2]; // Path for creating a PDF-File

    std::error_code path_error;

    const bool output_exists = std::filesystem::exists(output_pdf_path, path_error);

    if (path_error)
    {
        std::cerr << "Error: could not inspect output path: " << path_error.message() << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }
    if (output_exists)
    {
        std::cerr << "Error: output PDF already exists: " << output_pdf_path << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    const std::filesystem::path output_path(output_pdf_path);

    const std::filesystem::path output_folder = 
        output_path.has_parent_path()
        ? output_path.parent_path()
        : std::filesystem::path(".");

    path_error.clear();

    const bool folder_exists = std::filesystem::is_directory(output_folder, path_error);

    if (path_error || !folder_exists)
    {
        std::cerr << "Error: output folder is missing or inaccessible: " << output_folder << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    HPDF_Doc pdf = HPDF_New(print_haru_error, nullptr); // Create new PDF in memory

    if (pdf == nullptr) // PDF allocation failure
    {
        std::cerr << "Error: could not create PDF document\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    std::unique_ptr<
        std::remove_pointer_t<HPDF_Doc>,
        decltype(&HPDF_Free)
    > pdf_guard(pdf, &HPDF_Free);

    HPDF_Page page = HPDF_AddPage(pdf); // Create a page in the PDF-File

    if (page == nullptr)
    {
        std::cerr << "Error: could not add PDF page\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    HPDF_Page_SetSize(page, HPDF_PAGE_SIZE_A4, HPDF_PAGE_PORTRAIT); // Set format for PDF
    
    if (HPDF_UseUTFEncodings(pdf) != HPDF_OK)
    {
        std::cerr << "Error: could not enable UTF-8 PDF encoding\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    const std::filesystem::path font_path = 
        std::filesystem::path(argv[0]).parent_path() /
        "assets" /
        "DejaVuSans.ttf";

    const std::string font_path_string = font_path.string();

    const char* font_name = HPDF_LoadTTFontFromFile(
        pdf,
        font_path_string.c_str(),
        HPDF_TRUE
    );

    if (font_name == nullptr)
    {
        std::cerr << "Error: could not load PDF font\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    HPDF_Font font = HPDF_GetFont(pdf, font_name, "UTF-8");

    if (font == nullptr)
    {
        std::cerr << "Error: could not create UTF-8 PDF font\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    const std::string organization_line = "Org.nr.: " + organization_number;
    const std::string customer_line = "Kund: " + customer_name;
    const std::string year_line = "År: " + std::to_string(report_year);
    const std::string generated_line = "Skapat: " + generated_display;
    const std::string currency_line = "Valuta: " + currency;

    HPDF_Page_BeginText(page);

    HPDF_Page_SetFontAndSize(page, font, 18);
    HPDF_Page_TextOut(page, 50, 780, "NORDISKA Skatterapport");

    HPDF_Page_SetFontAndSize(page, font, 12);
    HPDF_Page_TextOut(page, 50, 750, bank_name.c_str());
    HPDF_Page_TextOut(page, 50, 732, organization_line.c_str());
    HPDF_Page_TextOut(page, 50, 700, customer_line.c_str());
    HPDF_Page_TextOut(page, 50, 682, year_line.c_str());
    HPDF_Page_TextOut(page, 50, 664, generated_line.c_str());
    HPDF_Page_TextOut(page, 50, 646, currency_line.c_str());
    
    HPDF_Page_SetFontAndSize(page, font, 14);
    HPDF_Page_TextOut(page, 50, 610, "Konton");

    HPDF_Page_SetFontAndSize(page, font, 12);

    HPDF_REAL account_y = 588;

    const auto start_next_page = [&]() -> bool
    {
        if (HPDF_Page_EndText(page) != HPDF_OK)
            return false;
        
        page = HPDF_AddPage(pdf);

        if (page == nullptr)
            return false;
        
        if 
        (
            HPDF_Page_SetSize(page, HPDF_PAGE_SIZE_A4, HPDF_PAGE_PORTRAIT) != HPDF_OK ||
            HPDF_Page_BeginText(page) != HPDF_OK ||
            HPDF_Page_SetFontAndSize(page, font, 12) != HPDF_OK ||
            HPDF_Page_TextOut(page, 50, 780, "NORDISKA Skatterapport") != HPDF_OK
        )
        {
            return false;
        }

        account_y = 740;
        return true;
    };

    if (accounts.empty())
    {
        HPDF_Page_TextOut(page, 50, account_y, "Inga konton att redovisa.");

        account_y -= 26;
    }

    for (const auto& account : accounts)
    {
        if (account_y < 130 && !start_next_page())
        {
            std::cerr << "Error: could not create continuation page\n";
            return NORDISKA_EXIT_PDF_GENERATION_ERROR;
        }

        const std::string account_line = account["displayNumber"].get<std::string>() + " (" + account["accountType"].get<std::string>() + ")";
        const std::string balance_line = "Ingående saldo: " + account["openingBalance"].get<std::string>() + "  Utgående saldo: " + account["closingBalance"].get<std::string>();

        HPDF_Page_SetFontAndSize(page, font, 12);
        HPDF_Page_TextOut(page, 50, account_y, account_line.c_str());
        account_y -= 26;

        HPDF_Page_TextOut(page, 50, account_y, balance_line.c_str());
        account_y -= 18;

        HPDF_Page_SetFontAndSize(page, font, 10);
        HPDF_Page_TextOut(page, 50, account_y, "Datum");
        HPDF_Page_TextOut(page, 120, account_y, "Typ");
        HPDF_Page_TextOut(page, 220, account_y, "Beskrivning");

        const std::string amount_heading = "Belopp (" + currency + ")";
        HPDF_Page_TextOut(page, 440, account_y, amount_heading.c_str());
        account_y -= 18;

        if (account["transactions"].empty())
        {
            HPDF_Page_TextOut(page, 50, account_y, "Inga transactioner under rapportåret.");
            account_y -= 18;
        }

        for (const auto& transaction : account["transactions"])
        {

            const std::string date = transaction["bookedAt"].get<std::string>().substr(0, 10);
            const std::string amount = transaction["amount"].get<std::string>();

            HPDF_Page_SetFontAndSize(page, font, 10);

            const auto type_lines = wrap_text(page, transaction["type"].get<std::string>(), 90);
            const auto description_lines = wrap_text(page, transaction["description"].get<std::string>(), 210);
            const auto amount_lines = wrap_text(page, amount, 105);
            
            const std::size_t line_count = std::max({type_lines.size(), description_lines.size(), amount_lines.size()});

            for (std::size_t index = 0; index < line_count; ++index)
            {
                if (account_y < 50)
                {
                    if (!start_next_page())
                    {
                        std::cerr << "Error: could not create continuation page\n";
                        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
                    }
                
                    HPDF_Page_SetFontAndSize(page, font, 12);
                    HPDF_Page_TextOut(page, 50, account_y, account_line.c_str());
                    account_y -= 26;
                
                    HPDF_Page_SetFontAndSize(page, font, 10);
                    HPDF_Page_TextOut(page, 50, account_y, "Datum");
                    HPDF_Page_TextOut(page, 120, account_y, "Typ");
                    HPDF_Page_TextOut(page, 220, account_y, "Beskrivning");
                    HPDF_Page_TextOut(page, 440, account_y, amount_heading.c_str());
                    account_y -= 18;
                }

                if (index == 0)
                {
                    HPDF_Page_TextOut(page, 50, account_y, date.c_str());
                }
                if (index < type_lines.size())
                {
                    HPDF_Page_TextOut(page, 120, account_y, type_lines[index].c_str());
                }
                if (index < description_lines.size())
                {
                    HPDF_Page_TextOut(page, 220, account_y, description_lines[index].c_str()); // Susie Deltarune
                }
                if (index < amount_lines.size())
                {
                    HPDF_Page_TextOut(page, 440, account_y, amount_lines[index].c_str());
                }
                account_y -= 18;
            }
        }

        account_y -= 24;
    }

    if (account_y < 170 && !start_next_page())
    {
        std::cerr << "Error: could not create summary page\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }
    
    HPDF_Page_SetFontAndSize(page, font, 14);
    HPDF_Page_TextOut(page, 50, account_y, "Sammanfattning");
    account_y -= 26;

    HPDF_Page_SetFontAndSize(page, font, 12);

    const std::string summary_lines[] =
    {
        "Totala insättningar: " + summary["totalDeposits"].get<std::string>() + " " + currency,
        "Totala uttag: " + summary["totalWithdrawals"].get<std::string>() + " " + currency,
        "Ränteinkomster: " + summary["interestIncome"].get<std::string>() + " " + currency,
        "Skattesats (decimalform): " + summary["capitalTaxRate"].get<std::string>(),
        "Kapitalskatt: " + summary["capitalTax"].get<std::string>() + " " + currency,
    };

    for (const auto& line : summary_lines)
    {
        HPDF_Page_TextOut(page, 50, account_y, line.c_str());
        account_y -= 18;
    }

    if (HPDF_Page_EndText(page) != HPDF_OK)
    {
        std::cerr << "Error: could not finish PDF text\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    // Create a unique temporary file beside the requested PDF
    std::string temporary_path = std::string(output_pdf_path) + ".tmp.XXXXXX"; // that buncha X's is placeholder for mkstemp

    const int temporary_fd = mkstemp(temporary_path.data());

    if (temporary_fd == -1)
    {
        std::cerr << "Error: could not create temporary PDF: " << std::strerror(errno) << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    // Remove only temp file when this scope ends
    const auto remove_temporary = [](char* path)
    {
        std::error_code cleanup_error;
        std::filesystem::remove(path, cleanup_error);

        if (cleanup_error)
        {
            std::cerr << "Warning: could not remove temporary PDF: " << path << ": " << cleanup_error.message() << "\n";
        }
    };

    std::unique_ptr<char, decltype(remove_temporary)>temporary_guard(temporary_path.data(), remove_temporary);

    if (close(temporary_fd) != 0)
    {
        std::cerr << "Error: could not close temporary PDF: " <<  std::strerror(errno) << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    const HPDF_STATUS save_status = HPDF_SaveToFile(pdf, temporary_path.c_str());

    if (save_status != HPDF_OK)
    {
        std::cerr << "Error: could not save PDF\n";

        if 
        (
            save_status == HPDF_FILE_OPEN_ERROR ||
            save_status == HPDF_FILE_IO_ERROR
        )
        {
            return NORDISKA_EXIT_FILE_ERROR;
        }

        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    // Publish the completed file without replacing an existing destination
    std::error_code publish_error;

    std::filesystem::create_hard_link(temporary_path, output_pdf_path, publish_error);

    if (publish_error)
    {
        std::cerr << "Error: could not publish PDF: " << publish_error.message() << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    std::cout << "Created PDF: " << output_pdf_path << "\n";
    return NORDISKA_EXIT_SUCCESS;

}
