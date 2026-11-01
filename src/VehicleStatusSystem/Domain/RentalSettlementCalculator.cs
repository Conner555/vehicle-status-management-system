namespace VehicleStatusSystem.Domain;

public record RentalSettlementResult(
    int BookedDays,
    int ChargedDays,
    decimal OriginalAmount,
    decimal FinalAmount,
    decimal RefundAmount
);

public static class RentalSettlementCalculator
{
    public static RentalSettlementResult Calculate(
        DateTime actualHandedOverAt,
        DateTime actualReturnedAt,
        int bookedDays,
        decimal dailyRate)
    {
        // 1. 参数校验
        if (bookedDays <= 0)
            throw new ArgumentException(
                "Booked days must be positive.");

        if (dailyRate < 0)
            throw new ArgumentException(
                "Daily rate cannot be negative.");

        if (actualReturnedAt < actualHandedOverAt)
            throw new ArgumentException(
                "Return time cannot be before handover.");

        // 2. 实际使用时长
        TimeSpan duration =
            actualReturnedAt - actualHandedOverAt;

        // 3. 不足一天按一天收费
        int actualDays = Math.Max(
            1,
            (int)Math.Ceiling(duration.TotalDays));

        // 4. 提前归还结算
        int chargedDays =
            Math.Min(bookedDays, actualDays);

        // 5. 计算退款
        decimal originalAmount =
            bookedDays * dailyRate;

        decimal finalAmount =
            chargedDays * dailyRate;

        decimal refundAmount =
            originalAmount - finalAmount;

        return new RentalSettlementResult(
            bookedDays,
            chargedDays,
            originalAmount,
            finalAmount,
            refundAmount
        );
    }
}
