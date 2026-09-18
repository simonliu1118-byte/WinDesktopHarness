cmake_minimum_required(VERSION 3.20)
project(WinDesktopHarness LANGUAGES CXX RC)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(WinDesktopHarness WIN32
    src/main.cpp
    src/app.rc
)

target_compile_definitions(WinDesktopHarness PRIVATE UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)

if(MSVC)
    target_compile_options(WinDesktopHarness PRIVATE /utf-8 /W4 /permissive-)
endif()

target_link_libraries(WinDesktopHarness PRIVATE
    comctl32
    comdlg32
    shell32
    shlwapi
    uxtheme
)
