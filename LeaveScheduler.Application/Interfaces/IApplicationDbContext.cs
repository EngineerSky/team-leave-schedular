using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Domain.Entities;

namespace LeaveScheduler.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Team> Teams { get; }
    DbSet<Employee> Employees { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<PublicHoliday> PublicHolidays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
