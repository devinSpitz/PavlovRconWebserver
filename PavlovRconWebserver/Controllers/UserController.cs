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
        //Todo GDPR so user or admin can delete everything that is related to the user. If the user is get removed it should also also remove everything which is related to him(pavlovserver just reset on rent. OnPremise remove sshserver etc. Mod/admin just remove from Mod/whitelists). 
        public UserController(UserService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            return View("Index", await _service.FindAll());
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _service.Delete(id);
            return await Index();
        }
    }
}