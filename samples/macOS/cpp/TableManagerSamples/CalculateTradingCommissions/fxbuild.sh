#!/bin/sh

[ -e CMakeCache.txt ] && rm CMakeCache.txt
[ -e CMakeFiles ] && rm -r CMakeFiles

cmake . && make
