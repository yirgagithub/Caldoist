using Newtonsoft.Json;

namespace TodoistCalendarSync.Models
{
    public class TodoistWebhookModel
    {

     [JsonProperty(PropertyName = "event_name")]
    public string EventName { get; set; }
    [JsonProperty(PropertyName = "user_id")]
    public long? UserId { get; set; }
    [JsonProperty(PropertyName = "event_data")]
    public EventData EventData { get; set; }
    [JsonProperty(PropertyName = "initiator")]
    public Initiator Initiator { get; set; }
    [JsonProperty(PropertyName = "version")]
    public string Version { get; set; }

    }

    public class EventData
    {
        [JsonProperty(PropertyName = "added_by_uid")]
        public long? AddedByUid { get; set; }
        [JsonProperty(PropertyName = "assigned_by_uid")]
        public long? AssignedByUid { get; set; }
        [JsonProperty(PropertyName = "checked")]
        public int Checked { get; set; }
        [JsonProperty(PropertyName = "child_order")]
        public int ChildOrder { get; set; }
        [JsonProperty(PropertyName = "collapsed")]
        public int Collapsed{get;set;}
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "date_added")]
        public string DateAdded { get; set; }
        [JsonProperty(PropertyName = "date_completed")]
        public string DateCompleted { get; set; }
        [JsonProperty(PropertyName = "due")]
        public string Due { get; set; }
        [JsonProperty(PropertyName = "id")]
        public long? Id { get; set; }
        [JsonProperty(PropertyName = "in_history")]
        public int InHistory { get; set; }
        [JsonProperty(PropertyName = "is_deleted")]
        public int IsDeleted { get; set; }
        [JsonProperty(PropertyName = "labels")]
        public string[] Labels { get; set; }
        [JsonProperty(PropertyName = "parent_id")]
        public long? ParentId { get; set; }
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }
        [JsonProperty(PropertyName = "project_id")]
        public long? ProjectId { get; set; }
        [JsonProperty(PropertyName = "responsible_uid")]
        public long? ResponsibleUid { get; set; }
        [JsonProperty(PropertyName = "section_id")]
        public long? section_id { get; set; }
        [JsonProperty(PropertyName = "sync_id")]
        public long? SyncId { get; set; }
        [JsonProperty(PropertyName = "url")]
        public string Url {get;set;}
        [JsonProperty(PropertyName = "user_id")]
        public long? UserId { get; set; }
    }

    public class Initiator
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "full_name")]
        public string FullName { get; set; }
        [JsonProperty(PropertyName = "id")]
        public long? Id { get; set; }
        [JsonProperty(PropertyName = "image_id")]
        public string ImageId { get; set; }
        [JsonProperty(PropertyName = "is_premium")]
        public bool IsPremium { get; set; }
    }
}
