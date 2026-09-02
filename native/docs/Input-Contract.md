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