namespace VehicleManagementApi.Caching;

public static class CacheKeys
{
    public static class Customers
    {
        public const string All = "customers:all";

        public static string ById(int id) => $"customers:{id}";
    }

    public static class Vehicles
    {
        public const string All = "vehicles:all";

        public static string ById(int id) => $"vehicles:{id}";

        public static string ByCustomerId(int customerId) => $"customers:{customerId}:vehicles";
    }
}