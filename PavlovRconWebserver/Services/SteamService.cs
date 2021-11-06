using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using HtmlAgilityPack;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using Serilog.Events;

namespace PavlovRconWebserver.Services
{
    public class SteamService
    {
        private readonly MapsService _mapsService;
        private readonly IToastifyService _notifyService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly SteamIdentityStatsServerService _steamIdentityStatsServerService;

        public SteamService(SshServerSerivce sshServerSerivce,
            MapsService mapsService,
            ServerSelectedMapService serverSelectedMapService,
            IToastifyService notyfService,
            SteamIdentityStatsServerService steamIdentityStatsServerService)
        {
            _notifyService = notyfService;
            _mapsService = mapsService;
            _sshServerSerivce = sshServerSerivce;
            _serverSelectedMapService = serverSelectedMapService;
            _steamIdentityStatsServerService = steamIdentityStatsServerService;
        }

        public async Task DeleteAllUnsedMapsFromAllServers()
        {
            var servers = await _sshServerSerivce.FindAll();
            foreach (var server in servers)
            foreach (var signleServer in server.PavlovServers)
                try
                {
                    RconStatic.DeleteUnusedMaps(signleServer,
                        (await _serverSelectedMapService.FindAllFrom(signleServer)).ToList());
                }
                catch (Exception e)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose, _notifyService);
                    // ingore for now
                }
        }

        public async Task<bool> CrawlSteamMaps()
        {
            var client = new HttpClient();
            var response =
                await client.GetAsync(
                    "https://steamcommunity.com/workshop/browse/?appid=555160&browsesort=trend&section=readytouseitems&actualsort=trend&p=1&numperpage=30");
            var pageContents = await response.Content.ReadAsStringAsync();

            var pageDocument = new HtmlDocument();
            pageDocument.LoadHtml(pageContents);

            var pages = new List<HtmlDocument>();
            // get highest site number
            var pageDiv = pageDocument.DocumentNode.SelectSingleNode("//div[@class='workshopBrowsePagingControls']")
                .OuterHtml;
            var regex = new Regex(@"(?<=>)([0-9]*)(?=</a)");
            var matches = regex.Matches(pageDiv);
            if (matches.Count < 1) throw new Exception("There where no maps found on steam? some bigger problem maybe");
            var highest = matches[^1];

            var seq = Enumerable.Range(1, int.Parse(highest.Value)).ToArray();

            var pageTasks = Enumerable.Range(0, seq.Count())
                .Select(getPage);
            pages = (await Task.WhenAll(pageTasks)).ToList();


            var mapsTasks = pages.Select(GetMapsFromPage);
            var pagesMaps =
                (await Task.WhenAll(mapsTasks))
                .ToList(); // This uses like 1 GB RAM what i think everybody should have :( But i try to parse 52 sites which each have 30 maps on it parallel so this is obvious
            var maps = pagesMaps.SelectMany(x => x).ToList();
            var rconMapsViewModels = maps.Prepend(new Map
            {
                Id = "datacenter",
                Name = "datacenter",
                ImageUrl =
                    "http://wiki.pavlov-vr.com/images/thumb/c/c0/Datacenter_middle.jpg/600px-Datacenter_middle.jpg",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "sand",
                Name = "sand",
                ImageUrl = "http://wiki.pavlov-vr.com/images/thumb/d/d9/Sand_B_site.jpg/600px-Sand_B_site.jpg",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "santorini_night",
                Name = "santorini_night",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "sand_night",
                Name = "sand_night",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "stalingrad_night",
                Name = "stalingrad_night",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "bridge",
                Name = "bridge",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "bunker",
                Name = "bunker",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "containeryard",
                Name = "containeryard",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "hospital",
                Name = "hospital",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "stalingrad",
                Name = "stalingrad",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "santorini",
                Name = "santorini",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "station",
                Name = "station",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map
            {
                Id = "industry",
                Name = "industry",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            //g

            var tmpRconMaps = new List<Map>();
            foreach (var map in rconMapsViewModels) // got double entry from maps dont know from where have to check
            {
                var mapsTmp = tmpRconMaps.FirstOrDefault(x => x.Id == map.Id);
                if (mapsTmp == null) tmpRconMaps.Add(map);
            }

            foreach (var tmpMap in tmpRconMaps) await _mapsService.Upsert(tmpMap);
            //Delete Maps in the database which are not in steam anymore!
            foreach (var map in await _mapsService.FindAll())
            {
                var tmp = tmpRconMaps.FirstOrDefault(x => x.Id == map.Id);
                var isNumeric = int.TryParse(map.Id, out _);
                if (tmp == null && isNumeric)
                    await _mapsService.Delete(map.Id);
                // i should my here delete them from the serverSelectedMaps as well
            }

            return true;
        }

        
        public async Task<bool> CrawlSteamProfile()
        {
            
            
            var client = new HttpClient();
            var allSteamIdentityStats = await _steamIdentityStatsServerService.FindAll();

            var steamIdentities = allSteamIdentityStats.GroupBy(x => x.SteamId).Select(x => x.Key);
            foreach (var single in steamIdentities)
            {
                if(string.IsNullOrEmpty(single)) continue;   
                var response =
                    await client.GetAsync(
                        "https://steamcommunity.com/profiles/"+single);
                var pageContents = await response.Content.ReadAsStringAsync();

                var pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(pageContents);

                // get highest site number
                var pageDiv = pageDocument.DocumentNode.SelectSingleNode("//div[@class='playerAvatarAutoSizeInner']")
                    .OuterHtml;
                var regex = new Regex("src=\"(.*)\">");
                if(string.IsNullOrEmpty(pageDiv)) 
                    continue;
                var matches = regex.Matches(pageDiv);
                if (matches.Count < 1) 
                    throw new Exception("There where no steamprofile picture found on steam? some bigger problem maybe");
                var singleElement = allSteamIdentityStats.Where(x => !string.IsNullOrEmpty(x.SteamId) && x.SteamId == single).ToArray();
                if (!singleElement.Any()) 
                    continue;
                foreach (var singleOne in singleElement)
                {
                    singleOne.SteamPicture = matches.Last().Value.Replace("src=\"","").Replace("\">","");
                    await _steamIdentityStatsServerService.Update(singleOne);
                }

            }

            return true;
        }


        private async Task<List<Map>> GetMapsFromPage(HtmlDocument page)
        {
            var notes = page.DocumentNode.SelectNodes("//div[@class='workshopItem']");

            var mapsTasks = notes.Select(getMapFromNote);
            var maps = (await Task.WhenAll(mapsTasks)).ToList();


            return maps;
        }

        private async Task<Map> getMapFromNote(HtmlNode note)
        {
            var map = new Map();
            map.Id = new Regex(@"(?<=id=)([0-9]*)(?=&searchtext=)").Match(note.OuterHtml).Value;

            map.ImageUrl = "https://steamuserimages" +
                           new Regex(@"(?<=https://steamuserimages)(.*)(?=Letterbox)").Match(note.OuterHtml).Value +
                           "Letterbox&imcolor=%23000000&letterbox=true";

            if (map.ImageUrl == "https://steamuserimages" + "Letterbox&imcolor=%23000000&letterbox=true")
                map.ImageUrl = "https://community" +
                               new Regex(@"(?<=https://community)(.*)(?=steam_workshop_default_image.png)")
                                   .Match(note.OuterHtml).Value +
                               "steam_workshop_default_image.png";
            var correctOuter = note.OuterHtml.Replace("\"", "'");
            map.Name = new Regex(@"(?<=<div class='workshopItemTitle ellipsis'>)(.*)(?=</div></a>)").Match(correctOuter)
                .Value;
            map.Author = new Regex(@"(?<=/?appid=555160'>)(.*)(?=</a></div>)").Match(correctOuter).Value;
            return map;
        }

        private async Task<HtmlDocument> getPage(int index)
        {
            var client = new HttpClient();
            var singlePage = new HtmlDocument();
            var singleResponse =
                await client.GetAsync(
                    "https://steamcommunity.com/workshop/browse/?appid=555160&browsesort=trend&section=readytouseitems&actualsort=trend&p=" +
                    index + "&numperpage=30");
            var singlePageContents = await singleResponse.Content.ReadAsStringAsync();
            singlePage.LoadHtml(singlePageContents);
            return singlePage;
        }
    }
}