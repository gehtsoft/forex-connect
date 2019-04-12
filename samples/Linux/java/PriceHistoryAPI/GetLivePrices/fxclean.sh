#setting ant execution command, if ANT_HOME is not defined we will assume, that path to executable is added to PATH variable
ANT_BINARY=ant
if [ ! "$ANT_HOME" = "" ]; then
    ANT_BINARY="$ANT_HOME/bin/ant"
fi 

ant clean


