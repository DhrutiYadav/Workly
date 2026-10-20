using System.ComponentModel.DataAnnotations;

namespace Workly.Models
{
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [RegularExpression(
            @"^[a-zA-Z ]+$",
            ErrorMessage = "Full name can contain only English letters and spaces.")]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Password must be between 8 and 100 characters.")]
        public string? Password { get; set; }

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


