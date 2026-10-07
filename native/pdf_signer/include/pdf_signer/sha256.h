#ifndef PDF_SIGNER_SHA256_H
#define PDF_SIGNER_SHA256_H

#include "nordiska/error_codes.h"

#define	NORDISKA_SHA256_SIZE 32U
#define NORDISKA_SHA256_HEX_SIZE 65U

NordiskaExitCode nordiska_calculate_pdf_sha256(
	const char* pdf_path,
	unsigned char digest[NORDISKA_SHA256_SIZE]);
	
void nordiska_sha256_to_hex(
	const unsigned char digest[NORDISKA_SHA256_SIZE],
	char hex_output[NORDISKA_SHA256_HEX_SIZE]);

#endif
