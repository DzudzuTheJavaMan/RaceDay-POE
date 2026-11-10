namespace RaceDay.Web.Models;

public class CategoryItem
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class EventItem
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int OrganiserId { get; set; }
    public List<CategoryItem> Categories { get; set; } = new();
}

public class EnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
}

public class HomeIndexViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
}

public class ResultItem
{
    public int ResultId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}