using System;

namespace OLP.Core.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = "Student";
        public DateTime CreatedAt { get; set; }
    }
}
