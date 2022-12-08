REM UTF-8
chcp 65001

@echo off

for /l %%x in (1, 1, 1000) do (
	echo %%x
    REM curl.exe http://192.168.1.108:8001/jashliao
	curl.exe -H "Content-Type: text/xml" -X POST --data "<methodCall><methodName>sycgsti.launcher</methodName><params><param><value><string>amFzaGxpYW8=</string></value></param></params></methodCall>" "http://192.168.1.108:8001/jashliao"
    timeout 1
)

pause