using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PrimS.Telnet;
using Renci.SshNet;
using Renci.SshNet.Common;
using Serilog.Events;

namespace PavlovRconWebserver.Services
{
    public class RconService
    {
        private readonly IToastifyService _notifyService;
        public enum AuthType
        {
            PrivateKey,
            UserPass,
            PrivateKeyPassphrase
        }

        private readonly MapsService _mapsService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly ServerBansService _serverBansService;

        private readonly SshServerSerivce _sshServerSerivce;
        private readonly SteamIdentityService _steamIdentityService;

        public RconService(SteamIdentityService steamIdentityService,
            MapsService mapsService, PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            SshServerSerivce sshServerSerivce,
            PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService,
            ServerBansService serverBansService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _mapsService = mapsService;
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerPlayerHistoryService = pavlovServerPlayerHistoryService;
            _steamIdentityService = steamIdentityService;
            _sshServerSerivce = sshServerSerivce;
            _serverBansService = serverBansService;
        }


        public async Task ReloadPlayerListFromServerAndTheServerInfo(
            bool recursive = false)
        {
            var exceptions = new List<Exception>();
            try
            {
                var servers = await _sshServerSerivce.FindAll();
                foreach (var server in servers)
                foreach (var signleServer in server.PavlovServers.Where(x => x.ServerType == ServerType.Community))
                    try
                    {
                        await SShTunnelGetAllInfoFromPavlovServer(signleServer);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message,LogEventLevel.Verbose,_notifyService);
                    }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message,LogEventLevel.Verbose,_notifyService);
            }
            // Ignore them for now
            // if (exceptions.Count > 0)
            // {
            //     throw new Exception(String.Join(" | Next Exception:  ",exceptions.Select(x=>x.Message).ToList()));
            // }

