﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Models.AccountViewModels;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly SignInManager<LiteDbUser> _signInManager;
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly UserService _userService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly ReservedServersService _reservedServersService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;

        public AccountController(
            UserManager<LiteDbUser> userManager,
            SignInManager<LiteDbUser> signInManager,
            IEmailSender emailSender,
            ReservedServersService reservedServersService,
            SteamIdentityService steamIdentityService,
            PavlovServerService pavlovServerService,
            SshServerSerivce sshServerSerivce,
            ILogger<AccountController> logger,
            UserService userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _userService = userService;
            _sshServerSerivce = sshServerSerivce;
            _steamIdentityService = steamIdentityService;
            _reservedServersService = reservedServersService;
            _pavlovServerService = pavlovServerService;
        }

        [TempData] public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            var model = new LoginViewModel();
            model.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            
            model.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result =
                    await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe,
                        false); // again workaround for the same reason
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }

                if (result.RequiresTwoFactor)
                    return RedirectToAction(nameof(LoginWith2fa), new {returnUrl, model.RememberMe});
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null) throw new ApplicationException("Unable to load two-factor authentication user.");

            var model = new LoginWith2faViewModel {RememberMe = rememberMe};
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe,
            string returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result =
                await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe,
                    model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }

            _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) throw new ApplicationException("Unable to load two-factor authentication user.");

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model,
            string returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) throw new ApplicationException("Unable to load two-factor authentication user.");

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }

            _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }


        [Authorize(Roles = CustomRoles.AnyOtherThanUser)]
        [HttpGet]
        public async Task<IActionResult> Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [Authorize(Roles = CustomRoles.AnyOtherThanUser)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new LiteDbUser
                {
                    UserName = model.Username, Email = model.Username + "@" + model.Username
                }; // workeround caus dont like emails as account
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                    //await _signInManager.SignInAsync(user, false);
                    _logger.LogInformation("User created a new account with password.");
                    await _userManager.AddToRoleAsync(user, "User");

                    return RedirectToUserIndex();
                    //return RedirectToLocal(returnUrl);
                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new {returnUrl});
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction(nameof(Login));

            
            // Sign in the user with this external login provider if the user already has a login.
            var result =
                await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);

            if (result.Succeeded)
            {
                var emailSuc = GetEmailFromExternalProvider(info);
                
                //var user = await _userManager.GetUserAsync(HttpContext.User);
                var user = await _userService.GetUserByEmail(emailSuc);
                
                if (info.LoginProvider.ToLower()=="paypal" && user?.Email != null && emailSuc != user.Email)
                {
                    await _userManager.SetEmailAsync(user,emailSuc);
                }

                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut) return RedirectToAction(nameof(Lockout));

            // If the user does not have an account, then ask the user to create an account.
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["LoginProvider"] = info.LoginProvider;
            //GetSteamID
            return View("ExternalLogin", new ExternalLoginViewModel {UserName = ""});
        }
        private static string GetEmailFromExternalProvider(ExternalLoginInfo info)
        {
            var email = "";
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                email = info.Principal.FindFirstValue(ClaimTypes.Email);
            }

            return email;
        }
        private static SteamIdentity CrawlSteamIdentity(ExternalLoginInfo info)
        {
            var steamIdentity = new SteamIdentity();
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
            {
                steamIdentity.Id = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier).Split("/").Last();
            }
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Name))
            {
                steamIdentity.Name = info.Principal.FindFirstValue(ClaimTypes.Name);
            }

            return steamIdentity;
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model,
            string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                    throw new ApplicationException("Error loading external login information during confirmation.");
                
                var email = GetEmailFromExternalProvider(info);
                var emailAlreadyExist = await _userService.GetUserByEmail(email);
                if (emailAlreadyExist!=null)
                {
                    ModelState.AddModelError("UserName","Your email already exist. If you already have an account please remove it first or contact the Administrator for help.");
                }
                else
                {
                    LiteDbUser user = null;
                    if (info.LoginProvider.ToLower()=="paypal")
                    {
                        user = new LiteDbUser {UserName = model.UserName, Email = email};  
                    }
                    else
                    {
                        user = new LiteDbUser {UserName = model.UserName};
                    }
                    var result = await _userManager.CreateAsync(user);
                    
                    if (result.Succeeded)
                    {
                        result = await _userManager.AddLoginAsync(user, info);
                        if (result.Succeeded)
                        {
                            //GetSteamID
                            if (info.LoginProvider.ToLower() == "steam")
                            {
                                await OverWriteExistingSteamIdOrSaveNewOne(info, user);
                            }
                            
                            await _signInManager.SignInAsync(user, false);
                            await _userManager.AddToRoleAsync(user, "User");
                            
                            //todo add Steam identity
                            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    AddErrors(result);
                }
                
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        private async Task OverWriteExistingSteamIdOrSaveNewOne(ExternalLoginInfo info, LiteDbUser user)
        {
            var steam = CrawlSteamIdentity(info);
            //Todo get existing one and give it to the player or save it as a new one
            var realSteamIdentity = await _steamIdentityService.FindOne(steam.Id);
            if (realSteamIdentity != null)
            {
                realSteamIdentity.LiteDbUser = user;
                await _steamIdentityService.Upsert(realSteamIdentity);
            }
            else
            {
                steam.LiteDbUser = user;
                await _steamIdentityService.Upsert(steam);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null) return RedirectToAction(nameof(HomeController.Index), "Home");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null) throw new ApplicationException("A code must be supplied for password reset.");
            var model = new ResetPasswordViewModel {Code = code};
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction(nameof(ResetPasswordConfirmation));
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }


        private IActionResult RedirectToUserIndex()
        {
            return RedirectToAction(nameof(UserController.Index), "User");
        }

        #endregion
    }
}