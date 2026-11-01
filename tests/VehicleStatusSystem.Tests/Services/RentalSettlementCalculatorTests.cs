
using Xunit;
using VehicleStatusSystem.Services;

namespace VehicleStatusSystem.Tests;

public class RentalSettlementCalculatorTests
{
    [Theory]
    [InlineData(12, 1, 600)]
    [InlineData(24, 1, 600)]
    [InlineData(36, 2, 300)]
    [InlineData(48, 2, 300)]
    [InlineData(60, 3, 0)]
    [InlineData(72, 3, 0)]
    public void Calculate_ShouldRefundCorrectly(
        int usedHours,
        int expectedDays,
        int expectedRefund)
    {
        var handoverAt =
            new DateTime(
                2026, 10, 31, 8, 0, 0,
                DateTimeKind.Utc);

        var returnedAt =
            handoverAt.AddHours(usedHours);

        var result =
            RentalSettlementCalculator.Calculate(
                handoverAt,
                returnedAt,
                bookedDays: 3,
                dailyRate: 300m);

        Assert.Equal(
            expectedDays,
            result.ChargedDays);

        Assert.Equal(
            (decimal)expectedRefund,
            result.RefundAmount);
    }
}
