@if exist lib rmdir lib /q /s
@if exist obj rmdir obj /q /s

@if exist *.user   del *.user /f /q /s
@if exist *.ncb   del *.ncb /f /q /s
@if exist *.suo   del *.suo /f /q /s /a:h
