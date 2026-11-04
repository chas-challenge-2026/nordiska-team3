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
#include "pdf_signer/signature.h"
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

int main(int argc, char* argv[])
{
    const int expectedArgumentCount = 5; // program name + sign/verify + input PDF + key PEM + signature file

    if (argc != expectedArgumentCount)
    {
        return invalid_input(
            "expected an operation (sign/verify) and three file arguments"
        );
    }

    const char* operation = argv[1];

    if (strcmp(operation, "sign") != 0 &&
        strcmp(operation, "verify") != 0)
    {
        return invalid_input(
            "operation must be 'sign' or 'verify'"
        );
    }
    unsigned char pdfDigest[NORDISKA_SHA256_SIZE];

    NordiskaExitCode hashResult =
        nordiska_calculate_pdf_sha256(argv[2], pdfDigest);

    if (hashResult != NORDISKA_EXIT_SUCCESS)
    {
        return hashResult;
    }

    if (strcmp(operation, "sign") == 0)
    {
        NordiskaExitCode signResult =
            nordiska_sign_sha256_digest(
                pdfDigest,
                argv[3],
                argv[4]
            );

            if (signResult != NORDISKA_EXIT_SUCCESS)
            {
                return signResult;
            }

            printf(
                "SUCCESS: pdf_signer sign command accepted\n"
            );

            return NORDISKA_EXIT_SUCCESS;
        }

     NordiskaExitCode verifyResult =
            nordiska_verify_sha256_digest(
                pdfDigest,
                argv[3],
                argv[4]
            );

        if (verifyResult != NORDISKA_EXIT_SUCCESS)
        {
            return verifyResult;
        }

        printf(
            "SUCCESS: pdf_signer verify command accepted\n"
        );

        return NORDISKA_EXIT_SUCCESS;

}
