namespace ReactApp1.Server.Model.Dto
{
    // 用于返回用户信息给前端
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    // 用于登录响应，包含额外的登录信息
    public class LoginResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public UserRole Role { get; set; }
        public bool IsFirstLogin { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    // 用于创建用户（管理员创建Organizer时使用）
    public class CreateUserDto
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public UserRole Role { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    // 用于更新用户信息
    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? ProfilePictureUrl { get; set; }
        // 注意：Email和Role通常不允许用户自己修改
    }

    // 用户统计信息DTO
    public class UserStatsDto
    {
        public int Total { get; set; }
        public int Students { get; set; }
        public int Organizers { get; set; }
    }
}