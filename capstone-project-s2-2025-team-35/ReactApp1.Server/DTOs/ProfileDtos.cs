namespace ReactApp1.Server.DTOs
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = "Student";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
