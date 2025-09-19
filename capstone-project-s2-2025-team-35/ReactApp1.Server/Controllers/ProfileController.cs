// Controllers/ProfileController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Data;
using ReactApp1.Server.DTOs;
using ReactApp1.Server.Model;

namespace ReactApp1.Server.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly EventDbContext _db;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(EventDbContext db, ILogger<ProfileController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /api/profile/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfileDto>> Get(int id)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound();

            return Ok(new ProfileDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                Username = u.Username ?? "",
                ProfilePictureUrl = u.ProfilePictureUrl,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            });
        }

        // PUT /api/profile/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfileDto>> Update(int id, [FromBody] UserUpdateDto dto)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Username)) u.Username = dto.Username!;
            if (!string.IsNullOrWhiteSpace(dto.Email))    u.Email    = dto.Email!;
            if (dto.ProfilePictureUrl != null)            u.ProfilePictureUrl = dto.ProfilePictureUrl;

            await _db.SaveChangesAsync();

            var updated = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == id);
            return Ok(new ProfileDto
            {
                Id = updated.Id,
                Email = updated.Email ?? "",
                Username = updated.Username ?? "",
                ProfilePictureUrl = updated.ProfilePictureUrl,
                Role = updated.Role.ToString(),
                CreatedAt = updated.CreatedAt,
                LastLoginAt = updated.LastLoginAt
            });
        }

        // POST /api/profile/{id}/avatar
        [HttpPost("{id:int}/avatar")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadAvatar(int id)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound();

            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest("No file uploaded.");

            var file = Request.Form.Files[0];
            if (file.Length == 0) return BadRequest("Empty file.");
            if (file.Length > 4 * 1024 * 1024) return StatusCode(StatusCodes.Status413PayloadTooLarge);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
                return BadRequest("Unsupported image type.");

            var root = Directory.GetCurrentDirectory();
            var dir  = Path.Combine(root, "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(dir);

            var fileName = $"{id}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            await using (var fs = System.IO.File.Create(fullPath))
                await file.CopyToAsync(fs);

            var publicUrl = $"/uploads/avatars/{fileName}";
            u.ProfilePictureUrl = publicUrl;
            await _db.SaveChangesAsync();

            return Ok(new { avatarUrl = publicUrl });
        }
    }
}
