using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize(Roles = CustomRoles.Admin)]
    public class UserController : Controller
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var user = HttpContext.User;
            await _service.CreateDefaultRoles();
            return View("Index", await _service.FindAll());
        }

        public async Task<IActionResult> DeleteUser(string id)
        {
            await _service.Delete(id);
            return await Index();
        }
    }
}