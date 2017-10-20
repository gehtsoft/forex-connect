#!/bin/sh

mv makefile makefile_
rm CMakeCache.txt
cmake CMakeLists.txt && make clean && make
if [ -e libsample_tools.so ]; then
    if [ ! -d lib ]; then
        mkdir lib
    fi
    mv libsample_tools.so lib/
fi
if [ -e libsample_tools.dylib ]; then
    if [ ! -d lib ]; then
        mkdir lib
    fi
    mv libsample_tools.dylib lib/
fi

