namespace TodoistCalendarSync.Models
{
    public class TodoistWebhookModel
    {
        
    private string event_name { get; set; }
    private string user_id { get; set; }
    private EventData event_data { get; set; }
    private Initiator initiator { get; set; }
    private string version { get; set; }

    }

    class EventData
    {
        private string added_by_uid { get; set; }
        private string assigned_by_ui { get; set; }
        private string checkedd { get; set; }
        private string child_order { get; set; }
        private string collapsed{get;set;}
        private string content { get; set; }
        private string description { get; set; }
        private string date_added { get; set; }
        private string date_completed { get; set; }
        private string due { get; set; }
        private string id { get; set; }
        private string in_history { get; set; }
        private string is_deleted { get; set; }
        private string labels { get; set; }
        private string parent_id { get; set; }
        private string priority { get; set; }
        private string project_id { get; set; }
        private string responsible_uid { get; set; }
        private string section_id { get; set; }
        private string sync_id { get; set; }
        private string url {get;set;}
        private string user_id { get; set; }
    }

    class Initiator
    {
       private string email { get; set; }
      private string full_name { get; set; }
      private string id { get; set; }
      private string image_id { get; set; }
      private bool is_premium { get; set; }
    }
}
