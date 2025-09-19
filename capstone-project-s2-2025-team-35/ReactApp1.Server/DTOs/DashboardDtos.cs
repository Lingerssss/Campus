namespace ReactApp1.Server.DTOs
{
    public class UserRoleDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Student" or "Organizer"
        public string? ProfilePictureUrl { get; set; }
    }
    public class DashboardEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int Capacity { get; set; }
        public int Registered { get; set; }
        public int RemainingSeats => Math.Max(0, Capacity - Registered);
        
        // For student dashboard
        public string? OrganizerName { get; set; }
        public DateTime? RegisteredAt { get; set; }
        
        // For organizer dashboard
        public DateTime? CreatedAt { get; set; }
    }

    public class StudentDashboardDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public List<DashboardEventDto> RegisteredEvents { get; set; } = new();
        public int TotalRegistrations { get; set; }
    }

    public class OrganizerDashboardDto
    {
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string OrganizerEmail { get; set; } = string.Empty;
        public List<DashboardEventDto> PublishedEvents { get; set; } = new();
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
    }
}