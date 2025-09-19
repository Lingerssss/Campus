using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Model;
using System.Linq;

namespace ReactApp1.Server.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly EventDbContext _context;

        public EventRepository(EventDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Event> GetAllEvents()
        {
            var events = _context.Events.ToList();
            foreach (var e in events)
            {
                e.Registered = _context.EventRegistrations.Count(r => r.EventId == e.Id);
            }
            return events;
        }

        public Event? GetEventById(int id)
        {
            var e = _context.Events.FirstOrDefault(x => x.Id == id);
            if (e != null)
            {
                e.Registered = _context.EventRegistrations.Count(r => r.EventId == e.Id);
            }
            return e;
        }

        // check if Already registered
        public bool IsUserRegistered(int eventId, int userId) =>
            _context.EventRegistrations.Any(r => r.EventId == eventId && r.UserId == userId);

        // check if Time conflict (overlaps with user's other registered events)
        public bool HasTimeConflict(int userId, DateTime startAt, DateTime endAt, int? excludeEventId = null)
        {
            return _context.Events
                .Where(e =>
                    _context.EventRegistrations.Any(r => r.UserId == userId && r.EventId == e.Id) &&
                    (excludeEventId == null || e.Id != excludeEventId))
                .Any(e => e.StartAt < endAt && startAt < e.EndAt);
        }

        // ✅ Register (explicitly pass userId)
        public bool RegisterForEvent(int eventId, int userId)
        {
            var e = _context.Events.FirstOrDefault(x => x.Id == eventId);
            if (e == null) return false;

            var currentCount = _context.EventRegistrations.Count(r => r.EventId == eventId);
            if (currentCount >= e.Capacity) return false;
            if (e.EndAt < DateTime.UtcNow) return false;

            // Duplicate registration guard
            if (IsUserRegistered(eventId, userId)) return false;

            // Optional: time-conflict guard (if handled at the repository layer)
            // if (HasTimeConflict(userId, e.StartAt, e.EndAt, eventId)) return false;

            _context.EventRegistrations.Add(new EventRegistration
            {
                EventId = eventId,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            return true;

        }

        public IEnumerable<Event> GetEventsByOrganizer(int organizerId)
        {
            var events = _context.Events.Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.StartAt).ToList();
            foreach (var ev in events)
            {
                ev.Registered = _context.EventRegistrations.Count(r => r.EventId == ev.Id);
            }
            return events;

        }

        public bool HasRoomClash(int organizerId, string location, DateTime startAt, DateTime endAt, int? ignoreEventId = null)
        {
            var norm = location.Trim().ToLower();
            return _context.Events.Any(e =>
                e.OrganizerId == organizerId &&
                e.Location.ToLower() == norm &&
                e.StartAt < endAt && startAt < e.EndAt &&
                (ignoreEventId == null || e.Id != ignoreEventId.Value));
        }

        public int CreateEvent(Event e)
        {
            _context.Events.Add(e);
            _context.SaveChanges();
            return e.Id;
        }

        public bool UpdateEvent(Event e)
        {
            var existing = _context.Events.FirstOrDefault(x => x.Id == e.Id && x.OrganizerId == e.OrganizerId);
            if (existing == null) return false;

            existing.Title = e.Title;
            existing.StartAt = e.StartAt;
            existing.EndAt = e.EndAt;
            existing.Location = e.Location;
            existing.Capacity = e.Capacity;
            existing.Category = e.Category;
            existing.Description = e.Description;
            existing.ImageUrl = e.ImageUrl;
            _context.SaveChanges();
            return true;
        }

        public bool UnregisterForEvent(int eventId, int userId)
        {
            var reg = _context.EventRegistrations
                .FirstOrDefault(r => r.EventId == eventId && r.UserId == userId);
            if (reg == null) return false;
            _context.EventRegistrations.Remove(reg);
            _context.SaveChanges();
            return true;
        }
    }
}
