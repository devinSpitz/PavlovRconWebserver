using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public static class RightsHandler
    {
        public static readonly Dictionary<int, string> Roles = new Dictionary<int, string>
        {
            {4, "Admin"},
            {3, "Mod"},
            {2, "Captain"},
            {1, "User"}
        };

        private static Dictionary<string, string> _commandsRights;

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

        public static async Task<bool> IsModOrAdminOnServer(UserManager<LiteDbUser> userManager,
            ClaimsPrincipal userClaim, PavlovServerService pavlovServerService,
            ServerSelectedModsService serverSelectedModsService, int pavlovServerId)
        {
            var user = await userManager.GetUserAsync(userClaim);
            var server = await pavlovServerService.FindOne(pavlovServerId);
            return await IsModOnTheServer(serverSelectedModsService, server, user.Id) ||
                   await userManager.IsInRoleAsync(user, "Mod") || await userManager.IsInRoleAsync(user, "Admin");
        }

        public static async Task<bool> IsUserAtLeastInRoleForCommand(string command, ClaimsPrincipal cp,
            UserService userService, bool isMod)
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
            if (isMod) modStateIsEnough = await IsUserAtLeastInTeamRole("Mod", tmpCommand.Value);


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

        public static async Task<bool> IsUserAtLeastInTeamRole(string role, string TeamRole)
        {
            var result = false;

            foreach (var checkRole in Roles)
            {
                if (result) return result;
                if (checkRole.Value == TeamRole) result = true;
                if (checkRole.Value == role) return result;
            }

            return result;
        }

        public static async Task<bool> IsModOnTheServer(ServerSelectedModsService serverSelectedModsService,
            PavlovServer server, ObjectId id)
        {
            var mods = (await serverSelectedModsService.FindAllFrom(server)).ToList();
            if (mods.Count <= 0) return false;
            return mods.FirstOrDefault(x => x.LiteDbUser?.Id == id) != null;
        }


        public static async Task<List<string>> GetAllowCommands(RconViewModel viewModel, ClaimsPrincipal cp,
            UserService userService, bool isMod)
        {
            var allowCommands = new List<string>();
            foreach (var command in viewModel.PlayerCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
            foreach (var command in viewModel.SpecialCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);
            foreach (var command in viewModel.TwoValueCommands)
                if (await IsUserAtLeastInRoleForCommand(command.Name, cp, userService, isMod))
                    allowCommands.Add(command.Name);

            return allowCommands;
        }
    }
}