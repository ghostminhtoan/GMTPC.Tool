@echo off
set GIT_PATH="C:\Program Files\Git\bin\git.exe"
%GIT_PATH% config --global --add safe.directory "R:/HDD R/ZC SYMLINK/USERS/source/repos/ghostminhtoan/GMTPC.Tool"
%GIT_PATH% status
%GIT_PATH% log -n 3 --oneline
%GIT_PATH% push
pause
