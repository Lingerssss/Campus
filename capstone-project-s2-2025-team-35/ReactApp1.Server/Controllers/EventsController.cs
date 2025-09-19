using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReactApp1.Server.Data;
using ReactApp1.Server.Model;
using ReactApp1.Server.DTOs;
using System.Numerics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; 
using Microsoft.AspNetCore.Mvc.ModelBinding; 
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ReactApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;
        private readonly EventDbContext _db;

        public EventsController(IEventRepository eventRepository, EventDbContext db)
        {
            _eventRepository = eventRepository;
            _db = db;
        }

        //Unify any DateTime to UTC
        private static DateTime EnsureUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc   => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _                  => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            };
        }


        // GET: api/events
        [AllowAnonymous] // 公开
        [HttpGet]
        public ActionResult<IEnumerable<EventDto>> GetAllEvents()
        {
            var events = _eventRepository.GetAllEvents();
            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartAt = EnsureUtc(e.StartAt),
                EndAt = EnsureUtc(e.EndAt),
                Location = e.Location,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                Capacity = e.Capacity,
                Category = e.Category,
                Tags = e.Tags,
                Registered = e.Registered,
                OrganizerId = e.OrganizerId,
                CreatedAt = EnsureUtc(e.CreatedAt)
            }).ToList();
            
            return Ok(eventDtos);
        }
        
        private User? GetCurrentUser()
        {
            var uid = GetUserId();
            return uid is int id ? _db.Users.FirstOrDefault(u => u.Id == id) : null;
        }
        
        // GET: api/events/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous] // 公开
        public ActionResult<EventDto> GetEventDetail(int id)
        {
            var e = _eventRepository.GetEventById(id);
            if (e == null) return NotFound();

            var user = GetCurrentUser();
            var isReg  = user != null && _eventRepository.IsUserRegistered(id, user.Id);
            var canEdit = user != null && user.Role == UserRole.Organizer && e.OrganizerId == user.Id; // 只有“该活动的拥有者”可编辑

            var dto = new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartAt = EnsureUtc(e.StartAt),
                EndAt = EnsureUtc(e.EndAt),
                Location = e.Location,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                Capacity = e.Capacity,
                Category = e.Category,
                Tags = e.Tags,
                Registered = e.Registered,
                OrganizerId = e.OrganizerId,
                CreatedAt = EnsureUtc(e.CreatedAt),
                IsRegistered = isReg,
                CanEdit = canEdit
            };
            return Ok(dto);
        }

        //Resolving organizerId (Header → query → default 1)
        private int ResolveOrganizerId()
        {
            if (Request.Headers.TryGetValue("X-Debug-UserId", out var hv) && int.TryParse(hv, out var hid)) return hid;
            if (int.TryParse(Request.Query["organizerId"], out var qid)) return qid;
            return 1;
        }

        //Create: POST /api/Events
        [HttpPost]
        [Authorize(Policy = "OrganizerOnly")] // 只有组织者
        public ActionResult Create([FromBody] EventCreateDto dto)
        {
            var organizerId = ResolveOrganizerId();
            var startUtc = EnsureUtc(dto.StartAt);
            var endUtc   = EnsureUtc(dto.EndAt);

            if (endUtc <= startUtc)
            {
                var ms = new ModelStateDictionary();
                ms.AddModelError("endAt", "End time must be after start time.");
                return ValidationProblem(ms);
            }

            if (_eventRepository.HasRoomClash(organizerId, dto.Location, startUtc, endUtc))
                return Conflict(new { message = "Room already booked in that time slot." });

            var imageUrl = SaveImageDataUrl(dto.ImageDataUrl);

            var entity = new Event {
                Title = dto.Title, StartAt = startUtc, EndAt = endUtc, Location = dto.Location,
                Capacity = dto.Capacity, Category = dto.Category, Description = dto.Description,
                ImageUrl = imageUrl, OrganizerId = organizerId, CreatedAt = DateTime.UtcNow
            };

            var id = _eventRepository.CreateEvent(entity);
            return CreatedAtAction(nameof(GetEventDetail), new { id }, null);
        }

        //Update: PUT /api/Events/{id}
        [HttpPut("{id:int}")]
        [Authorize(Policy = "OrganizerOnly")] // 只有组织者
        public ActionResult Update(int id, [FromBody] EventUpdateDto dto)
        {
            var organizerId = ResolveOrganizerId();
            var startUtc = EnsureUtc(dto.StartAt);
            var endUtc   = EnsureUtc(dto.EndAt);

            if (endUtc <= startUtc)
            {
                var ms = new ModelStateDictionary();
                ms.AddModelError("endAt", "End time must be after start time.");
                return ValidationProblem(ms);
            }

            if (_eventRepository.HasRoomClash(organizerId, dto.Location, startUtc, endUtc, ignoreEventId: id))
                return Conflict(new { message = "Room already booked in that time slot." });

            var imageUrl = SaveImageDataUrl(dto.ImageDataUrl);

            var entity = new Event {
                Id = id, Title = dto.Title, StartAt = startUtc, EndAt = endUtc, Location = dto.Location,
                Capacity = dto.Capacity, Category = dto.Category, Description = dto.Description,
                ImageUrl = imageUrl, OrganizerId = organizerId
            };

            var ok = _eventRepository.UpdateEvent(entity);
            if (!ok) return NotFound();
            return NoContent();
        }

        //Save data: Image/... to wwwroot/uploads/events
        private string? SaveImageDataUrl(string? dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;

            var m = System.Text.RegularExpressions.Regex.Match(
                dataUrl, @"^data:(?<mime>image\/[a-zA-Z0-9\+\.\-]+);base64,(?<b64>.+)$");
            if (!m.Success) return null;

            var mime = m.Groups["mime"].Value.ToLowerInvariant();
            var b64  = m.Groups["b64"].Value;

            byte[] bytes;
            try { bytes = Convert.FromBase64String(b64); }
            catch { return null; }

            // It is explicitly declared as string to avoid type inference to int
            string ext = mime switch
            {
                "image/png"  => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg"  => ".jpg",
                _            => ".bin"
            };

            var dir  = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events");
            Directory.CreateDirectory(dir);
            var file = $"{Guid.NewGuid()}{ext}";
            var full = Path.Combine(dir, file);

            /// Use fully qualified names to avoid references to ControllerBase.File(...) conflict
            global::System.IO.File.WriteAllBytes(full, bytes);

            return $"/uploads/events/{file}";
        }

        [HttpPost("{id:int}/register")]
        [Authorize(Policy = "StudentOnly")] // 只有学生
        public ActionResult RegisterForEvent(int id)
        {
            var e = _eventRepository.GetEventById(id);
            if (e == null) return NotFound();

            var user = GetCurrentUser();
            if (user is null) return Unauthorized();

            // ✅ 组织者账号一律不能报名（包括报名自己的活动）
            if (user.Role == UserRole.Organizer)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Organizers cannot register for events." });

            // 双保险：即使是学生，也不能报名自己作为组织者的活动
            if (e.OrganizerId == user.Id)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Organizers cannot register for their own events." });

            if (_eventRepository.HasTimeConflict(user.Id, e.StartAt, e.EndAt, id))
                return Conflict(new { message = "Time conflict with another registered event." });

            if (e.Capacity - e.Registered <= 0)
                return Conflict(new { message = "No seats available" });

            if (e.EndAt < DateTime.UtcNow)
                return Conflict(new { message = "Event has ended" });

            var ok = _eventRepository.RegisterForEvent(id, user.Id);
            if (!ok) return Conflict(new { message = "Registration failed" });

            var updated = _eventRepository.GetEventById(id);
            return Ok(new { ok = true, registered = updated?.Registered ?? 0 });
        }

        // DELETE: api/events/{id}/register
        [HttpDelete("{id:int}/register")]
        [Authorize(Policy = "StudentOnly")] // 只有学生
        public ActionResult UnregisterForEvent(int id)
        {
            var user = GetCurrentUser();
            if (user is null) return Unauthorized();

            // ✅ 组织者账号不应存在报名记录，直接拒绝
            if (user.Role == UserRole.Organizer)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Organizers have no registrations to cancel." });

            var ok = _eventRepository.UnregisterForEvent(id, user.Id);
            if (!ok) return NotFound(new { message = "Not registered" });

            var updated = _eventRepository.GetEventById(id);
            return Ok(new { ok = true, registered = updated?.Registered ?? 0 });
        }

        
        
        // ✅ 用 Claims 读取当前登录用户；DEBUG 下保留后门头
        private int? GetUserId()
        {
            // 来自 Cookie 会话（Google 登录后由 AuthController 写入）
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(s, out var uid)) return uid;

            #if DEBUG
            // 仅本地联调时允许：X-Debug-UserId
            if (Request.Headers.TryGetValue("X-Debug-UserId", out var hv) && int.TryParse(hv, out var hid)) return hid;
            #endif

            return null;
        }

    }
}
