@echo off

set project_file=RemoveQuotes.csproj
set build_config=Release
set local_tgt_platform=x86

IF /i "%1" == "debug" (
    set build_config=Debug
) ELSE (
    set build_config=Release
)
set config=%build_config%
set platf_env=%local_tgt_platform%

@echo build_config=%build_config% %conf_suffix%
@echo.
cd "%~dp0"
@if /i "%VS140COMNTOOLS%" == "" (
        @echo Variable VS140COMNTOOLS is not set or contains wrong path.
        goto ENV_ERR
    )
call :unquoteStr ENV_SETUP "%VS140COMNTOOLS%vsvars32.bat"
@echo Using "%ENV_SETUP%" environment configuration script.
call "%ENV_SETUP%"
call msbuild %project_file% /p:Configuration=%build_config% /p:platform=%local_tgt_platform%
@goto :eof


:ENV_ERR
    echo Can not setup visual studio environment. 
    set null
@goto :eof


:unquoteStr
    set %1=%~2
@goto :eof
