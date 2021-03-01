using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserService.Models;
using UserService.ViewModels;

namespace UserService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private readonly ILogger<UserController> _logger;
        public const string StoreName = "statestore";
        public const string PubSubName = "pubsub";

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> Get([FromServices] DaprClient daprClient)
        {
            var totalUsers = await daprClient.GetStateAsync<int>(StoreName, "nextId");
            var tasks = Enumerable.Range(0, totalUsers).Select(x => daprClient.GetStateAsync<User>(StoreName, x.ToString())).ToArray();
            await Task.WhenAll(tasks);
            return tasks.Select(x => x.Result).ToList();
        }

        [HttpGet]
        [Route("{userId}")]
        public async Task<ActionResult<User>> Get(int userId, [FromServices] DaprClient daprClient)
        {
            var user = await daprClient.GetStateAsync<User>(StoreName, userId.ToString());
            if (user is not null)
            {
                return user;
            }
            return null;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateAsync([FromBody] UserCreationVM userVM, [FromServices] DaprClient daprClient)
        {
            _logger.LogInformation("Create a new user");
            var user = new User { Name = userVM.Name, Type = userVM.Type, Location = userVM.Location };
            user.Id = await daprClient.GetStateAsync<int>(StoreName, "nextId");
            user.CreatedOn = DateTime.Now;
            await daprClient.SaveStateAsync<User>(StoreName, user.Id.ToString(), user);
            await daprClient.SaveStateAsync<int>(StoreName, "nextId", user.Id + 1);

            // generate event
            await daprClient.PublishEventAsync<User>(PubSubName, "newuser", user);

            return Created(user.Id.ToString(), user);
        }
    }
}
