using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EmdatSSBEAService
{
    public class ApplicationServiceList
    {
        private readonly Dictionary<string, ApplicationService> _applicationServices;

        public ApplicationServiceList(IEnumerable<ApplicationServiceConfig> applicationServiceConfigs)
        {
            _applicationServices = applicationServiceConfigs
                .ToDictionary(
                    k => $"{k.DatabaseName}.{k.SchemaName}.{k.QueueName}".ToUpperInvariant(),
                    v => new ApplicationService(v));
        }

        internal void Activate(string databaseName, string schemaName, string objectName)
        {
            string key = $"{databaseName}.{schemaName}.{objectName}".ToUpperInvariant();
            if(_applicationServices.TryGetValue(key, out ApplicationService applicationService))
            {                
                applicationService.Execute();
            }            
            else
            {
                throw new QueueActivationException($"No application is configured for queue: {databaseName}.{schemaName}.{objectName}");
            }
        }

        internal void MonitorQueueReaders()
        {
            foreach(var applicationService in _applicationServices)
            {                                
                applicationService.Value?.EnsureMinimumConcurrency();
            }
        }
    }
}