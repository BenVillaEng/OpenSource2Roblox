@echo off
title OpenSource2Roblox - GoldSource Edition
setlocal

set "DIST_DIR=%~dp0dist\OpenSource2Roblox-win32-x64"

if exist "%DIST_DIR%\OpenSource2Roblox.exe" (
    cd /d "%DIST_DIR%"
    start "" "OpenSource2Roblox.exe"
) else (
    cd /d "%~dp0ui"
    start "" npm.cmd start
)