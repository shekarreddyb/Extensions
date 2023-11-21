@echo off
setlocal

REM Check if iteration count parameter is provided
if "%~1"=="" (
    echo Please provide an iteration count.
    exit /b
)

set ITERATION_COUNT=%~1

REM Loop through the specified number of iterations
for /L %%i in (1,1,%ITERATION_COUNT%) do (
    REM Execute cf push command with a unique app name in parallel
    START "Pushing app%%i" cf push app%%i
)

echo All commands have been started.
