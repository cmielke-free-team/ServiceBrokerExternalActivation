using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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
            var notificationConfigs = new List<NotificationServiceConfig>();
            var applicationConfigs = new List<ApplicationServiceConfig>();
            try
            {
                Logger.TraceEvent(TraceEventType.Information, "Loading configuration...");
                var appDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
                FileInfo file = new FileInfo(appDirectory);
                var configPath = Path.Combine(file.Directory.Parent.FullName, @"Config\EAService.config");
                var eaConfig = XElement.Load(configPath);
                Logger.TraceEvent(TraceEventType.Information, "Loaded config from file.");
                XmlNamespaceManager nsman = new XmlNamespaceManager(new NameTable());
                XNamespace ea = "http://schemas.microsoft.com/sqlserver/2008/10/servicebroker/externalactivator";
                nsman.AddNamespace("ea", ea.ToString());
                var storedProc = eaConfig.Element(ea + "StoredProcedure").AsString();
                if (string.IsNullOrWhiteSpace(storedProc))
                {
                    throw new Exception("The configuration needs to include the StoredProcedure element as a child of the Activator element.");
                }

                var notificationElements = eaConfig.XPathSelectElements("ea:NotificationServiceList/ea:NotificationService", nsman);
                if (notificationElements == null || !notificationElements.Any())
                {
                    throw new Exception("The EAService.config was not configured with notification services.");
                }
                
                foreach (var notificationElement in notificationElements)
                {
                    var config = new NotificationServiceConfig()
                    {
                        StoredProcedure = storedProc,
                        ConnectionString = notificationElement.XPathSelectElement("ea:ConnectionString/ea:Unencrypted", nsman).AsString()
                    };
                    notificationConfigs.Add(config);
                }

                var applicationElements = eaConfig.XPathSelectElements("ea:ApplicationServiceList/ea:ApplicationService", nsman);
                if (applicationElements == null || !applicationElements.Any())
                {
                    throw new Exception("The EAService.config was not configured with application services.");
                }
                
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
                Logger.TraceEvent(TraceEventType.Information, "Configuration parsed successfully.");
            }
            catch(Exception ex)
            {
                Logger.TraceEvent(TraceEventType.Error, $"{ex}");
                throw ex;
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
