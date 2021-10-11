using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.background = "#222";
            ViewBag.textColor = "#ffffff";
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