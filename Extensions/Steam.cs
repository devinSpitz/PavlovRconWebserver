using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public static class Steam
    { 
        
        public static async Task DeleteAllUnsedMapsFromAllServers(string connectionString)
        {
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
            var rconSerivce = new RconService(serverSelectedMapService,sshServerSerivce);
            var servers = await sshServerSerivce.FindAll();
            foreach (var server in servers)
            {
                
                foreach (var signleServer in server.PavlovServers)
                {
                    try
                    {
                        await rconSerivce.SendCommand(signleServer, "" ,true);
                    }
                    catch (Exception e)
                    {
                        // ingore for now
                    } 
                }
            }
        }
        
        public static async Task<bool> CrawlSteamMaps(string connectionString)
        {
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            
            HttpClient client = new HttpClient();
            var response = await client.GetAsync("https://steamcommunity.com/workshop/browse/?appid=555160&browsesort=trend&section=readytouseitems&actualsort=trend&p=1&numperpage=30");
            var pageContents = await response.Content.ReadAsStringAsync();

            HtmlDocument pageDocument = new HtmlDocument();
            pageDocument.LoadHtml(pageContents);

            List<HtmlDocument> pages = new List<HtmlDocument>();
            // get highest site number
            var pageDiv = pageDocument.DocumentNode.SelectSingleNode("//div[@class='workshopBrowsePagingControls']").OuterHtml;
            Regex regex = new Regex(@"(?<=>)([0-9]*)(?=</a)");
            var matches = regex.Matches(pageDiv);
            if(matches.Count<1) throw new Exception("There where no maps found on steam? some bigger problem maybe");
            var highest = matches[^1];
            
            var seq = Enumerable.Range(1, int.Parse(highest.Value)).ToArray();
            
            var pageTasks = Enumerable.Range(0, seq.Count())
                .Select(getPage);
            pages = (await Task.WhenAll(pageTasks)).ToList();
            
            
            var MapsTasks = pages.Select(GetMapsFromPage);
            var pagesMaps = (await Task.WhenAll(MapsTasks)).ToList(); // This uses like 1 GB RAM what i think everybody should have :( But i try to parse 52 sites which each have 30 maps on it parallel so this is obvious
            var maps = pagesMaps.SelectMany(x => x).ToList();
            var rconMapsViewModels = maps.Prepend(new Map()
            {
                Id = "datacenter",
                Name = "datacenter",
                ImageUrl = "http://wiki.pavlov-vr.com/images/thumb/c/c0/Datacenter_middle.jpg/600px-Datacenter_middle.jpg",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map()
            {
                Id = "sand",
                Name = "sand",
                ImageUrl = "http://wiki.pavlov-vr.com/images/thumb/d/d9/Sand_B_site.jpg/600px-Sand_B_site.jpg",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map()
            {
                Id = "bridge",
                Name = "bridge",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map()
            {
                Id = "containeryard",
                Name = "containeryard",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map()
            {
                Id = "prisonbreak",
                Name = "prisonbreak",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = rconMapsViewModels.Prepend(new Map()
            {
                Id = "hospital",
                Name = "hospital",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList();
            rconMapsViewModels = maps.Prepend(new Map()
            {
                Id = "stalingrad",
                Name = "stalingrad",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList(); 
            rconMapsViewModels = maps.Prepend(new Map()
            {
                Id = "santorini",
                Name = "santorini",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList(); 
            rconMapsViewModels = maps.Prepend(new Map()
            {
                Id = "station",
                Name = "station",
                ImageUrl = "",
                Author = "Vankrupt Games"
            }).ToList(); 
            rconMapsViewModels = maps.Prepend(new Map()
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
                if (mapsTmp==null)
                {
                    tmpRconMaps.Add(map);
                }
            }

            foreach (var tmpMap in tmpRconMaps)
            {
               await mapsService.Upsert(tmpMap);
            }
            //Delete Maps in the database which are not in steam anymore!
            foreach (var map in await mapsService.FindAll())
            {
                var tmp = tmpRconMaps.FirstOrDefault(x => x.Id == map.Id);
                var isNumeric = int.TryParse( map.Id, out _);
                if (tmp == null&&isNumeric)
                {
                    await mapsService.Delete(map.Id);
                    // i should my here delete them from the serverSelectedMaps as well
                }
            }
            return true;
        }
        
        
        private static async Task<List<Map>> GetMapsFromPage(HtmlDocument page)
        {
            var notes = page.DocumentNode.SelectNodes("//div[@class='workshopItem']");
            
            var mapsTasks = notes.Select(getMapFromNote);
            var maps = (await Task.WhenAll(mapsTasks)).ToList();


            return maps;
        }

        private static async Task<Map> getMapFromNote(HtmlNode note)
        {
            var map = new Map();
            map.Id = new Regex(@"(?<=id=)([0-9]*)(?=&searchtext=)").Match(note.OuterHtml).Value;

            map.ImageUrl = "https://steamuserimages" +
                           (new Regex(@"(?<=https://steamuserimages)(.*)(?=Letterbox)").Match(note.OuterHtml).Value) +
                           "Letterbox&imcolor=%23000000&letterbox=true";

            if (map.ImageUrl=="https://steamuserimages"+"Letterbox&imcolor=%23000000&letterbox=true")
            {
                map.ImageUrl = "https://community" +
                    (new Regex(@"(?<=https://community)(.*)(?=steam_workshop_default_image.png)").Match(note.OuterHtml).Value) +
                    "steam_workshop_default_image.png";
                
            }
            var correctOuter = note.OuterHtml.Replace("\"","'");
            map.Name = new Regex(@"(?<=<div class='workshopItemTitle ellipsis'>)(.*)(?=</div></a>)").Match(correctOuter).Value;
            map.Author  = new Regex(@"(?<=/?appid=555160'>)(.*)(?=</a></div>)").Match(correctOuter).Value;
            return map;
        }
        
        private static async Task<HtmlDocument> getPage(int index)
        {
            HttpClient client = new HttpClient();
            var singlePage = new HtmlDocument();
            var singleResponse = await client.GetAsync("https://steamcommunity.com/workshop/browse/?appid=555160&browsesort=trend&section=readytouseitems&actualsort=trend&p="+index+"&numperpage=30");
            var singlePageContents = await singleResponse.Content.ReadAsStringAsync();
            singlePage.LoadHtml(singlePageContents);
            return singlePage;
        }
        
    }
}