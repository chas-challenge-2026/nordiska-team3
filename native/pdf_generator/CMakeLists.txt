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