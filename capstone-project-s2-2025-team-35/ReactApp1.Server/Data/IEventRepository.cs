using ReactApp1.Server.Model;

namespace ReactApp1.Server.Data
{
    public interface IEventRepository
    {
        IEnumerable<Event> GetAllEvents();
        Event? GetEventById(int id);
        IEnumerable<Event> GetEventsByOrganizer(int organizerId);
        bool HasRoomClash(int organizerId, string location, DateTime startAt, DateTime endAt, int? ignoreEventId = null);
        int CreateEvent(Event e);
        bool UpdateEvent(Event e);
        // check register status
        bool IsUserRegistered(int eventId, int userId);
        // check time conflict
        bool HasTimeConflict(int userId, DateTime startAt, DateTime endAt, int? excludeEventId = null);

        // register for event 
        bool RegisterForEvent(int eventId, int userId);

        // unregister for event
        bool UnregisterForEvent(int eventId, int userId);
    }
}