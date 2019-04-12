@ECHO off
rem ---------------------------------------------------------------------------
rem GetLivePrices sample build script
rem Parameters:
rem     %1 build|rebuild|clean - default build action rebuild
rem ----------------------------------------------------------------------------

rem Configure variables
rem ----------------------------------------------------------------------------
SET build_action=rebuild
SET "THIS_DIR=%~dp0"

IF /i "%1" == "build" (
    SET build_action=build
) ELSE (
    IF /i "%1" == "clean" (
        SET build_action=clean   
    )
)


REM setting ant execution command, if ANT_HOME is not defined we will hope, that path to executable is added to PATH variable
SET ANT_BINARY=ant
IF DEFINED ANT_HOME (
    SET ANT_BINARY="%ANT_HOME%\bin\ant"
    @ECHO "Found Apache Ant at: %ANT_HOME%"
)

SET "FOREXCONNECT_PATH=%THIS_DIR%..\..\..\.."
CALL %ANT_BINARY% -DFC_PATH="%FOREXCONNECT_PATH%" %build_action%
