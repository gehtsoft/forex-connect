@echo off

IF /i "%1" == "debug" (
    set config=Debug
) ELSE (
    set config=Release
)
@set platf_env=x86

call "%VS100COMNTOOLS%vsvars32.bat"
msbuild ShowCommissionDetails.csproj /p:Configuration=%config% /p:platform=x86
