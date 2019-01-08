using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmdatSSBEAService
{
    public static class Logger
    {
        private static TraceSource _traceSource = new TraceSource("Emdat.SSBEA.Service");

        public static void TraceEvent(TraceEventType type, string message)
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0, message);
        }
    }
}
