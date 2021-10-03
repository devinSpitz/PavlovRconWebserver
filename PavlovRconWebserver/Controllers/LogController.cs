using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize(Roles = CustomRoles.Admin)]
    public class LogController : Controller
    {
        private readonly LogService _logService;
        
        public LogController(LogService logService)
        {
            _logService = logService;
        }
        
        public async Task<IActionResult> Index()
        {
            var logs = await _logService.FindAll();
            return View("Index",logs);
        }

    }
}