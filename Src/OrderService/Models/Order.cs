using System;

namespace OrderService.Models
{
    public record Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int JobId { get; set; }
        public int Price { get; set; }
        public int? ProfessionalId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime Timestamp { get; set; }

        public enum OrderStatus
        {
            Placed, WaitingForConfirmation, Approved, Rejected, Completed
        }
        public Order()
        {
        }
        public Order(int orderId, int userId, int jobId, int price, OrderStatus status, DateTime timestamp)
        {
            Id = orderId;
            UserId = userId;
            JobId = jobId;
            Price = price;
            Status = status;
            Timestamp = timestamp;
        }
    }
}