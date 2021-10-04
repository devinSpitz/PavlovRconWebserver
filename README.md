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
* Logs 24h for errors and stuff
* Hangfire only for admins even when in production Environment
* Better notification when something happens in the background that could interrupt your work

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
- If you build it by yourself be sure to add the database.db file and the other required folders(see release) befor start the application.  
- The old user and roles system ist not compatible to the new one. So you have to restart with a new database, if you are from the version 0.0.1!  

* Telnet direct connections are not supported anymore cause i have to clean the maps from the cache!  

Help:
=======
1. If you expirience any problem first press "CTRL"+"F5" to reload JavaScript.  
2. Known issues found by makupi/pavlov-bot: https://github.com/makupi/pavlov-bot#known-issues-with-rcon-that-bot-cant-fix


   
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

<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br />This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.
