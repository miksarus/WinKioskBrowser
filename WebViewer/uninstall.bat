
@ECHO OFF
@chcp 1251
cls

@ECHO ==========================================================
@ECHO ==== ��� ������������ ��������� ��������� �� ������ ======
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

:: �������� ����
net session > NUL 2>&1 
if NOT %ERRORLEVEL% EQU 0 goto NotAdmin

@ECHO �������� ������������
@echo.

@net user %user_name% /delete
@rd /s /q %USERS%\test_user\

:: �������� ������
@c:
@rd /s /q %appfolder%

@ECHO ����������� �������� ��������� �������!
@echo.
pause
goto eof

:user_uninstall

@ECHO �������� �������
@echo.

@c:
@cd "%userprofile%\Desktop\"
del "KioskBrowser.lnk"

@ECHO �������� ����������� ��������
@echo.

::REG DELETE "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" /v KioskBrowser
::REG DELETE "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\WebViewer.exe" /ve
::REG DELETE "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\WebViewer.exe" /v Path

@cd "%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\"
del "KioskBrowser.lnk"


@echo.
@ECHO ��� ���������� ������� � ������� ������� �������������� � ��������� ���� C:\KIOSK\uninstall.bat
@echo.
pause
goto eof

:NotAdmin
@echo ������: � ������ ������� ������ ��� ���� ��������������
@echo.
goto error

:error
@echo.
@echo �������� ����������� ��������.
@echo ��� ��������� ������� ���������� � ��������������.
@echo.
pause
EXIT

:eof
rem SCHTASKS /Delete /TN SupercompactorSetup /F
@echo ON
