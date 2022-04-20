using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoistCalendarSync.Models
{
    public class GoogleCalendar
    {
        public string Id { get; set; }
        public string Summary { get; set; }
        public string Kind { get; set; }
        public string Email { get; set; }
        public string Background { get; set; }
        public string ColorId { get; set; }
        public bool isPrimary { get; set; }

    }
}
