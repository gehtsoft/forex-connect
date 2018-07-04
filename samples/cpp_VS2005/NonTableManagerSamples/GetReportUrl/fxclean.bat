@echo off
@if exist bin\*.*  del bin\*.* /f /q /s
@if exist bin       rmdir bin /q /s
@if exist obj\*.*  del obj\*.* /f /q /s
@if exist obj       rmdir obj /q /s
