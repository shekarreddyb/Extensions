@echo off
setlocal enabledelayedexpansion

REM Loop from 1 to 40
for /L %%i in (1,1,40) do (
  set app_name=app%%i
  cf push !app_name! &
)

REM Wait for all background jobs to finish
wait

endlocal
