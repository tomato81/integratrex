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
using System.IO;
using System.Xml;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Service
    /// </summary>
    public partial class Service : ServiceBase {

        // the integratrex
        private Integratrex m_Integratrex;

        // log
        private ILog SvcLog;
        private ILog SysLog;
        private ILog XmlLog;
        private ILog DebugLog;        

        // threading
        private Thread SvcMasterThread;

        // message queues
        private MessageQueue SysQ;
        private MessageQueue XmlQ;

        // message queue signals
        private AutoResetEvent SysQReset = new AutoResetEvent(true);
        private AutoResetEvent XmlQReset = new AutoResetEvent(true);

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
        /// Use this as a template!
        /// </summary>
        private void MethodTemplate() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);                
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

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
                // create the queues
                InitSysQ();
                InitXmlQ();              
                // fire up the integratrex
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
                    // check on queues
                    DoSystemQ();
                    DoXmlQ();                                        
                }
                // post-loop logic
                WindDown();
                // stop service
                Stop();
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
        /// MainLoop System Queue Activities
        /// </summary>
        private void DoSystemQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                if (SysQReset.WaitOne(0)) {
                    SysLog.Info("Waiting for message...");
                    SysQ.BeginReceive();
                }
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }

        /// <summary>
        /// MainLoop Xml Queue Activities
        /// </summary>
        private void DoXmlQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                if (XmlQReset.WaitOne(0)) {
                    XmlLog.Info("Waiting for message...");
                    XmlQ.BeginReceive();
                }
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }

        /// <summary>
        /// Initialize the system message queue
        /// </summary>
        private void InitSysQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // aquire the system queue
                if (!MessageQueue.Exists(Global.AppSettings.IntegratrexSysQueue)) {
                    SvcLog.FatalFormat("Message Queue \"{0}\" does not exist.", Global.AppSettings.IntegratrexSysQueue);
                    return;
                }
                // got it
                SysQ = new MessageQueue(Global.AppSettings.IntegratrexSysQueue);
                SysQ.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                // hook up event
                SysQ.ReceiveCompleted += SysQ_ReceiveCompleted;
                // log
                SysLog.Info("System Queue Ready");
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Initialize the XML message queue
        /// </summary>
        private void InitXmlQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // aquire the system queue
                if (!MessageQueue.Exists(Global.AppSettings.IntegratrexXmlQueue)) {
                    SvcLog.FatalFormat("Message Queue \"{0}\" does not exist.", Global.AppSettings.IntegratrexXmlQueue);
                    return;
                }
                // got it
                XmlQ = new MessageQueue(Global.AppSettings.IntegratrexXmlQueue);
                XmlQ.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                // hook up event
                XmlQ.ReceiveCompleted += XmlQ_ReceiveCompleted;
                // log
                XmlLog.Info("XML Queue Ready");
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// System Message Received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysQ_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                Thread.CurrentThread.Name = ThisMethod.Name;
                // the Q
                MessageQueue Q = (MessageQueue)sender;
                // get the message                 
                string message = Q.EndReceive(e.AsyncResult).Body.ToString();
                // log it
                SysLog.InfoFormat(Global.Messages.SysMessage, message);
                // check and action the message                    
                if (message.Equals("STOP", StringComparison.OrdinalIgnoreCase)) {
                    SvcShutdownEvent.Set();
                }
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                // let 'er rip
                SysQReset.Set();
            }
        }

        /// <summary>
        /// XML Message Received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XmlQ_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();            
            try {
                Thread.CurrentThread.Name = ThisMethod.Name;
                // the Q
                MessageQueue Q = (MessageQueue)sender;
                // get the message
                string message = e.Message.Body.ToString();
                // log it
                XmlLog.InfoFormat(Global.Messages.XmlMessage, message);
                // do XML things
                // done
                Q.EndReceive(e.AsyncResult);
            }
            catch (MessageQueueException ex) {
                if ((int)ex.MessageQueueErrorCode == -1073741536) {  // queue is closed
                    return;
                }
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                // let 'er rip
                XmlQReset.Set();
            }
        }

        /// <summary>
        /// Wind down the integratrex before shutting her down
        /// </summary>
        private void WindDown() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // stop listening to queues
                SysQ.Close();
                XmlQ.Close();
                // stop all integrations
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


#if (DEBUG)

        /// <summary>
        /// Debug Start
        /// </summary>
        public void Start() {
            OnStart(null);
        }

#endif
        
        /// <summary>
        /// On Stop
        /// </summary>
        protected override void OnStop() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                SvcLog.InfoFormat("OnStop thread={0}", Thread.CurrentThread.Name);
                // log
                SvcLog.InfoFormat(Global.Messages.ServiceEvent, ThisMethod.Name);
                // if the shutdown event has not been signaled, signal it!
                if (!SvcShutdownEvent.WaitOne(0)) {
                    SvcShutdownEvent.Set();
                }              
                // log service start
                SvcLog.Info("Service Stopped.");
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {                
#if (DEBUG)
                AppEntry.StopDebugService.Set();
#endif                
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