            // BackgroundJob.Schedule(() => ReloadPlayerListFromServerAndTheServerInfo(recursive),
            //     new TimeSpan(0, 1, 0)); // Check for bans and remove them is necessary
        }

        public async Task CheckBansForAllServers()
        {
            var servers = await _sshServerSerivce.FindAll();
            foreach (var server in servers)
            foreach (var signleServer in server.PavlovServers)
                try
                {
                    var bans = await _serverBansService.FindAllFromPavlovServerId(signleServer.Id, true);
                    SaveBlackListEntry(signleServer, bans.ToList());
                }
                catch (Exception e)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message,LogEventLevel.Verbose,_notifyService);
                }
        }

        public bool SaveBlackListEntry(PavlovServer server, List<ServerBans> NewBlackListContent)
        {
            var blacklistArray = NewBlackListContent.Select(x => x.SteamId).ToArray();
            var content = string.Join(Environment.NewLine, blacklistArray);
            RconStatic.WriteFile(server, server.ServerFolderPath + FilePaths.BanList, content,_notifyService);
            return true;
        }

        public async Task<string> SShTunnelGetAllInfoFromPavlovServer(PavlovServer server)
        {
            var type = RconStatic.GetAuthType(server);
            var connectionInfo = RconStatic.ConnectionInfoInternal(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            var costumesToSet = new Dictionary<string, string>();
            try
            {
                client.Connect();

                if (client.IsConnected)
                {
                    var portToForward = server.TelnetPort + 50;
                    var portForwarded = new ForwardedPortLocal("127.0.0.1", (uint) portToForward, "127.0.0.1",
                        (uint) server.TelnetPort);
                    client.AddForwardedPort(portForwarded);
                    portForwarded.Start();
                    using (var client2 = new Client("127.0.0.1", portToForward, new CancellationToken()))
                    {
                        if (client2.IsConnected)
                        {
                            var password = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Answer: " + password,LogEventLevel.Verbose,_notifyService);
                            if (password.ToLower().Contains("password"))
                            {
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("start sending password!",LogEventLevel.Verbose,_notifyService);
                                await client2.WriteLine(server.TelnetPassword);
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("did send password and wait for auth",LogEventLevel.Verbose,_notifyService);
                                
                                var auth = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));
                                
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("waited for auth get : "+auth,LogEventLevel.Verbose,_notifyService);
                                if (auth.ToLower().Contains("authenticated=1"))
                                {
                                    // it is authetificated
                                    var commandOne = "RefreshList";
                                    var singleCommandResult1 =
                                        await RconStatic.SingleCommandResult(client2, commandOne);

                                    var playersList =
                                        JsonConvert.DeserializeObject<PlayerListClass>(singleCommandResult1);
                                    var steamIds = playersList.PlayerList.Select(x => x.UniqueId);
                                    //Inspect PlayerList
                                    var commands = new List<string>();
                                    if (steamIds != null)
                                        foreach (var steamId in steamIds)
                                            commands.Add("InspectPlayer " + steamId);

                                    var playerListRaw = new List<string>();
                                    foreach (var command in commands)
                                    {
                                        var commandResult = await RconStatic.SingleCommandResult(client2, command);
                                        playerListRaw.Add(commandResult);
                                    }

                                    var playerListJson = string.Join(",", playerListRaw);
                                    playerListJson = "[" + playerListJson + "]";
                                    var finsihedPlayerList = new List<PlayerModelExtended>();
                                    var tmpPlayers = JsonConvert.DeserializeObject<List<PlayerModelExtendedRconModel>>(
                                        playerListJson, new JsonSerializerSettings {CheckAdditionalContent = false});
                                    var steamIdentities = (await _steamIdentityService.FindAll()).ToList();

                                    if (tmpPlayers != null)
                                    {
                                        foreach (var player in tmpPlayers)
                                        {
                                            player.PlayerInfo.Username = player.PlayerInfo.PlayerName;
                                            var identity =
                                                steamIdentities?.FirstOrDefault(x =>
                                                    x.Id == player.PlayerInfo.UniqueId);
                                            if (identity != null && (identity.Costume != "None" ||
                                                                     !string.IsNullOrEmpty(identity.Costume)))
                                                costumesToSet.Add(identity.Id, identity.Costume);
                                        }

                                        finsihedPlayerList = tmpPlayers.Select(x => x.PlayerInfo).ToList();
                                    }

                                    var pavlovServerPlayerList = finsihedPlayerList.Select(x => new PavlovServerPlayer
                                    {
                                        Username = x.Username,
                                        UniqueId = x.UniqueId,
                                        KDA = x.KDA,
                                        Cash = x.Cash,
                                        TeamId = x.TeamId,
                                        Score = x.Score,
                                        ServerId = server.Id
                                    }).ToList();
                                    await _pavlovServerPlayerService.Upsert(pavlovServerPlayerList, server.Id);
                                    await _pavlovServerPlayerHistoryService.Upsert(pavlovServerPlayerList.Select(x =>
                                        new PavlovServerPlayerHistory
                                        {
                                            Username = x.Username,
                                            UniqueId = x.UniqueId,
                                            PlayerName = x.PlayerName,
                                            KDA = x.KDA,
                                            Cash = x.Cash,
                                            TeamId = x.TeamId,
                                            Score = x.Score,
                                            ServerId = x.ServerId,
                                            date = DateTime.Now
                                        }).ToList(), server.Id, 1);

                                    var singleCommandResultTwo =
                                        await RconStatic.SingleCommandResult(client2, "ServerInfo");
                                    var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(
                                        singleCommandResultTwo.Replace("\"\"", "\"ServerInfo\""));
                                    if(!string.IsNullOrEmpty(tmp.ServerInfo.GameMode))
                                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Got the server info for the server: "+ server.Name+"\n "+singleCommandResultTwo,LogEventLevel.Verbose,_notifyService);
                                    var map = await _mapsService.FindOne(tmp.ServerInfo.MapLabel.Replace("UGC", ""));
                                    if (map != null)
                                        tmp.ServerInfo.MapPictureLink = map.ImageUrl;


                                    var tmpinfo = new PavlovServerInfo
                                    {
                                        MapLabel = tmp.ServerInfo.MapLabel,
                                        MapPictureLink = tmp.ServerInfo.MapPictureLink,
                                        GameMode = tmp.ServerInfo.GameMode,
                                        ServerName = tmp.ServerInfo.ServerName,
                                        RoundState = tmp.ServerInfo.RoundState,
                                        PlayerCount = tmp.ServerInfo.PlayerCount,
                                        Teams = tmp.ServerInfo.Teams,
                                        Team0Score = tmp.ServerInfo.Team0Score,
                                        Team1Score = tmp.ServerInfo.Team1Score,
                                        ServerId = server.Id
                                    };

                                    await _pavlovServerInfoService.Upsert(tmpinfo);

                                    result.Success = true;

                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Set skins for "+costumesToSet.Count+" players of the server:"+ server.Name,LogEventLevel.Verbose,_notifyService);

                                    foreach (var customToSet in costumesToSet)
                                        await RconStatic.SendCommandSShTunnel(server,
                                            "SetPlayerSkin " + customToSet.Key + " " + customToSet.Value,_notifyService);
                                }
                                else
                                {
                                    
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Send password but did not get Authenticated=1 answer: "+auth,LogEventLevel.Verbose,_notifyService);
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Telnet Client could not authenticate ..." + server.Name,LogEventLevel.Fatal ,_notifyService, result);
                                }
                            }
                            else
                            {
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Telnet Client did not ask for Password ..." + server.Name,LogEventLevel.Fatal,_notifyService , result);
                            }
                        }
                        else
                        {
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Telnet Client could not connect ..." + server.Name,LogEventLevel.Fatal,_notifyService , result);
                        }

                        client2.Dispose();
                    }

                    client.Disconnect();
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Telnet Client cannot be reached..." + server.Name,LogEventLevel.Fatal,_notifyService , result);
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not Login over ssh!" + server.Name,LogEventLevel.Fatal ,_notifyService, result);
                        break;
                    case SshConnectionException _:
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host over ssh!" + server.Name,LogEventLevel.Fatal,_notifyService , result);
                        break;
                    case SshOperationTimeoutException _:
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host cause of timeout over ssh!" + server.Name,LogEventLevel.Fatal,_notifyService , result);
                        break;
                    case SocketException _:
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host!" + server.Name,LogEventLevel.Fatal ,_notifyService, result);
                        break;
                    case InvalidOperationException _:
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message + " <- most lily this error is from telnet" + server.Name,LogEventLevel.Fatal ,_notifyService, result);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
            }

            return RconStatic.EndConnection(result);
        }

        public List<ServerBans> GetServerBansFromBlackList(PavlovServer server, List<ServerBans> banlist)
        {
            var answer = "";

            try
            {
                answer = RconStatic.GetFile(server, server.ServerFolderPath + FilePaths.BanList,_notifyService);
            }
            catch (Exception e)
            {
                throw new CommandException(e.Message);
            }

            var lines = answer.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            );
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var split = line.Split("#");
                var tmp = new ServerBans();
                if (split.Length == 1)
                {
                    var dataBaseObject = banlist.FirstOrDefault(x => x.SteamId == split[0]);
                    if (dataBaseObject != null) continue;

                    tmp.SteamId = split[0];
                }

                if (split.Length == 2) tmp.Comment = split[1];

                banlist.Add(tmp);
            }

            return banlist;
        }
    }
}