# Nordiska System Developer Context

Use this file as the shared handoff for the three C/C++ system developers and their AI helpers. Read it together with the current repository before every task.

The repository is the source of truth for implemented code. This file records team decisions, responsibilities, and the planned direction. If the repository and this document differ, point out the difference before making changes. Never silently assume that a planned feature already exists.

## Current repository reality

Last inspected on 2026-08-31:

- The application is still the deliberately flawed v1 starting point.
- The backend is a .NET 6 Razor Pages monolith using raw Npgsql queries.
- PostgreSQL is version 12 in Docker Compose.
- The frontend and shared folders contain no implementation.
- `native/` contains only `README.md`.
- There are no C, C++, header, CMake, native test, generator, signer, benchmark, or native integration files yet.
- The existing tax-report page synchronously queries transactions, waits 50 ms per transaction, builds plain text, and returns it with a `.pdf` extension. It is not a real PDF.
- There is no backend `TaxReport` entity, report-status lifecycle, background report worker, `AuditEntry` entity, or native process invocation.
- The current CI only protects the Docker Compose platform contract.

Re-inspect the repository at the start of future work because this snapshot will become outdated as the team implements v2.

## Target project direction

Nordiska v2 replaces v1 with approximately this architecture:

```text
React 18 + TypeScript frontend
        ↓ HTTP/JSON
.NET 8 ASP.NET Core Web API
        ↓
Services
        ↓
Repositories
        ↓
EF Core 8 / DbContext
        ↓
PostgreSQL 15
```

An API is a defined way for programs to communicate. An endpoint is one address and operation exposed by an API. JSON is a text format for structured data. A repository is a backend component that reads and writes data, and a service contains application operations and rules. A background job performs work outside the normal web request so the user does not have to wait for long processing.

The target tax-report flow is:

```text
Customer/frontend
        ↓
.NET API and background job
        ↓ prepared report input
Native C++ PDF generator
        ↓ real PDF
Native C PDF signer
        ↓ hash/signature result
.NET updates TaxReport and AuditEntry
        ↓
Frontend shows status and download
```

## Ownership boundary

The .NET/backend team owns:

- Authentication, JWT, BCrypt, and rate limiting
- Account, deposit, withdrawal, and transaction APIs
- Financial business rules and the immutable ledger
- Database access, EF Core, and migrations
- `TaxReport` records and statuses such as `QUEUED`, `PROCESSING`, `READY`, and `FAILED`
- Background-job scheduling and retry behavior
- Persistent `AuditEntry` records
- FAQ, notification workers, backend logging, exception handling, and `/health`

A ledger is a list of permanent money movements. The backend calculates a balance by summing entries instead of allowing native code to mutate balances.

Our three system developers own:

- The native C++ PDF generator
- The native C PDF signer
- CMake and native build structure
- Native input validation and predictable error handling
- The .NET-to-native interface contract
- Native tests, batch benchmarks, memory/resource safety, and Valgrind checks
- Windows, WSL/Linux, Docker, and native CI compatibility
- Native technical documentation
- Hash, signature, status, and error metadata returned to .NET

Native code must not access the database, own financial business rules, manage the complete audit system, or persist `TaxReport` state. It receives backend-prepared data, processes it, and returns technical results.

## System Developer roles

The roles identify temporary leads. They do not prevent anyone from reviewing or learning another area. All three system developers should understand and demonstrate the complete native pipeline.

### System Developer A — PDF Generator lead

Primary responsibilities:

- C++ generator structure
- Report and transaction data structures
- JSON reading and generator-side validation
- Real PDF generation with an approved native PDF library such as libharu
- Transaction rendering, totals, page layout, and multiple pages
- Generator performance and generator-specific tests
- Generator architecture and performance documentation

### System Developer B — PDF Signer and Security lead

Primary responsibilities:

- C signer structure
- File hashing with SHA-256
- RSA signing using the OpenSSL 3.x EVP API and PKCS#1 rules
- Signature verification
- Key input validation and safe OpenSSL resource cleanup
- Clear hash, signature, success, and failure metadata
- Signer tests and security/key-handling documentation

A hash is a fixed-size fingerprint of data. SHA-256 is the selected hash algorithm. A private key creates a digital signature, while its matching public key verifies it. RSA is a public-key cryptography method, and PKCS#1 defines rules and formats for RSA operations. OpenSSL is the library our C code calls; we do not implement the cryptographic mathematics ourselves.

### System Developer C — Integration, Build, and Testing lead

Primary responsibilities:

