#include "nordiska/error_codes.h"
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

    if (strcmp(operation, "sign") == 0)
    {
        printf(
            "SUCCESS: pdf_signer sign command accepted\n"
        );

        return NORDISKA_EXIT_SUCCESS;
    }

    if (strcmp(operation, "verify") == 0)
    {
        printf(
            "SUCCESS: pdf_signer verify command accepted\n"
        );

        return NORDISKA_EXIT_SUCCESS;
    }

    return invalid_input(
        "operation must be sign or verify"
    );

    
}
