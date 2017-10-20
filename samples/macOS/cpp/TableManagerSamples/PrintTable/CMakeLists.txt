cmake_minimum_required (VERSION 2.6) 
 
set (PROJECT PrintTable) 
  
project (${PROJECT}) 

if (DEFINED Mac32)
    set (ARCHFLAGS "-arch i386 -DMac32")
elseif (${CMAKE_SYSTEM} MATCHES "Linux" AND ${CMAKE_SYSTEM_PROCESSOR} MATCHES "i[3-6]86")
    set (ARCHFLAGS "-march=i486")
else ()
    set (ARCHFLAGS "")
endif ()

if (${CMAKE_SYSTEM} MATCHES "Linux")
    set (SHAREDSUFFIX "so")
elseif (${CMAKE_SYSTEM} MATCHES "Darwin")
    set (SHAREDSUFFIX "dylib")
endif ()

set (SAMPLE_TOOLS_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../sample_tools")
set (FOREXCONNECT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../../..")

set (EXECUTABLE_OUTPUT_PATH ${CMAKE_CURRENT_SOURCE_DIR}/bin)
set (LIBRARY_OUTPUT_PATH ${CMAKE_CURRENT_SOURCE_DIR}/bin)

set (CMAKE_C_FLAGS_DEBUG "${CMAKE_C_FLAGS} ${ARCHFLAGS}")
set (CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS} ${ARCHFLAGS}")

set (CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS} ${ARCHFLAGS}")
set (CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS} ${ARCHFLAGS}")

set (CMAKE_C_FLAGS_RELWITHDEBINFO "${CMAKE_C_FLAGS} ${ARCHFLAGS}")
set (CMAKE_CXX_FLAGS_RELWITHDEBINFO "${CMAKE_CXX_FLAGS} ${ARCHFLAGS}")

if (NOT CMAKE_BUILD_TYPE)
    set (CMAKE_BUILD_TYPE Release CACHE STRING
         "Choose the type of build, options are: None Debug Release RelWithDebInfo MinSizeRel."
         FORCE
    )
endif (NOT CMAKE_BUILD_TYPE)

include_directories (${FOREXCONNECT_DIR}/include
                     ${SAMPLE_TOOLS_DIR}/include
                    )   

link_directories (${FOREXCONNECT_DIR}/lib
                  ${SAMPLE_TOOLS_DIR}/lib)

set (HEADERS 
    source/stdafx.h
    source/CommonSources.h
    source/EachRowListener.h
    source/LoginParams.h
    source/SampleParams.h
    source/SessionStatusListener.h
    )
     
set (SOURCES
    source/main.cpp
    source/CommonSources.cpp
    source/EachRowListener.cpp
    source/LoginParams.cpp
    source/SampleParams.cpp
    source/SessionStatusListener.cpp
    ) 

if (NOT CMAKE_CROSSCOMPILING)
    add_executable (${PROJECT} ${HEADERS} ${SOURCES})
else ()
    add_library (${PROJECT} STATIC ${HEADERS} ${SOURCES})
endif ()

if (${CMAKE_SYSTEM} MATCHES "Linux")
    target_link_libraries (${PROJECT} ForexConnect sample_tools)
elseif (${CMAKE_SYSTEM} MATCHES "Darwin")
    target_link_libraries (${PROJECT} ForexConnect sample_tools)
endif ()

# Post - build event to create symlinks for shared objects
add_custom_command (TARGET ${PROJECT}
    POST_BUILD
    COMMAND find ${FOREXCONNECT_DIR}/lib/ -name lib*.${SHAREDSUFFIX} -exec ln -sf {} \;
    COMMAND find ${SAMPLE_TOOLS_DIR}/lib/ -name lib*.${SHAREDSUFFIX} -exec ln -sf {} \;

    WORKING_DIRECTORY ${EXECUTABLE_OUTPUT_PATH}
    VERBATIM
)

if (${CMAKE_SYSTEM} MATCHES "Linux")
    set_target_properties (${PROJECT} PROPERTIES
          LINK_FLAGS -Wl,-rpath,.
    )
elseif (${CMAKE_SYSTEM} MATCHES "Darwin")
    set_target_properties (${PROJECT} PROPERTIES
          BUILD_WITH_INSTALL_RPATH 1
          INSTALL_NAME_DIR "@rpath"
          LINK_FLAGS "-Wl,-rpath,@loader_path,-rpath,./"
    )
endif ()

