
@ECHO OFF
@chcp 1251
cls

@ECHO ==========================================================
@ECHO ==== Вас приветствует программа установки ПО КИОСКА ======
@ECHO ==========================================================
@echo.

set user_name=Kiosk
set user_fullname="Kiosk terminal user"
SET user_sid=NoValue
set user_passw=
set appname=WebViewer.exe
set appfolder=C:\KIOSK

FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO (SET current_user=%%F)
if /I "%current_user%"=="%userdomain%\%user_name%" goto user_uninstall

:main_uninstall

:: Проверка прав
net session > NUL 2>&1 
if NOT %ERRORLEVEL% EQU 0 goto NotAdmin

@ECHO Удаление пользователя
@echo.

@net user %user_name% /delete
@rd /s /q %USERS%\test_user\

:: удаление файлов
@c:
@rd /s /q %appfolder%

@ECHO Поздравляем удаление завершено успешно!
@echo.
pause
goto eof

:user_uninstall

@ECHO Удаление ярлыков
@echo.

@c:
@cd "%userprofile%\Desktop\"
del "KioskBrowser.lnk"

@ECHO Удаление автозапуска браузера
@echo.

::REG DELETE "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" /v KioskBrowser
::REG DELETE "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\WebViewer.exe" /ve
::REG DELETE "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\WebViewer.exe" /v Path

@cd "%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\"
del "KioskBrowser.lnk"


@echo.
@ECHO Для завершения войдите с учётной записью администратора и запустите файл C:\KIOSK\uninstall.bat
@echo.
pause
goto eof

:NotAdmin
@echo ОШИБКА: У данной учетной записи нет прав администратора
@echo.
goto error

:error
@echo.
@echo Удаление завершилось неудачей.
@echo При повторной неудаче обратитесь к администратору.
@echo.
pause
EXIT

:eof
rem SCHTASKS /Delete /TN SupercompactorSetup /F
@echo ON
