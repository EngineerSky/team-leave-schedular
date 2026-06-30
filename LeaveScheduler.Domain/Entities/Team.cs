using System.Collections.Generic;

namespace LeaveScheduler.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
