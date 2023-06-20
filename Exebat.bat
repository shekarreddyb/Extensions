@echo off

REM Get the current date in the desired format
for /F "tokens=1-4 delims=/ " %%a in ('date /T') do set "timestamp=%%b-%%c-%%d"

REM Run your executable and redirect output and error to timestamped files
your_executable > "output-%timestamp%.txt" 2> "error-%timestamp%.txt"
