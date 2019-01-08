using System.Configuration;

namespace EmdatSSBEAService.Configuration
{
    public class ApplicationElement : ConfigurationElement
    {
        [ConfigurationProperty("AppName")]
        public ServiceElement AppName
        {
            get
            {
                return this["AppName"] as ServiceElement;
            }
            set
            {
                this["AppName"] = value;
            }
        }

        [ConfigurationProperty("DatabaseName")]
        public ServiceElement DatabaseName
        {
            get
            {
                return this["DatabaseName"] as ServiceElement;
            }
            set
            {
                this["DatabaseName"] = value;
            }
        }

        [ConfigurationProperty("SchemaName")]
        public ServiceElement SchemaName
        {
            get
            {
                return this["SchemaName"] as ServiceElement;
            }
            set
            {
                this["SchemaName"] = value;
            }
        }

        [ConfigurationProperty("QueueName")]
        public ServiceElement QueueName
        {
            get
            {
                return this["QueueName"] as ServiceElement;
            }
            set
            {
                this["QueueName"] = value;
            }
        }

        [ConfigurationProperty("ExecutablePath")]
        public ServiceElement ExecutablePath
        {
            get
            {
                return this["ExecutablePath"] as ServiceElement;
            }
            set
            {
                this["ExecutablePath"] = value;
            }
        }

        [ConfigurationProperty("CommandLineArguments")]
        public ServiceElement CommandLineArguments
        {
            get
            {
                return this["CommandLineArguments"] as ServiceElement;
            }
            set
            {
                this["CommandLineArguments"] = value;
            }
        }

        [ConfigurationProperty("MaxConcurrency")]
        public ServiceElement MaxConcurrency
        {
            get
            {
                return this["MaxConcurrency"] as ServiceElement;
            }
            set
            {
                this["MaxConcurrency"] = value;
            }
        }
    }
}