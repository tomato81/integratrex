using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Configuration;

// log4net
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Global variables
    /// </summary>
    public static class Global {

        // high level globals
        public static readonly int ServiceStopWaitTime = 5000;  // 5 seconds
        public static readonly int MainLoopStartTimeout = 10000;    // 10 seconds
        public static readonly int MainLoopCycleTime = 10 * 1000;    // 1 second

        public static readonly int IntegrationInterruptWait = 5000;  // 5 seconds
        public static readonly int IntegrationInterruptTimeout = (5 * 60000);  // 5 minutes        
        public static readonly int SysQueueTimeout = 1;
        public static readonly int XmlQueueTimeout = 1;
                
        public static readonly string DebugLogName = "DebugLog";
        public static readonly string ServiceLogName = "ServiceLog";
        public static readonly string SysLogName = "SysQueueLog";
        public static readonly string XmlLogName = "XmlQueueLog";
        public static readonly string IntegrationLogName = "Integration";
        public static readonly string MasterThreadName = "Integratrex.MainLoop";
        public static readonly string SysThreadName = "Integratrex.SysQueue";
        public static readonly string XmlThreadName = "Integratrex.XmlQueue";
        public static SourceLevels ServiceLogLevel { get; set; }
        public static NetworkCredential SmtpCredential { get; set; }
        public static ThreadPriority SvcPriority = ThreadPriority.AboveNormal;
        

        public static readonly string WorkLogDir = "Log";
        public static readonly string WorkSupprtDir = "Support";
        public static readonly string WorkInstDir = "Inst";


        /// <summary>
        /// Message Templates
        /// </summary>
        public static class Messages {
            public static string EnterMethod = "Enter Method: {0}.{1}";
            public static string ExitMethod = "Exit Method: {0}.{1}";

            /// <summary>
            /// {1=Class}.{2=Method}; {0=Exception}; Message: {3=Message}
            /// </summary>
            public static string Exception = "{1}.{2}; {0}; Message: {3}";
        }   // Messages

        /// <summary>
        /// Error Messages
        /// </summary>
        public static class ErrMessage {

            // startup activities 0###
            public static string ERR0001 = "ERROR; Service Main Loop did not start in a timely manner.";

            // read config activities 1###
            public static string ERR1000 = "ERROR; Configuration file error.";
            public static string ERR1001 = "ERROR; Multiple integrations with Desc \"{0}\" are present in the configuration file. All integrations must have a unique description. Only the first integration will be scheduled, others are ignored.";

            // read calendar activities 3###

            // schedule setup activities 4###

            // integration intialization activities 5###
            public static string ERR5001 = "ERROR; Integration Source did not intialize correctly.";

            // integration run activities 6###

            // XML queue activities 7###
            
                
            // shutdown activities 9###       


            // configuration errors 1###_#
            public static string ERR1001guess
                = "ERROR; Multiple integrations with Desc \"{0}\" are present in the configuration file. All integrations must have a unique description. Only the first integration will be scheduled, others are ignored.";

        }

        /// <summary>
        /// Create a new file appender
        /// </summary>
        /// <param name="name">name of the appender</param>
        /// <param name="fileName">path to the log file</param>
        /// <returns></returns>
        public static IAppender CreateFileAppender(string p_appenderName, string p_logFile) {
            FileAppender appender = new FileAppender();
            appender.Name = p_appenderName;
            appender.File = p_logFile;
            appender.AppendToFile = true;

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%d [%t] %-5p %c [%x] - %m%n";
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        public static ILog CreateLogger(string p_loggerName) {
            return LogManager.GetLogger(p_loggerName);
        }

        // Add an appender to a logger
        public static void AddAppender(string p_loggerName, IAppender p_Appender) {
            ILog Log = LogManager.GetLogger(p_loggerName);
            Logger Logger = (Logger)Log.Logger;
            Logger.AddAppender(p_Appender);
        }

        /// <summary>
        /// Access to AppSettings
        /// </summary>
        public static class AppSettings {
            public static string IntegratrexConfig {
                get {
                    return ConfigurationManager.AppSettings["Integratrex.Config"];
                }
            }
            public static string IntegratrexConfigNamespace {
                get {
                    return ConfigurationManager.AppSettings["Integratrex.Config.Namespace"];
                }
            }  
            public static string BusinessCalendarFolder {
                get {
                    return ConfigurationManager.AppSettings["BusinessCalendar.Folder"];
                }
            }
            public static string BusinessCalendarNamespace {
                get {
                    return ConfigurationManager.AppSettings["BusinessCalendar.Namespace"];
                }
            }
            public static string IntegratrexSysQueue {
                get {
                    return ConfigurationManager.AppSettings["Integratrex.Sys.Queue"];
                }
            }
            public static string IntegratrexXmlQueue {
                get {
                    return ConfigurationManager.AppSettings["Integratrex.Xml.Queue"];
                }
            }
            public static string IntegratrexWorkFolder {
                get {
                    return ConfigurationManager.AppSettings["WorkFolder"];
                }
            }  
        }


    }   // Global
}