- Shared and per-module CMake configuration
- Repeatable Windows and Linux build commands
- Command-line arguments and executable names
- JSON input-contract and native error-contract coordination
- Sample valid and invalid inputs
- CTest and end-to-end pipeline tests
- Generator-to-signer orchestration tests
- Benchmark tooling and recorded results
- Native CI, Docker integration support, and Valgrind procedure
- Documentation for the .NET team

CMake is a tool that creates repeatable build instructions for C and C++. CTest is CMake's test runner. CI, or continuous integration, automatically builds and tests code after it is pushed. Valgrind is a Linux tool that can detect invalid memory use and memory leaks.

## Agreed native file structure

Follow this structure throughout the project:

```text
native/
├── README.md
├── CMakeLists.txt
├── CMakePresets.json
├── .gitignore
├── common/
│   └── include/nordiska/error_codes.h
├── pdf_generator/
│   ├── CMakeLists.txt
│   ├── include/pdf_generator/
│   │   ├── report_data.hpp
│   │   ├── json_reader.hpp
│   │   └── pdf_generator.hpp
│   └── src/
│       ├── main.cpp
│       ├── json_reader.cpp
│       └── pdf_generator.cpp
├── pdf_signer/
│   ├── CMakeLists.txt
│   ├── include/pdf_signer/
│   │   ├── sha256.h
│   │   └── pdf_signer.h
│   └── src/
│       ├── main.c
│       ├── sha256.c
│       └── pdf_signer.c
├── tests/
│   ├── CMakeLists.txt
│   ├── generator_tests.cpp
│   ├── signer_tests.c
│   └── fixtures/
│       ├── valid_report.json
│       ├── empty_transactions.json
│       ├── missing_customer.json
│       └── malformed.json
├── scripts/
│   ├── run_pipeline.ps1
│   └── run_pipeline.sh
├── benchmarks/
│   ├── CMakeLists.txt
│   └── batch_benchmark.cpp
└── docs/
    ├── build.md
    ├── input-contract.md
    ├── error-contract.md
    ├── integration.md
    ├── security.md
    └── performance.md
```

File meanings:

- `.c` contains C implementation code.
- `.cpp` contains C++ implementation code.
- `.h` and `.hpp` are header files containing declarations shared between source files.
- `src/` contains implementations.
- `include/` contains public declarations.
- `fixtures/` contains saved test inputs.
- `scripts/` contains repeatable manual pipeline commands.

Do not create every planned file in advance. Add a file when its responsibility is reached. Keep new work within this structure. If a genuinely necessary file does not fit, explain why and ask the team/user before adding it. When the team says the structure or project is done, treat that decision as authoritative unless the repository introduces a clear conflict.

Some integration work belongs outside `native/`, for example `.github/workflows/native-build.yml`, Docker changes, or backend process-invocation code. Coordinate those changes with their owners.

## Integration decisions

The agreed student-friendly starting direction is process invocation:

```text
.NET background job
        ↓ writes prepared input
Starts native executable
        ↓
Native reads input and creates output
        ↓
.NET reads exit code, stdout, stderr, and result files
        ↓
.NET updates TaxReport and AuditEntry
```

Process invocation means .NET starts our executable like another command-line program. `ProcessStartInfo` is the .NET configuration object commonly used to select the executable, arguments, working directory, and captured output.

The preferred initial generator command is:

```text
pdf_generator <input-json> <output-pdf>
```

For example:

```text
pdf_generator report.json report.pdf
```

JSON files are the preferred initial data boundary. The exact JSON fields must be agreed with the backend team and documented in `native/docs/input-contract.md`. The exact signer command must be agreed with System Developer B and the backend team before it is treated as final.

Process invocation and JSON are agreed recommendations, not existing repository implementations. A shared-library C ABI/P/Invoke approach may be considered later only if measurements or integration requirements justify it. An ABI is the low-level agreement that lets compiled programs call each other; P/Invoke is .NET's mechanism for calling native-library functions.

## Error and output contract

Native programs must use:

- `stdout` for normal useful results
- `stderr` for errors
- The process exit code for success or failure

The provisional error-code design is:

```text
0 = success
1 = invalid input
2 = file error
3 = PDF generation error
4 = signing error
```

These values are not final until agreed with the .NET team and recorded in `native/docs/error-contract.md`. Do not silently swallow errors. A failed operation must not report success or leave an output that looks complete.

## How the three developers work independently

System Developer C can make progress before A or B finishes by working against stable external contracts rather than unfinished internal implementations.

