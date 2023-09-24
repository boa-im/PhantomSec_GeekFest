using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using BellHackathon.Entities;
using BellHackathon.Models;
using BCrypt.Net;
using System.Net;

namespace BellHackathon.Controllers
{
    public class AccountController : Controller
    {
        public AccountController(UserManager<User> userMngr, SignInManager<User> signInMngr)
        {
            _userManager = userMngr;
            _signInManager = signInMngr;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User { UserName = model.Username };
                int salt = 10;
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password, salt);
                var result = await _userManager.CreateAsync(user, hashedPassword);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult LogIn(string returnURL = "")
        {
            var model = new LoginViewModel { ReturnUrl = returnURL };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogIn(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Get the client's IP address
                var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Check if the location is within Canada
                if (!GetLocationFromIpAddress(clientIpAddress))
                {
                    ModelState.AddModelError("", "Access is restricted to users outside of Canada.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password,
                            isPersistent: model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError("", "Invalid username/password.");
            return View(model);
        }

        private bool GetLocationFromIpAddress(string ipAddress)
        {
            // IP address ranges for Canada (example ranges)
            string[] canadaIpRanges = { "192.0.2.0-192.0.2.255", "198.51.100.0-198.51.100.255" };

            // Convert the user's IP address to a numeric representation for comparison
            long userIp = IpToLong(ipAddress);

            foreach (var ipRange in canadaIpRanges)
            {
                var range = ipRange.Split('-');
                long startIp = IpToLong(range[0]);
                long endIp = IpToLong(range[1]);

                if (userIp >= startIp && userIp <= endIp)
                {
                    return true; // The user's IP is within a Canada IP range
                }
            }

            return false;
        }

        private long IpToLong(string ipAddress)
        {
            IPAddress parsedIp;

            // Try parsing the IP address in standard IPv4 format
            if (!IPAddress.TryParse(ipAddress, out parsedIp))
            {
                // Try parsing the IP address in IPv6 format
                if (!IPAddress.TryParse("::ffff:" + ipAddress, out parsedIp))
                {
                    throw new FormatException("Invalid IP address format.");
                }
            }

            // Convert the parsed IP address to a numeric representation
            byte[] bytes = parsedIp.GetAddressBytes();
            Array.Reverse(bytes); // Ensure correct endianness
            return BitConverter.ToUInt32(bytes, 0);
        }

        [HttpGet]
        public ViewResult AccessDenied()
        {
            return View();
        }

        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;
    }
}
