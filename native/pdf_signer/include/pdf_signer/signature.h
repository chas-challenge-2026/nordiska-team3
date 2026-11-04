#ifndef PDF_SIGNER_SIGNATURE_H
#define PDF_SIGNER_SIGNATURE_H

#include "nordiska/error_codes.h"
#include "pdf_signer/sha256.h"

NordiskaExitCode nordiska_sign_sha256_digest(
    const unsigned char digest[NORDISKA_SHA256_SIZE],
    const char* private_key_path,
    const char* signature_path);

NordiskaExitCode nordiska_verify_sha256_digest(
    const unsigned char digest[NORDISKA_SHA256_SIZE],
    const char* public_key_path,
    const char* signature_path);

    #endif