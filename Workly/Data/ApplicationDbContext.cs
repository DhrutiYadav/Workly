using Microsoft.EntityFrameworkCore;
using Workly.Models;

namespace Workly.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing Users configuration: preserve salary precision.
            modelBuilder.Entity<User>()
                .Property(u => u.Salary)
                .HasPrecision(18, 2);

            // Existing Users configuration: store Role as a string.
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            // Store attendance dates without a time component.
            modelBuilder.Entity<Attendance>()
                .Property(a => a.AttendanceDate)
                .HasColumnType("date");

            // Store attendance status as readable text.
            modelBuilder.Entity<Attendance>()
                .Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Link attendance records to existing employees.
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate attendance for the same employee
            // on the same date at the database level.
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.EmployeeId,
                    a.AttendanceDate
                })
                .IsUnique();
        }
    }
}