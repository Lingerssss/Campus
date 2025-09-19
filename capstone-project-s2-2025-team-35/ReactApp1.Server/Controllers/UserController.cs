using Microsoft.AspNetCore.Mvc;
using ReactApp1.Server.Model;
using ReactApp1.Server.Data;

namespace ReactApp1.Server.Controllers
{
    [ApiController]
    [Route("api/user")] 
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepository userRepository, ILogger<UserController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/email/{email}
        [HttpGet("email/{email}")]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email {Email}", email);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/google/{googleId}
        [HttpGet("google/{googleId}")]
        public async Task<ActionResult<User>> GetUserByGoogleId(string googleId)
        {
            try
            {
                var user = await _userRepository.GetByGoogleIdAsync(googleId);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by Google ID {GoogleId}", googleId);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/students
        [HttpGet("students")]
        public async Task<ActionResult<IEnumerable<User>>> GetStudents()
        {
            try
            {
                var students = await _userRepository.GetStudentsAsync();
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/organizers
        [HttpGet("organizers")]
        public async Task<ActionResult<IEnumerable<User>>> GetOrganizers()
        {
            try
            {
                var organizers = await _userRepository.GetOrganizersAsync();
                return Ok(organizers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizers");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/search/{term}
        [HttpGet("search/{term}")]
        public async Task<ActionResult<IEnumerable<User>>> SearchUsers(string term)
        {
            try
            {
                var users = await _userRepository.SearchUsersByUsernameAsync(term);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm}", term);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/stats/count
        [HttpGet("stats/count")]
        public async Task<ActionResult<object>> GetUserStats()
        {
            try
            {
                var totalCount = await _userRepository.GetTotalUserCountAsync();
                var studentCount = await _userRepository.GetUserCountByRoleAsync(UserRole.Student);
                var organizerCount = await _userRepository.GetUserCountByRoleAsync(UserRole.Organizer);

                return Ok(new
                {
                    Total = totalCount,
                    Students = studentCount,
                    Organizers = organizerCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/user (Admin only - for creating predefined organizers)
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            try
            {
                // This endpoint should only be used by administrators to create predefined organizers
                // In a production environment, you should add proper authorization here
                
                // Validate Auckland email
                if (!await _userRepository.IsValidAucklandEmailAsync(user.Email))
                    return BadRequest("Email must be from @aucklanduni.ac.nz domain");

                // Only allow creating organizers through this endpoint
                // Students should be created through Google OAuth login
                if (user.Role != UserRole.Organizer)
                    return BadRequest("This endpoint can only be used to create organizer accounts. Students are created automatically on first login.");

                var createdUser = await _userRepository.CreateAsync(user);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/user/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            try
            {
                if (id != user.Id)
                    return BadRequest("ID mismatch");

                if (!await _userRepository.ExistsAsync(id))
                    return NotFound();

                await _userRepository.UpdateAsync(user);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/user/{id}/update-login
        [HttpPost("{id:int}/update-login")]
        public async Task<IActionResult> UpdateLastLogin(int id)
        {
            try
            {
                if (!await _userRepository.ExistsAsync(id))
                    return NotFound();

                await _userRepository.UpdateLastLoginAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/user/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var deleted = await _userRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}