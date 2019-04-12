@ECHO OFF

SET THIS_DIR="%~dp0"

REM Check if the java binary is located in %JAVA_HOME%
SET JAVA_BINARY="%JAVA_HOME%\bin\java"
"%JAVA_BINARY%" -version>NUL 2>NUL
IF %ERRORLEVEL% NEQ 0 (
    SET JAVA_BINARY=java
)

PUSHD "%THIS_DIR%\..\build"
CALL %JAVA_BINARY% -Djava.library.path="%CD%" -jar .\getliveprices.jar %*
POPD
