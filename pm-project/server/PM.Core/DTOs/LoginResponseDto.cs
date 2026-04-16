namespace PM.Core.DTOs 
{
    public class LoginResponseDto
    {
        public Guid Id { get; set; }
        public string? Token { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string Username { get; set; } = null!;
    }
}