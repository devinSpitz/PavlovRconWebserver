using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public static class RightsHandler
    {
        public static readonly Dictionary<int, string> Roles = new()
        {
            {4, "Admin"},
            {3, "Mod"},
            {2, "Captain"},
            {1, "User"}
        };

        private static Dictionary<string, string> _commandsRights;


        public static async Task<bool> HasRightsToThisSshServer(ClaimsPrincipal principal, LiteDbUser user,
            int sshServerId, SshServerSerivce sshServerSerivce)
        {
            if (sshServerId == 0)
            {
                if (principal.IsInRole("Admin"))
                    return true;
                if (principal.IsInRole("OnPremise"))
                    return true;
                return false;
                //Only on permise and Admin   
            }

            var query = await sshServerSerivce.FindAll();
            var list = new List<SshServer>();
            var rental = principal.IsInRole("ServerRent");
            if (principal.IsInRole("Admin")) return true;

            if (principal.IsInRole("OnPremise"))
            {
                list.AddRange(query.Where(x => x.Owner != null && x.Owner.Id == user.Id).ToList());
                if (list.Any(x => x.Id == sshServerId))
                    return true;
            }
            else if (rental)
            {
                return false;
            }

            return false;
        }


        public static async Task<bool> HasRightsToThisPavlovServer(ClaimsPrincipal principal, LiteDbUser user,
            int pavlovServerId, SshServerSerivce sshServerSerivce, PavlovServerService pavlovServerService)
        {
            if (pavlovServerId == 0)
                return principal.IsInRole("Admin") || principal.IsInRole("OnPremise");

            //Only on permise and Admin   

            var query = await sshServerSerivce.FindAll();
            var list = new List<SshServer>();
            var rental = principal.IsInRole("ServerRent");
            if (principal.IsInRole("Admin")) return true;

            if (principal.IsInRole("OnPremise"))
                list.AddRange(query.Where(x => x.Owner != null && x.Owner.Id == user.Id).ToList());
            else if (rental) list.AddRange(query.Where(x => true).ToList());
            foreach (var single in list)
                single.PavlovServers = (await pavlovServerService.FindAllFrom(single.Id)).ToList();

            if (principal.IsInRole("OnPremise"))
                return list.Any(x =>
                    x.Owner.Id == user.Id && x.PavlovServers.FirstOrDefault(y => y.Id == pavlovServerId) != null);
            return rental && list.Any(x =>
                x.PavlovServers.FirstOrDefault(y => y.Owner !=null && y.Owner.Id == user.Id && y.Id == pavlovServerId) != null);
        }

        private static Dictionary<string, string> GetCommandRights()
        {
            if (_commandsRights == null)
            {
                _commandsRights = new Dictionary<string, string>();
                var tmpCommandModel = new RconViewModel();
                tmpCommandModel.PlayerCommands.ForEach(x => _commandsRights.Add(x.Name, x.MinRole));
                tmpCommandModel.TwoValueCommands.ForEach(x => _commandsRights.Add(x.Name, x.MinRole));
                tmpCommandModel.SpecialCommands.ForEach(x => _commandsRights.Add(x.Name, x.MinRole));
                return _commandsRights;
            }

            return _commandsRights;
        }
        

        public static async Task<bool> IsUserAtLeastInRoleForCommand(string command, ClaimsPrincipal cp,
            UserService userService,bool isModSomeWhere)
        {
            var commandOnly = "";
            if (command.Contains(" "))
                commandOnly = command.Split(" ")[0];
            else
                commandOnly = command;
            var commandRights = GetCommandRights();
            var tmpCommand = commandRights.FirstOrDefault(x => x.Key == commandOnly);
            var isInRole = await IsUserAtLeastInRole(tmpCommand.Value, cp, userService);
            var modStateIsEnough = false;
            if (isModSomeWhere) modStateIsEnough = IsUserAtLeastInRoleEasy("Mod", tmpCommand.Value);


            return isInRole || modStateIsEnough;
        }


        public static async Task<bool> IsUserAtLeastInRole(string role, ClaimsPrincipal cp, UserService userService)
        {
            var result = false;

            foreach (var checkRole in Roles)
            {
                if (result) return result;
                if (await userService.IsUserInRole(checkRole.Value, cp)) result = true;
                if (checkRole.Value == role) return result;
            }

            return result;
        }

        public static bool IsUserAtLeastInRoleEasy(string actualRoleNow, string roleNeeded)
        {
            var result = false;

            foreach (var checkRole in Roles)
            {
                if (result) return result;
                if (checkRole.Value == roleNeeded) result = true;
                if (checkRole.Value == actualRoleNow) return result;
            }

            return result;
        }

        public static async Task<bool> IsModOnTheServer(ServerSelectedModsService serverSelectedModsService,
            PavlovServer server, ObjectId id)
        {
            var mods = (await serverSelectedModsService.FindAllFrom(server)).ToArray();
            if (mods.Length <= 0) return false;
            return mods.FirstOrDefault(x => x.LiteDbUser?.Id == id) != null;
        }
        
        public static async Task<List<string>> GetAllowCommands(RconViewModel viewModel, ClaimsPrincipal cp,
            UserService userService, bool isMod)
        {
            var allowCommands = new List<string>();
            foreach (var command in viewModel.PlayerCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
                else if(cp.IsInRole("OnPremise")||cp.IsInRole("ServerRent"))
                    allowCommands.Add(command.Name);
            foreach (var command in viewModel.SpecialCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
                else if(cp.IsInRole("OnPremise")||cp.IsInRole("ServerRent"))
                    allowCommands.Add(command.Name);
            foreach (var command in viewModel.TwoValueCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
                else if(cp.IsInRole("OnPremise")||cp.IsInRole("ServerRent"))
                    allowCommands.Add(command.Name);

            return allowCommands;
        }
        
                
        public static async Task<List<string>> GetAllowCommands(RconViewModel viewModel, ClaimsPrincipal cp,
            UserService userService, bool isMod,PavlovServer server,LiteDbUser user)
        {
            var allowCommands = new List<string>();
            foreach (var command in viewModel.PlayerCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
                else if(server.Owner!=null&&server.Owner.Id==user.Id||server.SshServer.Owner!=null&&server.SshServer.Owner.Id==user.Id)
                    allowCommands.Add(command.Name);
            foreach (var command in viewModel.SpecialCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
                else if(server.Owner!=null&&server.Owner.Id==user.Id||server.SshServer.Owner!=null&&server.SshServer.Owner.Id==user.Id)
                    allowCommands.Add(command.Name);
            foreach (var command in viewModel.TwoValueCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
                else if(server.Owner!=null&&server.Owner.Id==user.Id||server.SshServer.Owner!=null&&server.SshServer.Owner.Id==user.Id)
                    allowCommands.Add(command.Name);

            return allowCommands;
        }
    }
}