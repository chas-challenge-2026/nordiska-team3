#include "pdf_signer/signature.h"

#include <openssl/crypto.h>
#include <openssl/evp.h>
#include <openssl/pem.h>
#include <openssl/rsa.h>

#include <stdio.h>

static int reject_password_prompt(
    char* buffer,
    int size,
    int read_or_write,
    void* user_data)
{
    (void)buffer;
    (void)size;
    (void)read_or_write;
    (void)user_data;

    return 0; // Reject any password prompt
}

static NordiskaExitCode load_private_rsa_key(
    const char* private_key_path,
    EVP_PKEY** private_key_out)
{
    if (private_key_path == NULL || private_key_out == NULL)
    {
        fprintf(
            stderr,
            "ERROR 1 INVALID_INPUT: missing private key argument\n"
        );
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    *private_key_out = NULL;

    FILE* key_file = fopen(private_key_path, "rb");

    if (key_file == NULL)
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not open private key file: %s\n",
            private_key_path
        );
        return NORDISKA_EXIT_FILE_ERROR;
    }

    EVP_PKEY* private_key = PEM_read_PrivateKey(
        key_file,
        NULL,
        reject_password_prompt,
        NULL
    );

    if (fclose(key_file) != 0)
    {
        EVP_PKEY_free(private_key);
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not close private key file: %s\n",
            private_key_path
        );
        return NORDISKA_EXIT_FILE_ERROR;
    }

    if (private_key == NULL || EVP_PKEY_is_a(private_key, "RSA") != 1)
    {
        EVP_PKEY_free(private_key);
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: invalid RSA private key\n"
        );
        return NORDISKA_EXIT_SIGNING_ERROR;
    }

    *private_key_out = private_key;
    return NORDISKA_EXIT_SUCCESS;
}

NordiskaExitCode nordiska_sign_sha256_digest(
    const unsigned char digest[NORDISKA_SHA256_SIZE],
    const char* private_key_path,
    const char* signature_path)
{
    if (digest == NULL || private_key_path == NULL || signature_path == NULL)
    {
        fprintf(
            stderr,
            "ERROR 1 INVALID_INPUT: missing argument for signing\n"
        );
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    EVP_PKEY* private_key = NULL;
    EVP_PKEY_CTX* context = NULL;
    unsigned char* signature = NULL;
    FILE* signature_file = NULL;
    int signature_file_created = 0;
    size_t signature_size = 0;

    NordiskaExitCode result = load_private_rsa_key(
        private_key_path,
        &private_key
    );

    if (result != NORDISKA_EXIT_SUCCESS)
    {
        return result;
    }

    result = NORDISKA_EXIT_SIGNING_ERROR;

    context = EVP_PKEY_CTX_new(private_key, NULL);

    if (context == NULL)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not create signing context\n"
        );
        goto cleanup;
    }

    if (EVP_PKEY_sign_init(context) <= 0 ||
        EVP_PKEY_CTX_set_rsa_padding(
            context,
            RSA_PKCS1_PADDING
        ) <= 0 ||
        EVP_PKEY_CTX_set_signature_md(
            context,
            EVP_sha256()
        ) <= 0)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not initialize RSA signing context\n"
        );
        goto cleanup;
    }

    if (EVP_PKEY_sign(
        context,
        NULL,
        &signature_size,
        digest,
        NORDISKA_SHA256_SIZE
        ) <= 0)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not determine signature size\n"
        );
        goto cleanup;
    }

    signature = OPENSSL_malloc(signature_size);

    if (signature == NULL)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not allocate signature memory\n"
        );
        goto cleanup;
    }

    if (EVP_PKEY_sign(
        context,
        signature,
        &signature_size,
        digest,
        NORDISKA_SHA256_SIZE
        ) <= 0)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: cannot sign PDF fingerprint\n"
        );
        goto cleanup;
    }

    signature_file = fopen(signature_path, "wbx");

    if (signature_file == NULL)
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: cannot create signature file %s\n",
            signature_path
        );
        result = NORDISKA_EXIT_FILE_ERROR;
        goto cleanup;
    }

    signature_file_created = 1;

    if (fwrite(
        signature,
        1,
        signature_size,
        signature_file) != signature_size)
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not write signature to file %s\n",
            signature_path
        );
        result = NORDISKA_EXIT_FILE_ERROR;
        goto cleanup;
    }

    if (fclose(signature_file) !=0)
    {
        signature_file = NULL;
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not close signature file %s\n",
            signature_path
        );
        result = NORDISKA_EXIT_FILE_ERROR;
        goto cleanup;
    }

    signature_file = NULL;
    result = NORDISKA_EXIT_SUCCESS;

cleanup:
if (signature_file != NULL)
{
    fclose(signature_file);
}

if (result != NORDISKA_EXIT_SUCCESS &&
    signature_file_created)
{
    if (remove(signature_path) != 0)
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not delete incomplete signature file %s\n",
            signature_path
        );
    }
}

