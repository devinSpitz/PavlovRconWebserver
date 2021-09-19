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
        private readonly UserService _userService;
        private readonly RoleManager<LiteDbRole> roleManager;
        private readonly UserManager<LiteDbUser> userManager;

        public RoleController(RoleManager<LiteDbRole> roleMgr, UserManager<LiteDbUser> userMrg, UserService userService)
        {
            roleManager = roleMgr;
            userManager = userMrg;
            _userService = userService;
            // for updaters add roles which are should be there
            if (roleManager.Roles.FirstOrDefault(x => x.Name == "Mod") == null)
                roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Mod"
                });
            if (roleManager.Roles.FirstOrDefault(x => x.Name == "Captain") == null)
                roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Captain"
                });
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (await _userService.IsUserNotInRole("Admin", HttpContext.User)) return new UnauthorizedResult();
            return View(roleManager.Roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (await _userService.IsUserNotInRole("Admin", HttpContext.User)) return new UnauthorizedResult();
            if (ModelState.IsValid)
            {
                var role = new LiteDbRole
                {
                    Name = name
                };
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                Errors(result);
            }

            return View(name);
        }

        [HttpGet]
        public async Task<IActionResult> Update(string id)
        {
            if (await _userService.IsUserNotInRole("Admin", HttpContext.User)) return new UnauthorizedResult();
            var role = await roleManager.FindByIdAsync(id);
            var members = new List<LiteDbUser>();
            var nonMembers = new List<LiteDbUser>();
            foreach (var user in await _userService.FindAll())
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
            if (await _userService.IsUserNotInRole("Admin", HttpContext.User)) return new UnauthorizedResult();
            IdentityResult result;
            if (ModelState.IsValid)
            {
                foreach (var userId in model.AddIds ?? new string[] { })
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }

                foreach (var userId in model.DeleteIds ?? new string[] { })
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
            return await Update(model.RoleId);
        }

        private void Errors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}