#include <iostream>
#include <string>

int main(int argc, char* argv[])
{
    constexpr int expectedArgumentCount = 3; // program name + input JSON + output PDF, to avoid magic numbers!

    if (argc != expectedArgumentCount) // runs when argument count is incorrect
    { 
        std::cerr << "Usage: " << argv[0] // for .NET to seperate error messages from normal output (CERR)
        << " <input-json> <output-pdf>\n"; // print expected arguments
        return 1;   
    
    }


    const std::string inputpath = argv[1]; //  converts C-Style string to C++ string
    const std::string outputpath = argv[2];

    std::cout << "Input: " << inputpath << "\n";
    std::cout << "Output: " << outputpath << "\n";

    return 0;

}
