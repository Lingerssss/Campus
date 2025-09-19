using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Data;
using ReactApp1.Server.DTOs;
using ReactApp1.Server.Model;

namespace ReactApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        // ▶ CHANGED: split repos
        private readonly IDashboardRepository _dashboard;   // dashboard / async queries
        private readonly IEventRepository _events;          // core repo (used for optional helpers)
        private readonly ILogger<DashboardController> _logger;

        // ▶ CHANGED: inject both interfaces
        public DashboardController(
            IDashboardRepository dashboardRepository,
            IEventRepository eventRepository,
            ILogger<DashboardController> logger)
        {
            _dashboard = dashboardRepository;   // ▶ CHANGED
            _events = eventRepository;          // ▶ CHANGED
            _logger = logger;
        }

        // === Core Methods (Required) ===

        // 1. GET: api/dashboard/student/{studentId}
        [HttpGet("student/{studentId:int}")] // ▶ CHANGED: constrain to :int (optional)
        public async Task<ActionResult<StudentDashboardDto>> GetStudentDashboard(int studentId)
        {
            try
            {
                // ▶ CHANGED: use _dashboard
                var user = await _dashboard.GetUserByIdAsync(studentId);
                if (user == null) return NotFound($"User with ID {studentId} not found");
                if (user.Role != UserRole.Student) return BadRequest($"User {studentId} is not a student");

                var registeredEvents = await _dashboard.GetRegisteredEventsByStudentIdAsync(studentId); // ▶ CHANGED
                var totalRegistrations = await _dashboard.GetTotalRegistrationsByStudentAsync(studentId); // ▶ CHANGED

                var eventDtos = new List<DashboardEventDto>();
                foreach (var eventItem in registeredEvents)
                {
                    var registrationCount = await _dashboard.GetRegistrationCountByEventIdAsync(eventItem.Id); // ▶ CHANGED
                    var registrationTime = await _dashboard.GetUserRegistrationTimeAsync(studentId, eventItem.Id); // ▶ CHANGED

                    eventDtos.Add(new DashboardEventDto
                    {
                        Id = eventItem.Id,
                        Title = eventItem.Title,
                        StartAt = eventItem.StartAt,
                        EndAt = eventItem.EndAt,
                        Location = eventItem.Location,
                        Description = eventItem.Description,
                        Category = eventItem.Category,
                        Capacity = eventItem.Capacity,
                        Registered = registrationCount,
                        OrganizerName = eventItem.Organizer?.Username ?? "Unknown",
                        RegisteredAt = registrationTime
                    });
                }

                var dashboard = new StudentDashboardDto
                {
                    StudentId = user.Id,
                    StudentName = user.Username,
                    StudentEmail = user.Email,
                    RegisteredEvents = eventDtos,
                    TotalRegistrations = totalRegistrations
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student dashboard for user {StudentId}", studentId);
                return StatusCode(500, "Internal server error");
            }
        }
        // === Operation Methods (Required) ===

        // 3. DELETE: api/dashboard/student/{studentId}/events/{eventId}
        [HttpDelete("student/{studentId:int}/events/{eventId:int}")] // ▶ CHANGED: :int (optional)
        public async Task<IActionResult> WithdrawFromEvent(int studentId, int eventId)
        {
            try
            {
                var user = await _dashboard.GetUserByIdAsync(studentId); // ▶ CHANGED
                if (user == null || user.Role != UserRole.Student) return NotFound("Student not found");

                var isRegistered = await _dashboard.IsStudentRegisteredForEventAsync(studentId, eventId); // ▶ CHANGED
                if (!isRegistered) return BadRequest("Student is not registered for this event");

                var success = await _dashboard.WithdrawFromEventAsync(studentId, eventId); // ▶ CHANGED
                if (!success) return StatusCode(500, "Failed to withdraw from event");

                _logger.LogInformation("Student {StudentId} withdrew from event {EventId}", studentId, eventId);
                return Ok(new { message = "Successfully withdrew from event" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing student {StudentId} from event {EventId}", studentId, eventId);
                return StatusCode(500, "Internal server error");
            }
        }
        // === Helper Methods (Recommended) ===

        // 5. GET: api/dashboard/user/{userId}/role
        [HttpGet("user/{userId:int}/role")] // ▶ CHANGED: :int (optional)
        public async Task<ActionResult<UserRoleDto>> GetUserRole(int userId)
        {
            try
            {
                var user = await _dashboard.GetUserByIdAsync(userId); // ▶ CHANGED
                if (user == null) return NotFound($"User with ID {userId} not found");

                return Ok(new UserRoleDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    ProfilePictureUrl = user.ProfilePictureUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /* === Advanced Features (Optional) ===

        // GET: api/dashboard/student/{studentId}/conflicts/{eventId}
        [HttpGet("student/{studentId:int}/conflicts/{eventId:int}")] // ▶ CHANGED: :int (optional)
        public async Task<ActionResult<List<DashboardEventDto>>> GetEventConflicts(int studentId, int eventId)
        {
            try
            {
                var user = await _dashboard.GetUserByIdAsync(studentId); // ▶ CHANGED
                if (user == null || user.Role != UserRole.Student) return NotFound("Student not found");

                // Use core repo to get event times
                var targetEvent = _events.GetEventById(eventId); // ▶ CHANGED
                if (targetEvent == null) return NotFound("Event not found");

                var conflicts = await _dashboard.GetConflictingEventsAsync(
                    studentId, targetEvent.StartAt, targetEvent.EndAt); // ▶ CHANGED

                var conflictDtos = conflicts.Select(e => new DashboardEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartAt = e.StartAt,
                    EndAt = e.EndAt,
                    Location = e.Location,
                    Description = e.Description
                }).ToList();

                return Ok(conflictDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conflicts for student {StudentId} and event {EventId}", studentId, eventId);
                return StatusCode(500, "Internal server error");
            }
        }
        */

        [HttpGet("user/email/{email}")]
        public async Task<ActionResult<UserRoleDto>> GetUserByEmail(string email)
        {
            try
            {
                var user = await _dashboard.GetUserByEmailAsync(email); // ▶ CHANGED
                if (user == null) return NotFound($"User with email {email} not found");

                return Ok(new UserRoleDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    ProfilePictureUrl = user.ProfilePictureUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
