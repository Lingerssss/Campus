using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Data;
using ReactApp1.Server.DTOs;
using ReactApp1.Server.Model;

namespace ReactApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "MyAuthentication")]
    [Authorize(Policy = "OrganizerOnly")]
    public class ManageController : ControllerBase
    {
        private readonly EventDbContext _context;
        private readonly ILogger<ManageController> _logger;

        public ManageController(EventDbContext context, ILogger<ManageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /* ------------------------------ helpers ------------------------------ */

        private static EventDto Map(Event e) => new()
        {
            Id          = e.Id,
            Title       = e.Title,
            StartAt     = e.StartAt,
            EndAt       = e.EndAt,
            Location    = e.Location,
            Description = e.Description,
            ImageUrl    = e.ImageUrl,
            Capacity    = e.Capacity,
            Category    = e.Category,
            Tags        = e.Tags,
            Registered  = e.Registrations != null ? e.Registrations.Count : e.Registered,
            OrganizerId = e.OrganizerId,
            CreatedAt   = e.CreatedAt
        };

        private async Task<int?> GetOrganizerIdAsync()
        {
            // Prefer NameIdentifier if present
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idClaim, out var uid))
            {
                var ok = await _context.Users.AsNoTracking()
                    .AnyAsync(u => u.Id == uid && u.Role == UserRole.Organizer);
                return ok ? uid : null;
            }

            // Fallback to username
            var username = User.FindFirstValue(ClaimTypes.Name);
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username && u.Role == UserRole.Organizer);

            return user?.Id;
        }

        private async Task<ActionResult<int>> EnsureCallerMatchesAsync(int userId)
        {
            var me = await GetOrganizerIdAsync();
            if (me is null) return Forbid();
            if (me.Value != userId) return Forbid(); // don’t let users access others’ data
            return me.Value;
        }

        /* ------------- PARAM-BASED ROUTES (keep your frontend calls) ------------- */
        // GET: /api/manage/organizer/{userId}/events
        [HttpGet("organizer/{userId:int}/events")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetMyEventsByParam(int userId)
        {
            var ensured = await EnsureCallerMatchesAsync(userId);
            if (ensured.Result is ForbidResult) return Forbid();

            var items = await _context.Events
                .AsNoTracking()
                .Where(e => e.OrganizerId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EventDto
                {
                    Id          = e.Id,
                    Title       = e.Title,
                    StartAt     = e.StartAt,
                    EndAt       = e.EndAt,
                    Location    = e.Location,
                    Description = e.Description,
                    ImageUrl    = e.ImageUrl,
                    Capacity    = e.Capacity,
                    Category    = e.Category,
                    Registered  = e.Registrations.Count(),
                    Tags        = e.Tags,
                    OrganizerId = e.OrganizerId,
                    CreatedAt   = e.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: /api/manage/organizer/{userId}/events/{eventId}
        [HttpGet("organizer/{userId:int}/events/{eventId:int}")]
        public async Task<ActionResult<EventDto>> GetMyEventByParam(int userId, int eventId)
        {
            var ensured = await EnsureCallerMatchesAsync(userId);
            if (ensured.Result is ForbidResult) return Forbid();

            var evt = await _context.Events
                .AsNoTracking()
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId);

            if (evt == null) return NotFound("Event not found");
            return Ok(Map(evt));
        }

        // POST: /api/manage/organizer/{userId}/events
        [HttpPost("organizer/{userId:int}/events")]
        public async Task<ActionResult<EventDto>> CreateEventByParam(int userId, [FromBody] EventDto dto)
        {
            var ensured = await EnsureCallerMatchesAsync(userId);
            if (ensured.Result is ForbidResult) return Forbid();

            if (dto.StartAt >= dto.EndAt) return BadRequest("StartAt must be before EndAt");
            if (dto.Capacity < 0) return BadRequest("Capacity must be non-negative");

            var entity = new Event
            {
                Title       = dto.Title,
                StartAt     = dto.StartAt,
                EndAt       = dto.EndAt,
                Location    = dto.Location,
                Description = dto.Description,
                ImageUrl    = dto.ImageUrl,
                Capacity    = dto.Capacity,
                Category    = dto.Category,
                Tags        = dto.Tags ?? new List<string>(),
                Registered  = 0,
                OrganizerId = userId,
                CreatedAt   = DateTime.UtcNow
            };

            _context.Events.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyEventByParam),
                new { userId = userId, eventId = entity.Id },
                Map(entity)
            );
        }

        // PUT: /api/manage/organizer/{userId}/events/{eventId}
        [HttpPut("organizer/{userId:int}/events/{eventId:int}")]
        public async Task<IActionResult> UpdateEventByParam(int userId, int eventId, [FromBody] EventDto dto)
        {
            var ensured = await EnsureCallerMatchesAsync(userId);
            if (ensured.Result is ForbidResult) return Forbid();

            var evt = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null) return NotFound("Event not found");
            if (evt.OrganizerId != userId) return Forbid();

            if (dto.StartAt >= dto.EndAt) return BadRequest("StartAt must be before EndAt");

            var currentRegistered = evt.Registrations?.Count ?? evt.Registered;
            if (dto.Capacity < currentRegistered)
                return BadRequest("Capacity cannot be less than current registrations");

            evt.Title       = dto.Title;
            evt.StartAt     = dto.StartAt;
            evt.EndAt       = dto.EndAt;
            evt.Location    = dto.Location;
            evt.Description = dto.Description;
            evt.ImageUrl    = dto.ImageUrl;
            evt.Capacity    = dto.Capacity;
            evt.Category    = dto.Category;
            evt.Tags        = dto.Tags ?? evt.Tags;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/manage/organizer/{userId}/events/{eventId}
        [HttpDelete("organizer/{userId:int}/events/{eventId:int}")]
        public async Task<IActionResult> DeleteMyEventByParam(int userId, int eventId)
        {
            var ensured = await EnsureCallerMatchesAsync(userId);
            if (ensured.Result is ForbidResult) return Forbid();

            var evt = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null) return NotFound("Event not found");
            if (evt.OrganizerId != userId) return Forbid();

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /* --------- ORIGINAL (param-less) ROUTES — optional/back-compat --------- */
        // GET: /api/manage/events
        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetMyEvents()
        {
            var organizerId = await GetOrganizerIdAsync();
            if (organizerId is null) return Forbid();

            var items = await _context.Events
                .AsNoTracking()
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EventDto
                {
                    Id          = e.Id,
                    Title       = e.Title,
                    StartAt     = e.StartAt,
                    EndAt       = e.EndAt,
                    Location    = e.Location,
                    Description = e.Description,
                    ImageUrl    = e.ImageUrl,
                    Capacity    = e.Capacity,
                    Category    = e.Category,
                    Registered  = e.Registrations.Count(),
                    Tags        = e.Tags,
                    OrganizerId = e.OrganizerId,
                    CreatedAt   = e.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: /api/manage/events/{eventId}
        [HttpGet("events/{eventId:int}")]
        public async Task<ActionResult<EventDto>> GetMyEvent(int eventId)
        {
            var organizerId = await GetOrganizerIdAsync();
            if (organizerId is null) return Forbid();

            var evt = await _context.Events
                .AsNoTracking()
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

            if (evt == null) return NotFound("Event not found");
            return Ok(Map(evt));
        }

        // POST: /api/manage/events
        [HttpPost("events")]
        public async Task<ActionResult<EventDto>> CreateEvent([FromBody] EventDto dto)
        {
            var organizerId = await GetOrganizerIdAsync();
            if (organizerId is null) return Forbid();

            if (dto.StartAt >= dto.EndAt) return BadRequest("StartAt must be before EndAt");
            if (dto.Capacity < 0) return BadRequest("Capacity must be non-negative");

            var entity = new Event
            {
                Title       = dto.Title,
                StartAt     = dto.StartAt,
                EndAt       = dto.EndAt,
                Location    = dto.Location,
                Description = dto.Description,
                ImageUrl    = dto.ImageUrl,
                Capacity    = dto.Capacity,
                Category    = dto.Category,
                Tags        = dto.Tags ?? new List<string>(),
                Registered  = 0,
                OrganizerId = organizerId.Value,
                CreatedAt   = DateTime.UtcNow
            };

            _context.Events.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyEvent),
                new { eventId = entity.Id },
                Map(entity)
            );
        }

        // PUT: /api/manage/events/{eventId}
        [HttpPut("events/{eventId:int}")]
        public async Task<IActionResult> UpdateEvent(int eventId, [FromBody] EventDto dto)
        {
            var organizerId = await GetOrganizerIdAsync();
            if (organizerId is null) return Forbid();

            var evt = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null) return NotFound("Event not found");
            if (evt.OrganizerId != organizerId) return Forbid();

            if (dto.StartAt >= dto.EndAt) return BadRequest("StartAt must be before EndAt");

            var currentRegistered = evt.Registrations?.Count ?? evt.Registered;
            if (dto.Capacity < currentRegistered)
                return BadRequest("Capacity cannot be less than current registrations");

            evt.Title       = dto.Title;
            evt.StartAt     = dto.StartAt;
            evt.EndAt       = dto.EndAt;
            evt.Location    = dto.Location;
            evt.Description = dto.Description;
            evt.ImageUrl    = dto.ImageUrl;
            evt.Capacity    = dto.Capacity;
            evt.Category    = dto.Category;
            evt.Tags        = dto.Tags ?? evt.Tags;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/manage/events/{eventId}
        [HttpDelete("events/{eventId:int}")]
        public async Task<IActionResult> DeleteMyEvent(int eventId)
        {
            var organizerId = await GetOrganizerIdAsync();
            if (organizerId is null) return Forbid();

            var evt = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null) return NotFound("Event not found");
            if (evt.OrganizerId != organizerId) return Forbid();

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