Use these techniques:

1. Agree executable names, command arguments, file locations, exit codes, and output rules early.
2. Use tiny placeholder executables during the foundation stage. A placeholder is temporary code that prints its identity and returns a predictable result. It proves the build and process boundary without implementing another developer's module.
3. Create valid and invalid JSON fixtures before the parser is finished.
4. Add contract tests for externally visible behavior, such as missing arguments returning a nonzero exit code.
5. Allow some future-facing tests to fail clearly until the responsible implementation is delivered.
6. Keep module internals behind stable headers and command-line interfaces so implementations can change without breaking integration work.
7. Merge the shared CMake foundation before A and B build their deeper modules, then keep later pull requests small.

Creating a placeholder entry point is shared integration work. Replacing it with actual PDF or signing behavior belongs to the relevant module lead.

## Nine-week native plan

### Week 1 — Native foundation

Goal: prove the native build environment works before adding PDF or cryptography complexity.

System Developer A:

- Create the minimal C++ `pdf_generator` entry program.
- Confirm basic C++ compilation and execution.

System Developer B:

- Create the minimal C `pdf_signer` entry program.
- Confirm basic C compilation and check OpenSSL availability.

System Developer C:

- Create the shared and per-module CMake structure.
- Document repeatable configure, build, and run commands.
- Confirm both executables build and run.
- Record toolchain, OpenSSL, libharu, and Valgrind availability.

Week 1 is done when both minimal programs build, print an identification message, return exit code `0`, and can be built using documented commands.

### Week 2 — Input and interface contract

Goal: define how prepared report data enters the generator and how errors leave native programs.

System Developer A:

- Add report and transaction structures.
- Begin JSON file reading.

System Developer B:

- Define validation needs for signer inputs.
- Review shared error behavior.

System Developer C:

- Coordinate and document JSON fields and command-line arguments.
- Establish provisional exit codes, `stdout`, and `stderr` behavior.
- Add initial valid and invalid fixtures.

Week 2 is done when a sample report can be parsed or rejected predictably and the v1 input/error contracts are documented.

### Week 3 — First real PDF and SHA-256 hash

Goal: produce one simple valid PDF and prove PDF hashing.

System Developer A:

- Connect the approved PDF library.
- Generate a simple one-page tax report from sample input.

System Developer B:

- Read a PDF file using C/OpenSSL.
- Calculate and output its SHA-256 fingerprint.

System Developer C:

- Add command-line and fixture-based tests.
- Verify that expected output files and error results are produced.

Week 3 is done when sample JSON produces a real PDF and the signer can calculate its SHA-256 hash.

### Week 4 — Report content and initial signing

Goal: support realistic reports and create a verifiable RSA signature.

System Developer A:

- Render multiple transactions, totals, customer/account information, and report year.
- Add multiple pages when content exceeds one page.

System Developer B:

- Load a private key safely.
- Sign the SHA-256 result using OpenSSL and RSA/PKCS#1.
- Verify the signature with the matching public key.

System Developer C:

- Create the first generator-to-signer pipeline test.
- Record repeatable success and failure commands.

Week 4 is done when prepared report data becomes a real PDF, hash, signature, and successful verification result.

### Week 5 — .NET integration

Goal: make the native programs callable by the backend team's background job.

System Developer A:

- Remove remaining hardcoded report values.
- Ensure all report content comes through the agreed input.

System Developer B:

- Make signer output machine-readable and predictable.
- Return hash, signature status, and useful errors.

System Developer C:

- Lead native-side coordination with the .NET team.
- Provide executable paths, arguments, exit-code rules, and captured-output examples.
- Help test `ProcessStartInfo` calls without taking ownership of the backend job or database lifecycle.

Week 5 is done when a backend test or test harness can call both native programs and receive useful results.

### Week 6 — Reliability and failure safety

Goal: make failures predictable and protect resources.

System Developer A:

- Test empty transactions, long text, missing files, bad output paths, and large transaction lists.

System Developer B:

- Test missing PDFs, missing/invalid keys, malformed inputs, signing failures, and OpenSSL cleanup.

System Developer C:

- Add CTest coverage for generator, signer, and pipeline failures.
- Confirm failures never appear as successful/ready reports.
- Verify exit codes and error messages match the contract.

Week 6 is done when expected failures are covered by repeatable tests and no obvious resource leak remains.

### Week 7 — Batch and performance

Goal: measure the 10,000-PDF target before considering concurrency.

System Developer A:

