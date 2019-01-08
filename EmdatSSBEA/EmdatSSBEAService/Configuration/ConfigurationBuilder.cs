using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmdatSSBEAService.Configuration
{
    public static class ServiceBuilder
    {
        public static List<NotificationServiceConfig> GetServices()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var servicesElement = config.SectionGroups["ExternalActivation"].SectionGroups["NotificationServices"];
            List<NotificationServiceConfig> notificationServices = new List<NotificationServiceConfig>();
            foreach (var serviceElement in servicesElement.Sections)
            {
                var applicationServices = new List<ApplicationServiceConfig>();
                foreach (var application in ((NotificationServiceSection)serviceElement).Applications)
                {
                    var appService = new ApplicationServiceConfig()
                    {
                        DatabaseName = ((ApplicationElement)application).DatabaseName.InnerText,
                        SchemaName = ((ApplicationElement)application).SchemaName.InnerText,
                        QueueName = ((ApplicationElement)application).QueueName.InnerText,
                        ExecutablePath = ((ApplicationElement)application).ExecutablePath.InnerText,
                        CommandLineArguments = ((ApplicationElement)application).CommandLineArguments.InnerText,
                        MaxConcurrency = int.Parse(((ApplicationElement)application).MaxConcurrency.InnerText)
                    };
                    applicationServices.Add(appService);
                }
                var notificationService = new NotificationServiceConfig()
                {
                    ConnectionString = ((NotificationServiceSection)serviceElement).ConnectionString.InnerText,
                    StoredProcedure = ((NotificationServiceSection)serviceElement).StoredProcedure.InnerText,
                    ApplicationServices = new ApplicationServiceList(applicationServices)
                };
                notificationServices.Add(notificationService);
            }
            return notificationServices;
        }
    }
}
