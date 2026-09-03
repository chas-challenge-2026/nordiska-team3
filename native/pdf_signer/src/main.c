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

#include <stdio.h>
#include <string.h>

int main(int argc, char* argv[]) //argv used later to call real data, I suppose.
{
    const int expectedArgumentCount = 5;

    if (argc != expectedArgumentCount)
    {
        fprintf(stderr,
                "Usage:\n"
                "  %s sign <input-pdf> <private-key-pem> "
                "<output-signature>\n"
                "  %s verify <input-pdf> <public-key-pem> "
                "<signature-file>\n",
                argv[0],
                argv[0]);
        
        return 1;
    }

    const char* operation = argv[1];

    if (strcmp(operation, "sign") == 0)
    {
        printf("Operation: sign\n");
        printf("Input PDF: %s\n", argv[2]);
        printf("Private Key: %s\n", argv[3]);
        printf("Output Signature: %s\n", argv[4]);

        return 0;
    }

    if (strcmp(operation, "verify") == 0)
    {
        printf("Operation: verify\n");
        printf("Input PDF: %s\n", argv[2]);
        printf("Public Key: %s\n", argv[3]);
        printf("Signature: %s\n", argv[4]);

        return 0;
    }

    fprintf(stderr, "Error: Unknown operation - '%s'\n", operation);

    return 1;
}
