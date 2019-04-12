# Clean script for *nix systems
#!/bin/bash
if [ -f Makefile ]
then
  make clean
fi

rm -f CMakeCache.txt
rm -f Makefile
rm -f cmake_install.cmake
rm -rf CMakeFiles

if [ -d "bin" ]
then
  rm -rf bin
fi
