
@echo off

IF /i "%1" == "debug" (
    set config=Debug
) ELSE (
    set config=Release
)
@set platf_env=x86

rem set VS140COMNTOOLS=C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools
rem set VS140ENV_SETUP=C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat

call "%VS140COMNTOOLS%\VsDevCmd.bat"
call "%VS140ENV_SETUP%"

msbuild OpenPosition.csproj /p:Configuration=%config% /p:platform=x86

