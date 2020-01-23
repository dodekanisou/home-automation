# Home automation project

This is a hobby project that was created during Christmas 2019 in order to:
- Play with the Auth0.com service.
- Discover what's new in .net core 3.1.
- Allow people in my building to control public doors (including garage) from their mobile phones
- Expose internal video feed from the motion project that runs in the RPI.

If you are looking for open source well adopted solutions for your house automation, please consider:
- https://www.openhab.org/
- https://www.home-assistant.io/

## Run in RPI

Publish the web host.

Zip the contents of `bin\Release\netcoreapp3.1\publish` and transfer the zip file in the RPI.

Unzip the zip into `/var/www/rpihost`.
``` bash
sudo unzip publish.zip -d /var/www/rpihost
```

Ensure that the RpiHost executable has the required permission:
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

If you just need to restart the service after a new deployment:

``` bash
sudo systemctl start rpihost.service
```

## Running behind nginx

Read the following articles:
- https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-3.1#configure-nginx
- https://certbot.eff.org/lets-encrypt/ubuntubionic-nginx

Which boils down to:
``` bash
sudo apt-get install nginx
sudo nano /etc/nginx/sites-enabled/default
sudo systemctl start nginx
```

To get a let's encrypt certificate if you can expose port 80 in the net

``` bash
sudo apt-get install certbot python-certbot-nginx
sudo certbot --nginx
```

Otherwise, you have to do it manually:
```bash
sudo apt-get install certbot
sudo certbot certonly --manual --preferred-challenges=dns -d <YourDomain> -m <YourEmail>
```


## RPI's ubuntu maintenance

Various commands for maintenance of the ubuntu in RPI

``` bash
sudo apt-get update
sudo apt-get upgrade
sudo apt autoremove
sudo certbot renew
```

Oneline bin only update
``` bash
sudo mv RpiHos* /var/www/rpihost/ && sudo chmod +x /var/www/rpihost/RpiHost && sudo systemctl restart rpihost && ls -la /var/www/rpihost/ 
```

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