OPENSSL_free(signature);
EVP_PKEY_CTX_free(context);
EVP_PKEY_free(private_key);

return result;
}

static NordiskaExitCode load_public_rsa_key(
    const char* public_key_path,
    EVP_PKEY** public_key_out)
{
    if (public_key_path == NULL || public_key_out == NULL)
    {
        fprintf(
            stderr,
            "ERROR 1 INVALID_INPUT: missing public key argument\n"
        );
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    *public_key_out = NULL;

    FILE* key_file = fopen(public_key_path, "rb");

    if (key_file == NULL)
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not open public key %s\n",
            public_key_path
        );
        return NORDISKA_EXIT_FILE_ERROR;
    }

    EVP_PKEY* public_key = PEM_read_PUBKEY(
        key_file,
        NULL,
        NULL,
        NULL
    );

    if (fclose(key_file) != 0)
    {
        EVP_PKEY_free(public_key);
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not close public key %s\n",
            public_key_path
        );
        return NORDISKA_EXIT_FILE_ERROR;
    }

    if (public_key == NULL ||
        EVP_PKEY_is_a(public_key, "RSA") != 1)
    {
        EVP_PKEY_free(public_key);
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: invalid RSA public key\n"
        );
        return NORDISKA_EXIT_SIGNING_ERROR;
    }

    *public_key_out = public_key;
    return NORDISKA_EXIT_SUCCESS;
}

NordiskaExitCode nordiska_verify_sha256_digest(
    const unsigned char digest[NORDISKA_SHA256_SIZE],
    const char* public_key_path,
    const char* signature_path)
{
    if (digest == NULL ||
        public_key_path == NULL ||
        signature_path == NULL)
    {
        fprintf(
            stderr,
            "ERROR 1 INVALID_INPUT: missing argument for verification\n"
        );
        return NORDISKA_EXIT_INVALID_INPUT;
    }

    EVP_PKEY* public_key = NULL;
    EVP_PKEY_CTX* context = NULL;
    unsigned char* signature = NULL;
    FILE* signature_file = NULL;
    size_t signature_size = 0;

    NordiskaExitCode result = load_public_rsa_key(
        public_key_path,
        &public_key
    );

    if (result != NORDISKA_EXIT_SUCCESS)
    {
        return result;
    }

    result = NORDISKA_EXIT_SIGNING_ERROR;

    int expected_signature_size = EVP_PKEY_get_size(public_key);

    if (expected_signature_size <= 0)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not determine signature size\n"
        );
        goto cleanup;
    }

    signature = OPENSSL_malloc((size_t)expected_signature_size + 1U);

    if (signature == NULL)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not allocate signature memory\n"
        );
        goto cleanup;
    }

    signature_file = fopen(signature_path, "rb");

    if (signature_file == NULL)
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not open signature file %s\n",
            signature_path
        );
        result = NORDISKA_EXIT_FILE_ERROR;
        goto cleanup;
    }

    signature_size = fread(
        signature,
        1,
        (size_t)expected_signature_size + 1U,
        signature_file
    );

    if (ferror(signature_file))
    {
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not read signature file %s\n",
            signature_path
        );
        result = NORDISKA_EXIT_FILE_ERROR;
        goto cleanup;
    }

    if (fclose(signature_file) != 0)
    {
        signature_file = NULL;
        fprintf(
            stderr,
            "ERROR 2 FILE_ERROR: could not close signature file %s\n",
            signature_path
        );
        result = NORDISKA_EXIT_FILE_ERROR;
        goto cleanup;
    }

    signature_file = NULL;

    if (signature_size != (size_t)expected_signature_size)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: invalid signature file size \n"
        );
        goto cleanup;
    }

    context = EVP_PKEY_CTX_new(public_key, NULL);

    if (context == NULL)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not create verification context\n"
        );
        goto cleanup;
    }

    if (EVP_PKEY_verify_init(context) <= 0 ||
        EVP_PKEY_CTX_set_rsa_padding(
            context,
            RSA_PKCS1_PADDING
        ) <= 0 ||
        EVP_PKEY_CTX_set_signature_md(
            context,
            EVP_sha256()
        ) <= 0)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: could not initialize RSA verification context\n"
        );
        goto cleanup;
    }

    int verification_result = EVP_PKEY_verify(
        context,
        signature,
        signature_size,
        digest,
        NORDISKA_SHA256_SIZE
    );

    if (verification_result == 1)
    {
        result = NORDISKA_EXIT_SUCCESS;
    }
    else if (verification_result == 0)
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: signature is invalid\n"
        );
    }
    else
    {
        fprintf(
            stderr,
            "ERROR 4 SIGNING_ERROR: signature verification failed\n"
        );
    }

cleanup:
    if (signature_file != NULL)
    {
        fclose(signature_file);
    }

    OPENSSL_free(signature);
    EVP_PKEY_CTX_free(context);
    EVP_PKEY_free(public_key);

    return result;
}