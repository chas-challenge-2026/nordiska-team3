execute_process(
    COMMAND "${GENERATOR}"
    RESULT_VARIABLE exit_code
    OUTPUT_VARIABLE standart_output
    ERROR_VARIABLE standart_error
    TIMEOUT 10
)

if(NOT "${exit_code}" STREQUAL "1")
    message(FATAL_ERROR "Expected exit code 1, got: ${exit_code}")
endif()

if(NOT "${standart_error}" MATCHES "Usage: ")
    message(FATAL_ERROR "Expected usage instructions on stderr")
endif()

if(NOT "${standart_output}" STREQUAL "")
    message(FATAL ERROR "Unexpected output on stdout: ${standart_output}")
endif()