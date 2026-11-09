
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.PostAsJsonAsync(
                "api/auth/login",
                new
                {
                    Email = email,
                    Password = password
                });

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password. Please try again.";
                return View();
            }

            var user = await response.Content.ReadFromJsonAsync<JsonElement>();

            HttpContext.Session.SetInt32(
                "UserId",
                user.GetProperty("userId").GetInt32());

            HttpContext.Session.SetString(
                "FullName",
                user.GetProperty("fullName").GetString() ?? "");

            HttpContext.Session.SetString(
                "Email",
                user.GetProperty("email").GetString() ?? "");

            HttpContext.Session.SetString(
                "Role",
                user.GetProperty("role").GetString() ?? "");

            var role = HttpContext.Session.GetString("Role");

            if (role == "Organiser")
            {
                return Redirect("/Organiser/Index");
            }

            if (role == "Participant")
            {
                return Redirect("/Participant/Index");
            }

            return RedirectToAction("Login");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string email,
            string password,
            string role,
            string? organiserCode)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.PostAsJsonAsync(
                "api/auth/register",
                new
                {
                    FullName = fullName,
                    Email = email,
                    Password = password,
                    Role = role,
                    OrganiserCode = organiserCode
                });

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Registration failed. Check your details, email address and organiser code.";
                return View();
            }

            TempData["Success"] = "Registration successful! You can now log in.";

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            await client.PostAsync("api/auth/logout", null);

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
