@echo off

IF /i "%1" == "debug" (
    set config=Debug
) ELSE (
    set config=Release
)
@set platf_env=x86

dotnet restore
dotnet build If-then.csproj /p:Configuration=%config% /p:platform=AnyCPU