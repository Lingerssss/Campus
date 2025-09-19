using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Model;

namespace ReactApp1.Server.Data
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly EventDbContext _context;

        public DashboardRepository(EventDbContext context)
        {
            _context = context;
        }

        // 1. User information and roles
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<UserRole?> GetUserRoleAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Role;
        }

        // 2. Student Dashboard data
        public async Task<List<Event>> GetRegisteredEventsByStudentIdAsync(int studentId)
        {
            return await _context.EventRegistrations
                .Where(r => r.UserId == studentId)
                .Include(r => r.Event)
                    .ThenInclude(e => e.Organizer)
                .OrderByDescending(r => r.RegisteredAt)
                .Select(r => r.Event)
                .ToListAsync();
        }

        public async Task<int> GetTotalRegistrationsByStudentAsync(int studentId)
        {
            return await _context.EventRegistrations
                .CountAsync(r => r.UserId == studentId);
        }

        // 3. Organizer Dashboard data
        public async Task<List<Event>> GetPublishedEventsByOrganizerIdAsync(int organizerId)
        {
            return await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalRegistrationsByOrganizerAsync(int organizerId)
        {
            return await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .SelectMany(e => e.Registrations)
                .CountAsync();
        }

        // 4. Student operations
        public async Task<bool> WithdrawFromEventAsync(int studentId, int eventId)
        {
            try
            {
                var registration = await _context.EventRegistrations
                    .FirstOrDefaultAsync(r => r.UserId == studentId && r.EventId == eventId);

                if (registration == null)
                    return false;

                _context.EventRegistrations.Remove(registration);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsStudentRegisteredForEventAsync(int studentId, int eventId)
        {
            return await _context.EventRegistrations
                .AnyAsync(r => r.UserId == studentId && r.EventId == eventId);
        }

        // 5. Organizer operations
        public async Task<bool> DeleteEventAsync(int organizerId, int eventId)
        {
            try
            {
                var eventItem = await _context.Events
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

                if (eventItem == null)
                    return false;

                _context.EventRegistrations.RemoveRange(eventItem.Registrations);
                _context.Events.Remove(eventItem);

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsEventOwnedByOrganizerAsync(int eventId, int organizerId)
        {
            return await _context.Events
                .AnyAsync(e => e.Id == eventId && e.OrganizerId == organizerId);
        }

        // Common statistics
        public async Task<int> GetRegistrationCountByEventIdAsync(int eventId)
        {
            return await _context.EventRegistrations
                .CountAsync(r => r.EventId == eventId);
        }

        // Advanced / optional
        public async Task<List<Event>> GetConflictingEventsAsync(int studentId, DateTime startTime, DateTime endTime)
        {
            return await _context.EventRegistrations
                .Where(r => r.UserId == studentId)
                .Include(r => r.Event)
                .Select(r => r.Event)
                .Where(e => e.StartAt < endTime && startTime < e.EndAt)
                .ToListAsync();
        }

        public async Task<DateTime?> GetUserRegistrationTimeAsync(int studentId, int eventId)
        {
            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.UserId == studentId && r.EventId == eventId);
            return registration?.RegisteredAt;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
