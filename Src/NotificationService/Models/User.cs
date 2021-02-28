using System;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models
{
    public record User(int id, string name);

    public record Notification
    {
        public DateTime Timestamp { get; }
        public object Payload { get; }
        public string Type { get; }

        public Notification(object payload, string type, DateTime timeStamp)
        {
            Timestamp = timeStamp;
            Payload = payload;
            Type = type;
        }
    }
}
