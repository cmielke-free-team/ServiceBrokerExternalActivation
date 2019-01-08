namespace EmdatSSBEAService
{
    public class NotificationServiceConfig
    {
        public NotificationServiceConfig()
        {
        }

        public string ConnectionString { get; internal set; }
        public string StoredProcedure { get; internal set; }
        public ApplicationServiceList ApplicationServices { get; internal set; } 
    }
}