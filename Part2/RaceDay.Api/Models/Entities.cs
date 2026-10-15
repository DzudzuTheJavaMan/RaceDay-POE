namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Profile? Profile { get; set; }
    public List<Event> Events { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Profile
{
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public User User { get; set; } = null!;
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }

    public User Organiser { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Event Event { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "Pending";

    public User Participant { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}