- Measure and improve generator performance for 1, 10, 100, 1,000, and 10,000 reports.

System Developer B:

- Measure hashing, signing, and verification cost.

System Developer C:

- Build the repeatable batch benchmark.
- Record machine/environment details and timing results.
- Identify the actual bottleneck with A and B.

Week 7 is done when the team has reproducible measurements and evidence for any proposed optimization. Do not add threads merely because the target sounds large.

### Week 8 — Linux, Docker, CI, and memory checks

Goal: prove the native pipeline works in the real deployment-style environment.

System Developer A:

- Make generator and PDF-library dependencies work under Linux/Docker.

System Developer B:

- Make OpenSSL signing and safe runtime key loading work under Linux/Docker.

System Developer C:

- Add native CMake build/test steps to CI.
- Coordinate native Docker build/runtime integration.
- Run or coordinate Valgrind tests and document results.

Week 8 is done when native code builds and tests on Linux/CI and the integrated container path is proven.

### Week 9 — Documentation, hardening, and demonstration

Goal: finish a student-readable, reproducible native deliverable.

System Developer A:

- Document generator architecture, input use, PDF behavior, and performance results.

System Developer B:

- Document SHA-256, signing, verification, errors, and key handling.

System Developer C:

- Document CMake, testing, .NET integration, Docker/CI, and the complete demonstration procedure.

All three:

- Review the entire native pipeline.
- Run the full clean build and test sequence.
- Prepare to explain why native modules are used, how data enters, how errors return, how signing works, and how performance and memory safety were tested.

Week 9 is done when another developer can follow the documentation on a clean environment and reproduce the build, tests, and demonstration.

## Immediate starting scope

At the beginning of Week 1, create only:

```text
native/
├── README.md
├── CMakeLists.txt
├── .gitignore
├── docs/build.md
├── pdf_generator/
│   ├── CMakeLists.txt
│   └── src/main.cpp
└── pdf_signer/
    ├── CMakeLists.txt
    └── src/main.c
```

The entry programs should initially print a simple identification message and return `0`. Do not add PDF generation, JSON parsing, OpenSSL signing, or a large collection of empty files during the first step.

## Stable engineering principles

- Start with simple, student-readable code.
- Prefer small functions, clear structures, and clear headers.
- Avoid hidden global state unless it is justified.
- Do not hardcode customer data, secrets, or private keys.
- Never commit real private keys.
- Keep generator and signer responsibilities separate.
- Validate inputs at the native boundary.
- Do not duplicate backend financial rules.
- Use predictable exit codes and useful messages.
- Build, run, and test after meaningful changes.
- Explain errors before fixing them.
- Measure before optimizing or adding concurrency.
- Keep manual and automated testing reproducible.
- Clean up files, memory, and OpenSSL resources safely.
- Target CMake 3.25+, OpenSSL 3.x, an approved native PDF library, and Valgrind before integration.
- Target approximately 10,000 PDFs in under five minutes, but record the actual test environment and results.
- Support Windows development and eventual WSL/Linux/Docker execution.

## Git and collaboration workflow

Use:

```text
feature branch → pull request → develop → integration testing → main
```

Do not put unfinished native work directly into `main`. Keep pull requests small and assign clear file ownership to reduce merge conflicts. Each system developer should have traceable commits, code, tests, documentation, decision notes, and the ability to explain their work.

The main Docker Compose file must remain at `infra/docker-compose.yml`; its web service must remain named `app`; the main file uses `expose`, while local port mappings belong in `docker-compose.override.yml`.

## Teaching and AI-helper instructions

Assume the team members are students learning C/C++ and professional development. For every task:

1. Inspect the repository first.
2. Explain what currently exists.
3. State the smallest current goal.
4. Explain why the project needs it and which role owns it.
5. Explain every new concept and relevant syntax in simple English before using it.
6. Show only a small example when needed.
7. Implement the real project in small steps rather than generating complete modules.
8. Build after meaningful changes.
9. Run and test after meaningful changes.
10. Explain errors before fixing them.
11. Do not commit until the team member understands the change.
12. Clearly distinguish repository requirements from recommended implementation choices.

Refer to the group as “we”, “our”, and “us”, and refer to individual roles only as System Developer A, System Developer B, and System Developer C.

When blocked, record:

1. What was attempted
2. What was expected
3. What actually happened
4. The exact error
5. What has already been tried
6. Whether other meaningful work can continue

Difficulty alone is not a blocker. Continue with safe independent work when the external contracts and fixtures allow it.
