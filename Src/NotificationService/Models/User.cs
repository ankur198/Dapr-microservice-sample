using System;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models
{
    public record User(int id, string name);

    public record Order(int id, int userId, int? professionalId);

    public record Notification(string message, DateTime timeStamp);
}
