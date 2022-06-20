cmake_minimum_required(VERSION 3.20)
project(OSLV)
set(CMAKE_CXX_STANDARD 20)

# OpenGL
find_package(OpenGL REQUIRED)

# GLFW
set(GLFW_BUILD_DOCS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
add_subdirectory(thirdparty/glfw)

include(src/core/CMakeLists.txt)
add_executable(OSLV src/main.cpp ${APP_SOURCES})

target_include_directories(${PROJECT_NAME} PRIVATE ${OPENGL_INCLUDE_DIRS})
target_link_libraries(OSLV ${OPENGL_LIBRARIES} glfw)

set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -static")