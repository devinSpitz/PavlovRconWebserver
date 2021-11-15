using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PrimS.Telnet;
using Renci.SshNet;
using Serilog.Events;

namespace PavlovRconWebserver.Services
{
    public class RconService
    {
        public enum AuthType
        {
            PrivateKey,
            UserPass,
            PrivateKeyPassphrase
        }

        private readonly MapsService _mapsService;
        private readonly IToastifyService _notifyService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly ServerBansService _serverBansService;

        private readonly SshServerSerivce _sshServerSerivce;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly SteamIdentityStatsServerService _steamIdentityStatsServerService;
        

        public RconService(SteamIdentityService steamIdentityService,
            MapsService mapsService, PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            SshServerSerivce sshServerSerivce,
            PavlovServerService pavlovServerService,
            PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService,
            ServerBansService serverBansService,
            ServerSelectedMapService serverSelectedMapService,
            SteamIdentityStatsServerService steamIdentityStatsServerService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _mapsService = mapsService;
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerService = pavlovServerService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerPlayerHistoryService = pavlovServerPlayerHistoryService;
            _steamIdentityService = steamIdentityService;
            _sshServerSerivce = sshServerSerivce;
            _serverBansService = serverBansService;
            _serverSelectedMapService = serverSelectedMapService;
            _steamIdentityStatsServerService = steamIdentityStatsServerService;
        }


