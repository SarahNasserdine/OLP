namespace OLP.Core.DTOs
{
    public class UpdateUserDto
    {
        public string FullName { get; set; } = null!;
        public bool? IsActive { get; set; } // Admin / SuperAdmin only
    }
}
