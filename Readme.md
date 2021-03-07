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
``` bash
sudo apt-get install certbot
sudo certbot certonly --manual --preferred-challenges=dns -d <YourDomain> -m <YourEmail>
```

To edit nginx configuration:
``` bash
sudo nano /etc/nginx/sites-enabled/default
sudo systemctl restart nginx
```

The nginx configuration:
``` config
server {
        server_name _;
        # SSL configuration
        listen 9080 ssl default_server;
        listen [::]:9080 ssl default_server;
        ssl_certificate /etc/letsencrypt/live/public.domain.name/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/public.domain.name/privkey.pem;

        root /var/www/rpihost/wwwroot;

        # Try direct files first and then reverse proxy
        try_files $uri @proxy;

        location @proxy {
                proxy_bind $server_addr;
                proxy_pass http://localhost:5000;
                proxy_http_version 1.1;
                proxy_set_header   Upgrade $http_upgrade;
                proxy_set_header   Connection keep-alive;
                proxy_set_header   Host $host:$server_port;
                proxy_cache_bypass $http_upgrade;
                proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header   X-Forwarded-Proto $scheme;
                # Needed to pass the external port as part of the host 
                # since it was not a default 80/443 one
                proxy_set_header   X-Forwarded-Host  $host:$server_port;
                proxy_set_header   X-Forwarded-Port  $server_port;
                # oAuth forced me to do that
                proxy_buffer_size  128k;
                proxy_buffers      4 256k;
                proxy_busy_buffers_size 256k;
        }
}
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

View service logs
```bash
sudo journalctl -fu rpihost.service
```


## Handling secrets

You should always treat your codebase as if it was public, thus no secrets should be stored in the repository. See [Microsoft docs reference](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1).
In this project's case, we are using the secret manager approach. Right click on the project and select "Manage User Secrets" to specify your settings.

## Video stream reverse proxy

In the RPI there is Motion installed with an RPI Camera module. We could use https://github.com/proxykit/ProxyKit to expose the feed but unfortunately the steam is not passing through. This is why we implemented our own reverse proxy that flushes the buffers.


## TODO list

Perhaps setup a DDNS linux service through azure DNS
https://www.lewisroberts.com/2016/05/28/using-azure-dns-dynamic-dns/


## Corners that were cut

Dependency injection and testability of this project was not in scope of the MVP.


## Docker deploy via IoT Hub

A public docker image of this repo is available at https://hub.docker.com/repository/docker/dodekanisou/home-automation.

The application.json can be overridden using docker volumes. Upload your json file in the RPI `/etc/iotedge/storage/home-automation.appsettings.json`.

Assuming you have (installed and configured the IoT Edge runtime](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge) you will need to deploy a module:
- Name: home-automation
- Image url: dodekanisou/home-automation:2021-03-07 (or later)
- Container create options: Note that we give access to gpiomem and make it privileged to access the gpio pins
  ```
  {
    "HostConfig": {
      "Binds": [
        "/etc/iotedge/storage/home-automation.appsettings.json:/app/appsettings.json",
        "/dev/gpiomem:/dev/gpiomem"
      ],
      "Privileged": true,
      "PortBindings": {
        "5000/tcp": [
          {
            "HostPort": "5000"
          }
        ]
      }
    }
  }
  ```

### IoT Edge runtime on Ubuntu 20 in RPI 4b

Here are the commands used to deploy the iot edge runtime:

```
apt install curl -y
curl https://packages.microsoft.com/config/ubuntu/18.04/multiarch/prod.list > ./microsoft-prod.list
cp ./microsoft-prod.list /etc/apt/sources.list.d/
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
cp ./microsoft.gpg /etc/apt/trusted.gpg.d/
apt-get update
apt-get install moby-engine -y
apt list -a iotedge
apt-get install iotedge -y
# Add your connection string
nano /etc/iotedge/config.yaml

systemctl restart iotedge
systemctl status iotedge
iotedge check
iotedge list
docker ps


# Troubleshoot
journalctl -u iotedge -p warning
iotedge logs home-automation -f
```