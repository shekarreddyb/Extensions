@echo off

REM Get the current date in the desired format
for /F "tokens=2-4 delims=/ " %%a in ('date /T') do (
    set "day=%%a"
    set "month=%%b"
    set "year=%%c"
)

REM Define the mapping of month names
set "month_map=Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec"

REM Get the abbreviated month name based on the month number
for /F "tokens=%month%" %%m in ("%month_map%") do set "month=%%m"

REM Concatenate the day, month, and year to form the timestamp
set "timestamp=%day%%month%%year%"

REM Run your executable and redirect output and error to timestamped files
your_executable > "output-%timestamp%.txt" 2> "error-%timestamp%.txt"
