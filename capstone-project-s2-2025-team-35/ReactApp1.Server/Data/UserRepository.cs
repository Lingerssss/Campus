using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Model;

namespace ReactApp1.Server.Data;
public class UserRepository : IUserRepository
{
    private readonly EventDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(EventDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Basic CRUD Operations

    public async Task<User?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Registrations)
                .Include(u => u.OrganizedEvents)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users
                .Include(u => u.Registrations)
                .Include(u => u.OrganizedEvents)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            throw;
        }
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(googleId))
                return null;

            return await _context.Users
                .Include(u => u.Registrations)
                .Include(u => u.OrganizedEvents)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by Google ID: {GoogleId}", googleId);
            throw;
        }
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _context.Users
                .Include(u => u.Registrations)
                .Include(u => u.OrganizedEvents)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username: {Username}", username);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            return await _context.Users
                .Include(u => u.Registrations)
                .Include(u => u.OrganizedEvents)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    public async Task<User> CreateAsync(User user)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(user.GoogleId))
                throw new ArgumentException("GoogleId is required");
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new ArgumentException("Username is required");

            // Validate email domain for students
            if (user.Role == UserRole.Student && !await IsValidAucklandEmailAsync(user.Email))
                throw new ArgumentException("Only @aucklanduni.ac.nz emails are allowed for students");

            // Check for duplicates
            if (await EmailExistsAsync(user.Email))
                throw new InvalidOperationException($"User with email {user.Email} already exists");
            if (await GoogleIdExistsAsync(user.GoogleId))
                throw new InvalidOperationException($"User with Google ID {user.GoogleId} already exists");
            if (await UsernameExistsAsync(user.Username))
                throw new InvalidOperationException($"Username {user.Username} already exists");

            // Set creation time
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user: {Username} ({Email})", user.Username, user.Email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user?.Email);
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
                throw new InvalidOperationException($"User with ID {user.Id} not found");

            // Check if email/username is being changed and if it conflicts with existing users
            if (existingUser.Email != user.Email && await EmailExistsAsync(user.Email))
                throw new InvalidOperationException($"Email {user.Email} is already in use");
            if (existingUser.Username != user.Username && await UsernameExistsAsync(user.Username))
                throw new InvalidOperationException($"Username {user.Username} is already in use");

            // Update fields
            existingUser.Email = user.Email;
            existingUser.Username = user.Username;
            existingUser.ProfilePictureUrl = user.ProfilePictureUrl;
            existingUser.Role = user.Role;
            existingUser.LastLoginAt = user.LastLoginAt;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user: {Username} ({Email})", user.Username, user.Email);
            return existingUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user?.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted user: {Username} ({Email})", user.Username, user.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            throw;
        }
    }

    // Existence Checks

    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence: {UserId}", id);
            throw;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username existence: {Username}", username);
            throw;
        }
    }

    public async Task<bool> GoogleIdExistsAsync(string googleId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(googleId))
                return false;

            return await _context.Users.AnyAsync(u => u.GoogleId == googleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Google ID existence: {GoogleId}", googleId);
            throw;
        }
    }

    // Role-based Queries

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        try
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role: {Role}", role);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetStudentsAsync()
    {
        return await GetUsersByRoleAsync(UserRole.Student);
    }

    public async Task<IEnumerable<User>> GetOrganizersAsync()
    {
        return await GetUsersByRoleAsync(UserRole.Organizer);
    }

    // Advanced Queries

    public async Task<IEnumerable<User>> GetRecentUsersAsync(int count = 10)
    {
        try
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent users");
            throw;
        }
    }

    public async Task<IEnumerable<User>> SearchUsersByUsernameAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            return await _context.Users
                .Where(u => u.Username.ToLower().Contains(searchTerm.ToLower()) ||
                            u.Email.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(u => u.Username)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<int> GetTotalUserCountAsync()
    {
        try
        {
            return await _context.Users.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total user count");
            throw;
        }
    }

    public async Task<int> GetUserCountByRoleAsync(UserRole role)
    {
        try
        {
            return await _context.Users.CountAsync(u => u.Role == role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user count by role: {Role}", role);
            throw;
        }
    }

    // Activity Tracking

    public async Task UpdateLastLoginAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.Now;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated last login for user: {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(DateTime since)
    {
        try
        {
            return await _context.Users
                .Where(u => u.LastLoginAt != null && u.LastLoginAt >= since)
                .OrderByDescending(u => u.LastLoginAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users since: {Since}", since);
            throw;
        }
    }

    // Validation Helpers

    public async Task<bool> IsValidAucklandEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return email.ToLower().EndsWith("@aucklanduni.ac.nz");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Auckland email: {Email}", email);
            return false;
        }
    }

    public async Task<bool> IsOrganizerEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Check if this email exists in database as an organizer
            // This ensures only predefined organizers can login as organizers
            return await _context.Users.AnyAsync(u => 
                u.Email.ToLower() == email.ToLower() && 
                u.Role == UserRole.Organizer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking organizer email: {Email}", email);
            return false;
        }
    }

    public async Task<User> GetOrCreateUserFromGoogleAsync(string googleId, string email, string username, string? profilePictureUrl = null)
    {
        try
        {
            // First try to find existing user by Google ID
            var existingUser = await GetByGoogleIdAsync(googleId);
            if (existingUser != null)
            {
                // Update last login
                await UpdateLastLoginAsync(existingUser.Id);
                return existingUser;
            }

            // Try to find by email (in case Google ID changed or user exists but hasn't logged in with Google yet)
            existingUser = await GetByEmailAsync(email);
            if (existingUser != null)
            {
                // Update Google ID and last login
                existingUser.GoogleId = googleId;
                if (!string.IsNullOrWhiteSpace(profilePictureUrl))
                    existingUser.ProfilePictureUrl = profilePictureUrl;
                
                await UpdateAsync(existingUser);
                await UpdateLastLoginAsync(existingUser.Id);
                return existingUser;
            }

            // Check if this is a predefined organizer first
            var isOrganizerEmail = await IsOrganizerEmailAsync(email);
            
            if (isOrganizerEmail)
            {
                // This is a predefined organizer - create organizer account
                var newOrganizer = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    Username = username,
                    ProfilePictureUrl = profilePictureUrl,
                    Role = UserRole.Organizer,
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now
                };

                return await CreateAsync(newOrganizer);
            }
            else
            {
                // Not an organizer, validate Auckland email for students
                if (!await IsValidAucklandEmailAsync(email))
                    throw new InvalidOperationException("Only @aucklanduni.ac.nz emails are allowed for students");

                // Create new student account
                var newStudent = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    Username = username,
                    ProfilePictureUrl = profilePictureUrl,
                    Role = UserRole.Student,
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now
                };

                return await CreateAsync(newStudent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrCreateUserFromGoogle: {Email}", email);
            throw;
        }
    }

    public async Task<bool> CanUserRegisterAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Must be Auckland email
            if (!await IsValidAucklandEmailAsync(email))
                return false;

            // Check if user already exists
            if (await EmailExistsAsync(email))
                return false;

            // Students can register (will be handled by GetOrCreateUserFromGoogleAsync)
            // Organizers cannot self-register
            return !await IsOrganizerEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can register: {Email}", email);
            return false;
        }
    }
}