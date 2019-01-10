using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmdatSSBEAService
{
    public partial class GenericWindowsService : ServiceBase
    {
        private Task[] _tasks;
        private CancellationTokenSource _cancellationTokenSource;

        public GenericWindowsService(string serviceName, Task[] tasks, CancellationTokenSource cancellationTokenSource)
        {
            this.ServiceName = serviceName;
            _tasks = tasks.ToArray();
            _cancellationTokenSource = cancellationTokenSource;
        }

        protected override void OnStart(string[] args)
        {
            Logger.TraceEvent(TraceEventType.Information, $"Starting the service {this.ServiceName}.");
            foreach (var task in _tasks)
            {
                task.Start();
            }
        }

        protected override void OnStop()
        {
            Logger.TraceEvent(TraceEventType.Information, "Stopping the service...");
            _cancellationTokenSource.Cancel();
            try
            {
                Task.WaitAll(_tasks);
            }
            catch(AggregateException ex)
            {
                Logger.TraceEvent(TraceEventType.Information, $"{ex}");
                foreach(var exception in ex.InnerExceptions)
                {
                    Logger.TraceEvent(TraceEventType.Information, $"{exception}");
                }
            }
            catch(Exception ex)
            {
                Logger.TraceEvent(TraceEventType.Information, $"{ex}");
            }
            Logger.TraceEvent(TraceEventType.Information, "Stopped the service.");
        }
    }
}
