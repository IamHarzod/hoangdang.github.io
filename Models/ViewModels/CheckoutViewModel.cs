using System.ComponentModel.DataAnnotations;

namespace AnanasClone.Models;

public class CheckoutViewModel
{
    public Cart Cart { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
} 