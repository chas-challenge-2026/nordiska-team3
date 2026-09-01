add_executable(pdf_generator src/main.cpp)

set_target_properties(pdf_generator PROPERTIES
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
)
