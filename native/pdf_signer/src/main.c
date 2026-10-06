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
    ./native/build/pdf_signer/pdf_signer 'prompt' report.pdf private-key.pem report.sig
    '''
*/ 
#include "nordiska/error_codes.h"
#include "pdf_signer/sha256.h"
#include <stdio.h>
#include <string.h>

static void print_usage(void)
{
    fprintf(
        stderr,
        "USAGE: pdf_signer sign "
        "<input-pdf> <private-key-pem> <output-signature>\n"
        "USAGE: pdf_signer verify "
        "<input-pdf> <public-key-pem> <signature-file>\n"
    );
}

static int invalid_input(const char* message)
{
    fprintf(
        stderr,
        "ERROR 1 INVALID_INPUT: %s\n",
        message
    );

    print_usage();

    return NORDISKA_EXIT_INVALID_INPUT;
}

int main(int argc, char* argv[]) //argv used later to call real data, I suppose.
{
    
     const int expectedArgumentCount = 5;

    if (argc != expectedArgumentCount)
    {
        return invalid_input(
            "expected an operation and three file arguments"
        );
    }

    const char* operation = argv[1];

    if (strcmp(operation, "sign") == 0 ||
        strcmp(operation, "verify") == 0)
    {
        unsigned char pdfDigest[NORDISKA_SHA256_SIZE];

        int hashResult =
            nordiska_calculate_pdf_sha256(argv[2], pdfDigest);

        if (hashResult != NORDISKA_EXIT_SUCCESS)
        {
            return hashResult;
        }
        char pdfHashHex[NORDISKA_SHA256_HEX_SIZE];
        nordiska_sha256_to_hex(pdfDigest, pdfHashHex);
    }


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

    return invalid_input(
        "operation must be sign or verify"
    );

    
}
