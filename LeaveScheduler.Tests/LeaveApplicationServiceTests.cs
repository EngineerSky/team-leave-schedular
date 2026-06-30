using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Application.Services;
using LeaveScheduler.Domain.Entities;
using LeaveScheduler.Domain.Enums;
using LeaveScheduler.Infrastructure.Persistence;
using Xunit;

namespace LeaveScheduler.Tests;

public class LeaveApplicationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public LeaveApplicationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private ApplicationDbContext CreateContext()
    {
        var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldSucceed_WhenNoOverlappingApproved()
    {
        using var context = CreateContext();
        var team = new Team { Name = "Test Team" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var employee = new Employee { Name = "John Doe", TeamId = team.Id, LeaveBalance = 30 };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var service = new LeaveApplicationService(context);
        var request = await service.SubmitLeaveRequestAsync(employee.Id, new DateTime(2026, 7, 6), new DateTime(2026, 7, 10));

        Assert.NotNull(request);
        Assert.Equal(LeaveStatus.Pending, request.Status);
        Assert.Equal(employee.Id, request.EmployeeId);
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldThrow_WhenOverlappingApprovedExists()
    {
        using var context = CreateContext();
        var team = new Team { Name = "Test Team" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var employee = new Employee { Name = "John Doe", TeamId = team.Id, LeaveBalance = 30 };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        // Approved request
        var approvedRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 7, 6),
            EndDate = new DateTime(2026, 7, 10),
            Status = LeaveStatus.Approved
        };
        context.LeaveRequests.Add(approvedRequest);
        await context.SaveChangesAsync();

        var service = new LeaveApplicationService(context);
        
        // Attempt to submit overlapping range: July 8 to July 12
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitLeaveRequestAsync(employee.Id, new DateTime(2026, 7, 8), new DateTime(2026, 7, 12)));
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldDeductOnlyWorkingDays()
    {
        using var context = CreateContext();
        
        // Friday July 3 is working. Sat/Sun July 4-5 are weekends. Monday July 6 is seeded as a holiday. Tuesday July 7 is working.
        context.PublicHolidays.Add(new PublicHoliday { Date = new DateTime(2026, 7, 6), Name = "Summer Bank Holiday" });
        
        var team = new Team { Name = "Dev Team" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var employee = new Employee { Name = "Alice", TeamId = team.Id, LeaveBalance = 30 };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 7, 3), // Friday
            EndDate = new DateTime(2026, 7, 7),   // Tuesday (total 5 calendar days, but only 2 working days)
            Status = LeaveStatus.Pending
        };
        context.LeaveRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new LeaveApplicationService(context);
        var (success, reason) = await service.ApproveLeaveRequestAsync(request.Id);

        Assert.True(success);
        
        // Verify balance decremented by 2 working days
        var updatedEmployee = await context.Employees.FindAsync(employee.Id);
        Assert.Equal(28, updatedEmployee!.LeaveBalance);
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldAutoRejectOverlappingPendingRequests()
    {
        using var context = CreateContext();
        var team = new Team { Name = "Dev Team" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var employee = new Employee { Name = "Bob", TeamId = team.Id, LeaveBalance = 30 };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        // Target request
        var requestToApprove = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 7, 6),
            EndDate = new DateTime(2026, 7, 10),
            Status = LeaveStatus.Pending
        };

        // Overlapping pending request
        var requestToAutoReject = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 7, 8),
            EndDate = new DateTime(2026, 7, 12),
            Status = LeaveStatus.Pending
        };

        // Non-overlapping pending request
        var requestToKeepPending = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 7, 13),
            EndDate = new DateTime(2026, 7, 17),
            Status = LeaveStatus.Pending
        };

        context.LeaveRequests.AddRange(requestToApprove, requestToAutoReject, requestToKeepPending);
        await context.SaveChangesAsync();

        var service = new LeaveApplicationService(context);
        var (success, _) = await service.ApproveLeaveRequestAsync(requestToApprove.Id);

        Assert.True(success);

        // Reload requests
        var approved = await context.LeaveRequests.FindAsync(requestToApprove.Id);
        var autoRejected = await context.LeaveRequests.FindAsync(requestToAutoReject.Id);
        var keptPending = await context.LeaveRequests.FindAsync(requestToKeepPending.Id);

        Assert.Equal(LeaveStatus.Approved, approved!.Status);
        Assert.Equal(LeaveStatus.Rejected, autoRejected!.Status);
        Assert.Equal("Overlapping approved request.", autoRejected.StatusReason);
        Assert.Equal(LeaveStatus.Pending, keptPending!.Status);
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldReject_WhenInsufficientLeaveBalance()
    {
        using var context = CreateContext();
        var team = new Team { Name = "Dev Team" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var employee = new Employee { Name = "Charlie", TeamId = team.Id, LeaveBalance = 2 };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 7, 6), // Monday
            EndDate = new DateTime(2026, 7, 8),   // Wednesday (3 working days)
            Status = LeaveStatus.Pending
        };
        context.LeaveRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new LeaveApplicationService(context);
        var (success, reason) = await service.ApproveLeaveRequestAsync(request.Id);

        Assert.False(success);
        Assert.Equal("Insufficient leave balance.", reason);

        var updatedRequest = await context.LeaveRequests.FindAsync(request.Id);
        Assert.Equal(LeaveStatus.Rejected, updatedRequest!.Status);
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldEnforce30PercentRule()
    {
        using var context = CreateContext();
        
        // Create a team of size 3 (Allowed limit = max(1, floor(3 * 0.3)) = 1)
        var team = new Team { Name = "Small Team" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var emp1 = new Employee { Name = "Alice", TeamId = team.Id, LeaveBalance = 30 };
        var emp2 = new Employee { Name = "Bob", TeamId = team.Id, LeaveBalance = 30 };
        var emp3 = new Employee { Name = "Charlie", TeamId = team.Id, LeaveBalance = 30 };
        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        // Alice has an APPROVED request for July 6
        var aliceRequest = new LeaveRequest
        {
            EmployeeId = emp1.Id,
            StartDate = new DateTime(2026, 7, 6),
            EndDate = new DateTime(2026, 7, 6),
            Status = LeaveStatus.Approved
        };

        // Bob submits a request for July 6
        var bobRequest = new LeaveRequest
        {
            EmployeeId = emp2.Id,
            StartDate = new DateTime(2026, 7, 6),
            EndDate = new DateTime(2026, 7, 6),
            Status = LeaveStatus.Pending
        };

        context.LeaveRequests.AddRange(aliceRequest, bobRequest);
        await context.SaveChangesAsync();

        var service = new LeaveApplicationService(context);
        
        // Attempt to approve Bob's request: should fail because limit is 1 and Alice is already approved
        var (success, reason) = await service.ApproveLeaveRequestAsync(bobRequest.Id);

        Assert.False(success);
        Assert.Contains("Capacity limit exceeded on 2026-07-06", reason);

        var updatedBobRequest = await context.LeaveRequests.FindAsync(bobRequest.Id);
        Assert.Equal(LeaveStatus.Rejected, updatedBobRequest!.Status);
    }
}
