using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Data;
using ReactApp1.Server.Model;

namespace ReactApp1.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly EventDbContext _db;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserRepository _userRepository;

    public AuthController(EventDbContext db, ILogger<AuthController> logger, IUserRepository userRepository)
    {
        _db = db;
        _logger = logger;
        _userRepository = userRepository;
    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyAuthentication");
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            name = User.Identity?.Name,
            email = User.FindFirstValue(ClaimTypes.Email),
            role = User.FindFirstValue(ClaimTypes.Role),
            ProfilePictureUrl = User.FindFirst("ProfilePictureUrl")?.Value
        });
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin(string? returnUrl = "/dashboard")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Content("~/api/auth/google-callback") + $"?returnUrl={Uri.EscapeDataString(returnUrl!)}"
        };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        try
        {
            var auth = await HttpContext.AuthenticateAsync();
            if (!auth.Succeeded)
            {
                _logger.LogWarning("Google authentication failed");
                return Redirect($"http://localhost:5173/login?error=google_failed");
            }

            // 获取Google用户信息
            var email = auth.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = auth.Principal?.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = auth.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            {
                _logger.LogError("Missing email or Google ID from authentication");
                return Redirect($"http://localhost:5173/login?error=missing_info");
            }

            // 查找或创建用户（内部会处理邮箱验证逻辑）
            var (user, isNewUser) = await FindOrCreateUser(email, name, googleId);

            if (user == null)
            {
                _logger.LogError("Failed to find or create user for email: {Email}", email);
                return Redirect($"http://localhost:5173/login?error=user_creation_failed");
            }

            // 更新最后登录时间
            await _userRepository.UpdateLastLoginAsync(user.Id);

            // 设置Cookie认证，使用数据库的userId作为NameIdentifier
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "MyAuthentication");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync("MyAuthentication", claimsPrincipal);

            // 生成前端需要的用户信息
            var userInfo = new
            {
                id = user.Id,
                email = user.Email,
                username = user.Username,
                role = user.Role,
                googleId = user.GoogleId,
                isFirstLogin = isNewUser
            };

            // 将用户信息传递给前端
            var userJson = System.Text.Json.JsonSerializer.Serialize(userInfo);

            return Content($@"
                <script>
                    window.opener.postMessage({{
                        type: 'GOOGLE_AUTH_SUCCESS',
                        user: {userJson}
                    }}, 'http://localhost:5173');
                    window.close();
                </script>", "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google callback");
            return Redirect($"http://localhost:5173/login?error=callback_error");
        }
    }

    private async Task<(User?, bool isNewUser)> FindOrCreateUser(string email, string? name, string googleId)
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
                return (existingUser, false); // 现有用户，不是新用户
            }

            // 用户不存在，创建新的学生用户
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

            return (createdUser, true); // 新创建的用户，是新用户
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding or creating user for email: {Email}", email);
            return (null, false);
        }
    }
}