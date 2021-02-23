using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerInfoService
    {
        
        private ILiteDbIdentityContext _liteDb;
        private readonly PavlovServerService _pavlovServerService;
        private readonly RconService _rconService;
        private readonly MapsService _mapsService;
        
        
        public PavlovServerInfoService(ILiteDbIdentityContext liteDbContext,PavlovServerService pavlovServerService,RconService rconService,MapsService mapsService)
        {
            _rconService = rconService;
            _pavlovServerService = pavlovServerService;
            _mapsService = mapsService;
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerInfo> FindServer(int serverId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .Find(x=>x.ServerId==serverId).FirstOrDefault();
        }

        public async Task Upsert(PavlovServerInfo pavlovServerInfo)
        {
            _liteDb.LiteDatabase.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .DeleteMany(x=>x.ServerId == pavlovServerInfo.ServerId);
            
            _liteDb.LiteDatabase.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .Insert(pavlovServerInfo);
        }

        
        public async Task<bool> SaveRealServerInfoFromServer(int serverId)
        {
            var server = await _pavlovServerService.FindOne(serverId);
            var serverInfo = "";
            try
            {
                serverInfo = await _rconService.SendCommand(server, "ServerInfo");
            }
            catch (CommandException e)
            {
                throw  new PavlovServerPlayerException(e.Message);
            }
            
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(serverInfo);
            var map = await _mapsService.FindOne(tmp.ServerInfo.MapLabel.Replace("UGC",""));
            if(map!=null)
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
                ServerId = serverId
            };
            
            await Upsert(tmpinfo);
            return true;
        }
        
        
    }
}