namespace WholesaleOrderSystem.API.DTOs
{
    public class ProductDto
    {
        public required string ProductName { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Unit { get; set; }
        public string? ImageUrl { get; set; }
    }
}
