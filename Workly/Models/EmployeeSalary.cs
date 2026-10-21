using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workly.Models
{
    public class EmployeeSalary
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Range(1, 12)]
        public int Month { get; set; }

        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(0, 31)]
        public int PresentDays { get; set; }

        [Range(0, 31)]
        public int HalfDays { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        public decimal Allowances { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        public decimal Deductions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        public decimal NetSalary { get; set; }

        [Required]
        public string PaymentStatus { get; set; } = "Pending";

        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }
    }
}