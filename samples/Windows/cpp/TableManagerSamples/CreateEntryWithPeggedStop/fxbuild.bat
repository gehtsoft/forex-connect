
@echo off

set build_config=Release
set tgt_platform=x86

rem --------------------------------------------------------------------------------------------
rem Parse command line argument values.
rem Note: For ambiguous arguments the last one wins (ex: debug release)
rem --------------------------------------------------------------------------------------------
:Parse_Args
    @IF /I "%~1"=="Debug"   SET "build_config=Debug"   & SHIFT & GOTO Parse_Args
    @IF /I "%~1"=="Release" SET "build_config=Release" & SHIFT & GOTO Parse_Args
    @IF /I "%~1"=="x86"     SET "tgt_platform=x86"     & SHIFT & GOTO Parse_Args
    @IF /I "%~1"=="x64"     SET "tgt_platform=x64"     & SHIFT & GOTO Parse_Args
    @IF /I "%~1"=="/?"      GOTO Error_Usage
    @IF    "x%~1"=="x"      GOTO Done_Args
    ECHO Unknown command-line switch: %~1
    GOTO Error_Usage
:Done_Args
@echo build_config=%build_config% %conf_suffix%

@if /i "%tgt_platform%"=="x86" (
    set local_tgt_platform=Win32
) else (
    set local_tgt_platform=x64
)


rem set VS140COMNTOOLS=C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools
rem set VS140ENV_SETUP=C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat

rem ---------------------------------------------------------------------------
rem Set VS140ENV_SETUP variable
rem ---------------------------------------------------------------------------

@if /i "%VS140ENV_SETUP%" == "" (
    @if /i "%VS140COMNTOOLS%" == "" (
        @goto :ENV_ERR
    )
    call "%VS140COMNTOOLS%\VsDevCmd.bat"
)

call "%VS140ENV_SETUP%"

call msbuild CreateEntryWithPeggedStop.vcxproj /p:Configuration=%build_config% /p:platform=%tgt_platform%

@goto :eof

:ENV_ERR
    echo "Environment configuration failed. Variables VS140ENV_SETUP and VS140COMNTOOLS is not defined"
    set null
@goto :eof


:unquoteStr
    set %1=%~2
@goto :eof

:Error_Usage
    @rem --------------------------------------------------------------------------------------------
    @rem Display command usage
    @rem --------------------------------------------------------------------------------------------
    echo Usage: "%~nx0 [debug|release] [x86|x64] [/?]"
    echo.
    echo                 debug    - Debug build configuration
    echo                 release  - Release build configuration
    echo                 x86      - Create 32-bit x86 applications
    echo                 x64      - Create 64-bit x64 applications
    echo.                /?       - Show usage
    echo.
@goto :eof

