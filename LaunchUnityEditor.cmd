@echo off
set "UNITY_EXE=C:\Users\matth\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
set "PROJECT_DIR=%~dp0"

if not exist "%UNITY_EXE%" (
  echo Unity Editor was not found at:
  echo %UNITY_EXE%
  pause
  exit /b 1
)

start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%"
