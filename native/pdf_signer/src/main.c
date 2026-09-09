// main.c

/*  
    Simple program that takes the input of 'user' 
    and and gives out the assigned arguments. 
    There are total 5 arguments expected. 
    Currently serves as a small draft of a complete function.

    To test, first you need to build the project: 
    (make sure to have all required libraries installed)

    '''bash
    cd "D:/'yourpath'/nordiska-team3"

    cmake -S native -B native/build
    cmake --build native/build
    '''

    Command to start the program:
    '''bash
    ./native/build 'prompt' report.pdf private-key.pem report.sig
    '''
*/ 
#include "nordiska/error_codes.h"
#include <stdio.h>
#include <string.h>

static int can_read_pdf(const char* pdfPath)
{
    FILE* pdfFile = fopen(pdfPath, "rb"); //rb = read binary

    if (pdfFile == NULL)
    {
        fprintf(stderr, "Error: cannot open input PDF '%s'\n", pdfPath);
        return NORDISKA_EXIT_FILE_ERROR;
    }

    unsigned char buffer[4096];
    size_t bytesRead;

    while ((bytesRead = fread(buffer, 1, sizeof(buffer), pdfFile)) > 0)
    {
        // NAT-36.
    }

    if (ferror(pdfFile))
    {
        fprintf(stderr, "Error: could not read input PDF '%s'\n", pdfPath);
        fclose(pdfFile);
        return NORDISKA_EXIT_FILE_ERROR;
    }

    fclose(pdfFile);
    return NORDISKA_EXIT_SUCCESS;
}

int main(int argc, char* argv[]) //argv used later to call real data, I suppose.
{
    const int expectedArgumentCount = 5;

    if (argc != expectedArgumentCount)
    {
        fprintf(stderr,  // fprintf is used to write to the standard error stream
                "Usage:\n"
                "  %s sign <input-pdf> <private-key-pem> "
                "<output-signature>\n"
                "  %s verify <input-pdf> <public-key-pem> "
                "<signature-file>\n",
                argv[0],
                argv[0]);
        
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    const char* operation = argv[1];

    if (strcmp(operation, "sign") == 0) // Checks if the operation is 'sign' returns 0(SUCCESS)
    {
        printf("Operation: sign\n");
        printf("Input PDF: %s\n", argv[2]);
        printf("Private Key: %s\n", argv[3]);
        printf("Output Signature: %s\n", argv[4]);

        return NORDISKA_EXIT_SUCCESS;
    }

    if (strcmp(operation, "verify") == 0) // checks if the operation is 'verify' returns 0(SUCCESSb)
    {
        printf("Operation: verify\n");
        printf("Input PDF: %s\n", argv[2]);
        printf("Public Key: %s\n", argv[3]);
        printf("Signature: %s\n", argv[4]);

        return NORDISKA_EXIT_SUCCESS;
    }

    fprintf(stderr, "Error: Unknown operation - '%s'\n", operation);

    return NORDISKA_EXIT_INVALID_INPUT;
}
