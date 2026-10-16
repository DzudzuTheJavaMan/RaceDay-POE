using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999)]
    public decimal Distance { get; set; }

    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}