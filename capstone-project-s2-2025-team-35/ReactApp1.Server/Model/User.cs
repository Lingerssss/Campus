using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ReactApp1.Server.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string GoogleId { get; set; }  // Google OAuth
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }  // must be @aucklanduni.ac.nz

        [Required]
        public string Username { get; set; }  // set up username when login first time
        
        public string? ProfilePictureUrl { get; set; }  // profile URL
        
        [Required]
        public UserRole Role { get; set; }  
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        
        [JsonIgnore]
        public List<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
        
        [JsonIgnore]
        public List<Event> OrganizedEvents { get; set; } = new List<Event>();
    }
    
    public enum UserRole
    {
        Student = 0,
        Organizer = 1
    }
}