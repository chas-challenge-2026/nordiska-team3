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
        printf("Operation: SIGN\n");
        printf("Input PDF: %s\n", argv[2]);
        printf("Private Key: %s\n", argv[3]);
        printf("Output Signature: %s\n", argv[4]);

        return 0;
    }

    if (strcmp(operation, "verify") == 0)
    {
        printf("Operation: VERIFY\n");
        printf("Input PDF: %s\n", argv[2]);
        printf("Public Key: %s\n", argv[3]);
        printf("Signature: %s\n", argv[4]);

        return 0;
    }

    fprintf(stderr, "Error: Unknown operation - '%s'\n", operation);

    return 1;
}
