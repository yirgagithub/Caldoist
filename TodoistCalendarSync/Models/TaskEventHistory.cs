using System;

namespace TodoistCalendarSync.Models
{
    public class TaskEventHistory
    {
        public int Id { get; set; }
        public string TaskId { get; set; }
        public string EventId { get; set; }
        public string Email { get; set; }  
        public DateTime Createdat { get; set; }  

    }
}
