using ReactApp1.Server.Model;

namespace ReactApp1.Server.Data
{
    public interface IDashboardRepository
    {
        // 1. User information and roles
        Task<User?> GetUserByIdAsync(int userId);
        Task<UserRole?> GetUserRoleAsync(int userId);

        // 2. Student Dashboard data
        Task<List<Event>> GetRegisteredEventsByStudentIdAsync(int studentId);
        Task<int> GetTotalRegistrationsByStudentAsync(int studentId);

        // 3. Organizer Dashboard data
        Task<List<Event>> GetPublishedEventsByOrganizerIdAsync(int organizerId);
        Task<int> GetTotalRegistrationsByOrganizerAsync(int organizerId);

        // 4. Student operations
        Task<bool> WithdrawFromEventAsync(int studentId, int eventId);
        Task<bool> IsStudentRegisteredForEventAsync(int studentId, int eventId);

        // 5. Organizer operations  
        Task<bool> DeleteEventAsync(int organizerId, int eventId);
        Task<bool> IsEventOwnedByOrganizerAsync(int eventId, int organizerId);

        // Common statistics
        Task<int> GetRegistrationCountByEventIdAsync(int eventId);

        // Advanced / optional
        Task<List<Event>> GetConflictingEventsAsync(int studentId, DateTime startTime, DateTime endTime);
        Task<DateTime?> GetUserRegistrationTimeAsync(int studentId, int eventId);

        Task<User?> GetUserByEmailAsync(string email);
    }
}
