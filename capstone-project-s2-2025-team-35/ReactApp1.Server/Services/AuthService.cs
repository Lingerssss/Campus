using ReactApp1.Server.Data;
using ReactApp1.Server.Model;
using System.Security.Claims;

namespace ReactApp1.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<(User user, bool isNewUser)> ProcessUserLoginAsync(string email, string? name, string googleId)
        {
            // 1. 查找现有用户
            var existingUser = await FindExistingUserAsync(email, googleId);
            if (existingUser != null)
            {
                return (existingUser, false); // 现有用户，不是新用户
            }

            // 2. 验证邮箱域名
            if (!await ValidateEmailForRegistrationAsync(email))
            {
                _logger.LogWarning("Invalid email domain for student registration: {Email}", email);
                throw new InvalidOperationException("Only @aucklanduni.ac.nz emails are allowed for students");
            }

            // 3. 创建新学生用户
            var newUser = await CreateNewStudentUserAsync(email, googleId, name);
            return (newUser, true); // 新创建的用户，是新用户
        }

        public async Task<User?> FindExistingUserAsync(string email, string googleId)
        {
            try
            {
                // 首先尝试通过邮箱查找现有用户
                var existingUser = await _userRepository.GetByEmailAsync(email);
                if (existingUser != null)
                {
                    // 如果找到现有用户，更新GoogleId（如果不匹配的话）
                    if (existingUser.GoogleId != googleId)
                    {
                        existingUser.GoogleId = googleId;
                        await _userRepository.UpdateAsync(existingUser);
                        _logger.LogInformation("Updated GoogleId for existing user: {Email}", email);
                    }
                    return existingUser;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding existing user for email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ValidateEmailForRegistrationAsync(string email)
        {
            try
            {
                return await _userRepository.IsValidAucklandEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email: {Email}", email);
                throw;
            }
        }

        public async Task<User> CreateNewStudentUserAsync(string email, string googleId, string? name)
        {
            try
            {
                var emailPrefix = email.Split('@')[0]; // 使用@前的字符串作为username
                var newUser = new User
                {
                    Email = email,
                    Username = emailPrefix,   // Username使用邮箱前缀
                    GoogleId = googleId,      // 使用真实的GoogleId
                    Role = UserRole.Student,  // 新用户默认为学生
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.CreateAsync(newUser);
                _logger.LogInformation("Created new student user: {Email} with GoogleId: {GoogleId}", email, googleId);
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new student user for email: {Email}", email);
                throw;
            }
        }

        public List<Claim> CreateUserClaims(User user)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? "")
            };
        }

        public object CreateUserInfo(User user, bool isNewUser)
        {
            return new
            {
                id = user.Id,
                email = user.Email,
                username = user.Username,
                role = user.Role,
                googleId = user.GoogleId,
                isFirstLogin = isNewUser
            };
        }
    }
}
