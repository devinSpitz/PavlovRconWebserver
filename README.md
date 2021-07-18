# PavlovRconWebserver

![GitHub All Releases](https://img.shields.io/github/downloads/devinspitz/PavlovRconWebserver/total)
![GitHub tag (latest by date)](https://img.shields.io/github/v/tag/devinspitz/PavlovRconWebserver?label=release)
![Plaforms](https://img.shields.io/static/v1?label=platform:&message=windows10%20|%20linux&color=green)  
Pictures:  
![Index](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Index.png?raw=true)
![Commands2](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Commands2.png?raw=true)
![ChooseItem](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/ChooseItem.png?raw=true)
![ChooseMap](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/ChooseMap.png?raw=true)
![Modals](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Modals.png?raw=true)


Videos:  
[Feature Guide](https://youtu.be/iSmAP6_DAyM)  
[How To Install/Build](https://youtu.be/GBgW4mP2zgI)    


Auth logic with priority(with added multiple options):

1. ssh key + username + passphrase
2. ssh key + username
3. ssh username pass

Default users:  
User: admin  
pw: A2345a$  

How to install:  
[How To Install/Build](https://youtu.be/GBgW4mP2zgI)   
  

Also may take a look at this tutorial (But be also aware the the builds are standalone so you dont need any sdk or runtime if you just take the build):  
https://dev.to/ianknighton/hosting-a-net-core-app-with-nginx-and-let-s-encrypt-1m50  

[Commands](https://pastebin.com/dbGUsvUn)


Features:
=======
newly added:  
* The ssh user now should be the steam user. (Root user will fail in code)
* Root user is needed to create a pavlov server(will not be saved!). The service that gets create need root to make the .service file
* Users can now change there skin
* Server handle Stop and Start  
* Chosen maps not only have effect on deleting also has effect on the server settings.  
* You can edit the server settings now
* The system knows which state the server of a pavlov server has
* You can now edit the Mod and White list of a pavlov server
* Mod on a pavlov server now also means Mod in case of commands for this single server in the GUI
* Users are now no longer able to inspect server where they are not a mod. (they can get the infos from a website which gets the HTML from /PublicViewLists/PlayersFromServers/*)

 older:   

* Player list with stats etc.
* Ban list over time. You can now ban People for a specific time.
* You can select maps that will not get deleted, when the cache will get cleaned.
* Maps will be deleted every day on 3 o clock in the morning(so the cache will not overflow on your server)
* Maps from steam will be crawled every day on 2 o clock in the morning(While this  is happening the server may have a lot to do and will answer with some delay)
* the selected maps from the server will be first in the map selector
* Telnet direct connections are not supported anymore cause i have to clean the maps from the cache!  
* Swagger is only available in development mode: http://localhost:5001/swagger and without registration / Thats why its disabled on production  
* Hangfire dashboard is only available in development mode: http://localhost:5001/hangfire and without registration/ Thats why its disabled on production  

[Feature Guide](https://youtu.be/iSmAP6_DAyM)


Note:
======= 
- If you build it by yourself be sure to add the database.db file and the other riquired folders(see release) befor start the application.  
- The old user and roles system ist not compatible to the new one. So you have to restart with a new database, if you are from the version 0.0.1!  

* Telnet direct connections are not supported anymore cause i have to clean the maps from the cache!  

Help:
=======
1. If you expirience any problem first press "CTRL"+"F5" to reload JavaScript.  
2. Known issues found by makupi/pavlov-bot: https://github.com/makupi/pavlov-bot#known-issues-with-rcon-that-bot-cant-fix
3. Under Windows(on my system) there is the problem that the background services can not access the db even if it is shared.  
   Then you will get the following exception:  
```The process cannot access the file '...\Database.db' because it is being used by another process.```  
   I dont know how to fix this problem. It works under linux xD

Credits:
=======

Implementation of AspNetCore.Identity for LiteDB database engine.   
https://github.com/quicksln/LiteDB.Identity

The most simple, unambiguous, and lightweight .NET guard clause library.  
https://github.com/adamralph/liteguard

Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation  
https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation/3.1.6

Json.NET is a popular high-performance JSON framework for .NET  
https://www.newtonsoft.com/json

SSH.NET is a Secure Shell (SSH) library for .NET, optimized for parallelism.  
https://github.com/sshnet/SSH.NET/

Boostrap: The most popular front-end framework for developing responsive, mobile first projects on the web.  
https://getbootstrap.com

Html Agility Pack (HAP)
https://github.com/zzzprojects/html-agility-pack  

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

=====================  
Thanks to all this people who worked for this nuget packages. Without that it wouldn't be possible to do this.  
  
  
Swagger tools for documenting APIs built on ASP.NET Core  
https://github.com/domaindrivendev/Swashbuckle.AspNetCore  
  
Thanks to all this people who worked for this nuget packages. Without that it wouldn't be possible to do this.  



<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br />This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.
