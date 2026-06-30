using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Application.Interfaces;
using LeaveScheduler.Domain.Entities;

namespace LeaveScheduler.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Team -> Employees relationship
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Team)
            .WithMany(t => t.Employees)
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Employee -> LeaveRequests relationship
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(r => r.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
