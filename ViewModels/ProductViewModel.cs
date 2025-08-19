using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AnanasClone.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả sản phẩm")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
        public int DiscountPercentage { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng sản phẩm")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng sản phẩm phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; }

        // Ảnh chính
        public IFormFile MainImage { get; set; }
        public string CurrentMainImage { get; set; }

        // Các ảnh phụ
        public List<IFormFile> AdditionalImagesUpload { get; set; }
        public List<string> AdditionalImages { get; set; } = new List<string>();
        public List<string> CurrentAdditionalImages { get; set; }

        // Danh sách ảnh cần xóa
        public List<string> ImagesToDelete { get; set; }

        public string ImageUrl { get; set; }

        // Giá sau khi giảm
        public decimal DiscountedPrice => Price * (1 - DiscountPercentage / 100.0m);

        public string Status { get; set; }
        public string Style { get; set; }
        public string Line { get; set; }
    }
} 