@echo ****************************************************************
@echo ------------ Start Cleaning sample_tools -----------------------

set DOC_CPP="doc\cpp"

@if exist bin rmdir bin /q /s
@if exist lib rmdir lib /q /s
@if exist obj rmdir obj /q /s
