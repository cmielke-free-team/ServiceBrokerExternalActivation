using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace EmdatSSBEAService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(params string[] args)
        {
			var eaConfig = XElement.Load("EAService.config");
			XmlNamespaceManager nsman = new XmlNamespaceManager(new NameTable());
			XNamespace ea = "http://schemas.microsoft.com/sqlserver/2008/10/servicebroker/externalactivator";
			nsman.AddNamespace("ea", ea.ToString());
			var notificationElements = eaConfig.XPathSelectElements("ea:NotificationServiceList/ea:NotificationService", nsman);
			if (notificationElements == null || !notificationElements.Any())
			{
				throw new Exception("The EAService.config was not configured with notification services.");
			}
			var notificationConfigs = new List<NotificationServiceConfig>();
			foreach (var notificationElement in notificationElements)
			{
				var config = new NotificationServiceConfig()
				{
					StoredProcedure = notificationElement.Element(ea + "StoredProcedure").AsString(),
					ConnectionString = notificationElement.XPathSelectElement("ea:ConnectionString/ea:Unencrypted", nsman).AsString()
				};
				notificationConfigs.Add(config);
			}

			var applicationElements = eaConfig.XPathSelectElements("ea:ApplicationServiceList/ea:ApplicationService", nsman);
			if (applicationElements == null || !applicationElements.Any())
			{
				throw new Exception("The EAService.config was not configured with application services.");
			}
			var applicationConfigs = new List<ApplicationServiceConfig>();
			foreach (var applicationElement in applicationElements)
			{
				string maxConcVal = applicationElement.XPathSelectElement("ea:Concurrency", nsman)?.Attribute("max")?.Value;
				int maxConc = int.TryParse(maxConcVal, out maxConc) ? Math.Max(maxConc, 0) : 0;
				var config = new ApplicationServiceConfig()
				{
					DatabaseName = applicationElement.XPathSelectElement("ea:OnNotification/ea:DatabaseName", nsman).AsString(),
					SchemaName = applicationElement.XPathSelectElement("ea:OnNotification/ea:SchemaName", nsman).AsString(),
					QueueName = applicationElement.XPathSelectElement("ea:OnNotification/ea:QueueName", nsman).AsString(),
					ExecutablePath = applicationElement.XPathSelectElement("ea:LaunchInfo/ea:ImagePath", nsman).AsString(),
					CommandLineArguments = applicationElement.XPathSelectElement("ea:LaunchInfo/ea:CmdLineArgs", nsman).AsString(),
					MaxConcurrency = maxConc
				};
				applicationConfigs.Add(config);
			}

			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			var applicationServiceList = new ApplicationServiceList(applicationConfigs);
			var tasks = notificationConfigs
				.Select(c => new NotificationService(c, applicationServiceList))
				.Select(n => new Task(() => n.Execute(cancellationTokenSource.Token), TaskCreationOptions.LongRunning))
				.ToArray();

			if (args.Length == 0)
            {
                ExecuteAsWindowsService(tasks, cancellationTokenSource);
            }
            else if (args[0].Equals("--console", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteAsConsoleApplication(tasks, cancellationTokenSource);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationTokenSource"></param>
        private static void ExecuteAsConsoleApplication(Task[] tasks, CancellationTokenSource cancellationTokenSource)
        {
            //run as console app            
            Console.WriteLine("Running as a console app");
            Console.CancelKeyPress += (s, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };
            foreach(var task in tasks)
            {
                task.Start();
            }
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationTokenSource"></param>
        private static void ExecuteAsWindowsService(Task[] tasks, CancellationTokenSource cancellationTokenSource)
        {
            //run as service             
            ServiceBase[] ServicesToRun = new ServiceBase[]
            {
                new GenericWindowsService(
                    serviceName: "EmdatSSBEAService",
                    tasks: tasks,
                    cancellationTokenSource: cancellationTokenSource)
            };
            ServiceBase.Run(ServicesToRun);
        }        
    }
}
