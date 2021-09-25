using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var claims = HttpContext.User.Claims;
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}