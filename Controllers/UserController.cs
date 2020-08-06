using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize]
    public class UserController : Controller
    {
        
        private readonly UserService _service;
        private readonly RoleController _roleController;
        
        public UserController(UserService service)
        {
            _service = service;
        }
        
        public async Task<IActionResult> Index()
        {
            if(await _service.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Index",_service.FindAll());
        }
        
        public async Task<IActionResult> DeleteUser(string id)
        {
            if(await _service.IsUserInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            _service.Delete(id);
            return await Index();
        }

    }
}