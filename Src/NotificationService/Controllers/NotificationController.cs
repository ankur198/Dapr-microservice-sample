using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        public async Task NewUserCreatedAsync(User user, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New User Created Event");
            await AddNotificationToUser(user.id, $"Hello {user.name}, welcome to nagp-urbanclap.", client);
        }

        [Topic(PubSubName, "orderPlaced")]
        [HttpPost("orderPlaced")]
        public async Task OrderPlacedAsync(Order order, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New Order Placed Event");
            await AddNotificationToUser(order.userId, $"Your order (ID = {order.id}) has been placed successfully.", client);
            await AddNotificationToAdmin($"A new order (ID = {order.id}) has been placed.", client);
        }

        [Topic(PubSubName, "orderAssigned")]
        [HttpPost("orderAssigned")]
        public async Task OrderAssignedAsync(Order order, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New Order Assigned Event");
            if (order.professionalId is not null)
            {
                var professional = await client.InvokeMethodAsync<User>(HttpMethod.Get, "user", $"User/{order.professionalId}");
                await AddNotificationToUser(order.userId, $"Your order (ID = {order.id}) has been assigned to {professional.name}.", client);
                await AddNotificationToUser(professional.id, $"A new order (ID = {order.id}) has been assigned to you.", client);
            }
            else
            {
                _logger.LogError("Invalid notification, professionalId missing");
                return;
            }
        }

        [Topic(PubSubName, "orderApproved")]
        [HttpPost("orderApproved")]
        public async Task OrderApprovedAsync(Order order, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New Order Approved Event");
            await AddNotificationToUser(order.userId, $"Your order (ID = {order.id}) has been approved, expect a visit soon.", client);
        }

        [Topic(PubSubName, "orderRejected")]
        [HttpPost("orderRejected")]
        public async Task OrderRejectedAsync(Order order, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New Order Rejected Event");
            await AddNotificationToUser(order.userId, $"Your order (ID = {order.id}) has been reject.", client);
        }

        [Topic(PubSubName, "orderCompleted")]
        [HttpPost("orderCompleted")]
        public async Task OrderCompletedAsync(Order order, [FromServices] DaprClient client)
        {
            _logger.LogInformation("New Order Completed Event");
            await AddNotificationToUser(order.userId, $"Your order (ID = {order.id}) has been completed.", client);
        }

        private async Task AddNotificationToUser(int userId, string message, DaprClient client)
        {
            var stateUser = await client.GetStateEntryAsync<List<Notification>>(StoreName, $"user-{userId}");
            await AddNotification(stateUser, message);
        }
        private async Task AddNotificationToAdmin(string message, DaprClient client)
        {
            var stateUser = await client.GetStateEntryAsync<List<Notification>>(StoreName, $"admin");
            await AddNotification(stateUser, message);
        }

        private Task AddNotification(StateEntry<List<Notification>> state, string message)
        {
            state.Value ??= new();
            state.Value.Add(new Notification(message, DateTime.Now));
            return state.SaveAsync();
        }
    }
}
