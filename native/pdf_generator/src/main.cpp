// main.cpp

/*
    The program inits libharu for PDF generation,
    nlohmann/json for json parsing and prints out
    an input json. Right now uses example .json from NordiskaAPI.
*/

#include "nordiska/error_codes.h"

#include <hpdf.h>

#include <iostream>
#include <string>
#include <filesystem>
#include <fstream>
#include <nlohmann/json.hpp>

static void print_haru_error(
    HPDF_STATUS error_number,
    HPDF_STATUS detail_number,
    void*)
{
    std::cerr << "LibHaru error: " << error_number
              << ", detail: " << detail_number << "\n";
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

    if (std::filesystem::exists(output_pdf_path))
    {
        std::cerr << "Error: output PDF already exists: " << output_pdf_path << "\n";
        return NORDISKA_EXIT_FILE_ERROR;
    }

    HPDF_Doc pdf = HPDF_New(print_haru_error, nullptr); // Create new PDF in memory

    if (pdf == nullptr) // 
    {
        std::cerr << "Error: could not create PDF document\n";
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    HPDF_Page page = HPDF_AddPage(pdf); // Create a page in the PDF-File

    if (page == nullptr)
    {
        std::cerr << "Error: could not add PDF page\n";
        HPDF_Free(pdf);
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    HPDF_Page_SetSize(page, HPDF_PAGE_SIZE_A4, HPDF_PAGE_PORTRAIT); // Set format for PDF
    
    if (HPDF_UseUTFEncodings(pdf) != HPDF_OK)
    {
        std::cerr << "Error: could not enable UTF-8 PDF encoding\n";
        HPDF_Free(pdf);
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
        HPDF_Free(pdf);
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    HPDF_Font font = HPDF_GetFont(pdf, font_name, "UTF-8");

    if (font == nullptr)
    {
        std::cerr << "Error: could not create UTF-8 PDF font\n";
        HPDF_Free(pdf);
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
            HPDF_Page_TextOut(page, 50, 780, "NORDISKA Skatteraport") != HPDF_OK
        )
        {
            return false;
        }

        account_y = 740;
        return true;
    };

    for (const auto& account : accounts)
    {
        if (account_y < 130 && !start_next_page())
        {
            std::cerr << "Error: could not create continuation page\n";
            HPDF_Free(pdf);
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

        for (const auto& transaction : account["transactions"])
        {
            if (account_y < 50)
            {
                if (!start_next_page())
                {
                    std::cerr << "Error: report requires multiple pages\n";
                    HPDF_Free(pdf);
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

            const std::string date = transaction["bookedAt"].get<std::string>().substr(0, 10);
            const std::string type = transaction["type"].get<std::string>();
            const std::string description = transaction["description"].get<std::string>();
            const std::string amount = transaction["amount"].get<std::string>();

            HPDF_Page_TextOut(page, 50, account_y, date.c_str());
            HPDF_Page_TextOut(page, 120, account_y, type.c_str());
            HPDF_Page_TextOut(page, 220, account_y, description.c_str());
            HPDF_Page_TextOut(page, 440, account_y, amount.c_str());

            account_y -= 18;
        }

        account_y -= 24;
    }

    if (account_y < 170 && !start_next_page())
    {
        std::cerr << "Error: could not create summary page\n";
        HPDF_Free(pdf);
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

    HPDF_Page_EndText(page);

    if (HPDF_SaveToFile(pdf, output_pdf_path) != HPDF_OK)
    {
        std::cerr << "Error: could not save PDF: " << output_pdf_path << "\n";
        HPDF_Free(pdf);
        return NORDISKA_EXIT_PDF_GENERATION_ERROR;
    }

    HPDF_Free(pdf);

    std::cout << "Created PDF: " << output_pdf_path << "\n";
    return NORDISKA_EXIT_SUCCESS;

}
