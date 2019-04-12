@ECHO off

REM setting ant execution command, if ANT_HOME is not defined we will hope, that path to executable is added to PATH variable
SET ANT_BINARY=ant
IF DEFINED ANT_HOME (
    SET ANT_BINARY="%ANT_HOME%\bin\ant"
    @ECHO "Found Apache Ant at: %ANT_HOME%"
)

call %ANT_BINARY% clean
