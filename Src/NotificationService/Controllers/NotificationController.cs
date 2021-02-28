using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NotificationService.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        public const string StoreName = "statestore";
        public const string PubSubName = "pubsub";


        public NotificationController(ILogger<NotificationController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{userId}")]
        public async Task<List<Notification>> GetNotifications(int userId, [FromServices] DaprClient client)
        {
            var notifications = await client.GetStateAsync<List<Notification>>(StoreName, $"user-{userId}");
            return notifications;
        }

        [Topic(PubSubName, "newuser")]
        [HttpPost("newuser")]
        public async Task<string> NewUserCreatedAsync(User user, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New User Created Event");
            await AddNotificationToUser(user.id, user, "newuser", client);
            return $"Hello {user.name}";
        }


        private static async Task AddNotificationToUser(int userId, object payload, string type, DaprClient client)
        {
            var stateUser = await client.GetStateEntryAsync<List<Notification>>(StoreName, $"user-{userId}");
            stateUser.Value ??= new();
            stateUser.Value.Add(new Notification(payload, type, DateTime.Now));
            await stateUser.SaveAsync();
        }
    }
}
