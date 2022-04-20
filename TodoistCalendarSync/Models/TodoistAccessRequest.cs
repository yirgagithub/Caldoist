using System;

namespace Hopesoftware.TodoistCalendarSync.Models
{
    public class TodoistAccessRequest
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string redirect_uri { get; set; }
        public string code { get; set; }
    }
}
