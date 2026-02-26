namespace WholesaleOrderSystem.API.DTOs
{
    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        public required List<CreateOrderItemDto> Items { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public required string Status { get; set; } // Pending, Approved, Delivered, Rejected
    }
}
