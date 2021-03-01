namespace OrderService.Models
{
    public record User(int id, string name, string location, UserType type);
}