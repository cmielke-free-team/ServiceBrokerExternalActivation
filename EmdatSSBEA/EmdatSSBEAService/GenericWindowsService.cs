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
            foreach (var task in _tasks)
            {
                task.Start();
            }
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
            Task.WaitAll(_tasks);
        }
    }
}