        public async Task ReloadPlayerListFromServerAndTheServerInfo()
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
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose,
                            _notifyService);
                    }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose, _notifyService);
            }
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
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose, _notifyService);
                }
        }

        public bool SaveBlackListEntry(PavlovServer server, List<ServerBans> NewBlackListContent)
        {
            var blacklistArray = NewBlackListContent.Select(x => x.SteamId).ToArray();
            RconStatic.WriteFile(server, server.ServerFolderPath + FilePaths.BanList, blacklistArray, _notifyService);
            return true;
        }

        /// <summary>
        /// return back if the match should forceStop
        /// </summary>
        /// <param name="server"></param>
        /// <param name="match"></param>
        /// <param name="matchId"></param>
        /// <returns></returns>
        public async Task<string> SShTunnelGetAllInfoFromPavlovServer(PavlovServer server,bool match = false)
        {
            var result = RconStatic.StartClient(server, out var client);
            var costumesToSet = new Dictionary<string, string>();
            try
            {
                client.Connect();

                if (client.IsConnected)
                {
                    var nextFreePort = RconStatic.GetAvailablePort();
                    var portToForward = nextFreePort;
                    var portForwarded = new ForwardedPortLocal("127.0.0.1", (uint) portToForward, "127.0.0.1",
                        (uint) server.TelnetPort);
                    client.AddForwardedPort(portForwarded);
                    portForwarded.Start();
                    using (var client2 = new Client("127.0.0.1", portToForward, new CancellationToken()))
                    {
                        if (client2.IsConnected)
                        {
                            var password = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Answer: " + password,
                                LogEventLevel.Verbose, _notifyService);
                            if (password.ToLower().Contains("password"))
                            {
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("start sending password!",
                                    LogEventLevel.Verbose, _notifyService);
                                await client2.WriteLine(server.TelnetPassword);
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("did send password and wait for auth",
                                    LogEventLevel.Verbose, _notifyService);

                                var auth = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));

                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("waited for auth get : " + auth,
                                    LogEventLevel.Verbose, _notifyService);
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
                                    var oldPavlovServerPlayerList =
                                        await _pavlovServerPlayerService.FindAllFromServer(server.Id);
                                    var oldServerInfo = await _pavlovServerInfoService.FindServer(server.Id);
                                    //Todo maybe check if we lost a player its possible that we need that if allstats after the end of a match doesent give back all the player if some had a disconnect
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
                                    if (!string.IsNullOrEmpty(tmp.ServerInfo.GameMode))
                                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                            "Got the server info for the server: " + server.Name + "\n " +
                                            singleCommandResultTwo, LogEventLevel.Verbose, _notifyService);
                                   
                                    var map = await _mapsService.FindOne(tmp.ServerInfo.MapLabel.Replace("UGC", ""));
                                    if (map != null)
                                        tmp.ServerInfo.MapPictureLink = map.ImageUrl;
                                    
                                    //Check of next round
                                    bool nextRound = oldServerInfo != null && oldServerInfo.MapLabel != tmp.ServerInfo.MapLabel;
                                    var round = oldServerInfo?.Round ?? 0;
                                    if (nextRound)
                                    {
                                        if (round == 999)
                                        {
                                            round = 0; // reset value that's why you have to use the nextRound bool down from here to know if its the next round
                                        }
                                        
                                        round++;
                                    }

                                    if (match && nextRound)
                                    {
                                        result.Success = true;
                                        return "ForceStopNowUrgent"; // very bad practice i have to change that to a nice thing
                                    }
                                    else
                                    {
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
                                            ServerId = server.Id,
                                            Round = round
                                        };

                                        await _pavlovServerInfoService.Upsert(tmpinfo);
                                    
                                        // if stuck on datacenter while its not in selected maps rotate to the next map and hope the server works forward as expected
                                        if (tmp.ServerInfo.MapLabel == "datacenter")
                                        {
                                            var maps = await _serverSelectedMapService.FindAllFrom(server);
                                            if (maps.FirstOrDefault(x => x.Map.Name == "datacenter") == null)
                                            {
                                                await RconStatic.SingleCommandResult(client2, "RotateMap");
                                            }
                                        }

                                        if (!match)
                                        {
                                            //Todo read logs to add killfeed
                                            if (server.SaveStats)
                                            {
                                                var allStats =
                                                    await _steamIdentityStatsServerService.FindAllFromServer(server.Id);
                                                foreach (var player in pavlovServerPlayerList)
                                                {
                                                    var tmpStats =
                                                        allStats.FirstOrDefault(x => x.SteamId == player.UniqueId);
                                                    if (tmpStats != null)
                                                    {
                                                        // use case 1: User disconnect in round 99 and reconects in round 100
                                                        // use case 2: User disconnect in round 99 and reconnect in round 99
                                                        if (!nextRound && tmpStats.ForRound == round &&
                                                            !((DateTime.Now - tmpStats.logDateTime).Minutes > 2 ||
                                                              (tmpStats.Assists == 0 && tmpStats.Deaths == 0 &&
                                                               tmpStats.Kills ==
                                                               0)) || // use case 2 if log is longer away than or all stats suddenly 0 / no score cause of single mods that does not support score
                                                            !(nextRound || tmpStats.ForRound != round)) // use case 1
                                                        {
                                                            tmpStats.Exp -= tmpStats.LastAddedScore;
                                                            tmpStats.Assists -= tmpStats.LastAddedAssists;
                                                            tmpStats.Deaths -= tmpStats.LastAddedDeaths;
                                                            tmpStats.Kills -= tmpStats.LastAddedKills;
                                                        }


                                                        tmpStats.Exp += player.Score;
                                                        tmpStats.Kills += int.Parse(player.getKills());
                                                        tmpStats.Deaths += int.Parse(player.getDeaths());
                                                        tmpStats.Assists += int.Parse(player.getAssists());
                                                        tmpStats.UpTime +=
                                                            new TimeSpan(0, 0, 1,
                                                                0); // will get checked every !!Attention!! needs to be the same than the cron in startup
                                                        tmpStats.LastAddedScore = player.Score;
                                                        tmpStats.LastAddedAssists = int.Parse(player.getAssists());
                                                        tmpStats.LastAddedDeaths = int.Parse(player.getDeaths());
                                                        tmpStats.LastAddedKills = int.Parse(player.getKills());
                                                        tmpStats.ForRound = round;
                                                        tmpStats.logDateTime = DateTime.Now;

                                                        await _steamIdentityStatsServerService.Update(tmpStats);
                                                    }
                                                    else
                                                    {
                                                        await _steamIdentityStatsServerService.Insert(
                                                            new SteamIdentityStatsServer
                                                            {
                                                                SteamId = player.UniqueId,
                                                                SteamName = player.Username,
                                                                SteamPicture = "",
                                                                Kills = player.Kills,
                                                                LastAddedKills = player.Kills,
                                                                Deaths = player.Deaths,
                                                                LastAddedDeaths = player.Deaths,
                                                                Assists = player.Assists,
                                                                LastAddedAssists = player.Assists,
                                                                Exp = player.Score,
                                                                LastAddedScore = player.Score,
                                                                ServerId = server.Id,
                                                                ForRound = round
                                                            });
                                                    }
                                                }
                                            }

                                            // Autobalanced only when teams are there and there is no match
                                            if (server.AutoBalance &&
                                                (server.AutoBalanceLast == null ||
                                                 (server.AutoBalanceLast +
                                                     new TimeSpan(0, 0, server.AutoBalanceCooldown, 0) <= DateTime.Now)) &&
                                                tmp.ServerInfo.Teams == "true" && server.ServerType == ServerType.Community)
                                            {
                                                // balance players if needed
                                                var balanced = await switchLogic(client2, pavlovServerPlayerList);
                                                if (balanced)
                                                {
                                                    server.AutoBalanceLast = DateTime.Now;
                                                    await _pavlovServerService.Upsert(server, false);
                                                }
                                            }

                                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                                "Set skins for " + costumesToSet.Count + " players of the server:" +
                                                server.Name, LogEventLevel.Verbose, _notifyService);

                                            foreach (var customToSet in costumesToSet)
                                                await RconStatic.SendCommandSShTunnel(server,
                                                    "SetPlayerSkin " + customToSet.Key + " " + customToSet.Value,
                                                    _notifyService);
                                        }

                                        result.Success = true;
                                        
                                    }
                                }
                                else
                                {
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                        "Send password but did not get Authenticated=1 answer: " + auth,
                                        LogEventLevel.Verbose, _notifyService);
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                        "Telnet Client could not authenticate ..." + server.Name, LogEventLevel.Fatal,
                                        _notifyService, result);
                                }
                            }
                            else
                            {
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                    "Telnet Client did not ask for Password ..." + server.Name, LogEventLevel.Fatal,
                                    _notifyService, result);
                            }
                        }
                        else
                        {
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Telnet Client could not connect ..." + server.Name, LogEventLevel.Fatal,
                                _notifyService, result);
                        }

                        client2.Dispose();
                    }

                    client.Disconnect();
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Telnet Client cannot be reached..." + server.Name,
                        LogEventLevel.Fatal, _notifyService, result);
                }
            }
            catch (Exception e)
            {
                RconStatic.ExcpetionHandlingSshSftp(server, _notifyService, e, result, client);
            }
            finally
            {
                client.Disconnect();
            }

            return RconStatic.EndConnection(result);
        }

        private async Task<bool> switchLogic(
            Client client2,List<PavlovServerPlayer> pavlovServerPlayerList )
        {
            PavlovServerPlayer[] teamWithMoreScore;
            PavlovServerPlayer[] teamWithLessScore;
            var team0 = pavlovServerPlayerList.Where(x => x.TeamId == 0).ToArray();
            var team1 = pavlovServerPlayerList.Where(x => x.TeamId == 1).ToArray();
            var team0Score = team0.Sum(x => x.Score);
            var team1Score = team1.Sum(x => x.Score);
            var teamScoreDifference = team0Score - team1Score;
            if (teamScoreDifference >= 0)
            {
                teamWithMoreScore = team0;
                teamWithLessScore = team1;
            }
            else
            {
                teamWithMoreScore = team1;
                teamWithLessScore = team0;
                teamScoreDifference = Math.Abs(teamScoreDifference);
            }
            var balanceCounter = 0;
            var balanced = false;
            var userScoreToSwitch = teamScoreDifference / 2;
            var switchThreshold = Math.Sqrt(userScoreToSwitch); // dynamic limit for score based balancing
            
            //Score based balancing: teamWithMoreScores -> teamWithLessScore 
            foreach (var pavlovServerPlayer in teamWithMoreScore.OrderByDescending(x=>x.Score))
            {
                if (userScoreToSwitch <= switchThreshold)
                {
                    break;
                }
                //user would overcompensate, skip
                if (pavlovServerPlayer.Score > userScoreToSwitch)
                {
                    continue;
                }
                await RconStatic.SingleCommandResult(client2, "SwitchTeam " + pavlovServerPlayer.UniqueId + " " + teamWithLessScore.First().TeamId);
                await RconStatic.SingleCommandResult(client2, "GiveCash  " + pavlovServerPlayer.UniqueId + " 500");
                //Todo: tell the player what happen waiting for: https://pavlovvr.featureupvote.com/suggestions/229367/motd-and-the-possibility-to-give-the-player-a-message-over-rcon
                //remaining score to balance
                userScoreToSwitch -= pavlovServerPlayer.Score;
                balanceCounter ++;
                balanced = true;
            }
            // Player count based balancing: teamWithLessScore -> teamWithMoreScores
            // assumption teamWithLessScore has more players (Also consider previous balancing)
            var userCountToSwitch = ((teamWithLessScore.Length-teamWithMoreScore.Length) / 2) + balanceCounter;
            // users eligible for switching ( same dynamic threshold ) 
            var lowBobs = teamWithLessScore.Where(x => x.Score < switchThreshold).ToArray();

            Random rnd = new Random();
            // pick userCountToSwitch random player from lowBobs
            for (; userCountToSwitch > 0 && lowBobs.Length > 0 ; userCountToSwitch--)
            {
                int index = rnd.Next(lowBobs.Length);
                await RconStatic.SingleCommandResult(client2, "SwitchTeam " + lowBobs[index].UniqueId + " " + teamWithMoreScore.First().TeamId);
                await RconStatic.SingleCommandResult(client2, "GiveCash  " + lowBobs[index].UniqueId + " 500");
                //Todo: tell the player what happen waiting for: https://pavlovvr.featureupvote.com/suggestions/229367/motd-and-the-possibility-to-give-the-player-a-message-over-rcon
                lowBobs = lowBobs.Where(x => x.UniqueId != lowBobs[index].UniqueId).ToArray();
                balanced = true;
            }
            
            return balanced;
        }

        public List<ServerBans> GetServerBansFromBlackList(PavlovServer server, List<ServerBans> banlist)
        {
            var answer = "";

            try
            {
                answer = RconStatic.GetFile(server, server.ServerFolderPath + FilePaths.BanList, _notifyService);
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