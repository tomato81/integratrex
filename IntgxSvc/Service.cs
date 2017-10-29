// Core
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Messaging;

// Other
using log4net;

// C2
using C2InfoSys.FileIntegratrex.Lib;


namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Service
    /// </summary>
    public partial class Service : ServiceBase {

        // the integratrex
        Integratrex m_Integratrex;

        // log
        private ILog SvcLog;
        private ILog SysLog;
        private ILog XmlLog;
        private ILog DebugLog;        

        // threading
        private Thread SvcMasterThread;
        private Thread SysQueueThread;
        private Thread XmlQueueThread;

        // shutdown trigger
        private ManualResetEvent SvcShutdownEvent = new ManualResetEvent(false);
        private ManualResetEvent MainLoopStartEvent = new ManualResetEvent(false);

        // integration switch
        private ManualResetEvent IntegrationInteruptEvent = new ManualResetEvent(true);

        /// <summary>
        /// Constructor
        /// </summary>
        public Service() {
            InitializeComponent();
            // setup trace sources
            DebugLog = LogManager.GetLogger(Global.DebugLogName);
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);
            SysLog = LogManager.GetLogger(Global.SysLogName);
            XmlLog = LogManager.GetLogger(Global.XmlLogName);            
        }        

        /// <summary>
        /// Setup the integratrex prior to starting the main loop
        /// </summary>
        private void PreRoll() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic                
                CalendarAccess.Instance.LoadCalendars(Global.AppSettings.BusinessCalendarFolder, false, Global.AppSettings.BusinessCalendarNamespace);
                // create the integratrex...
                m_Integratrex = new Integratrex(IntegrationInteruptEvent);
                m_Integratrex.ReadIntegrationConfig(Global.AppSettings.IntegratrexConfig, Global.AppSettings.IntegratrexConfigNamespace);
                // fire it up
                m_Integratrex.StartAllIntegrations();
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }
        
        /// <summary>
        /// Main Loop
        /// </summary>
        private void MainLoop() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // pre-loop logic
                PreRoll();
                MainLoopStartEvent.Set();   // release threads
                // looper
                int count = 0;
                while (!SvcShutdownEvent.WaitOne(Global.MainLoopCycleTime)) {                  
                    SvcLog.DebugFormat("MainLoop Iteration:{0}", ++count);
                    // check system queue thread
                    if (!SysQueueThread.IsAlive) {
                        // TODO: attempt to revive thread
                        SvcShutdownEvent.Set(); 
                    }
                    // check XML queue thread
                    if (!XmlQueueThread.IsAlive) {
                        // TODO: attempt to revive thread
                        SvcShutdownEvent.Set();
                    }                                        
                }
                // post-loop logic
                WindDown();                               
            }
            catch(Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                // shut 'er down?
                SvcShutdownEvent.Set();
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Wind down the integratrex before shutting her down
        /// </summary>
        private void WindDown() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // shut 'em down                
                m_Integratrex.StopAllIntegrations();
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }    
        }

        /// <summary>
        /// Monitor the sys queue ... perhaps this should just be the main loop because im not sure what the main loop does ... 
        /// </summary>
        private void SysLoop() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // aquire the system queue
                if (!MessageQueue.Exists(Global.AppSettings.IntegratrexSysQueue)) {                  
                    SvcLog.FatalFormat("Message Queue \"{0}\" does not exist.", Global.AppSettings.IntegratrexSysQueue);
                    return;
                }
                MessageQueue SysQ = new MessageQueue(Global.AppSettings.IntegratrexSysQueue);
                TimeSpan TimeOut = new TimeSpan(0, 0, Global.SysQueueTimeout);
                // don't do anything until the main loop starts looping
                if (!MainLoopStartEvent.WaitOne(Global.MainLoopStartTimeout))
                {
                    SvcLog.FatalFormat("System message thread failed waiting for the main service loop to start");                    
                    return;
                }
                // looper
                while (!SvcShutdownEvent.WaitOne(0)) {
                    Message SysMsg = null;
                    try {
                        SysMsg = SysQ.Receive(TimeOut);
                    }
                    catch (MessageQueueException ex) {
                        SvcLog.WarnFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);                        
                    }
                    catch (Exception ex) {
                        SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);                        
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Monitor the xml queue
        /// </summary>
        private void XmlLoop() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // aquire the system queue                
                if (!MessageQueue.Exists(Global.AppSettings.IntegratrexXmlQueue)) {
                    XmlLog.FatalFormat("Message Queue \"{0}\" does not exist.", Global.AppSettings.IntegratrexXmlQueue);                    
                    return;
                }
                MessageQueue XmlQ = new MessageQueue(Global.AppSettings.IntegratrexXmlQueue);
                TimeSpan TimeOut = new TimeSpan(0, 0, Global.XmlQueueTimeout);               
                // don't do anything until the main loop starts looping
                if (!MainLoopStartEvent.WaitOne(Global.MainLoopStartTimeout)) {
                    XmlLog.FatalFormat("XML message thread failed waiting for the main service loop to start");                    
                    return;
                }
                // looper
                while (!SvcShutdownEvent.WaitOne(0)) {
                    Message XmlMsg = null;
                    try {
                        XmlMsg = XmlQ.Receive(TimeOut);
                    }
                    catch(MessageQueueException ex) {
                        XmlLog.WarnFormat("Message Queue: {0} receive timeout after {1}. Message={2}", XmlQ.QueueName, Global.XmlQueueTimeout.ToString(), ex.Message);
                    }
                    catch(Exception ex) {
                        XmlLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
        }        

#if (DEBUG)

        /// <summary>
        /// Debug Start
        /// </summary>
        public void Start() {
            OnStart(null);
        }

#endif

        /// <summary>
        /// On Start
        /// </summary>
        /// <param name="args">arrrrrgs!</param>
        protected override void OnStart(string[] args) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {               
                // service master thread
                SvcMasterThread = new Thread(new ThreadStart(new Action(MainLoop)));
                SvcMasterThread.Name = Global.MasterThreadName;
                SvcMasterThread.IsBackground = true;
                SvcMasterThread.Priority = Global.SvcPriority;   // hm

                // system queue thread
                SysQueueThread = new Thread(new ThreadStart(new Action(SysLoop)));
                SysQueueThread.Name = Global.SysThreadName;
                SysQueueThread.IsBackground = true;
                SysQueueThread.Priority = Global.SvcPriority;   // hm

                // xml queue thread
                XmlQueueThread = new Thread(new ThreadStart(new Action(XmlLoop)));
                XmlQueueThread.Name = Global.XmlThreadName;
                XmlQueueThread.IsBackground = true;
                XmlQueueThread.Priority = Global.SvcPriority;   // hm                        

                // fire up the queues
                SysQueueThread.Start();
                XmlQueueThread.Start();

                // fire up the main thread
                SvcMasterThread.Start();

                // log service start
                SvcLog.Info("Service Started.");
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            } 
        }

        /// <summary>
        /// On Stop
        /// </summary>
        protected override void OnStop() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                SvcShutdownEvent.Set();
                if (!SvcMasterThread.Join(Global.ServiceStopWaitTime)) {
                    SvcMasterThread.Abort();
                }
                if (!SysQueueThread.Join(Global.ServiceStopWaitTime)) {
                    SysQueueThread.Abort();
                }
                if (!XmlQueueThread.Join(Global.ServiceStopWaitTime)) {
                    XmlQueueThread.Abort();
                }

                // log service start
                SvcLog.Info("Service Stopped.");
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                // hm
            }            
        }

        /// <summary>
        /// Pause
        /// </summary>
        protected override void OnPause() {
            SvcLog.Info("Service Paused.");
        }

        /// <summary>
        /// Play
        /// </summary>
        protected override void OnContinue() {
            SvcLog.Info("Service Resume.");
        }

        /// <summary>
        /// Shut it down
        /// </summary>
        protected override void OnShutdown() {
            OnStop();
        }

    }   // end class Service
}
