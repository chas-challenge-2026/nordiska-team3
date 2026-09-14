#include "pdf_signer/sha256.h"

#include <openssl/evp.h>
#include <stdio.h>

NordiskaExitCode nordiska_calculate_pdf_sha256(
    const char* pdf_path,
    unsigned char digest[NORDISKA_SHA256_SIZE])
    {
        FILE* pdf_file = fopen(pdf_path, "rb");

        if (pdf_file == NULL)
        {
            fprintf(stderr, "Error: cannot open input PDF '%s'\n", pdf_path);
            return NORDISKA_EXIT_FILE_ERROR;
        }

        EVP_MD_CTX* context = EVP_MD_CTX_new();

        if (context == NULL)
        {
            fprintf(stderr, "Error: could not create SHA-256 context\n");
            fclose(pdf_file);
            return NORDISKA_EXIT_SIGNING_ERROR;
        }

        if (EVP_DigestInit_ex(context, EVP_sha256(), NULL) != 1)
        {
            fprintf(stderr, "Error: could not start SHA-256 calculation\n");
            EVP_MD_CTX_free(context);
            fclose(pdf_file);
            return NORDISKA_EXIT_SIGNING_ERROR;
        }

        unsigned char buffer[4096];
        size_t bytes_read;

        while ((bytes_read = fread(buffer, 1, sizeof(buffer), pdf_file)) > 0)
        {
            if (EVP_DigestUpdate(context, buffer, bytes_read) != 1)
            {
                fprintf(stderr, "Error: could not calculate PDF SHA-256\n");
                EVP_MD_CTX_free(context);
                fclose(pdf_file);
                return NORDISKA_EXIT_SIGNING_ERROR;
            }
        }

        if (ferror(pdf_file))
        {
            fprintf(stderr, "Error: could not read input PDF '%s'\n", pdf_path);
            EVP_MD_CTX_free(context);
            fclose(pdf_file);
            return NORDISKA_EXIT_FILE_ERROR;
        }

        unsigned int digest_size;

        if (EVP_DigestFinal_ex(context, digest, &digest_size) != 1 || digest_size != NORDISKA_SHA256_SIZE)
        {
            fprintf(stderr, "Error: could not finish SHA-256 calculation\n");
            EVP_MD_CTX_free(context);
            fclose(pdf_file);
            return NORDISKA_EXIT_SIGNING_ERROR;
        }

        EVP_MD_CTX_free(context);

        if (fclose(pdf_file) != 0)
        {
            fprintf(stderr, "Error: could not close input PDF '%s'\n", pdf_path);
            return NORDISKA_EXIT_FILE_ERROR;
        }

        return NORDISKA_EXIT_SUCCESS;
    }
