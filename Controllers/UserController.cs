using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            await _service.CreateDefaultRoles();
            if (await _service.IsUserNotInRole("Admin", HttpContext.User)) return new UnauthorizedResult();
            return View("Index", await _service.FindAll());
        }

        public async Task<IActionResult> DeleteUser(string id)
        {
            if (await _service.IsUserNotInRole("Admin", HttpContext.User)) return new UnauthorizedResult();
            await _service.Delete(id);
            return await Index();
        }
    }
}