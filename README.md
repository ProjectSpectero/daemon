## Spectero Daemon
##### Daemon component that runs the { proxy | vpn } server and exposes a RESTful WEB API to enable controlling these components dynamically.

#### Project Specification

Core Technology - .NET CORE 2.0 and ASP.NET Core, written in Visual C#

Components - Proxy Handler, VPN Handler, Local DBAL, RESTful API to manipulate Local DBAL

Optional Enhancements - SSH Tunnel Handler (future)

Any 3rd party libraries imported MUST be compliant with .NET CORE instead of .NET framework, we are targetting true multiplatform compatibility.


### Code Style

Currently unenforced, future integration with StyleCI planned. Please take a look at existing code and try as best as possible to write similar looking code so we don't cause confusion.

#### Folder Structure

* Controllers go in the Controllers folder, route definitions are done in ASP.NET core style
* Static assets (if needed) go in the wwwroot, but we shouldn't have many (if any) of these.
* Environment specific configuration goes in `appsettings.<environment>.json`. Generic configuration goes in `appsettings.json`
* The SQLite DB and its template (which is only used to initially create the schema on either firstrun or corruption) resides in Database
* All app specific code that are not meant for being directly consumed should reside in the Library folder, create subfolders here as necessary to properly namespace components as needed.

#### Testing

Full Unit Testing code coverage is planned, will likely integrate with a continuous integration platform like Travis-CI at a later date.

#### Deployment / Join the Fray

Import daemon.sln on VS2017, dedicated test builds are not yet available.