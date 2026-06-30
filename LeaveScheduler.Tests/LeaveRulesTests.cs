using System;
using System.Collections.Generic;
using LeaveScheduler.Domain.Common;
using Xunit;

namespace LeaveScheduler.Tests;

public class LeaveRulesTests
{
    [Theory]
    [InlineData(1, 1)] // 30% of 1 is 0.3 -> floor 0 -> max(1,0) = 1
    [InlineData(2, 1)] // 30% of 2 is 0.6 -> floor 0 -> max(1,0) = 1
    [InlineData(3, 1)] // 30% of 3 is 0.9 -> floor 0 -> max(1,0) = 1
    [InlineData(4, 1)] // 30% of 4 is 1.2 -> floor 1 -> max(1,1) = 1
    [InlineData(5, 1)] // 30% of 5 is 1.5 -> floor 1 -> max(1,1) = 1
    [InlineData(6, 1)] // 30% of 6 is 1.8 -> floor 1 -> max(1,1) = 1
    [InlineData(7, 2)] // 30% of 7 is 2.1 -> floor 2 -> max(1,2) = 2
    [InlineData(8, 2)] // 30% of 8 is 2.4 -> floor 2 -> max(1,2) = 2
    [InlineData(9, 2)] // 30% of 9 is 2.7 -> floor 2 -> max(1,2) = 2
    [InlineData(10, 3)] // 30% of 10 is 3.0 -> floor 3 -> max(1,3) = 3
    public void CalculateAllowedLimit_ShouldReturnExpectedLimit(int teamSize, int expectedLimit)
    {
        var result = LeaveRules.CalculateAllowedLimit(teamSize);
        Assert.Equal(expectedLimit, result);
    }

    [Fact]
    public void IsWorkingDay_ShouldReturnFalseForWeekends()
    {
        var saturday = new DateTime(2026, 7, 4); // Saturday
        var sunday = new DateTime(2026, 7, 5); // Sunday
        var monday = new DateTime(2026, 7, 6); // Monday

        var holidays = new List<DateTime>();

        Assert.False(LeaveRules.IsWorkingDay(saturday, holidays));
        Assert.False(LeaveRules.IsWorkingDay(sunday, holidays));
        Assert.True(LeaveRules.IsWorkingDay(monday, holidays));
    }

    [Fact]
    public void IsWorkingDay_ShouldReturnFalseForPublicHolidays()
    {
        var holiday = new DateTime(2026, 7, 6);
        var holidays = new List<DateTime> { holiday };

        Assert.False(LeaveRules.IsWorkingDay(holiday, holidays));
    }

    [Fact]
    public void GetWorkingDaysInRange_ShouldExcludeWeekendsAndHolidays()
    {
        // Friday July 3 to Tuesday July 7, 2026
        // Friday July 3: Working
        // Saturday July 4: Weekend
        // Sunday July 5: Weekend
        // Monday July 6: Holiday
        // Tuesday July 7: Working
        var startDate = new DateTime(2026, 7, 3);
        var endDate = new DateTime(2026, 7, 7);

        var holidays = new List<DateTime> { new DateTime(2026, 7, 6) };

        var workingDays = LeaveRules.GetWorkingDaysInRange(startDate, endDate, holidays);

        Assert.Equal(2, workingDays.Count);
        Assert.Contains(new DateTime(2026, 7, 3), workingDays);
        Assert.Contains(new DateTime(2026, 7, 7), workingDays);
    }

    [Theory]
    [InlineData("2026-07-01", "2026-07-05", "2026-07-04", "2026-07-10", true)]  // Overlap at end
    [InlineData("2026-07-05", "2026-07-10", "2026-07-01", "2026-07-05", true)]  // Overlap at start (single day)
    [InlineData("2026-07-01", "2026-07-05", "2026-07-06", "2026-07-10", false)] // No overlap
    [InlineData("2026-07-05", "2026-07-05", "2026-07-05", "2026-07-05", true)]  // Same day overlap
    public void DoDatesOverlap_ShouldIdentifyOverlapsCorrectly(string s1, string e1, string s2, string e2, bool expectedOverlap)
    {
        var start1 = DateTime.Parse(s1);
        var end1 = DateTime.Parse(e1);
        var start2 = DateTime.Parse(s2);
        var end2 = DateTime.Parse(e2);

        var result = LeaveRules.DoDatesOverlap(start1, end1, start2, end2);
        Assert.Equal(expectedOverlap, result);
    }
}
