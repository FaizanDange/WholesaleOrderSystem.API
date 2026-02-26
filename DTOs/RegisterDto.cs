namespace WholesaleOrderSystem.API.DTOs
{
    public class RegisterDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string Role { get; set; } = "Customer"; // Admin or Customer
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }
}
