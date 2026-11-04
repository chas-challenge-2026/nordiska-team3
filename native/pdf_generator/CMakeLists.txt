find_path(LIBHARU_INCLUDE_DIR NAMES hpdf.h REQUIRED) # find hpdf.h path

find_library(LIBHARU_LIBRARY    # find compiled libhpdf 
    NAMES hpdf hpdf-2.3.0
    REQUIRED
)

add_executable(pdf_generator src/main.cpp) # add pdf_gen main.cpp to executables

set_target_properties(pdf_generator PROPERTIES  # set versions and extensions
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF 
)

target_include_directories(pdf_generator PRIVATE # alllows for #include <hpdf.h>
    "${PROJECT_SOURCE_DIR}/common/include"
    "${LIBHARU_INCLUDE_DIR}"
)

target_link_libraries(pdf_generator PRIVATE
    "${LIBHARU_LIBRARY}"
)

add_custom_command(
    TARGET pdf_generator POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory
        "$<TARGET_FILE_DIR:pdf_generator>/assets"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/assets/DejaVuSans.ttf"
        "$<TARGET_FILE_DIR:pdf_generator>/assets/DejaVuSans.ttf"
)

if(BUILD_TESTING)
    add_test(
        NAME pdf_generator_usage
        COMMAND "${CMAKE_COMMAND}"
            "-DGENERATOR=$<TARGET_FILE:pdf_generator>"
            -P "${CMAKE_CURRENT_SOURCE_DIR}/tests/check_usage.cmake"
    )
endif()