@echo off

@if exist lib\*.*   del lib\*.* /f /q /s
@if exist lib       rmdir lib /q /s
@if exist obj\*.*   del obj\*.* /f /q /s
@if exist obj       rmdir obj /q /s
