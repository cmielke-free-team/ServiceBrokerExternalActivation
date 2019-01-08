using EmdatSSBEAService.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmdatSSBEAService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(params string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var notificationServices = ServiceBuilder.GetServices();
            var tasks = notificationServices
                .Select(c => new NotificationService(c))
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
