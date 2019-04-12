cmake_minimum_required (VERSION 2.6) 
 
set (PROJECT hptools) 
  
project (${PROJECT})

# determine architecture
if(NOT ${CUSTOM_ARCH} MATCHES "x32" AND NOT ${CUSTOM_ARCH} MATCHES "x64")
      set(CUSTOM_ARCH x32)
endif()

if (NOT LIBRARY_OUTPUT_PATH)
  set (LIBRARY_OUTPUT_PATH ${CMAKE_CURRENT_SOURCE_DIR}/../bin)
endif ()

include_directories (
                     ${CMAKE_CURRENT_SOURCE_DIR}/include
                     ${CMAKE_CURRENT_SOURCE_DIR}/source
                    )   


set (SOURCES 
    source/stdafx.cpp
    source/date/date.cpp
    source/threading/ThreadHandle.cpp
    source/mutex/Mutex.cpp
    source/win_emul/winevent.cpp
    source/win_emul/CWinEventHandle.cpp
    ) 

if (${CMAKE_SYSTEM} MATCHES "Linux")
  if (CMAKE_COMPILER_IS_GNUCXX) 
    add_definitions (-fPIC)
  elseif (CMAKE_COMPILER_IS_GNUCC)
    add_definitions (-fPIC)
  endif()
endif()

# Uncomment next line to enable generating debugging symbols
# add_definitions (-g)

add_library (${PROJECT} SHARED ${HEADERS} ${SOURCES})  
if (${CMAKE_SYSTEM} MATCHES "Linux")
  target_link_libraries (${PROJECT} rt pthread)
elseif (${CMAKE_SYSTEM} MATCHES "Darwin")
  target_link_libraries (${PROJECT} pthread)
endif ()

if (${CMAKE_SYSTEM} MATCHES "Linux")
    SET(CMAKE_CXX_FLAGS "--std=c++11 ${CMAKE_CXX_FLAGS}")
    set_target_properties (${PROJECT} PROPERTIES
          LINK_FLAGS -Wl,-rpath,.
    )
elseif (${CMAKE_SYSTEM} MATCHES "Darwin")
    SET(CMAKE_CXX_FLAGS "--stdlib=libc++ --std=c++11 ${CMAKE_CXX_FLAGS}")
    set_target_properties (${PROJECT} PROPERTIES
          BUILD_WITH_INSTALL_RPATH 1
          INSTALL_NAME_DIR "."
    )
endif ()

