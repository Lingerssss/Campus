using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using ReactApp1.Server.Data;
using ReactApp1.Server.DTOs;
using ReactApp1.Server.Model;
using ReactApp1.Server.Services;

namespace ReactApp1.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IUserRepository userRepository, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _userRepository = userRepository;
        _logger = logger;
        _configuration = configuration;
    }

    private string GetFrontendUrl()
    {
        return _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
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
                return Content($@"
                    <script>
                        window.opener.postMessage({{
                            type: 'GOOGLE_AUTH_ERROR',
                            error: 'google_failed',
                            message: 'Google authentication failed'
                        }}, '{GetFrontendUrl()}');
                        window.close();
                    </script>", "text/html");
            }

            // 获取Google用户信息
            var email = auth.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = auth.Principal?.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = auth.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            {
                _logger.LogError("Missing email or Google ID from authentication");
                return Content($@"
                    <script>
                        window.opener.postMessage({{
                            type: 'GOOGLE_AUTH_ERROR',
                            error: 'missing_info',
                            message: 'Missing information from Google'
                        }}, '{GetFrontendUrl()}');
                        window.close();
                    </script>", "text/html");
            }

            // 处理用户登录（查找或创建用户）
            var (user, isNewUser) = await _authService.ProcessUserLoginAsync(email, name, googleId);

            // 更新最后登录时间
            await _userRepository.UpdateLastLoginAsync(user.Id);

            // 设置Cookie认证
            var claims = _authService.CreateUserClaims(user);
            var claimsIdentity = new ClaimsIdentity(claims, "MyAuthentication");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync("MyAuthentication", claimsPrincipal);

            // 生成前端需要的用户信息
            var userInfo = _authService.CreateUserInfo(user, isNewUser);
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("@aucklanduni.ac.nz"))
        {
            _logger.LogWarning("Invalid email domain: {Message}", ex.Message);
            return Content($@"
                <script>
                    window.opener.postMessage({{
                        type: 'GOOGLE_AUTH_ERROR',
                        error: 'user_creation_failed',
                        message: 'Invalid email domain. Please use @aucklanduni.ac.nz email.'
                    }}, 'http://localhost:5173');
                    window.close();
                </script>", "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google callback");
            return Content($@"
                <script>
                    window.opener.postMessage({{
                        type: 'GOOGLE_AUTH_ERROR',
                        error: 'callback_error',
                        message: 'An error occurred during authentication'
                    }}, 'http://localhost:5173');
                    window.close();
                </script>", "text/html");
        }
    }

}