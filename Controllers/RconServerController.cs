using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize]
    
    [Route("[controller]/[action]")]
    public class RconServerController : Controller
    {
        private readonly RconServerSerivce _service;
        private readonly UserService _userservice;
        public RconServerController(RconServerSerivce service,UserService userService)
        {
            _service = service;
            _userservice = userService;
        }
        
        public async Task<IActionResult> Index()
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Index",_service.FindAll());
        }
        public async Task<IActionResult> EditServer(int? serverId)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            var server = new RconServer();
            if (serverId != null && serverId != 0)
            {
                server = _service.FindOne((int)serverId);
            }
            
            return View("Server",server);
        }

        [HttpPost]
        public async Task<IActionResult> SaveServer(RconServer server)
        {
            if(!ModelState.IsValid) 
                return View("Server",server);
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            try
            {

                if (server.Id == 0)
                {
                    _service.Insert(server);
                }
                else
                {
                    _service.Update(server);
                }
            }
            catch (SaveServerException e)
            {
                ModelState.AddModelError(e.FieldName, e.Message);
            }
            return View("Server",server);
        }

        public async Task<IActionResult> DeleteServer([FromQuery]int id)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            _service.Delete(id);
            return await Index();
        }
        
    }
}