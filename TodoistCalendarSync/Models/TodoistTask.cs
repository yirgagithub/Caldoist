namespace TodoistCalendarSync.Models
{
    public class TodoistTask
    {
        public string Id { get; set; }
        public string Name { get; set; } 
        public string Content { get; set; }
        public string Assignee { get; set; }
        public string DueDate { get; set; }
        public string DateAdded { get; set; }
        public string DateCompleted { get; set; }
        public string ProjectId { get; set; }   
        public string[] LabelIds { get; set; } 
        public string Priority { get; set; }
        public string Description { get; set; }
    }
}
