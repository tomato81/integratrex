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
    /// Service Attribute
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceAttr<T> {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_attr"></param>
        public ServiceAttr(T p_attr, string p_units) {
            m_attr = p_attr;
            m_units = p_units;
        }

        /// <summary>
        /// Attribute Value
        /// </summary>
        public T Value {
            get => m_attr;
        }
        // attribute value
        private T m_attr;

        /// <summary>
        /// Units of Attr
        /// </summary>
        public string Units {
            get => m_units;
        }
        private string m_units;

        /// <summary>
        /// To String
        /// </summary>
        /// <returns>string representation of the object</returns>
        public override string ToString() {
            return string.Format("{0} {1}", Value, Units);
        }

    }   // Service Attr

    /// <summary>
    /// Global variables
    /// </summary>
    public static class Global {

        // high level globals
        public static readonly int ServiceStopWaitTime = 10 * 1000;  // 10 seconds
        //public static readonly int MainLoopStartTimeout = 10 * 1000;    // 10 seconds


        public static readonly ServiceAttr<int> MainLoopStartTimeout = new ServiceAttr<int>(10 * 1000, "millisecond(s)");

        public static readonly ServiceAttr<int> MainLoopCycleTime = new ServiceAttr<int>(1 * 1000, "millisecond(s)");
                

        //public static readonly int MainLoopCycleTime = 1 * 1000;    // 1 second(s)

        public static readonly int IntegrationInterruptWait = 5000;  // 5 seconds
        public static readonly int IntegrationInterruptTimeout = (5 * 60000);  // 5 minutes        
        public static readonly int SysQueueTimeout = 600;   // 10 minutes
        public static readonly int XmlQueueTimeout = 600;   // 10 minutes
                
        public static readonly string DebugLogName = "DebugLog";
        public static readonly string ServiceLogName = "ServiceLog";
        public static readonly string QueueLogName = "QueueLog";        
        public static readonly string IntegrationLogName = "Integration";
        public static readonly string MasterThreadName = "Integratrex.MainLoop";        
        public static SourceLevels ServiceLogLevel { get; set; }
        public static NetworkCredential SmtpCredential { get; set; }
        public static ThreadPriority SvcPriority = ThreadPriority.AboveNormal;
        

        public static readonly string WorkLogDir = "Log";
        public static readonly string WorkSupprtDir = "Support";
        public static readonly string WorkInstDir = "Inst";

        public static class SysQueue {

            public static readonly string STOP = "STOP";

        }


        /// <summary>
        /// Message Templates
        /// </summary>
        public static class Messages {       

            /// <summary>
            /// Debug Messages
            /// </summary>
            public static class Debug {

                /// <summary>
                /// {0=Class} {1=Method}
                /// </summary>
                public static string EnterMethod = "Enter Method: {0}.{1}";

                /// <summary>
                /// {0=Class} {1=Method}
                /// </summary>
                public static string ExitMethod = "Exit Method: {0}.{1}";
            }

            /// <summary>
            /// Error Messages
            /// </summary>
            public static class Error {

                /// <summary>
                /// {0=Exception} {1=Class} {2=Method} {3=Message}
                /// </summary>
                public static string Exception = "{1}.{2}; {0}; Message: {3}";


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
            /// Activity Messages
            /// </summary>
            public static class Activity {
            }

            public static class Integration {

                /// <summary>
                /// {0=Original Name} {1=Renamed File Name}
                /// </summary>
                public static readonly string SourceFileRenamed = "source file renamed from {0} to {1}";

                /// <summary>
                /// {0=Integration Source}
                /// </summary>
                public static readonly string SourceDeleteFiles = "{0} delete files";
                
                /// <summary>
                /// {0=Location}
                /// </summary>
                public static readonly string ResponseLocationCreated = "{0} has been created at the response location";

                /// <summary>
                /// {0=The file} {1=the target location}
                /// </summary>
                public static readonly string FileExistsAtTarget = "The file {0} exists at the target location {1}";

                /// <summary>
                /// {0=The overrwritten file}
                /// </summary>
                public static readonly string OverrwriteFileAtTarget = "The file {0} has been overrwritten at the target location";

                /// <summary>
                /// {0=Response Description}
                /// </summary>
                public static readonly string ResponseActionStarted = "Response [{0}] action started";

                /// <summary>
                /// {0=Response Description}
                /// </summary>
                public static readonly string ResponseActionComplete = "Response [{0}] action has completed";

                /// <summary>
                /// {0=the action}
                /// {1=the file}
                /// {2=the target location}
                /// </summary>
                public static readonly string ResponseFileActioned = "{0} {1} > {2}";
            }

            /// <summary>
            /// Service Messages
            /// </summary>
            public static class Service {

                /// <summary>
                /// No params
                /// </summary>
                public static string Shutdown = "System shutdown detected";

                /// <summary>
                /// No params
                /// </summary>
                public static string Paused = "Service paused";

                /// <summary>
                /// No params
                /// </summary>
                public static string Resume = "Service resumed";

                /// <summary>
                /// No params
                /// </summary>
                public static string Starting = "Service starting";

                /// <summary>
                /// No params
                /// </summary>
                public static string Started = "Service started";

                /// <summary>
                /// No params
                /// </summary>
                public static string Stopped = "Service stopped";

                /// <summary>
                /// No params
                /// </summary>
                public static string Stopping = "Service stopping";

                /// <summary>
                /// {0=current iteration}
                /// </summary>
                public static string MainLoopIterate = "MainLoop iteration: {0}";

                /// <summary>
                /// {0=Configuration Item Name} {1=Value} {2=Units}
                /// </summary>
                public static string ConfigurationItem = "Attribute:{0}={1} {2}";
            }

            /// <summary>
            /// Configuration Messages
            /// </summary>
            public static class Config {
            }

            /// <summary>
            /// System and XML Queue Messages
            /// </summary>
            public static class Queue {

                /// <summary>
                /// {0=Queue Name}
                /// </summary>
                public static string Opened = "{0} Opened";              

                /// <summary>
                /// {0=Queue Name}
                /// </summary>
                public static string Waiting = "{0} Waiting";

                /// <summary>
                /// {0=Queue Name}
                /// </summary>
                public static string Closed = "{0} Closed";

                /// <summary>
                /// {0=Queue Name} {1=Message}
                /// </summary>
                public static string MessageReceived = "{0} Message Received: {1}";

                /// <summary>
                /// {0=Queue Name}
                /// </summary>
                public static string DoesNotExist = "Queue: {0} does not exist";

                /// <summary>
                /// {0=Queue Name}
                /// </summary>
                public static string ClosedOnReceive = "Queue: {0} was closed while reading message";
            }



        }   // Messages

        /*
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
        */


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
