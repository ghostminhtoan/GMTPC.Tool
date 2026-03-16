@echo off
set PATH=C:\Program Files\Git\bin;%PATH%
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
%MSBUILD% GMTPC.Tool.csproj /p:Configuration=Release /p:Platform=x64
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b %ERRORLEVEL%
)
echo Build successful!
git status
git add MainWindow.xaml
git commit -m "[UI] Make tabs more compact: smaller font (9px), reduced padding, single row only"
git push
echo Done!
pause
