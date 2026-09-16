using System.ComponentModel.DataAnnotations;

namespace Workly.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public Role Role { get; set; }

        [Range(0, 10000000)]
        public decimal Salary { get; set; }

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
    }
}
