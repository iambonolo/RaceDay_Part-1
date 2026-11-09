
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ParticipantController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Participant Dashboard
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.GetAsync("api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to load events. Please try again.";
                return View(new List<EventViewModel>());
            }

            var events = await response.Content.ReadFromJsonAsync<List<EventViewModel>>()
                         ?? new List<EventViewModel>();

            var upcomingEvents = events
                .Where(e => e.EventDate.Date >= DateTime.Today)
                .OrderBy(e => e.EventDate)
                .ToList();

            return View(upcomingEvents);
        }

        // View Event Details
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null ||
                HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.GetAsync($"api/events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to find this event.";
                return RedirectToAction("Index");
            }

            var raceEvent = await response.Content.ReadFromJsonAsync<EventViewModel>();

            if (raceEvent == null)
            {
                TempData["Error"] = "Event details could not be loaded.";
                return RedirectToAction("Index");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            return View(raceEvent);
        }
    }
}
