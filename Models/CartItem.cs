namespace AnanasClone.Models;

public class CartItem
{
    public int Id { get; set; }
    public string CartId { get; set; } = string.Empty;
    public Cart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
} 