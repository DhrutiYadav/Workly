using Workly.Models;

namespace Workly.Models
{
    public class DashboardViewModel
    {
        // Welcome Section
        public string WelcomeName { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        // Dashboard Statistics
        public int TotalEmployees { get; set; }

        public int TotalManagers { get; set; }

        public int TotalDepartments { get; set; }

        public int RecentJoiners { get; set; }

        // Recently Joined Employees
        public List<RecentEmployeeViewModel> RecentEmployees { get; set; }
            = new List<RecentEmployeeViewModel>();

        // Department Overview
        public List<DepartmentSummaryViewModel> Departments { get; set; }
            = new List<DepartmentSummaryViewModel>();
    }

    public class RecentEmployeeViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateTime DateOfJoining { get; set; }
    }

    public class DepartmentSummaryViewModel
    {
        public string DepartmentName { get; set; } = string.Empty;

        public int EmployeeCount { get; set; }
    }
}
