# PavlovRconWebserver

Pictures:
![Index](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Index.png?raw=true)
![Login](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Login.png?raw=true)
![Commands2](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Commands2.png?raw=true)
![Modals](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Modals.png?raw=true)
![AddServerPart1](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/AddServerPart1.png?raw=true)
![AddServerPart2](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/AddServerPart2.png?raw=true)

Important for the server the Telnet password and port are riquired cause it will at the end always connect to localhost over Telnet to execute the commands. If you set the checkbox to use Telnet it will also (with the least priority) use Telnet over the Internet.

Auth logic:
If the riquired values are set the following auth system will be tried and in this order.

1. ssh key + passphrase
2. ssh key
3. ssh user pass
4. telnet

Testet and worked for me:

- ssh key + passphrase
- ssh user pass
- telnet
- Most of the commands stuff (Had the problem that i din't know wich ids are avaible so a entered 0,1,2)
- Add user
- Remove user
- Add user to user group
- User in user group can only execute commands
- User in admin group can add user, add users to role, add server, send commands

Contra:

- very less tests on linux and seems to crash a lot on my system every 10 mins? But i just testet it with dotnet run so there was maybe another problem?
- Only testet on 2 Systems Windows 10 and Ubuntu 20.04

Build parameter:

- On windows builded with Jetbrains Rider but should work with: "dotnet publish -c release"  
- On linux it need this parameter:  "dotnet publish -c release -o /home/pavlov/PavlovWebServerBuild/ --runtime linux-x64 --self-contained true --framework netcoreapp3.1" at least in my tests

Note: 
If you build it by youself be sure to add the database.db file and the other riquired folders(see release) befor start the application.

Other:

- It will get updates cause it need much of work and bug fixxing. Im not happy with a lot of code but it works for now.  
- Not very userfriendly right now cause of missing error handling and stuff.

Hot to install:
1. Unzip  
2. Install the dotnet core runtime 3.1 should be enouth (otherwide sdk)  
3. For local use only "dotnet PavlovRconWebserver.dll --urls=https://127.0.0.1:5001/"  
4. Goto to https://127.0.0.1:5001/  
5. Default admin to start with: admin pw: 123456  
6. Add your server  
7. Go to commands select your server an try to do stuff :D  


Security:

Not that much testet.
If you need to run it over the internet please use a https conf with cert!

Credits: 

A LiteDB provider for ASP.NET Core Identity framework.  
https://github.com/fabian-blum/AspNetCore.Identity.LiteDB 

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
