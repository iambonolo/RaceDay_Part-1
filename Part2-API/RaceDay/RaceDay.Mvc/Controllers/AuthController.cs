
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
                new { Email = email, Password = password });

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password. Please try again.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);
            var user = document.RootElement;

            string GetValue(string property)
            {
                foreach (var item in user.EnumerateObject())
                {
                    if (string.Equals(item.Name, property,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return item.Value.ValueKind == JsonValueKind.String
                            ? item.Value.GetString() ?? ""
                            : item.Value.ToString();
                    }
                }

                return "";
            }

            var userIdText = GetValue("userId");
            var fullName = GetValue("fullName");
            var userEmail = GetValue("email");
            var role = GetValue("role");

            if (!int.TryParse(userIdText, out int userId) ||
                string.IsNullOrWhiteSpace(role))
            {
                ViewBag.Error = "The API returned an incomplete user profile.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("FullName", fullName);
            HttpContext.Session.SetString("Email", userEmail);
            HttpContext.Session.SetString("Role", role.Trim());

            if (string.Equals(role.Trim(), "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Organiser");
            }

            if (string.Equals(role.Trim(), "Participant",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Participant");
            }

            HttpContext.Session.Clear();
            ViewBag.Error = "Unrecognised account role: " + role;
            return View();
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
