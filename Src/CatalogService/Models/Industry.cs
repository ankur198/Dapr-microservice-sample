using System.Collections.Generic;

namespace CatalogService.Models
{
    public record Industry
    {
        public Industry(int id, string Name, List<Job> jobs)
        {
            Id = id;
            this.Name = Name;
            Jobs = jobs;
        }

        public int Id { get; }
        public string Name { get; }
        public List<Job> Jobs { get; }
    }

    public record Job(int id, string name, int price);
}