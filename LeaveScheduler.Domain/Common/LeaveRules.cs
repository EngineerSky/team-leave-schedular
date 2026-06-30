using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaveScheduler.Domain.Common;

public static class LeaveRules
{
    public static int CalculateAllowedLimit(int teamSize)
    {
        return Math.Max(1, (int)Math.Floor(0.30 * teamSize));
    }

    public static bool IsWorkingDay(DateTime date, IEnumerable<DateTime> publicHolidays)
    {
        var dayOfWeek = date.DayOfWeek;
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        var dateOnly = date.Date;
        return !publicHolidays.Any(h => h.Date == dateOnly);
    }

    public static List<DateTime> GetWorkingDaysInRange(DateTime startDate, DateTime endDate, IEnumerable<DateTime> publicHolidays)
    {
        var workingDays = new List<DateTime>();
        var currentDate = startDate.Date;
        var endLimit = endDate.Date;

        while (currentDate <= endLimit)
        {
            if (IsWorkingDay(currentDate, publicHolidays))
            {
                workingDays.Add(currentDate);
            }
            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    public static bool DoDatesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1.Date <= end2.Date && start2.Date <= end1.Date;
    }
}
