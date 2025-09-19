namespace ReactApp1.Server.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int Capacity { get; set; }
        public string? Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Registered { get; set; }
        public int OrganizerId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // IsRegistered: true if the *current* authenticated user has already registered for this event.
        //IsRegistered: true if the *current* authenticated user has already registered for this event.
        // RemainingSeats: computed as Math.Max(Capacity - Registered, 0) to avoid negative numbers.
        public bool IsRegistered { get; set; }
        public bool CanEdit { get; set; }
        public int RemainingSeats => Math.Max(Capacity - Registered, 0);
    }

    public class EventCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; } = 50;
        public string? Category { get; set; }
        public string? Description { get; set; }
        // ImageURL from front-end
        public string? ImageDataUrl { get; set; } 
    }

    public class EventUpdateDto : EventCreateDto {}
}
