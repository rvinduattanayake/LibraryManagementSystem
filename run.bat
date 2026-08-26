@echo off
echo Starting Library Management System...
echo.
echo When you see "Now listening on: http://localhost:5000", open this in your browser:
echo   http://localhost:5000
echo.
echo Keep this window open while using the app. Press Ctrl+C to stop.
echo.
cd /d "%~dp0"
"C:\Program Files\dotnet\dotnet.exe" run
pause
