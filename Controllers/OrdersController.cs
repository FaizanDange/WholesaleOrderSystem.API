using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WholesaleOrderSystem.API.Data;
using WholesaleOrderSystem.API.DTOs;
using WholesaleOrderSystem.API.Models;

namespace WholesaleOrderSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Orders
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDto createDto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Invalid user token");
            }

            var order = new Order
            {
                UserId = userId,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                OrderItems = new List<OrderItem>(),
                TotalAmount = 0
            };

            foreach (var item in createDto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    return BadRequest($"Product with ID {item.ProductId} is not available.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Not enough stock for {product.ProductName}. Only {product.StockQuantity} available.");
                }

                // Adjust stock
                product.StockQuantity -= item.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                };

                order.OrderItems.Add(orderItem);
                order.TotalAmount += (item.Quantity * product.Price);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order created successfully", orderId = order.OrderId });
        }

        // GET: api/Orders/my-history
        [HttpGet("my-history")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetMyHistory()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.Status,
                    o.TotalAmount,
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.Product!.ProductName,
                        Unit = oi.Product.Unit,
                        oi.Quantity,
                        oi.Price
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/Orders
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    CustomerName = o.User!.Name,
                    o.OrderDate,
                    o.Status,
                    o.TotalAmount,
                    TotalItems = o.OrderItems.Count
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/Orders/customer/{userId}
        [HttpGet("customer/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetCustomerOrders(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.Status,
                    o.TotalAmount,
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.Product!.ProductName,
                        Unit = oi.Product.Unit,
                        oi.Quantity,
                        oi.Price
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // PATCH: api/Orders/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            string[] validStatuses = { "Pending", "Approved", "Delivered", "Rejected" };
            if (!validStatuses.Contains(statusDto.Status))
            {
                return BadRequest("Invalid status. Valid values: Pending, Approved, Delivered, Rejected");
            }

            order.Status = statusDto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Order status updated to {statusDto.Status}" });
        }
    }
}
