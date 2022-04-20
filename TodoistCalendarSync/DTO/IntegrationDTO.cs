using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoistCalendarSync.DTO
{
    public class IntegrationDTO
    {
        public int Id { get; set; }
        public string GoogleCalendarId { get; set; }
        public string TodoistItemId { get; set; }
        public string CalendarName { get; set; }
        public string TodoistItemName { get; set; }
        public bool IsFilter { get; set; }
        public bool AssignedFilter { get; set; }
        public bool LabelFilter { get; set; }
        public string LabelIds { get; set; }
        public bool PriorityFilter { get; set; }
        public string PriorityIds { get; set; }
        public string NewLabel { get; set; }
        public string Duration { get; set; }

    }
}
