# PavlovRconWebserver

Pictures:
![Index](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Index.png?raw=true)
![Login](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Login.png?raw=true)
![Commands](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/Commands.png?raw=true)
![AddServerPart1](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/AddServerPart1.png?raw=true)
![AddServerPart1](https://github.com/devinSpitz/PavlovRconWebserver/blob/master/Pictures/AddServerPart2.png?raw=true)


Auth Logic:
If the riquired values are sett the following auth system will be tried and in this order.

1. ssh key + passphrase
2. ssh key
3. ssh user pass
4. telnet

Testet and worked for me:

- ssh key + passphrase
- ssh user pass
- telnet
- Most of the Commands stuff (Had the problem that i din't know wich ids are avaible so a entered 0,1,2)
- Add User
- Remove User
- Add user to User >Group
- User in User group can only axecute commands
- User in Admin group can add user, add users to role, add server, send commands

Contra:

- very less tests on linux and seems to crash a lot on my system every 10 mins? but i just testet it with dotnet .run so there was maybe another problem?
- Only testet on 2 Systems Windows 10 and Ubuntu 20.04

I will make it better as soon as i have more time tit was 4 days of work.




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

Thanks to all this people who worked for this projects. Without that it wouldn't be possible to do this.




<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br />This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.
