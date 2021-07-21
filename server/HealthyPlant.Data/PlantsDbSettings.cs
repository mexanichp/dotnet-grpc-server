namespace HealthyPlant.Data
{
    public class PlantsDbSettings
    {
        public string DatabaseName { get; set; } = default!;
        public string UsersCollectionName { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;
        public string OldHistoryCollectionName { get; set; } = default!;
    }
}