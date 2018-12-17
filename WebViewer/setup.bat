
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
set user_passw=kiosk
set appname=WebViewer.exe
set appfolder=C:\KIOSK

FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO (SET current_user=%%F)
if /I "%current_user%"=="%userdomain%\%user_name%" goto user_setup

:main_setup

:: �������� ����
net session > NUL 2>&1 
if NOT %ERRORLEVEL% EQU 0 goto NotAdmin

:: S-1-5-32-545 - ��������� ������������
Set GroupSID=S-1-5-32-545
Set GroupName=
For /F "UseBackQ Tokens=1* Delims==" %%I In (`WMIC Group Where "SID = '%GroupSID%'" Get Name /Value ^| Find "="`) Do Set GroupName=%%J
Set GroupName=%GroupName:~0,-1%

@ECHO ���������� ������ ������������
@echo.

@net user %user_name% %user_passw% /add /passwordchg:no /fullname:%user_name% 
@net user %user_name% /expires:never /active:yes
:: �������������, ����� ������ �� ������� �������
:: ���� ��� - wmic path Win32_UserAccount where Name='%user_name%' set PasswordExpires=false
::@wmic USERACCOUNT where Name='%user_name%' set PasswordExpires=false
:: ���������� ���������� ������������ � �������� ��������� ������
@net localgroup %GroupName% %user_name% /ADD


:: ����������� ������
@MKDIR "%appfolder%"
@xcopy "%~dp0media" "%appfolder%\media" /S /Y /F /I
@COPY "%~dp0%appname%" "%appfolder%\%appname%"
@COPY "%~dp0main.conf" "%appfolder%\main.conf"
@COPY "%~dp0setup.bat" "%appfolder%\setup.bat"
@COPY "%~dp0uninstall.bat" "%appfolder%\uninstall.bat"

@ECHO ����������� ��������� ��������� �������!
@echo.
@ECHO ��� ���������� ������� � ������� ������� %user_name% � ��������� ���� C:\KIOSK\setup.bat
@echo.
pause
goto eof

:user_setup

@ECHO �������� �������
@echo.

echo set oWS=WScript.CreateObject("WScript.Shell") > CreateShortcut.vbs
echo sLinkFile = "%userprofile%\Desktop\KioskBrowser.lnk" >> CreateShortcut.vbs
echo set oLink = oWS.CreateShortcut(sLinkFile) >> CreateShortcut.vbs
echo oLink.TargetPath = "%appfolder%\%appname%" >> CreateShortcut.vbs
echo oLink.WorkingDirectory = "%appfolder%" >> CreateShortcut.vbs
echo oLink.Description = "Kiosk Safe Browser" >> CreateShortcut.vbs
rem echo oLink.IconLocation = "%appfolder%\MyApp48.bmp" >> CreateShortcut.vbs
echo oLink.Save >> CreateShortcut.vbs
cscript CreateShortcut.vbs
del CreateShortcut.vbs

@ECHO ��������� ����������� ��������
@echo.

::REG ADD "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" /v KioskBrowser /t REG_SZ /d C:\KIOSK\WebViewer.exe
::REG ADD "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\WebViewer.exe" /ve /t REG_SZ /d C:\KIOSK\WebViewer.exe
::REG ADD "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\WebViewer.exe" /v Path /t REG_SZ /d C:\KIOSK\

echo set oWS=WScript.CreateObject("WScript.Shell") > CreateShortcut.vbs
echo sLinkFile = "%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\KioskBrowser.lnk" >> CreateShortcut.vbs
echo set oLink = oWS.CreateShortcut(sLinkFile) >> CreateShortcut.vbs
echo oLink.TargetPath = "%appfolder%\%appname%" >> CreateShortcut.vbs
echo oLink.WorkingDirectory = "%appfolder%" >> CreateShortcut.vbs
echo oLink.Description = "Kiosk Safe Browser" >> CreateShortcut.vbs
rem echo oLink.IconLocation = "%appfolder%\MyApp48.bmp" >> CreateShortcut.vbs
echo oLink.Save >> CreateShortcut.vbs
cscript CreateShortcut.vbs
del CreateShortcut.vbs

@ECHO ����������� ��������� ��������� �������!
@echo.
@ECHO ��� ���������� ������������� ���������
@echo.
pause
goto eof

:NotAdmin
@echo ������: � ������ ������� ������ ��� ���� ��������������
@echo.
goto error

:error
@echo.
@echo ��������� ����������� ��������.
@echo ��� ��������� ������� ���������� � ��������������.
@echo.
pause
EXIT

:eof
rem SCHTASKS /Delete /TN SupercompactorSetup /F
@echo ON
