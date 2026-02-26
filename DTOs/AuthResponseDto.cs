namespace WholesaleOrderSystem.API.DTOs
{
    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public required string Role { get; set; }
        public required string Name { get; set; }
    }
}
