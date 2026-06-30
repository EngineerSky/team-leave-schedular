using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Application.Interfaces;
using LeaveScheduler.Domain.Entities;
using LeaveScheduler.Domain.Enums;
using LeaveScheduler.Domain.Common;

namespace LeaveScheduler.Application.Services;

public class LeaveApplicationService
{
    private readonly IApplicationDbContext _context;

    public LeaveApplicationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest> SubmitLeaveRequestAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException("Start date must be before or equal to end date.");
        }

        var employee = await _context.Employees
            .Include(e => e.LeaveRequests)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found.");
        }

        // Check if there is any approved request that overlaps
        var hasOverlappingApproved = employee.LeaveRequests.Any(r =>
            r.Status == LeaveStatus.Approved &&
            LeaveRules.DoDatesOverlap(r.StartDate, r.EndDate, startDate, endDate));

        if (hasOverlappingApproved)
        {
            throw new InvalidOperationException("Cannot submit request: overlaps with an already approved leave.");
        }

        var newRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Status = LeaveStatus.Pending
        };

        _context.LeaveRequests.Add(newRequest);
        await _context.SaveChangesAsync();

        return newRequest;
    }

    public async Task<(bool Success, string Reason)> ApproveLeaveRequestAsync(int requestId)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .ThenInclude(e => e!.Team)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new KeyNotFoundException($"Leave request with ID {requestId} not found.");
        }

        if (request.Status != LeaveStatus.Pending)
        {
            return (false, "Request is not in Pending status.");
        }

        var employee = request.Employee;
        if (employee == null || employee.Team == null)
        {
            return (false, "Employee or Team details missing.");
        }

        var holidays = await _context.PublicHolidays
            .Select(h => h.Date.Date)
            .ToListAsync();

        // 1. Calculate working days in request range
        var workingDays = LeaveRules.GetWorkingDaysInRange(request.StartDate, request.EndDate, holidays);
        var workingDaysCount = workingDays.Count;

        // If it's a request with 0 working days (e.g. only weekends/holidays)
        if (workingDaysCount == 0)
        {
            request.Status = LeaveStatus.Rejected;
            request.StatusReason = "Request contains 0 working days.";
            await _context.SaveChangesAsync();
            return (false, request.StatusReason);
        }

        // 2. Check Employee's Leave Balance
        if (employee.LeaveBalance < workingDaysCount)
        {
            request.Status = LeaveStatus.Rejected;
            request.StatusReason = "Insufficient leave balance.";
            await _context.SaveChangesAsync();
            return (false, request.StatusReason);
        }

        // 3. 30% Capacity Check (Evaluated at approval time per working day)
        var teamId = employee.TeamId;
        var teamSize = await _context.Employees.CountAsync(e => e.TeamId == teamId);
        var allowedLimit = LeaveRules.CalculateAllowedLimit(teamSize);

        // Get all approved requests for this team that intersect the request range
        var approvedRequests = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Employee!.TeamId == teamId && r.Status == LeaveStatus.Approved)
            .ToListAsync();

        foreach (var day in workingDays)
        {
            // Count team members on approved leave on this day (excluding current request)
            var countOnLeave = approvedRequests.Count(r => day >= r.StartDate && day <= r.EndDate);

            if (countOnLeave + 1 > allowedLimit)
            {
                request.Status = LeaveStatus.Rejected;
                request.StatusReason = $"Capacity limit exceeded on {day:yyyy-MM-dd}. Limit is {allowedLimit}, already approved: {countOnLeave}.";
                await _context.SaveChangesAsync();
                return (false, request.StatusReason);
            }
        }

        // 4. If checks pass, approve the request
        request.Status = LeaveStatus.Approved;
        request.StatusReason = "Approved by manager.";
        
        // Deduct from employee balance
        employee.LeaveBalance -= workingDaysCount;

        // 5. Overlapping pending requests for SAME employee are auto-rejected
        var overlappingPendingRequests = await _context.LeaveRequests
            .Where(r => r.EmployeeId == employee.Id &&
                        r.Status == LeaveStatus.Pending &&
                        r.Id != request.Id)
            .ToListAsync();

        foreach (var pr in overlappingPendingRequests)
        {
            if (LeaveRules.DoDatesOverlap(pr.StartDate, pr.EndDate, request.StartDate, request.EndDate))
            {
                pr.Status = LeaveStatus.Rejected;
                pr.StatusReason = "Overlapping approved request.";
            }
        }

        await _context.SaveChangesAsync();
        return (true, "Approved successfully.");
    }

    public async Task RejectLeaveRequestAsync(int requestId, string reason)
    {
        var request = await _context.LeaveRequests.FindAsync(requestId);
        if (request == null)
        {
            throw new KeyNotFoundException($"Leave request with ID {requestId} not found.");
        }

        if (request.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException("Can only reject pending requests.");
        }

        request.Status = LeaveStatus.Rejected;
        request.StatusReason = string.IsNullOrWhiteSpace(reason) ? "Rejected by manager." : reason;

        await _context.SaveChangesAsync();
    }

    public async Task<TeamCalendarDto> GetTeamCalendarAsync(int teamId, DateTime startDate, int daysCount)
    {
        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            throw new KeyNotFoundException($"Team with ID {teamId} not found.");
        }

        var endDate = startDate.AddDays(daysCount - 1);

        var employees = await _context.Employees
            .Where(e => e.TeamId == teamId)
            .ToListAsync();

        var employeeIds = employees.Select(e => e.Id).ToList();

        var approvedRequests = await _context.LeaveRequests
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.Status == LeaveStatus.Approved)
            .ToListAsync();

        var holidays = await _context.PublicHolidays
            .Where(h => h.Date >= startDate.Date && h.Date <= endDate.Date)
            .Select(h => h.Date.Date)
            .ToListAsync();

        var calendarDays = new List<CalendarDayDto>();
        var allowedLimit = LeaveRules.CalculateAllowedLimit(employees.Count);

        for (int i = 0; i < daysCount; i++)
        {
            var date = startDate.AddDays(i).Date;
            var isWorkingDay = LeaveRules.IsWorkingDay(date, holidays);

            var employeesOnLeave = new List<EmployeeLeaveDto>();
            if (isWorkingDay)
            {
                var requestsOnDay = approvedRequests.Where(r => date >= r.StartDate && date <= r.EndDate).ToList();
                foreach (var req in requestsOnDay)
                {
                    var emp = employees.First(e => e.Id == req.EmployeeId);
                    employeesOnLeave.Add(new EmployeeLeaveDto
                    {
                        EmployeeId = emp.Id,
                        EmployeeName = emp.Name
                    });
                }
            }

            calendarDays.Add(new CalendarDayDto
            {
                Date = date,
                IsWorkingDay = isWorkingDay,
                AllowedLimit = isWorkingDay ? allowedLimit : 0,
                EmployeesOnLeave = employeesOnLeave
            });
        }

        return new TeamCalendarDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            TeamSize = employees.Count,
            AllowedLimit = allowedLimit,
            Days = calendarDays
        };
    }
}

public class TeamCalendarDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public int AllowedLimit { get; set; }
    public List<CalendarDayDto> Days { get; set; } = new();
}

public class CalendarDayDto
{
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    public int AllowedLimit { get; set; }
    public List<EmployeeLeaveDto> EmployeesOnLeave { get; set; } = new();
}

public class EmployeeLeaveDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
}
