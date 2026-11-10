using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventFormModel
{
    public int EventId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, Display(Name = "Date and time")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30).AddHours(6);

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999, ErrorMessage = "Enter a distance in km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";
}

public class OrganiserEnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
}

public class DashboardRow
{
    public EventItem Event { get; set; } = new();
    public int Enrolments { get; set; }
}