using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleOrderSystem.API.Data;

namespace WholesaleOrderSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _context.Users
                .Where(u => u.Role == "Customer")
                .Select(u => new {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.CreatedDate,
                    u.IsActive,
                    u.PhoneNumber,
                    u.Address,
                    OrderCount = _context.Orders.Count(o => o.UserId == u.UserId)
                })
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _context.Users
                .Where(u => u.Role == "Admin")
                .Select(u => new {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.CreatedDate
                })
                .ToListAsync();

            return Ok(admins);
        }
    }
}
