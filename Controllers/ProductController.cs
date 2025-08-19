using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnanasClone.Data;
using AnanasClone.Models;
using AnanasClone.ViewModels;

namespace AnanasClone.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Index(string category, string status, string style, string line, decimal? priceMin, decimal? priceMax)
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        // Lấy filter động
        ViewBag.Categories = await _context.Categories.Select(c => c.Name).Distinct().ToListAsync();
        ViewBag.Statuses = await _context.Products.Select(p => p.Status).Distinct().ToListAsync();
        ViewBag.Styles = await _context.Products.Select(p => p.Style).Distinct().ToListAsync();
        ViewBag.Lines = await _context.Products.Select(p => p.Line).Distinct().ToListAsync();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.Name.ToLower() == category.ToLower());
        }
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status.ToLower() == status.ToLower());
        }
        if (!string.IsNullOrEmpty(style))
        {
            query = query.Where(p => p.Style.ToLower() == style.ToLower());
        }
        if (!string.IsNullOrEmpty(line))
        {
            query = query.Where(p => p.Line.ToLower() == line.ToLower());
        }
        if (priceMin.HasValue)
        {
            query = query.Where(p => p.Price >= priceMin.Value);
        }
        if (priceMax.HasValue)
        {
            query = query.Where(p => p.Price <= priceMax.Value);
        }

        var products = await query.ToListAsync();
        var productViewModels = products.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            DiscountPercentage = p.DiscountPercentage,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            StockQuantity = p.StockQuantity,
            ImageUrl = p.ImageUrl,
            Status = p.Status,
            Style = p.Style,
            Line = p.Line
        }).ToList();

        ViewBag.SelectedCategory = category;
        ViewBag.SelectedStatus = status;
        ViewBag.SelectedStyle = style;
        ViewBag.SelectedLine = line;
        ViewBag.SelectedPriceMin = priceMin;
        ViewBag.SelectedPriceMax = priceMax;

        return View(productViewModels);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        // Map sang ProductViewModel
        var viewModel = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            DiscountPercentage = product.DiscountPercentage,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            Status = product.Status,
            Style = product.Style,
            Line = product.Line,
            AdditionalImages = product.ProductImages?.Where(i => !i.IsMain).Select(i => i.ImageUrl).ToList()
        };

        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewBag.Categories = _context.Categories.ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (ModelState.IsValid)
        {
            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                DiscountPercentage = model.DiscountPercentage,
                CategoryId = model.CategoryId,
                StockQuantity = model.StockQuantity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Upload ảnh chính
            if (model.MainImage != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.MainImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.MainImage.CopyToAsync(fileStream);
                }

                product.ImageUrl = "/images/products/" + uniqueFileName;
            }

            _context.Add(product);
            await _context.SaveChangesAsync();

            // Upload các ảnh phụ
            if (model.AdditionalImagesUpload != null && model.AdditionalImagesUpload.Any())
            {
                foreach (var image in model.AdditionalImagesUpload)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    var productImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = "/images/products/" + uniqueFileName,
                        IsMain = false,
                        DisplayOrder = 0
                    };

                    _context.ProductImages.Add(productImage);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        else
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            // Ghi log lỗi ra console để debug
            System.Diagnostics.Debug.WriteLine("ModelState Errors: " + string.Join(" | ", errors));
        }
        ViewBag.Categories = _context.Categories.ToList();
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        var model = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            DiscountPercentage = product.DiscountPercentage,
            CategoryId = product.CategoryId,
            StockQuantity = product.StockQuantity,
            CurrentMainImage = product.ImageUrl,
            CurrentAdditionalImages = product.ProductImages
                .Where(pi => !pi.IsMain)
                .Select(pi => pi.ImageUrl)
                .ToList()
        };

        ViewBag.Categories = _context.Categories.ToList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound();
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.DiscountPercentage = model.DiscountPercentage;
                product.CategoryId = model.CategoryId;
                product.StockQuantity = model.StockQuantity;
                product.UpdatedAt = DateTime.UtcNow;

                // Xử lý ảnh chính mới
                if (model.MainImage != null)
                {
                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                            product.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Upload ảnh mới
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.MainImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.MainImage.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }

                // Xử lý các ảnh phụ mới
                if (model.AdditionalImagesUpload != null && model.AdditionalImagesUpload.Any())
                {
                    foreach (var image in model.AdditionalImagesUpload)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/products/" + uniqueFileName,
                            IsMain = false,
                            DisplayOrder = 0
                        };

                        _context.ProductImages.Add(productImage);
                    }
                }

                // Xóa các ảnh phụ đã chọn
                if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
                {
                    foreach (var imageUrl in model.ImagesToDelete)
                    {
                        var imageToDelete = product.ProductImages
                            .FirstOrDefault(pi => pi.ImageUrl == imageUrl);
                        
                        if (imageToDelete != null)
                        {
                            string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                                imageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            _context.ProductImages.Remove(imageToDelete);
                        }
                    }
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Categories = _context.Categories.ToList();
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product != null)
        {
            // Xóa ảnh chính
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                    product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            // Xóa các ảnh phụ
            foreach (var image in product.ProductImages)
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                    image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    [HttpGet]
    public async Task<IActionResult> ByCategory(int categoryId)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DiscountPercentage = p.DiscountPercentage,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync();

        return View("Index", products);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SeedSample()
    {
        var products = new List<Product>
        {
            new Product { Name = "Urbas SC - Mule Foliage", Description = "Urbas SC - Mule Foliage", Price = 580000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/foliage.jpg", Status = "", Style = "Mule", Line = "Urbas" },
            new Product { Name = "Urbas SC - Mule Aloe Wash", Description = "Urbas SC - Mule Aloe Wash", Price = 350000, DiscountPercentage = 40, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/aloe-wash.jpg", Status = "Sale off", Style = "Mule", Line = "Urbas" },
            new Product { Name = "Urbas SC - Mule Dusty Blue", Description = "Urbas SC - Mule Dusty Blue", Price = 350000, DiscountPercentage = 40, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/dusty-blue.jpg", Status = "Sale off", Style = "Mule", Line = "Urbas" },
            new Product { Name = "Urbas SC - Mule Fair Orchid", Description = "Urbas SC - Mule Fair Orchid", Price = 580000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/fair-orchid.jpg", Status = "", Style = "Mule", Line = "Urbas" },
            new Product { Name = "Urbas SC - Mule Cornsilk", Description = "Urbas SC - Mule Cornsilk", Price = 580000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/cornsilk.jpg", Status = "", Style = "Mule", Line = "Urbas" },
            new Product { Name = "Urbas SC - Mule Cosmic", Description = "Urbas SC - Mule Cosmic", Price = 580000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/cosmic.jpg", Status = "Online Only", Style = "Mule", Line = "Urbas" },
            new Product { Name = "Urbas SC - High Top Foliage", Description = "Urbas SC - High Top Foliage", Price = 650000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/foliage.jpg", Status = "", Style = "High Top", Line = "Urbas" },
            new Product { Name = "Urbas SC - High Top Dusty Blue", Description = "Urbas SC - High Top Dusty Blue", Price = 350000, DiscountPercentage = 46, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/dusty-blue.jpg", Status = "Sale off", Style = "High Top", Line = "Urbas" },
            new Product { Name = "Urbas SC - High Top Fair Orchid", Description = "Urbas SC - High Top Fair Orchid", Price = 650000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/fair-orchid.jpg", Status = "", Style = "High Top", Line = "Urbas" },
            new Product { Name = "Urbas SC - High Top Aloe Wash", Description = "Urbas SC - High Top Aloe Wash", Price = 350000, DiscountPercentage = 46, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/aloe-wash.jpg", Status = "Sale off", Style = "High Top", Line = "Urbas" },
            new Product { Name = "Urbas SC - High Top Cornsilk", Description = "Urbas SC - High Top Cornsilk", Price = 650000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/cornsilk.jpg", Status = "", Style = "High Top", Line = "Urbas" },
            new Product { Name = "Urbas SC - High Top Cosmic", Description = "Urbas SC - High Top Cosmic", Price = 650000, DiscountPercentage = 0, CategoryId = 1, StockQuantity = 10, ImageUrl = "/images/products/cosmic.jpg", Status = "Online Only", Style = "High Top", Line = "Urbas" },
        };
        foreach (var p in products)
        {
            p.IsActive = true;
            p.CreatedAt = DateTime.UtcNow;
        }
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
        return Content($"Đã seed {products.Count} sản phẩm mẫu!");
    }
} 