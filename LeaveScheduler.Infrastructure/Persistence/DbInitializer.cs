using System;
using System.Linq;
using LeaveScheduler.Domain.Entities;

namespace LeaveScheduler.Infrastructure.Persistence;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.PublicHolidays.Any()) return; // already seeded

        // ── Public Holidays 2026 ──────────────────────────────────────────────
        context.PublicHolidays.AddRange(
            new PublicHoliday { Date = new DateTime(2026,  1,  1), Name = "New Year's Day" },
            new PublicHoliday { Date = new DateTime(2026,  4,  3), Name = "Good Friday" },
            new PublicHoliday { Date = new DateTime(2026,  4,  6), Name = "Easter Monday" },
            new PublicHoliday { Date = new DateTime(2026,  5,  4), Name = "Early May Bank Holiday" },
            new PublicHoliday { Date = new DateTime(2026,  5, 25), Name = "Spring Bank Holiday" },
            new PublicHoliday { Date = new DateTime(2026,  7,  4), Name = "Independence Day (USA)" },
            new PublicHoliday { Date = new DateTime(2026,  8, 31), Name = "Summer Bank Holiday" },
            new PublicHoliday { Date = new DateTime(2026,  9,  7), Name = "Labor Day (USA)" },
            new PublicHoliday { Date = new DateTime(2026, 11, 26), Name = "Thanksgiving Day (USA)" },
            new PublicHoliday { Date = new DateTime(2026, 12, 25), Name = "Christmas Day" },
            new PublicHoliday { Date = new DateTime(2026, 12, 28), Name = "Boxing Day (Observed)" }
        );

        // ── Teams ─────────────────────────────────────────────────────────────
        var engineering = new Team { Name = "Engineering" };  // 5 members → limit 1
        var operations  = new Team { Name = "Operations"  };  // 5 members → limit 1
        var finance     = new Team { Name = "Finance"     };  // 5 members → limit 1

        context.Teams.AddRange(engineering, operations, finance);
        context.SaveChanges(); // flush to get IDs

        // ── Employees (matches seed/employees.csv) ────────────────────────────
        context.Employees.AddRange(
            // Engineering
            new Employee { Name = "Alice Smith",      TeamId = engineering.Id, LeaveBalance = 30 },
            new Employee { Name = "Bob Jones",        TeamId = engineering.Id, LeaveBalance = 25 },
            new Employee { Name = "Charlie Brown",    TeamId = engineering.Id, LeaveBalance = 22 },
            new Employee { Name = "David Miller",     TeamId = engineering.Id, LeaveBalance = 30 },
            new Employee { Name = "Emma Davis",       TeamId = engineering.Id, LeaveBalance = 18 },
            // Operations
            new Employee { Name = "Frank Wilson",     TeamId = operations.Id,  LeaveBalance = 30 },
            new Employee { Name = "Grace Taylor",     TeamId = operations.Id,  LeaveBalance = 28 },
            new Employee { Name = "Henry Anderson",   TeamId = operations.Id,  LeaveBalance = 30 },
            new Employee { Name = "Ivy Thomas",       TeamId = operations.Id,  LeaveBalance = 15 },
            new Employee { Name = "Jack Jackson",     TeamId = operations.Id,  LeaveBalance = 30 },
            // Finance
            new Employee { Name = "Karen White",      TeamId = finance.Id,     LeaveBalance = 30 },
            new Employee { Name = "Leo Harris",       TeamId = finance.Id,     LeaveBalance = 27 },
            new Employee { Name = "Mia Martin",       TeamId = finance.Id,     LeaveBalance = 30 },
            new Employee { Name = "Nathan Thompson",  TeamId = finance.Id,     LeaveBalance = 20 },
            new Employee { Name = "Olivia Garcia",    TeamId = finance.Id,     LeaveBalance = 30 }
        );

        context.SaveChanges();
    }
}
