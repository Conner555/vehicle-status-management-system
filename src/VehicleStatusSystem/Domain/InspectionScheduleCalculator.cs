
using System.Security.Cryptography;

namespace VehicleStatusSystem.Domain;

public static class InspectionScheduleCalculator
{
    private const int MaxMinutes = 12 * 60;

    public static (DateTime ScheduledAt, DateTime DeadlineAt)
        Generate(DateTime returnedAtUtc)
    {
        if (returnedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "ReturnedAt must use UTC.",
                nameof(returnedAtUtc));
        }

        // 随机生成 1~720 分钟的计划间隔
        int minutes = RandomNumberGenerator.GetInt32(
            1,
            MaxMinutes + 1);

        DateTime scheduledAt =
            returnedAtUtc.AddMinutes(minutes);

        DateTime deadlineAt =
            returnedAtUtc.AddHours(12);

        return (scheduledAt, deadlineAt);
    }
}
