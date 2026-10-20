using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workly.Models
{
    public class User
    {
        public int Id { get; set; }

        [RegularExpression(
            @"^[a-zA-Z ]+$",
            ErrorMessage = "Full name can contain only English letters and spaces."
        )]
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Full name must be between 2 and 100 characters."
        )]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [NotMapped]
        [DataType(DataType.Password)]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Password must be between 8 and 100 characters."
        )]
        public string? Password { get; set; }


        [StringLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public Role Role { get; set; }

        [Range(0, 10000000)]
        public decimal Salary { get; set; }

        [Required]
        public SalaryType SalaryType { get; set; } = SalaryType.MonthlySalary;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Joining")]
        public DateTime DateOfJoining { get; set; }
    }
}
