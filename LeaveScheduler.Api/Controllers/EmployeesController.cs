using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Application.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace LeaveScheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public EmployeesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _context.Employees
            .Include(e => e.Team)
            .Select(e => new { e.Id, e.Name, e.LeaveBalance, TeamName = e.Team != null ? e.Team.Name : "No Team", e.TeamId })
            .ToListAsync();
        return Ok(employees);
    }
}
