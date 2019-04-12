#!/bin/bash

# GetHistPrices sample run script

LIBS_DIR=../../../../lib

THIS_DIR="$(cd -P "$(dirname "$0")" && pwd)"
(
    cd "$THIS_DIR/../build"

    if [[ "$OSTYPE" == "darwin"* ]]; then
        export DYLD_LIBRARY_PATH="$DYLD_LIBRARY_PATH":"$PWD"
    else
        export LD_LIBRARY_PATH="$LD_LIBRARY_PATH":"pwd"
    fi

    java -Djava.library.path=. -jar ./gethistprices.jar "$@"
)
