using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Application Entry
    /// </summary>
    public static class AppEntry {

        // event source
        public const string EventSource = "File Integratrex";

#if (DEBUG)
        public static ManualResetEvent StopDebugService = new ManualResetEvent(false);
#endif

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {

            try {
                // register as event source
                if (!EventLog.SourceExists(AppEntry.EventSource)) {
                    EventLog.CreateEventSource(AppEntry.EventSource, "Application");
                }
            }
            catch { }

#if (DEBUG)
            Service S = new Service();            
            S.Start();
            StopDebugService.WaitOne();
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new Service() };
            ServiceBase.Run(ServicesToRun);                       
#endif

        }

    }   // end class Program
}
