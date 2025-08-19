using System.ComponentModel.DataAnnotations;

namespace AnanasClone.Models;

public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    [Required]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
} 