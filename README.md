# PavlovRconWebserver

![GitHub All Releases](https://img.shields.io/github/downloads/devinspitz/PavlovRconWebserver/total)
![GitHub tag (latest by date)](https://img.shields.io/github/v/tag/devinspitz/PavlovRconWebserver?label=release)
![Platforms](https://img.shields.io/static/v1?label=platform:&message=windows10%20|%20linux&color=green)
[![CodeFactor](https://codefactor.io/repository/github/devinspitz/pavlovrconwebserver/badge)](https://www.codefactor.io/repository/github/devinspitz/pavlovrconwebserver)
[![CircleCI](https://circleci.com/gh/devinSpitz/PavlovRconWebserver/tree/master.svg?style=shield)](https://circleci.com/gh/devinSpitz/PavlovRconWebserver/tree/circleci-project-setup)
[![Discord](https://badgen.net/discord/members/G5VpbgdYey)](http://dc.spitzen.solutions)  
Pictures:  
![Index](./PavlovRconWebserver/Pictures/Index.png?raw=true)
![Servers](./PavlovRconWebserver/Pictures/Servers.png?raw=true)
![PavlovServerSystemSettings](./PavlovRconWebserver/Pictures/PavlovServerSystemSettings.png?raw=true)
![PavlovServerSettings](./PavlovRconWebserver/Pictures/PavlovServerSettings.png?raw=true)
![MapsSelector](./PavlovRconWebserver/Pictures/MapsSelector.png?raw=true)
![Rcon](./PavlovRconWebserver/Pictures/rcon.png?raw=true)
![Commands](./PavlovRconWebserver/Pictures/Commands.png?raw=true)
![ChooseItem.png](./PavlovRconWebserver/Pictures/chooseItem.png?raw=true)
![IndexMapCycleView.png](./PavlovRconWebserver/Pictures/IndexMapCycleView.png?raw=true)


Attention:
=======
This software can easily manage your pavlov servers. This also means deleting.  
Please proceed only if you know what you are doing.  
Otherwise ask for help first:  
https://github.com/devinSpitz/PavlovRconWebserver/discussions

Features:
=======
newly added:  
* Map view on index page so visitors can see the map cycle
* Premium Role:  The costume clown is now only available for premium members or higher roles.(Global roles with access to Clown: Premium,OnPremise,ServerRent,Mod,Captain,Admin)
* OnPremise Role which administers an ssh server itself and can therefore also administer its own servers etc. via the platform.
* ServerRent Role which can administer a separate pavlov server in a limited way.
* Changed The Captain and Mod rights so they can not handle onPremise and ServerRent servers.
* Team roles only are active in the Team Manager and Match Handler or in the match itself. 
* 1 KeyFile per SSh server can be store in the Database(be aware breaking change to before!) 
* Index now shows all server online and there stats. 
* The ssh user now should be the steam user. (Root user can fail in code)
* Root user is needed to create a pavlov server(will not be saved!). Need root to be able to make the .service file.
* Users can now change there skin
* Server handle Stop and Start  
* Chosen maps not only have effect on deleting also has effect on the server settings.  
* You can edit the server settings now
* The system knows which state the server of a pavlov server has
* You can now edit the Mod and White list of a pavlov server
* Mod on a pavlov server now also means Mod in case of commands for this single server in the GUI
* Users are now no longer able to inspect server where they are not a mod. (they can get the infos from a website which gets the HTML from /PublicViewLists/PlayersFromServers/*)
* Logs 24h for errors and stuff
* Better notification when something happens in the background that could interrupt your work

 older:   

* Player list with stats etc.
* Ban list over time. You can now ban People for a specific time.
* You can select maps that will not get deleted, when the cache will get cleaned.
* Maps will be deleted every day on 3 o clock in the morning(so the cache will not overflow on your server)
* Maps from steam will be crawled every day on 3 o clock in the morning(While this  is happening the server may have a lot to do and will answer with some delay)
* the selected maps from the server will be first in the map selector
* Telnet direct connections are not supported anymore cause i have to clean the maps from the cache!  
* Swagger is only available in development mode: http://localhost:5001/swagger and without registration / Thats why its disabled on production  

Offer:
======
That applies to all offers:
- The primary aspect of these offers is the support of the developer with a consideration (OnPromise or ServerRent) without guarantee.
- The server is located in Frankfurt/Germany.
- No virtualization.
- The service can be reached at: https://pavlov.spitzen.solutions
- The arrangement can be canceled every month from your side or my side and you pay in front. 
  That means if you don't pay for a month your account on the service and the server will get removed from the service within 30 days.
  
Offers:
1. I can help installing or updating the software. If you want more information: https://github.com/sponsors/devinSpitz
2. you can now rent a 30 Slot Pavlov server(No Shack) from me for 30$/€ a month:  
   2.1 You can administrate the server with this software as a user in the ServerRent role.  
   2.2 You will get support with normally answer within 2 working days. Timezone Europe/Zurich from 9:00 to 16:00 Mon.-Fri.  
   2.3 You can appoint mods and configure maps yourself etc.  
   ![Rent](./PavlovRconWebserver/Pictures/Rent.png?raw=true)
   2.4 There is no uptime guarantee and no other guarantee. This package mainly supports this software.  
   2.5 If you are interested just contact: <a href="mailto:&#x64;&#x65;&#x76;&#x69;&#x6e;&#x40;&#x73;&#x70;&#x69;&#x74;&#x7a;&#x65;&#x6e;&#x2e;&#x73;&#x6f;&#x6c;&#x75;&#x74;&#x69;&#x6f;&#x6e;&#x73;">&#x64;&#x65;&#x76;&#x69;&#x6e;&#x40;&#x73;&#x70;&#x69;&#x74;&#x7a;&#x65;&#x6e;&#x2e;&#x73;&#x6f;&#x6c;&#x75;&#x74;&#x69;&#x6f;&#x6e;&#x73;</a>
3. you can now get an account with the on Promise Role for 30$/€ a month.  
   3.1 You will need your own Debian/Ubuntu server that has a static IP.  
   3.2 You will get support with normally answer within 2 working days. Time zone Europe/Zurich from 9:00 to 16:00 Mon-Fri.  
   3.3 You only have to enter your ssh login data and install steamcmd to create new Pavlov servers with this software.  
   3.4 You can then also appoint mods yourself etc. and everything that a rental can do.  
   3.5 There is no uptime guarantee and no other guarantee. This package mainly supports this software.  
   3.6 If you are interested just contact: <a href="mailto:&#x64;&#x65;&#x76;&#x69;&#x6e;&#x40;&#x73;&#x70;&#x69;&#x74;&#x7a;&#x65;&#x6e;&#x2e;&#x73;&#x6f;&#x6c;&#x75;&#x74;&#x69;&#x6f;&#x6e;&#x73;">&#x64;&#x65;&#x76;&#x69;&#x6e;&#x40;&#x73;&#x70;&#x69;&#x74;&#x7a;&#x65;&#x6e;&#x2e;&#x73;&#x6f;&#x6c;&#x75;&#x74;&#x69;&#x6f;&#x6e;&#x73;</a>      
   3.7 You can also edit the Game Ini settings:
   ![OnPremise](./PavlovRconWebserver/Pictures/OnPremise.png?raw=true)


Auth:
=======
Auth logic with priority(with added multiple options):

1. ssh key + username + passphrase
2. ssh key + username
3. ssh username pass

Default users:  
User: admin  
pw: A2345a$  


Note:
======= 
- If you build it by yourself be sure to add the database.db file and the other required folders(see release) before start the application.  
- The old user and roles system ist not compatible to the new one. So you have to restart with a new database, if you are from the version 0.0.1!  

* Telnet direct connections are not supported anymore cause i have to clean the maps from the cache!  

Help:
======= 
1. Known issues found by makupi/pavlov-bot: https://github.com/makupi/pavlov-bot#known-issues-with-rcon-that-bot-cant-fix
2. If you are had KeyFiles before 0.0.3 you need to restart with the defaultDb or drop the tables sshserver and pavlovServer with Lite DB Studio https://github.com/mbdavid/LiteDB.Studio  
3. If you are coming from an older version then you have to copy the database to the new location. ./Database/Database.db  

Install Linux:
======= 

1. Download newest build in the releases https://github.com/devinSpitz/PavlovRconWebserver/releases
2. unzip the zip archive: unzip PavlovRconBuildLinux.zip
3. Go into the folder and to Build Linux step 5

Build Linux:
======= 

Requirements:
1. dotnet sdk 5.0:  https://dotnet.microsoft.com/download/dotnet/5.0
2. git

Steps :
1. git clone https://github.com/devinSpitz/PavlovRconWebserver.git
2. cd PavlovRconWebserver/PavlovRconWebserver
3. (without brackets) dotnet publish -c release -o "Full build path" --runtime linux-x64 --self-contained true --framework net5.0
4. copy the default database to your "Full build path"/Database/Database.db.
5. create a service: sudo nano /etc/systemd/system/pavlovRconWebserver.service
6. Content (without brackets) and replace your variables:   
```
        
[Unit]
Description=PavlovWebServer
 
[Service]
WorkingDirectory="Full build path"
ExecStart="Full build path"/PavlovRconWebserver --urls=http://*:5001/
Restart=always
RestartSec=10 # Restart service after 10 seconds if dotnet service crashes
SyslogIdentifier=dotnet-core-app
User=steam
Environment=ASPNETCORE_ENVIRONMENT=Production
#Environment=ASPNETCORE_ENVIRONMENT=Development
[Install]
WantedBy=multi-user.target
```
7. sudo systemctl enable pavlovRconWebserver
8. sudo systemctl start pavlovRconWebserver  
9. sudo apt install nginx
10. sudo nano /etc/nginx/sites-available/default
11. Replace content (without brackets) and replace your variables:
```
server {
        listen 80 default_server;
        server_name "Domain/subdomain";
    location / {
        proxy_pass         http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
  }
}
```

12. sudo apt-get install certbot python-certbot-nginx  
13. sudo certbot --nginx -d "Domain/subdomain"  
14. Go to your Domain/subdomain  
15. Login as Admin  
16. Change password  
17. Goto Hangfire -> Recurring Jobs -> Start: 	SteamService.CrawlSteamMaps  
![Rent](./PavlovRconWebserver/Pictures/StartMapCrawl.png?raw=true)
18. Use the software as you like.

Docker:
======= 

1. Just wget the [docker-compose.yml](./PavlovRconWebserver/docker-compose.yml) file
2. In the same folder where you downloaded the file execute: docker-compose up -d
3. For lets encrypt etc. i would use something like: https://github.com/nginx-proxy/docker-gen


Install Windows:
======= 

1. Download newest build in the releases https://github.com/devinSpitz/PavlovRconWebserver/releases  
2. unzip the zip archive: PavlovRconBuildWindows.zip
3. Go into the folder and to Build Windows step 6  

Build Windows:
======= 

Requirements:
1. Install dotnet sdk 5.0:  https://dotnet.microsoft.com/download/dotnet/5.0
2. git CLI: https://git-scm.com/download/win


Steps:
1. open the git cli wherever you want to download the files.
2. git clone https://github.com/devinSpitz/PavlovRconWebserver.git
3. goto to created directory and then to this folder: PavlovRconWebserver/PavlovRconWebserver
4. open a Powershell and enter the command: dotnet publish -c release -o "Full build path" --runtime win-x64 --self-contained true --framework net5.0
5. copy the default database to your "Full build path"\Database\Database.db.
6. run the PavlovRconWebserver.exe in the "Full build path"
7. Please don't use it public like this. You need at least a SSL Certificate. Use something like that: https://certbot.eff.org/lets-encrypt/windows-other.html
8. After you have your ssl done go to your Domain/subdomain  
9. Login as Admin  
10. Change password  
11. Goto Hangfire -> Recurring Jobs -> Start: SteamService.CrawlSteamMaps  
![Rent](./PavlovRconWebserver/Pictures/StartMapCrawl.png?raw=true)
12. Use the software as you like.  

Any problem or bug?:
=======
Read and create issues: https://github.com/devinSpitz/PavlovRconWebserver/issues

Want to discuss, asking questions or having trouble installing the software?:
=======
Read and write here: https://github.com/devinSpitz/PavlovRconWebserver/discussions

Todo:
=======
Read: https://github.com/devinSpitz/PavlovRconWebserver/projects

Donate:
=======
Feel free to support my work by donating:  

<a href="https://www.paypal.com/donate?hosted_button_id=JYNFKYARZ7DT4">
<img src="https://www.paypalobjects.com/en_US/CH/i/btn/btn_donateCC_LG.gif" alt="Donate with PayPal" />
</a>

Business:
=======

For business inquiries please use:

<a href="mailto:&#x64;&#x65;&#x76;&#x69;&#x6e;&#x40;&#x73;&#x70;&#x69;&#x74;&#x7a;&#x65;&#x6e;&#x2e;&#x73;&#x6f;&#x6c;&#x75;&#x74;&#x69;&#x6f;&#x6e;&#x73;">&#x64;&#x65;&#x76;&#x69;&#x6e;&#x40;&#x73;&#x70;&#x69;&#x74;&#x7a;&#x65;&#x6e;&#x2e;&#x73;&#x6f;&#x6c;&#x75;&#x74;&#x69;&#x6f;&#x6e;&#x73;</a>


Credits:
=======

LiteDB.Identity  
Implementation of AspNetCore.Identity for LiteDB database engine.   
https://github.com/quicksln/LiteDB.Identity  

Serilog  
This package routes ASP.NET Core log messages through Serilog, so you can get information about ASP.NET's internal operations written to the same Serilog sinks as your application events.  
https://github.com/serilog/serilog-aspnetcore

Mock & Automock  
An automocking container for Moq. Use this if you're invested in your IoC container and want to decouple your unit tests from changes to their constructor arguments.
https://github.com/moq/Moq.AutoMocker  

Coverlet  
Cross platform code coverage for .NET  
https://github.com/coverlet-coverage/coverlet  

ToastNotification  
ToastNotification is a Minimal & Elegant Toast Notification Package for ASP.NET Core Web Applications that can be invoked via C#. Compatible with ASP.NET Core 3.1 and .NET 5.  
https://github.com/aspnetcorehero/ToastNotification  

Fluent Assertions  
A very extensive set of extension methods that allow you to more naturally specify the expected outcome of a TDD or BDD-style unit tests. Targets .NET Framework 4.7, .NET Core 2.1 and 3.0, as well as .NET Standard 2.0 and 2.1. Supports the unit test frameworks MSTest2, NUnit3, XUnit2, MSpec, and NSpec3.
https://github.com/fluentassertions/fluentassertions  


Json.NET  
Json.NET is a popular high-performance JSON framework for .NET  
https://www.newtonsoft.com/json

SSH.NET  
SSH.NET is a Secure Shell (SSH) library for .NET, optimized for parallelism.  
https://github.com/sshnet/SSH.NET/

Boostrap:   
The most popular front-end framework for developing responsive, mobile first projects on the web.  
https://getbootstrap.com

Html Agility Pack (HAP)  
Html Agility Pack (HAP) is a free and open-source HTML parser written in C# to read/write DOM and supports plain XPATH or XSLT. It is a .NET code library that allows you to parse "out of the web" HTML files.
https://github.com/zzzprojects/html-agility-pack

Hangfire  
An easy and reliable way to perform fire-and-forget, delayed and recurring, long-running, short-running,   
CPU or I/O intensive tasks inside ASP.NET applications. No Windows Service / Task Scheduler required. Even ASP.NET is not required. Backed by Redis, SQL Server, SQL Azure or MSMQ. This is a .NET alternative to Sidekiq, Resque and Celery.   
https://www.hangfire.io/   

Telnet  
A minimal open-source C# 3.5/4.0/4.5/4.5.1/NetStandard1.6/NetStandard2.0 Telnet client library implementation; just enough to send simple text commands and retrieve the response.  
https://github.com/9swampy/Telnet/


Font Awesome  
Get vector icons and social logos on your website with Font Awesome, the web's most popular icon set and toolkit.   
https://fontawesome.com/  

Bootswatch  
Free themes for Bootstrap  
https://bootswatch.com/  

Xunit  
xUnit.net is a free, open source, community-focused unit testing tool for the .NET Framework.  
https://github.com/xunit/xunit

Swagger tools for documenting APIs built on ASP.NET Core  
https://github.com/domaindrivendev/Swashbuckle.AspNetCore

LiteDB.Identity.Async  
Make LiteDB.Identity async  
https://github.com/devinSpitz/LiteDB.Identity.Async

Serilog.Sinks.LiteDb.Async  
Serilog event sink that writes to LiteDb database  
https://github.com/devinSpitz/Serilog.Sinks.LiteDb.Async  

  
Thanks to all this people who worked for this nuget packages. Without that it wouldn't be possible to do this.  




<b>Powered by Spitz IT Solutions</b>  

For commercial licences you can find more information here: https://github.com/sponsors/devinSpitz  

<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br />This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.
