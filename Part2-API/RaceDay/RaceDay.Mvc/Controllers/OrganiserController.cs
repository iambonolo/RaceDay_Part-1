
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrganiserController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private bool IsOrganiser()
        {
            return HttpContext.Session.GetInt32("UserId") != null &&
                   HttpContext.Session.GetString("Role") == "Organiser";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.GetAsync("api/events");

            ViewBag.EventCount = 0;

            if (response.IsSuccessStatusCode)
            {
                var events = await response.Content.ReadFromJsonAsync<JsonElement>();
                ViewBag.EventCount = events.GetArrayLength();
            }
            else
            {
                ViewBag.Error = "Unable to load events.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string? description,
            DateTime eventDate,
            string location,
            decimal distanceKm,
            string eventType)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var eventData = new
            {
                Name = name,
                Description = description,
                EventDate = eventDate,
                Location = location,
                DistanceKm = distanceKm,
                EventType = eventType
            };

            var response = await client.PostAsJsonAsync("api/events", eventData);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Event creation failed. Please check your details.";
                return View();
            }

            TempData["Success"] = "Event created successfully!";
            return RedirectToAction("Index");
        }
    }
}
