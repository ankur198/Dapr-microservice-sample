using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderService.Models;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        public const string StoreName = "statestore";
        public const string PubSubName = "pubsub";

        public OrderController(ILogger<OrderController> logger)
        {
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<List<Order>> GetAsync([FromServices] DaprClient client)
        {
            _logger.LogInformation("All orders fetched");
            var state = await client.GetStateEntryAsync<List<Order>>(StoreName, $"allOrders");
            state.Value ??= new();
            return state.Value;
        }

        [HttpGet("{userId}/my")]
        public async Task<List<Order>> GetAsync(int userId, [FromServices] DaprClient client)
        {
            var state = await client.GetStateEntryAsync<List<Order>>(StoreName, $"user-{userId}");
            state.Value ??= new();
            return state.Value;
        }

        [HttpGet("{userId}/my/{orderId}")]
        public async Task<Order?> GetAsync(int userId, int orderId, [FromServices] DaprClient client)
        {
            var state = await GetAsync(userId, client);
            return state.FirstOrDefault(x => x.Id == orderId);
        }

        [HttpGet("{userId}/placeOrder/{jobId}")]
        public async Task<ActionResult> PlaceOrder(int userId, int jobId, [FromServices] DaprClient client)
        {
            try
            {
                var job = await client.InvokeMethodAsync<Job>(HttpMethod.Get, "catalog", $"Catalog/Job/{jobId}");
                var user = await client.InvokeMethodAsync<User>(HttpMethod.Get, "user", $"User/{userId}");

                if (user.type == UserType.Professional)
                {
                    return BadRequest();
                }

                var nextOrderState = await client.GetStateEntryAsync<int>(StoreName, "nextOrderId");
                var newOrder = new Order(nextOrderState.Value, userId, jobId, job.price, Order.OrderStatus.Placed, DateTime.Now);
                nextOrderState.Value += 1;
                await nextOrderState.SaveAsync();

                await UpdateOrCreateOrder(client, newOrder);

                return Created($"{userId}/my/{newOrder.Id}", newOrder);
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("admin/{professionalId}/assign/{orderId}")]
        public async Task<ActionResult> AssignOrder(int professionalId, int orderId, [FromServices] DaprClient client)
        {
            try
            {
                var order = (await GetAsync(client)).FirstOrDefault(x => x.Id == orderId);
                var professional = await client.InvokeMethodAsync<User>(HttpMethod.Get, "user", $"User/{professionalId}");

                if (professional.type == UserType.Consumer || order == null)
                {
                    return BadRequest();
                }

                var updatedOrder = order with { ProfessionalId = professionalId, Status = Order.OrderStatus.WaitingForConfirmation };

                await UpdateOrCreateOrder(client, updatedOrder);

                return Ok(updatedOrder);
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("professional/{userId}/")]
        public async Task<List<Order>> GetJobsAsync(int userId, [FromServices] DaprClient client)
        {
            var state = await client.GetStateEntryAsync<List<Order>>(StoreName, $"professional-{userId}");
            state.Value ??= new();
            return state.Value;
        }

        [HttpGet("professional/{professionalId}/approve/{orderId}")]
        public async Task<ActionResult> ApproveOrder(int professionalId, int orderId, [FromServices] DaprClient client)
        {
            try
            {
                var order = (await GetAsync(client)).FirstOrDefault(x => x.Id == orderId);

                if (order == null || order.Status != Order.OrderStatus.WaitingForConfirmation || order.ProfessionalId != professionalId)
                {
                    return BadRequest();
                }

                var updatedOrder = order with { Status = Order.OrderStatus.Approved };

                await UpdateOrCreateOrder(client, updatedOrder);

                return Ok(updatedOrder);
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("professional/{professionalId}/reject/{orderId}")]
        public async Task<ActionResult> RejectOrder(int professionalId, int orderId, [FromServices] DaprClient client)
        {
            try
            {
                var order = (await GetAsync(client)).FirstOrDefault(x => x.Id == orderId);

                if (order == null || order.Status != Order.OrderStatus.WaitingForConfirmation || order.ProfessionalId != professionalId)
                {
                    return BadRequest();
                }

                var updatedOrder = order with { Status = Order.OrderStatus.Rejected };

                await UpdateOrCreateOrder(client, updatedOrder);

                return Ok(updatedOrder);
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("professional/{professionalId}/complete/{orderId}")]
        public async Task<ActionResult> CompleteOrder(int professionalId, int orderId, [FromServices] DaprClient client)
        {
            try
            {
                var order = (await GetAsync(client)).FirstOrDefault(x => x.Id == orderId);

                if (order == null || order.Status != Order.OrderStatus.Approved || order.ProfessionalId != professionalId)
                {
                    return BadRequest();
                }

                var updatedOrder = order with { Status = Order.OrderStatus.Completed };

                await UpdateOrCreateOrder(client, updatedOrder);

                return Ok(updatedOrder);
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        private static async Task UpdateOrCreateOrder(DaprClient client, Order order)
        {
            var userState = await client.GetStateEntryAsync<List<Order>>(StoreName, $"user-{order.UserId}");
            var allState = await client.GetStateEntryAsync<List<Order>>(StoreName, "allOrders");
            var tasks = new List<Task>
            {
                UpdateOrCreateOrderForState(userState),
                UpdateOrCreateOrderForState(allState)
            };

            if (order.ProfessionalId is not null)
            {
                var professionalState = await client.GetStateEntryAsync<List<Order>>(StoreName, $"professional-{order.ProfessionalId}");
                tasks.Add(UpdateOrCreateOrderForState(professionalState));
            }

            await Task.WhenAll(tasks);

            async Task UpdateOrCreateOrderForState(Dapr.StateEntry<List<Order>> state)
            {
                state.Value ??= new();
                var oldOrder = state.Value.FirstOrDefault(x => x.Id == order.Id);
                if (oldOrder is not null)
                {
                    var orderIndex = state.Value.IndexOf(oldOrder);
                    state.Value.RemoveAt(orderIndex);
                    state.Value.Insert(orderIndex, order);
                }
                else
                {
                    state.Value.Add(order);
                }
                await state.SaveAsync();
            }

        }
    }
}
