
namespace VehicleStatusSystem.Caching;

public sealed class VehicleCacheGate
{
    private const int StripeCount = 64;

    private readonly SemaphoreSlim[] _locks =
        Enumerable.Range(0, StripeCount)
            .Select(_ => new SemaphoreSlim(1, 1))
            .ToArray();

    public SemaphoreSlim GetLock(int vehicleId)
    {
        int index = (vehicleId & int.MaxValue) % StripeCount;
        return _locks[index];
    }
}
