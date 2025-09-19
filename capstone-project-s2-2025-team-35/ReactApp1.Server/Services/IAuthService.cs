using ReactApp1.Server.Model;
using System.Security.Claims;

namespace ReactApp1.Server.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// 处理用户登录（查找或创建用户）
        /// </summary>
        /// <param name="email">用户邮箱</param>
        /// <param name="name">用户姓名</param>
        /// <param name="googleId">Google ID</param>
        /// <returns>用户信息和是否为新用户</returns>
        Task<(User user, bool isNewUser)> ProcessUserLoginAsync(string email, string? name, string googleId);

        /// <summary>
        /// 查找现有用户
        /// </summary>
        /// <param name="email">用户邮箱</param>
        /// <param name="googleId">Google ID</param>
        /// <returns>现有用户或null</returns>
        Task<User?> FindExistingUserAsync(string email, string googleId);

        /// <summary>
        /// 验证邮箱是否可用于注册
        /// </summary>
        /// <param name="email">用户邮箱</param>
        /// <returns>是否有效</returns>
        Task<bool> ValidateEmailForRegistrationAsync(string email);

        /// <summary>
        /// 创建新学生用户
        /// </summary>
        /// <param name="email">用户邮箱</param>
        /// <param name="googleId">Google ID</param>
        /// <param name="name">用户姓名</param>
        /// <returns>新创建的用户</returns>
        Task<User> CreateNewStudentUserAsync(string email, string googleId, string? name);

        /// <summary>
        /// 创建用户Claims
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>Claims列表</returns>
        List<Claim> CreateUserClaims(User user);

        /// <summary>
        /// 生成前端用户信息
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="isNewUser">是否为新用户</param>
        /// <returns>前端用户信息</returns>
        object CreateUserInfo(User user, bool isNewUser);
    }
}
