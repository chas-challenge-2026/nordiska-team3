find_package(OpenSSL REQUIRED COMPONENTS Crypto)
add_executable(pdf_signer 
            src/main.c
            src/sha256.c
)

set_target_properties(pdf_signer PROPERTIES
    C_STANDARD 11
    C_STANDARD_REQUIRED ON
    C_EXTENSIONS OFF
)

target_include_directories(pdf_signer PRIVATE
    "${PROJECT_SOURCE_DIR}/common/include"
    "${CMAKE_CURRENT_SOURCE_DIR}/include")
target_link_libraries(pdf_signer PRIVATE OpenSSL::Crypto)