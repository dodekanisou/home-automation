# Home automation project

This is a hobby project that was created during Christmas 2019 in order to:
- Play with the Auth0.com service.
- Discover what's new in .net core 3.1.
- Get hands on experience with the [Azure Relay](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-what-is-it).
- Allow people in my building to control public doors (including garage) from their mobile phones

If you are looking for open source well adopted solutions for your house automation, please consider:
- https://www.openhab.org/
- https://www.home-assistant.io/

## Run in RPI

Publish the web host and copy the files in `/var/www/rpihost`. 
Ensure that the exec has the required permission:
``` bash
sudo chmod +x /var/www/rpihost/RpiHost
```

Create a new service definition file:

```  bash
sudo nano /etc/systemd/system/rpihost.service
```

with the following content:

``` ini
[Unit]
Description=The RPI host web app to control the relays

[Service]
WorkingDirectory=/var/www/rpihost
ExecStart=sudo /var/www/rpihost/RpiHost
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=rpihost
User=rootUser
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

and then install the service, execute and observe status:

``` bash
sudo systemctl enable rpihost.service
sudo systemctl start rpihost.service
sudo systemctl status rpihost.service
```

> If you modify the 'rpihost.service' file, you will need to reload the daemon by `sudo systemctl daemon-reload`. 

## Handling secrets

You should always treat your codebase as if it was public, thus no secrets should be stored in the repository. See [Microsoft docs reference](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1).
In this project's case, we are using the secret manager approach. Right click on the project and select "Manage User Secrets" to specify your settings.

## Video stream reverse proxy

In the RPI there is Motion installed with an RPI Camera module. We could use https://github.com/proxykit/ProxyKit to expose the feed but unfortunately the steam is not passing through.

## TODO list

Perhaps setup a DDNS linux service through azure DNS
https://www.lewisroberts.com/2016/05/28/using-azure-dns-dynamic-dns/


## Corners that were cut

Dependency injection and testability of this project was not in scope of the MVP.