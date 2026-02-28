using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WholesaleOrderSystem.API.Data;
using WholesaleOrderSystem.API.Models;
using WholesaleOrderSystem.API.DTOs;

namespace WholesaleOrderSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, IWebHostEnvironment environment, ILogger<ProductsController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("GetProducts requested by {User}", User?.Identity?.Name ?? "anonymous");
            // Both Admin and Customer can view active products
            var products = await _context.Products.Where(p => p.IsActive).ToListAsync();
            _logger.LogInformation("GetProducts returned {Count} items", products.Count);
            return products;
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            _logger.LogInformation("GetProduct {Id} requested by {User}", id, User?.Identity?.Name ?? "anonymous");
            var product = await _context.Products.FindAsync(id);

            if (product == null || !product.IsActive)
            {
                _logger.LogWarning("GetProduct {Id} not found or inactive", id);
                return NotFound();
            }

            _logger.LogInformation("GetProduct {Id} returned", id);
            return product;
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Product>> PostProduct([FromForm] ProductDto dto, IFormFile? image)
        {
            _logger.LogInformation("PostProduct requested by {User} with name {ProductName}", User?.Identity?.Name ?? "anonymous", dto.ProductName);
            string? imageUrl = null;
            if (image != null)
            {
                var uploads = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploads, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                imageUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
            }

            var product = new Product
            {
                ProductName = dto.ProductName,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                Unit = dto.Unit ?? "pcs",
                ImageUrl = imageUrl ?? dto.ImageUrl
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created {ProductId} by {User}", product.ProductId, User?.Identity?.Name ?? "anonymous");
            return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] ProductDto dto, IFormFile? image)
        {
            _logger.LogInformation("PutProduct {Id} requested by {User}", id, User?.Identity?.Name ?? "anonymous");
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("PutProduct {Id} not found", id);
                return NotFound();
            }

            if (image != null)
            {
                var uploads = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                product.ImageUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
            }
            else if (!string.IsNullOrEmpty(dto.ImageUrl))
            {
                product.ImageUrl = dto.ImageUrl;
            }

            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.Unit = dto.Unit ?? "pcs";

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    _logger.LogWarning("PutProduct {Id} concurrency - product not found", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError("PutProduct {Id} concurrency exception", id);
                    throw;
                }
            }

            _logger.LogInformation("PutProduct {Id} updated successfully", id);
            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            _logger.LogInformation("DeleteProduct {Id} requested by {User}", id, User?.Identity?.Name ?? "anonymous");
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("DeleteProduct {Id} not found", id);
                return NotFound();
            }

            // Soft delete
            product.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("DeleteProduct {Id} soft-deleted", id);
            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
