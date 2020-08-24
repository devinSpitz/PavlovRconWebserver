using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize]
    
    [Route("[controller]/[action]")]
    public class RoleController : Controller
    {
        private readonly RoleManager<LiteDB.Identity.Models.LiteDbRole> roleManager;
        private readonly UserManager<LiteDB.Identity.Models.LiteDbUser> userManager;
        private readonly UserService _userService;

        public RoleController(RoleManager<LiteDB.Identity.Models.LiteDbRole> roleMgr, UserManager<LiteDB.Identity.Models.LiteDbUser> userMrg,UserService userService)
        {
            roleManager = roleMgr;
            userManager = userMrg;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View(roleManager.Roles);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Required]string name)
        {
            
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            if (ModelState.IsValid)
            {
                var role = new LiteDB.Identity.Models.LiteDbRole()
                {
                    Name = name
                };
                IdentityResult result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    Errors(result);
            }
            return View(name);
        }

        //[HttpPost]
        //public async Task<IActionResult> Delete(string id)
        //{
        //    AspNetCore.Identity.LiteDB.IdentityRole role = await roleManager.FindByIdAsync(id);
        //    if (role != null)
        //    {
        //        IdentityResult result = await roleManager.DeleteAsync(role);
        //        if (result.Succeeded)
        //            return RedirectToAction("Index");
        //        else
        //            Errors(result);
        //    }
        //    else
        //        ModelState.AddModelError("", "No role found");
        //    return View("Index", roleManager.Roles);
        //}

        public async Task<IActionResult> Update(string id)
        {
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            LiteDB.Identity.Models.LiteDbRole role = await roleManager.FindByIdAsync(id);
            List<LiteDB.Identity.Models.LiteDbUser> members = new List<LiteDB.Identity.Models.LiteDbUser>();
            List<LiteDB.Identity.Models.LiteDbUser> nonMembers = new List<LiteDB.Identity.Models.LiteDbUser>();
            foreach (LiteDB.Identity.Models.LiteDbUser user in _userService.FindAll())
            {
                var list = await userManager.IsInRoleAsync(user, role.Name) ? members : nonMembers;
                list.Add(user);
            }
            return View(new RoleEdit
            {
                Role = role,
                Members = members,
                NonMembers = nonMembers
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(RoleModification model)
        {
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            IdentityResult result;
            if (ModelState.IsValid)
            {
                foreach (string userId in model.AddIds ?? new string[] { })
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }
                foreach (string userId in model.DeleteIds ?? new string[] { })
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.RemoveFromRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }
            }

            if (ModelState.IsValid)
                return RedirectToAction(nameof(Index));
            else
                return await Update(model.RoleId);
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

    }
}