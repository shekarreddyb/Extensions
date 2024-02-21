@echo off
setlocal enabledelayedexpansion

REM Name of the app to exclude from deletion
set "excludeApp=apixyz"

REM List all apps and parse the output. Skip the first 4 lines of cf apps command output.
for /f "skip=4 tokens=1" %%i in ('cf apps') do (
  set app=%%i
  REM Check if the line is not the summary line at the end and if the app is not the one to exclude.
  if "!app:~0,1!" NEQ "#" (
    if "!app!" NEQ "!excludeApp!" (
      echo Starting parallel deletion for: !app!
      start /b cf delete !app! -f -r
    ) else (
      echo Skipping !app!
    )
  )
)

echo Started parallel deletion for all apps except !excludeApp!.
