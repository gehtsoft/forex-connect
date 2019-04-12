#!/bin/bash
# Run the built program from working directory
THIS_DIR=`pwd`

cd ../bin
./RemoveQuotes "$@"
cd $THIS_DIR
