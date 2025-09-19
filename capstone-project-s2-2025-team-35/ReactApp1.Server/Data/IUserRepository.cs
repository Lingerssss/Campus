using ReactApp1.Server.Model;

namespace ReactApp1.Server.Data;
public interface IUserRepository
{
    // Basic CRUD Operations
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> GoogleIdExistsAsync(string googleId);

    // Role-based queries
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetStudentsAsync();
    Task<IEnumerable<User>> GetOrganizersAsync();

    // Advanced queries
    Task<IEnumerable<User>> GetRecentUsersAsync(int count = 10);
    Task<IEnumerable<User>> SearchUsersByUsernameAsync(string searchTerm);
    Task<int> GetTotalUserCountAsync();
    Task<int> GetUserCountByRoleAsync(UserRole role);

    // Activity tracking
    Task UpdateLastLoginAsync(int userId);
    Task<IEnumerable<User>> GetActiveUsersAsync(DateTime since);

    // Validation helpers
    Task<bool> IsValidAucklandEmailAsync(string email);
    Task<bool> IsOrganizerEmailAsync(string email);
    
    // OAuth login helpers
    Task<User> GetOrCreateUserFromGoogleAsync(string googleId, string email, string username, string? profilePictureUrl = null);
    Task<bool> CanUserRegisterAsync(string email);
}