#include "nordiska/error_codes.h"

#include <hpdf.h>

#include <iostream>
#include <filesystem>

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
    
    HPDF_Font font = HPDF_GetFont(pdf, "Helvetica", nullptr); // Used font 

    HPDF_Page_BeginText(page); 
    HPDF_Page_SetFontAndSize(page, font, 18);
    HPDF_Page_TextOut(page, 50, 780, "Nordiska Tax Report");
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