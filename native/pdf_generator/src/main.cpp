#include "nordiska/error_codes.h"
#include <iostream>
#include <string>

namespace
{
void printUsage()
{
    std::cerr
        << "USAGE: pdf_generator <input-json> <output-pdf>\n";
}
}

int main(int argc, char* argv[])
{
    constexpr int expectedArgumentCount = 3;

    if (argc != expectedArgumentCount)
    {
        std::cerr
            << "ERROR 1 INVALID_INPUT: expected exactly two arguments\n";

        printUsage();

        return NORDISKA_EXIT_INVALID_INPUT;
    }

    const std::string inputPath = argv[1];
    const std::string outputPath = argv[2];

    // These paths will be used when file handling is implemented.
    (void)inputPath;
    (void)outputPath;

    std::cout << "SUCCESS: pdf_generator command accepted\n";

    return NORDISKA_EXIT_SUCCESS;

}
