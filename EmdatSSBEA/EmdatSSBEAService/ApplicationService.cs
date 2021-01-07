using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace EmdatSSBEAService
{
    public class ApplicationService
    {
        private readonly string _databaseName;
        private readonly string _schemaName;
        private readonly string _queueName;
        private readonly string _executablePath;
        private readonly string _workingDirectory;
        private readonly string _commandLineArguments;
        private readonly Process[] _processes;
        private readonly int _minimumConcurrency;

        public ApplicationService(ApplicationServiceConfig config)
        {
            _databaseName = config.DatabaseName;
            _schemaName = config.SchemaName;
            _queueName = config.QueueName;
            _executablePath = config.ExecutablePath;
            _workingDirectory = config.WorkingDirectory;
            _commandLineArguments = config.CommandLineArguments;
            _processes = new Process[config.MaxConcurrency];
            _minimumConcurrency = config.MinConcurrency;
        }

        public void Execute()
        {
            bool newProcessStarted = TryLaunchProcess();
            if (!newProcessStarted)
            {
                throw new MaximumQueueReadersException($"The maximum number of processes are already running: {_processes.Length}");
            }
        }

        public void EnsureMinimumConcurrency()
        {
            if (_minimumConcurrency < 1)
            {
                return;
            }

            int activeProcessCount = GetActiveProcessCount();
            if (activeProcessCount < _minimumConcurrency)
            {
                Logger.TraceEvent(TraceEventType.Verbose, $"Queue {_databaseName}.{_schemaName}.{_queueName} has {activeProcessCount} active process(es). Minimum concurrency is {_minimumConcurrency}. Launcing additional process(es).");
                for (int i = activeProcessCount; i < _minimumConcurrency; i++)
                {
                    TryLaunchProcess();
                }
            }
        }

        private bool TryLaunchProcess()
        {
            lock (_processes)
            {
                bool newProcessStarted = false;
                for (int i = 0; i < _processes.Length && !newProcessStarted; i++)
                {
                    if (_processes[i] == null || _processes[i].HasExited)
                    {
                        Logger.TraceEvent(TraceEventType.Information,
                            $"Launching new process {_executablePath}{(string.IsNullOrWhiteSpace(_commandLineArguments) ? "." : " with arguments " + _commandLineArguments + ".")}");
                        var startInfo = new ProcessStartInfo
                        {
                            WorkingDirectory = _workingDirectory,
                            FileName = _executablePath,
                            Arguments = _commandLineArguments
                        };
                        _processes[i] = Process.Start(startInfo);
                        newProcessStarted = true;
                    }
                }
                return newProcessStarted;
            }
        }

        private int GetActiveProcessCount()
        {
            lock (_processes)
            {
                return _processes.Count(p => p != null && !p.HasExited);
            }
        }
    }
}