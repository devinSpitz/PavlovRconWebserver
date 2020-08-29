# PavlovRconWebserver

Pictures:
![Index](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Index.png?raw=true)
![Login](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Login.png?raw=true)
![Commands2](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Commands2.png?raw=true)
![ChooseItem](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/ChooseItem.png?raw=true)
![ChooseMap](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/ChooseMap.png?raw=true)
![Modals](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Modals.png?raw=true)
![AddServerPart1](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/AddServerPart1.png?raw=true)
![AddServerPart2](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/AddServerPart2.png?raw=true)


Videos:  
[Feature Guide](https://youtu.be/iSmAP6_DAyM)  
[How To Install/Build](https://youtu.be/GBgW4mP2zgI)    


Important for the server the Telnet password and port are riquired cause it will at the end always connect to localhost over Telnet to execute the commands. 
If you set the checkbox to use Telnet it will also (with the least priority) use Telnet over the Internet.

Auth logic with priority(with added multiple options):

1. ssh key + username + passphrase
2. ssh key + username
3. ssh username pass
4. telnet

Default users:  
User: admin  
pw: A2345a$  

How to install:  
[How To Install/Build](https://youtu.be/GBgW4mP2zgI)   
  
Also may take a look at this tutorial (But be also aware the the builds are standalone so you dont need any sdk or runtime if you just take the build):  
https://dev.to/ianknighton/hosting-a-net-core-app-with-nginx-and-let-s-encrypt-1m50  

[Commands](https://pastebin.com/dbGUsvUn)

What are the features?

[Feature Guide](https://youtu.be/iSmAP6_DAyM)

Note:   
If you build it by yourself be sure to add the database.db file and the other riquired folders(see release) befor start the application.  
The old user and roles system ist not compatible to the new one. So you have to restart with a new database, if you are from the version 0.0.1!  


Credits: 

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

Thanks to all this people who worked for this nuget packages. Without that it wouldn't be possible to do this.




<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br />This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.
