using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.Core;
using PavlovRconWebserver.Models;
using PrimS.Telnet;
using TcpClient = System.Net.Sockets.TcpClient;

namespace PavlovRconWebserver.Services
{
    public class RconService
    {
        public static readonly int BufferSize = 4096;
        public async Task<string> SendCommand(RconServer server, string command )
        {
            var answer = "";
           
            try
            {
                
                using (Client client = new Client(server.Adress, server.Port, new System.Threading.CancellationToken()))
                {
                    if (client.IsConnected)
                    {
                        
                        Task.Delay(300).Wait();
                        //Say hello
                        var hello = await client.ReadAsync();
                        //Check answer
                        if (!hello.StartsWith("Password:")) throw new Exception("There server "+server.Adress +" give stranges answers: "+hello); 

                        
                        // send password
                        await client.WriteLine(server.Password);
                        
                        Task.Delay(300).Wait();
                        // check answer
                        var loginIn = await client.ReadAsync();
                        if (!loginIn.StartsWith("Authenticated=1")) throw new Exception("Could not login to server: "+server.Adress);
                        
                        
                        Task.Delay(300).Wait();
                        // send command
                        await client.WriteLine(command);
                        // check answer
                        answer = await client.ReadAsync();
                        
                        Task.Delay(300).Wait();
                        // send Disconnect
                        await client.WriteLine("Disconnect");
                        client.Dispose();
                        
                        
                        
                    }
                }
                return answer;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            
        }

    }
}