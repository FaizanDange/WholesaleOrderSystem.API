using System;
using System.Collections.Generic;

namespace WholesaleOrderSystem.API.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public required string Status { get; set; } // Pending, Approved, Delivered, Rejected
        public decimal TotalAmount { get; set; }

        public User? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
