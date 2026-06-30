using System.Collections.Generic;

namespace LeaveScheduler.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int LeaveBalance { get; set; }

    // Navigation properties
    public Team? Team { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
