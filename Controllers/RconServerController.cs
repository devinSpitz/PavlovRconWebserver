using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            if(await _userservice.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Index",_service.FindAll());
        }

        public async Task<IActionResult> AddServer()
        {
            if(await _userservice.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("AddServer");
        }

        public async Task<IActionResult> UpdateServer(RconServer server)
        {
            if(await _userservice.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            _service.Update(server);
            return View("Update",server);
        }
        
        [HttpPost]
        public async Task<IActionResult> SaveNewServer(RconServer server)
        {
            if(await _userservice.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            _service.Insert(server);
            return await Index();
        }
        
        public async Task<IActionResult> DeleteServer([FromQuery]int id)
        {
            if(await _userservice.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            _service.Delete(id);
            return await Index();
        }
        
    }
}