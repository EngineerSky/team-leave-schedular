using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeaveScheduler.Application.Interfaces;
using LeaveScheduler.Application.Services;
using LeaveScheduler.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveScheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly LeaveApplicationService _service;

    public LeaveRequestsController(IApplicationDbContext context, LeaveApplicationService service)
    {
        _context = context;
        _service = service;
    }

    // GET /api/leaverequests?teamId=1
    [HttpGet]
    public async Task<IActionResult> GetLeaveRequests([FromQuery] int? teamId, [FromQuery] string? status)
    {
        var query = _context.LeaveRequests
            .Include(r => r.Employee)
            .AsQueryable();

        if (teamId.HasValue)
            query = query.Where(r => r.Employee!.TeamId == teamId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);

        var results = await query
            .OrderByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeName = r.Employee!.Name,
                r.StartDate,
                r.EndDate,
                Status = r.Status.ToString(),
                r.StatusReason
            })
            .ToListAsync();

        return Ok(results);
    }

    // GET /api/leaverequests/calendar?teamId=1&startDate=2026-07-01
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] int teamId, [FromQuery] DateTime? startDate)
    {
        try
        {
            var from = (startDate ?? DateTime.Today).Date;
            var calendar = await _service.GetTeamCalendarAsync(teamId, from, 30);
            return Ok(calendar);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // POST /api/leaverequests
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitLeaveRequestDto dto)
    {
        try
        {
            var request = await _service.SubmitLeaveRequestAsync(dto.EmployeeId, dto.StartDate, dto.EndDate);
            return CreatedAtAction(nameof(GetLeaveRequests), new { }, new
            {
                request.Id,
                request.EmployeeId,
                request.StartDate,
                request.EndDate,
                Status = request.Status.ToString()
            });
        }
        catch (ArgumentException ex)      { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex)   { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // POST /api/leaverequests/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var (success, reason) = await _service.ApproveLeaveRequestAsync(id);
            if (success) return Ok(new { message = reason });
            return UnprocessableEntity(new { message = reason });
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // POST /api/leaverequests/{id}/reject
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectLeaveRequestDto dto)
    {
        try
        {
            await _service.RejectLeaveRequestAsync(id, dto.Reason ?? "Rejected by manager.");
            return Ok(new { message = "Rejected successfully." });
        }
        catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}

public record SubmitLeaveRequestDto(int EmployeeId, DateTime StartDate, DateTime EndDate);
public record RejectLeaveRequestDto(string? Reason);
