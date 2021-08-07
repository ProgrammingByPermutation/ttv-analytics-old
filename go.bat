@echo off
:: Save where we are
set CURRENT_DIR_GO=%cd%

:: Check if we got the username and password as user input
if NOT [%1]==[] (
	if NOT [%2]==[] (
		if NOT [%3]==[] (
			if NOT [%4] == [] (
				if NOT [%5] == [] (
					if NOT [%6] == [] (
						set DB_USERNAME=%1
						set DB_PASSWORD=%2
						set EMAIL_NO_REPLY_PASSWORD=%3
						set TWITCH_CLIENT_ID=%4
						set TWITCH_CLIENT_SECRET=%5
						set TWITCH_CLIENT_REDIRECT=%6
						goto skipUserInput
					)
				)
			)
		)
	)
)

:: Get the username and password for the SQL database
set /p DB_USERNAME="DB Username: "
set /p DB_PASSWORD="DB Password: "
set /p EMAIL_NO_REPLY_PASSWORD="Email Password: "
set /p TWITCH_CLIENT_ID="Twitch Client ID: "
set /p TWITCH_CLIENT_SECRET="Twitch Client Secret: "
set /p TWITCH_CLIENT_REDIRECT="Twitch Redirect: "

:skipUserInput
:: Determine if there is an environment defined. If not, assume
:: this is a production build.
if NOT [%7]==[] (
	set BUILD_ENVIRONMENT=%7
) ELSE (
	set BUILD_ENVIRONMENT=prod
)

:: Grab our IP address. Because of the layers of networking involved, we need to point
:: the deployment to our machine by our intranet's IP address.
for /f "usebackq tokens=2 delims=:" %%f in (`ipconfig ^| findstr /c:"IPv4 Address"`) do (
	set IP_ADDRESS=%%f
    goto writeEnvFiles
)

:writeEnvFiles
:: Creating a file called ".env" with key-value pairs for the environmental
:: variables makes them available to the docker-compose build.
echo DB_USERNAME=%DB_USERNAME%> src\.env
echo DB_PASSWORD=%DB_PASSWORD%>> src\.env
echo BUILD_ENVIRONMENT=%BUILD_ENVIRONMENT%>> src\.env
echo TWITCH_CLIENT_ID=%TWITCH_CLIENT_ID%>> src\.env
echo TWITCH_CLIENT_SECRET=%TWITCH_CLIENT_SECRET%>> src\.env
echo TWITCH_CLIENT_REDIRECT=%TWITCH_CLIENT_REDIRECT%>> src\.env

:: Remove spaces
set IP_ADDRESS=%IP_ADDRESS: =%
echo IP_ADDRESS=%IP_ADDRESS%>> src\.env

:: Deploy the database
cd %CURRENT_DIR_GO%/src/database
call liquibase --logLevel=debug --username=%DB_USERNAME% --password=%DB_PASSWORD% --referenceUsername=%DB_USERNAME% --referencePassword=%DB_PASSWORD% update

IF %ERRORLEVEL% NEQ 0 ( 
   exit /B 1
)

cd "%CURRENT_DIR_GO%"
