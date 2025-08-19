using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AnanasClone.Data;
using AnanasClone.Models;

namespace AnanasClone.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;

    public CheckoutController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        var viewModel = new CheckoutViewModel
        {
            Cart = cart,
            ShippingAddress = string.Empty,
            PhoneNumber = string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessOrder(CheckoutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Create order
        var order = new Order
        {
            UserId = userId!,
            ShippingAddress = model.ShippingAddress,
            PhoneNumber = model.PhoneNumber,
            TotalAmount = cart.TotalAmount,
            Status = OrderStatus.Pending
        };

        _context.Orders.Add(order);

        // Create order items
        foreach (var item in cart.Items)
        {
            var orderItem = new OrderItem
            {
                Order = order,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };

            // Update product stock
            var product = item.Product;
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
            }
        }

        // Clear cart
        _context.CartItems.RemoveRange(cart.Items);

        await _context.SaveChangesAsync();

        return RedirectToAction("Confirmation", new { orderId = order.Id });
    }

    public async Task<IActionResult> Confirmation(int orderId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
} 