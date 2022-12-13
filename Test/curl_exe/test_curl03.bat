REM UTF-8
chcp 65001

@echo off

for /l %%x in (1, 1, 1000) do (
	echo %%x
	curl.exe -H "Content-Type: text/xml" -X POST --data "<methodCall><methodName>sycgsti.launcher</methodName><params><param><value><string>amFzaGxpYW8=</string></value></param></params></methodCall>" "http://192.168.1.108:8080/orderno
    
	REM seelp 100ms 
	ping 127.0.0.1 -n 1 -w 100> nul
)

pause