
namespace VehicleStatusSystem.Caching;

public static class CacheKeys
{
    public const string DashboardSummary =
        "dashboard:summary:v1";

    public static string VehicleNotFound(int vehicleId)
    {
        return $"vehicle:not-found:v1:{vehicleId}";
    }
}
