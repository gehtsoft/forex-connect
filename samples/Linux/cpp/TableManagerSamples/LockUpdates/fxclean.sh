#!/bin/sh

make clean

[ -e CMakeCache.txt ] && rm CMakeCache.txt
[ -e CMakeFiles ] && rm -r CMakeFiles
[ -e bin ] && rm -r bin
