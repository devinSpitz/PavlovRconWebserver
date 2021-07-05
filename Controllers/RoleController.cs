using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize]
    public class RoleController : Controller
    {
        private readonly RoleManager<LiteDbRole> roleManager;
        private readonly UserManager<LiteDbUser> userManager;
        private readonly UserService _userService;
        private readonly Dictionary<int, string> Roles;
        public RoleController(RoleManager<LiteDbRole> roleMgr, UserManager<LiteDbUser> userMrg,UserService userService)
        {
            roleManager = roleMgr;
            userManager = userMrg;
            _userService = userService;
            // for updaters add roles which are should be there
            if (roleManager.Roles.FirstOrDefault(x => x.Name == "Mod") == null)
            {
                roleManager.CreateAsync(new LiteDbRole()
                {
                    Name = "Mod"
                });
            }
            if (roleManager.Roles.FirstOrDefault(x => x.Name == "Captain") == null)
            {
                roleManager.CreateAsync(new LiteDbRole()
                {
                    Name = "Captain"
                });
            }

        }

        
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View(roleManager.Roles);
        }

        [HttpGet]
        public IActionResult Create() => View();

       
        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            if (ModelState.IsValid)
            {
                var role = new LiteDbRole()
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

        [HttpGet]
        public async Task<IActionResult> Update(string id)
        {
            if(await _userService.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            LiteDbRole role = await roleManager.FindByIdAsync(id);
            List<LiteDbUser> members = new List<LiteDbUser>();
            List<LiteDbUser> nonMembers = new List<LiteDbUser>();
            foreach (LiteDbUser user in _userService.FindAll())
            {
                var singleUser = await userManager.IsInRoleAsync(user, role.Name) ? members : nonMembers;
                singleUser.Add(user);
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