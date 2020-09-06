using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public static class RightsHandler
    {
        public static readonly Dictionary<int, string> Roles = new Dictionary<int, string>()
        {
            {4,"Admin"},
            {3,"Mod"},
            {2,"Captain"},
            {1,"User"}
        };

        private static Dictionary<string,string> _commandsRights = null;

        private static Dictionary<string, string> GetCommandRights()
        {
            if (_commandsRights == null)
            {
                _commandsRights = new Dictionary<string, string>();
                var tmpCommandModel = new RconViewModel();
                tmpCommandModel.PlayerCommands.ForEach(x=>_commandsRights.Add(x.Name,x.MinRole));
                tmpCommandModel.TwoValueCommands.ForEach(x=>_commandsRights.Add(x.Name,x.MinRole));
                tmpCommandModel.SpecialCommands.ForEach(x=>_commandsRights.Add(x.Name,x.MinRole));
                return _commandsRights;
            }
            
            return _commandsRights;
                
        }
        
        public static async Task<bool> IsUserAtLeastInRoleForCommand(string command,ClaimsPrincipal cp,UserService userService)
        {
            var commandOnly = "";
            if (command.Contains(" "))
            {
                commandOnly = command.Split(" ")[0];
            }
            else
            {
                commandOnly = command;
            }
            var commandRights = GetCommandRights();
            var tmpCommand = commandRights.FirstOrDefault(x => x.Key == commandOnly);
            return  await IsUserAtLeastInRole(tmpCommand.Value, cp,userService);
        }    
            
        public static async Task<bool> IsUserAtLeastInRole(string role,ClaimsPrincipal cp,UserService userService)
        {
            var result = false;
            
            foreach (var checkRole in Roles)
            {
               
                if(result) return result;
                if(await userService.IsUserInRole(checkRole.Value, cp)) result = true;
                if(checkRole.Value==role) return result;
            }
            return result;
        }       
        public static async Task<bool> IsUserAtLeastInTeamRole(string role,string TeamRole)
        {
            var result = false;
            
            foreach (var checkRole in Roles)
            {
               
                if(result) return result;
                if(checkRole.Value==TeamRole) result = true;
                if(checkRole.Value==role) return result;
            }
            return result;
        }   
        public static async Task<List<string>> GetAllowCommands(RconViewModel viewModel,ClaimsPrincipal cp,UserService userService)
        {
            List<string> allowCommands = new List<string>();
            foreach (var command in viewModel.PlayerCommands)
            {
                if(await RightsHandler.IsUserAtLeastInRoleForCommand(command.Name, cp, userService))
                    allowCommands.Add(command.Name);
            }
            foreach (var command in viewModel.SpecialCommands)
            {
                if(await RightsHandler.IsUserAtLeastInRoleForCommand(command.Name, cp, userService))
                    allowCommands.Add(command.Name);
            }            
            foreach (var command in viewModel.TwoValueCommands)
            {
                if(await RightsHandler.IsUserAtLeastInRoleForCommand(command.Name, cp, userService))
                    allowCommands.Add(command.Name);
            }

            return allowCommands;
        }
    }
}