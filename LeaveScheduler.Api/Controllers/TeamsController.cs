using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Application.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace LeaveScheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public TeamsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _context.Teams
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();
        return Ok(teams);
    }

    [HttpGet("{id}/employees")]
    public async Task<IActionResult> GetTeamEmployees(int id)
    {
        var employees = await _context.Employees
            .Where(e => e.TeamId == id)
            .Select(e => new { e.Id, e.Name, e.LeaveBalance })
            .ToListAsync();
        return Ok(employees);
    }
}
