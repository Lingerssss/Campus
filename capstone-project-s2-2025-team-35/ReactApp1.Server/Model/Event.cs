using ReactApp1.Server.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    [Required]
    public string Location { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public int Capacity { get; set; }

    public string? Category { get; set; }

    public string? TagsJson { get; set; }

    [NotMapped]
    public List<string> Tags
    {
        get => string.IsNullOrEmpty(TagsJson) ? new List<string>() : 
               System.Text.Json.JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
    }

    [NotMapped]
    public int Registered { get; set; }

    [Required]
    public int OrganizerId { get; set; }
    
    [JsonIgnore]
    public User Organizer { get; set; }

    [JsonIgnore]
    public List<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}