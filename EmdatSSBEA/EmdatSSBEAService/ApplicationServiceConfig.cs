namespace EmdatSSBEAService
{
    public class ApplicationServiceConfig
    {
        public string DatabaseName { get; internal set; }
        public string SchemaName { get; internal set; }
        public string QueueName { get; internal set; }
        public string ExecutablePath { get; internal set; }
        public string WorkingDirectory { get; internal set; }
        public string CommandLineArguments { get; internal set; }
        public int MaxConcurrency { get; internal set; }
        public int MinConcurrency { get; internal set; }
    }
}