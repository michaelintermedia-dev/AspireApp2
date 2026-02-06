@echo off
:: Disable USB/Ethernet
netsh interface set interface name="Ethernet 4" admin=disable

:: Enable Wi-Fi
netsh interface set interface name="Wi-Fi" admin=enable

:: Show current IP for confirmation
ipconfig | findstr /R "IPv4"

netsh interface show interface

pause