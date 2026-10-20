using System.ComponentModel.DataAnnotations;

namespace Workly.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an employee.")]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Attendance date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Attendance Date")]
        [NotFutureDate]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select an attendance status.")]
        [Display(Name = "Attendance Status")]
        public AttendanceStatus Status { get; set; }

        [StringLength(
            500,
            ErrorMessage = "Notes cannot exceed 500 characters."
        )]
        public string? Notes { get; set; }

        // Navigation property: the employee linked to this record.
        public User? Employee { get; set; }
    }
}