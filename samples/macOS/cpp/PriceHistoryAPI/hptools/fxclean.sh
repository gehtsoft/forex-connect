# Clean script for *nix 32 bit systems
#!/bin/bash
if [ -f Makefile ]
then
  make clean
fi

rm -f CMakeCache.txt
rm -f Makefile
rm -f cmake_install.cmake
rm -rf CMakeFiles
 
if [ -d "lib" ]
then
  rm -rf lib
fi
