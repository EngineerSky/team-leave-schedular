using System;
using LeaveScheduler.Domain.Enums;

namespace LeaveScheduler.Domain.Entities;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? StatusReason { get; set; }

    // Navigation property
    public Employee? Employee { get; set; }
}
