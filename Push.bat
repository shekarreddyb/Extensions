@echo off
setlocal enabledelayedexpansion

REM Check if a loop count argument is provided
if "%1"=="" (
  echo Usage: push_apps.bat <loop_count>
  exit /b 1
)

REM Get the loop count from the command-line argument
set loop_count=%1

REM Loop from 1 to the specified loop_count
for /L %%i in (1,1,%loop_count%) do (
  set app_name=app%%i
  cf push !app_name! &
)

REM Wait for all background jobs to finish
wait

endlocal
