@ECHO OFF

SET THIS_DIR="%~dp0"

PUSHD "%THIS_DIR%\..\bin\win32"
GetLivePrices.exe %*
POPD