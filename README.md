# BackMeUp - OneDrive backup service
A combination of a service worker, that fetches data from a OneDrive account, and HTTP API, that serves as a session gateway for Graph API OAuth login.

This project was motivated by my wish to do automatic and periodic backups of my OneDrive contents and other cloud services in a self-hosted manner. 

## Usage

To use the application as is you need to either have dotnet or docker installed. 
SMB is required to save the downloaded and zipped backup or changing the Worker.cs and FileStorageService.cs to save the file elsewhere.
An Azure instance, with the same account that your OneDrive is registered with, is needed along with an Active Directory App Registration setup in the following manner:
- Supported Account types - Personal Microsoft accounts only
- Redirect URI - http://localhost:7133/signin-oidc (or any other port)
- API Permission with the following Delegated Microsoft Graph Permission:
	- user.read 
	- files.read 
	- files.read.all 
	- offline_access
- Client Secret - create new and copy the value

The following configurations need to be set to run the application:
- ServiceWorker
	- Smb:StorageAddress - address of the SMB fileshare or localhost if on local machine
	- Smb:ShareName - the share name
	- Smb:User - username of the user that can access the SMB file share
	- Smb:Password - password of the user 
- SessionGateway
	- Authentication:ClientId - the Id of your App Registration in Azure Active Directory
	- Authentication:ClientSecret - the value that you copied

### Docker
Using docker-compose, after adding the necessary configurations to docker-compose.yml in the root folder, run:
- `docker-compose build` 
- `docker-compose up` 

Navigate then to http://locahost:7133/authorize, confirm consent and you will be logged in.
The service worker will start the download after auth is complete.

### Dotnet
Using the dotnet CLI (requires the .NET 6.0.2 SDK and runtime) or Visual Studio (Code), after adding the necessary configurations to the appsettings.json file for both of the applications, publish the two apps and start them.
With dotnet CLI:
- In the ServiceWorker and SessionGateway run: `dotnet publish -c`
- Navigate to the output directories (directory path indicated in the console after publish) and run: 
	- `dotnet BackMeUp.SessionGateway.dll`
	- `dotnet BackMeUp.ServiceWorker.dll --urls "http://localhost:7133"

As with Docker, nagivate to the authorize endpoint and give consent to the application.

## ServiceWorker

The service worker queries the HTTP API for an access token that is used for the Graph API client. The Graph API client lists all of the items on the OneDrive and then proceeds to download them one by one.
After the download is complete the service worker saves the file on a SMB file share.

Implementation details:
- Worker Service .NET 6 template 
- Graph .NET SDK client
- SMBLibrary SDK

The implementation, of the Worker.cs, can be easily changed to only save the file locally.

The service worker takes into account:
- Graph API throttling - implements rate limiting, and longer timeout periods for the HTTP Client
- Network failures - implements Polly for network resilience 
- Device memory limits - implements custom memory/disk paging
- Scheduled runs - uses Cron for setting a schedule
- Logging - relevant actions, exceptions and possible errors are logged. Serilog is used as the log provider and by default creates a log file

### Configuration

Relevant configuration:
- Cron:RunOnce - if set to true the service worker shuts down after the backup process is complete, default is set to true
- Cron:Schedule - sets the schedule for backup process execution, default is set to 10 seconds 
- Network:RateLimitInMiliseconds - the cooldown time in-between requests to the Graph API, added this after noticing that Graph API timing out after too many concurrent download requests
- Network:Retries - the amount of retries for the Polly retry policy
- OneDrive:PageSizeInMegabytes - after downloading a single of set memory size the service worker will write it to the disk, release the memory and force garbage collection. Be sure to set this size to be larger than the largest file you have stored in your OneDrive
- Smb:BackupFolder - the folder on the SMB file share where the files will finally saved to
- Serilog:UseSerilog - if set to true will keep logs on file. If false will only log through console and without Serilog

### Often errors:
1. Memory page setting higher than system RAM, out-of memory exception thrown if OneDrive contents larger than RAM
2. Memory page setting lower than largest file on OneDrive

## SessionGatway

A HTTP API that is used for OAuth delegated login with the Graph API. 
As access to personal OneDrive files requires the usage of the Graph API /me endpoint, which requires a delegated permission token, making use of client credentials (application permission) impossible.
The HTTP API exposes an /authorize endpoint which, after taking the users consent, does the entire OAuth flow of fetching the access token and refresh token.
The endpoint /access-token exposes the token and will automatically refresh the token if expired. This endpoint returns an Unauthorized HTTP Status Code if the refresh token is expired.

Implementation details:
- ASP.NET Core 6 Minimal API template
- Stores the tokens in memory--restarting the application will mean having to re-authenticate

