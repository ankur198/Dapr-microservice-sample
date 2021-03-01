namespace OrderService.Models
{
    public record Job(int jobId, int industryId, string job, string industry, int price);
}