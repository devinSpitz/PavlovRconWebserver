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
        private RoleManager<AspNetCore.Identity.LiteDB.IdentityRole> roleManager;
        private UserManager<InbuildUser> userManager;
        private UserService _userService;

        public RoleController(RoleManager<AspNetCore.Identity.LiteDB.IdentityRole> roleMgr, UserManager<InbuildUser> userMrg,UserService userService)
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

        //public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Required]string name)
        {
            
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            if (ModelState.IsValid)
            {
                IdentityResult result = await roleManager.CreateAsync(new AspNetCore.Identity.LiteDB.IdentityRole(name));
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
            AspNetCore.Identity.LiteDB.IdentityRole role = await roleManager.FindByIdAsync(id);
            List<InbuildUser> members = new List<InbuildUser>();
            List<InbuildUser> nonMembers = new List<InbuildUser>();
            foreach (InbuildUser user in _userService.FindAll())
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
                    InbuildUser user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }
                foreach (string userId in model.DeleteIds ?? new string[] { })
                {
                    InbuildUser user = await userManager.FindByIdAsync(userId);
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