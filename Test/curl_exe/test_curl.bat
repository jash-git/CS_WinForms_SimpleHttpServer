REM UTF-8
chcp 65001

@echo off

for /l %%x in (1, 1, 1000) do (
	echo %%x
    curl.exe http://192.168.1.108:8001/jashliao
    timeout 1
)

pause