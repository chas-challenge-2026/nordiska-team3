# Native Build Guide

This guide explains how to configure, build, and run the Nordiska native programs.

## Environment

The official native development environment is:

- Windows host
- WSL2
- Ubuntu Linux
- CMake 3.25 or newer
- GCC and G++

Run all commands below inside WSL unless stated otherwise.

## Required packages

On a new WSL environment, install the native development tools:

```bash
sudo apt update
sudo apt install build-essential cmake libssl-dev libhpdf-dev valgrind pkg-config
```

Package purposes:

- `build-essential` provides GCC, G++, and basic build tools.
- `cmake` configures the native build.
- `libssl-dev` provides OpenSSL development files.
- `libhpdf-dev` provides the libharu PDF library.
- `valgrind` checks native programs for memory problems.
- `pkg-config` helps build tools find installed libraries.

## Open WSL

From PowerShell:

```powershell
wsl
```

## Open the repository

Inside WSL:

```bash
cd "/mnt/d/nordiska 3/nordiska-team3"
```

The quotation marks are required because the folder path contains spaces.

## Check the toolchain

```bash
cmake --version
gcc --version
g++ --version
```

CMake must be version 3.25 or newer.

## Configure the native build

From the repository root:

```bash
cmake -S native -B native/build -DCMAKE_BUILD_TYPE=Debug
```

Options:

- `-S native` selects the native source directory.
- `-B native/build` selects the generated build directory.
- `-DCMAKE_BUILD_TYPE=Debug` creates a development build with debugging information.

The `native/build` directory contains generated files and must not be committed.

## Build both native programs

```bash
cmake --build native/build --parallel
```

This builds:

- `pdf_generator`
- `pdf_signer`

The `--parallel` option allows independent build operations to run at the same time.

## Run the PDF generator

```bash
./native/build/pdf_generator/pdf_generator
```

Expected output:

```text
Nordiska PDF generator ready
```

Check its exit code immediately:

```bash
echo $?
```

Expected exit code:

```text
0
```

## Run the PDF signer
  
Currently contains a simple program that takes the 
input of 'user' and gives out the assigned arguments. 
There are total 5 arguments expected. 
Currently serves as a small draft of a complete function.

### Run the program:

```bash
./native/build 'task' report.pdf private-key.pem report.sig
```

In **'task'** write either **'sign'** or **'verify'**.

No input will result in a Usage Error:
    ```bash
    Usage:
    ./native/build/pdf_signer/pdf_signer sign
        <input-pdf> <private-key-pem> <output-signature>
    ./native/build/pdf_signer/pdf_signer verify 
        <input-pdf> <public-key-pem> <signature-file>
    ```
    Exit Code: 1

**'sign'** and **'verify'** input results:

    ```bash
    Operation: sign
    Input PDF: report.pdf
    Private Key: private_key.pem
    Output Signature: report.sig
    ```
    Exit Code: 0.

    ```bash
    Operation: verify
    Input PDF: report.pdf
    Public Key: private_key.pem
    Signature: report.sig
    ```
    Exit code: 0

Invalid input result:
    ```bash
    Error: Unknown operation - '(invalid input)'
    ```
    Exit Code: 1.

Check the exit code:

```bash
echo $?
```

Exit code `0` means the program completed successfully.
Exit code `1` means the program has stumbled upon an error.

## Rebuild after changing source code

After editing a `.c` or `.cpp` file, run:

```bash
cmake --build native/build --parallel
```

CMake rebuilds only the targets affected by the changed files.

## Clean and rebuild

To clean the compiled targets and rebuild them:

```bash
cmake --build native/build --clean-first --parallel
```

## Current limitations

The current Week 1 programs are build-environment placeholders.

They do not yet:

- Read JSON report data
- Generate a PDF
- Calculate a SHA-256 hash
- Sign a PDF
- Use libharu or OpenSSL

## Build using CMake presets

From the repository root, enter the native directory:

```bash
cd native
```

Configure and build the Debug version:

```bash
cmake --preset debug
cmake --build --preset debug
```

Run the Debug executables:

```bash
./build/debug/pdf_generator/pdf_generator
./build/debug/pdf_signer/pdf_signer
```

Configure and build the optimized Release version:

```bash
cmake --preset release
cmake --build --preset release
```

Run the Release executables:

```bash
./build/release/pdf_generator/pdf_generator
./build/release/pdf_signer/pdf_signer
```

The direct CMake commands documented earlier remain available when presets are not desired.