using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogService.Models;
using CatalogService.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CatalogController : ControllerBase
    {

        private readonly ILogger<CatalogController> _logger;
        public const string StoreName = "statestore";
        public const string PubSubName = "pubsub";

        public CatalogController(ILogger<CatalogController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public List<Industry> Get()
        {
            return GetIndustries();
        }

        [HttpGet("job/{jobId}")]
        public JobVM? GetJob(int jobId)
        {
            var industry = GetIndustries().FirstOrDefault(x => x.Jobs.Any(x => x.id == jobId));
            var job = industry?.Jobs.FirstOrDefault(x => x.id == jobId);

            return industry is null || job is null
            ? null
            : new JobVM(job.id, industry.Id, job.name, industry.Name, job.price);
        }

        private static List<Industry> GetIndustries()
        {
            return new List<Industry>{
                new Industry(0, "Electrician", new List<Job>{ new (0, "AC Repair", 2000), new(1, "Fan Repair", 500)}),
                new Industry(1, "Plumber", new List<Job>{ new (2, "Pipe Replacement", 1000), new(3, "Tap Repair", 200)})
            };
        }
    }
}
