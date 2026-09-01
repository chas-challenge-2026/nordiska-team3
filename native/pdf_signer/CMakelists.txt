add_executable(pdf_signer src/main.c)

set_target_properties(pdf_signer PROPERTIES
    C_STANDARD 11
    C_STANDARD_REQUIRED ON
    C_EXTENSIONS OFF
)
