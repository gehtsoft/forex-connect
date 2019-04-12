#!/bin/sh

#----------------------------------------------------------------------------------------
# RemoveQuotes sample build script
# Parameters: 
# $1 - build|rebuild|clean - the default build action is build                              
#
#----------------------------------------------------------------------------------------

build_action=build
case "$1" in
    build)
        build_action=build
    ;;

    rebuild)
        build_action=rebuild
    ;;

    clean)
        build_action=clean
    ;;

    *)
        echo 'Build action was not specified. Fall back to '\'build\''.'
    ;;
esac

# setting ant execution command, if ANT_HOME is not defined we will assume, that path to executable is added to PATH variable
ANT_BINARY=ant
if [ ! "$ANT_HOME" = "" ]; then
    ANT_BINARY="$ANT_HOME/bin/ant"
fi 

echo "Ant command is $ANT_BINARY"

SNAPSHOT_PATH=../../../snapshot
if [ ! -e $SNAPSHOT_PATH ]; then 
    SNAPSHOT_PATH=../../../..
fi

"$ANT_BINARY" -DFC_PATH="$SNAPSHOT_PATH" $build_action

