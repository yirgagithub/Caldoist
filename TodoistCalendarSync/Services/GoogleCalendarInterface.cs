using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Services
{
    public interface GoogleCalendarInterface
    {
        public Task<bool> IsGoogleCalendarWatched(string email);
        public Task<bool> Add(GoogleCalendar googleCalendar);
        public Task<bool> AddRange(List<GoogleCalendar> list);
        public Task<bool> AddLookup(string email, string type, string value);
        public Task<List<GoogleCalendar>> GetAllGoogleCalendarByEmail(string email);
    }
}
