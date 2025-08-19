using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnanasClone.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, 100)]
    public int DiscountPercentage { get; set; }

    [NotMapped]
    public decimal DiscountedPrice => Price * (1 - DiscountPercentage / 100.0m);

    public string ImageUrl { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Thêm trường cho nhiều ảnh sản phẩm
    public ICollection<ProductImage> ProductImages { get; set; }

    public string Status { get; set; } // VD: Limited Edition, Online Only, Sale off, New Arrival
    public string Style { get; set; } // VD: Low Top, High Top, Slip-on, Mid Top, Mule
    public string Line { get; set; } // VD: Vintas, Urbas, Track 6, Patta
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public string ImageUrl { get; set; }
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
} 