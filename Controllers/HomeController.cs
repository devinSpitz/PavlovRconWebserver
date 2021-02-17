using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Controllers
{
   public class HomeController : Controller
   {
      public IActionResult Index() => View();

      public IActionResult Error() => View(new ErrorViewModel
      {
         RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
      });
   }
}
