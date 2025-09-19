using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ReactApp1.Server.Model
{
    public class EventRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [JsonIgnore]
        public User User { get; set; }

        [Required]
        public int EventId { get; set; }
        
        [JsonIgnore]
        public Event Event { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
