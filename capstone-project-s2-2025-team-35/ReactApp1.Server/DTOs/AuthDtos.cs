namespace ReactApp1.Server.DTOs
{
    public class AuthUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string GoogleId { get; set; } = string.Empty;
        public bool IsFirstLogin { get; set; }
    }

    public class AuthResultDto
    {
        public bool Success { get; set; }
        public AuthUserDto? User { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthErrorDto
    {
        public string Type { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
