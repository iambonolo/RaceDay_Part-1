
namespace RaceDay.Mvc.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = "";
        public int OrganiserId { get; set; }
        public string? OrganiserName { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
