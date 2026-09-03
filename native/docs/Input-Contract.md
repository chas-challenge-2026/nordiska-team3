# PDF Generator Input Contract

Status: Draft v0.1

This document defines how the .NET backend or a developer starts the native PDF generator.

## Command

```text
pdf_generator <input-json> <output-pdf>
```

Example:

```bash
pdf_generator report.json report.pdf
```

## Arguments

### Input JSON

The first argument is the path to a JSON file containing backend-prepared tax-report data.

The input file:

- Must exist
- Must be readable
- May use an absolute or relative path
- Must contain valid JSON
- Must not require native database access

The exact JSON fields will be defined during later stages

### Output PDF

The second argument is the requested output PDF path.

The output path:

- May be absolute or relative
- Should use the `.pdf` extension
- Must have an existing writable parent directory
- Must not be the same path as the input file
- Must not silently overwrite an existing file

## Argument count

The generator accepts exactly two user-supplied arguments.

No optional command-line flags are supported in contract version 0.1.

An incorrect argument count is invalid input.

## Paths containing spaces

Paths containing spaces must be passed as one quoted argument when running manually:

```bash
pdf_generator "input files/report.json" "output files/report.pdf"
```

The .NET backend should pass arguments using its structured process argument list rather than manually joining paths into one command string.

## Current implementation stage

generator only (will be upgraded in later stages):

- Validates the number of arguments
- Reads the two path strings
- Prints the received paths for testing

It does not open, parse, or generate files yet.

File validation and JSON parsing will be added in later backlog tasks.

## Related contracts

- Exit-code values will be finalized in later stages.
- `stdout` and `stderr` rules will be finalized in later stages.
- The JSON schema will be agreed with the backend team in later stages.

# PDF Signer Command-Line Contract

Status: Draft v0.1

The signer supports two operations:

- `sign`
- `verify`

## Sign command

```text
pdf_signer sign <input-pdf> <private-key-pem> <output-signature>
```

Example:

```bash
pdf_signer sign report.pdf private-key.pem report.sig
```

Arguments:

1. `sign` selects the signing operation.
2. `<input-pdf>` is the PDF whose exact bytes will be signed.
3. `<private-key-pem>` is the RSA private-key file used for signing.
4. `<output-signature>` is the detached signature output path.

The signer must never print or copy private-key contents into normal output, errors, logs, or result metadata.

A private-key password must not be passed directly as a command-line argument because command-line arguments can be visible to other processes.

## Verify command

```text
pdf_signer verify <input-pdf> <public-key-pem> <signature-file>
```

Example:

```bash
pdf_signer verify report.pdf public-key.pem report.sig
```

Arguments:

1. `verify` selects the verification operation.
2. `<input-pdf>` is the PDF being verified.
3. `<public-key-pem>` is the public key matching the private signing key.
4. `<signature-file>` is the detached signature being checked.

## Result contract

A successful signing operation will eventually produce:

- A detached signature file
- The PDF SHA-256 fingerprint
- The signing algorithm
- Signing success status
- The signature output path

A successful verification operation will eventually produce:

- The PDF SHA-256 fingerprint
- Verification success status
- Whether the signature matches the PDF and public key

The exact machine-readable result format will be implemented in NAT-41.

Exit-code values will be finalized in NAT-15, and `stdout`/`stderr` behavior will be finalized in NAT-17.

## Safety rules

- The signer computes SHA-256 itself.
- The private key is provided by file path, never hardcoded.
- A private key must never be committed to Git.
- Signing must not silently overwrite an existing signature.
- Verification does not modify the PDF or signature.
- A failed operation must not leave an output that looks successful.

## Current implementation stage

During NAT-13, the signer only:

- Validates the number of arguments
- Recognizes `sign` and `verify`
- Reads the supplied path strings
- Prints the received paths for testing

It does not open files, hash, sign, or verify anything yet.