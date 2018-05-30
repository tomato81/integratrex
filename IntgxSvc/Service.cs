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
        private ILog QueueLog;        
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
            QueueLog = LogManager.GetLogger(Global.QueueLogName);            
        }

        /// <summary>
        /// Use this as a template!
        /// </summary>
        private void MethodTemplate() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);                
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// On Start
        /// </summary>
        /// <param name="args">arrrrrgs!</param>
        protected override void OnStart(string[] args) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {

                // log service start
                SvcLog.InfoFormat(Global.Messages.Service.Starting);

                // service master thread
                SvcMasterThread = new Thread(new ThreadStart(new Action(MainLoop)));
                SvcMasterThread.Name = Global.MasterThreadName;
                SvcMasterThread.IsBackground = true;
                SvcMasterThread.Priority = Global.SvcPriority;   // hm

                // fire up the main thread
                SvcMasterThread.Start();

                // log service start
                SvcLog.InfoFormat(Global.Messages.Service.Started);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                try {                                        
                    Stop();
                }
                catch { }                
            }
        }

        /// <summary>
        /// Setup the integratrex prior to starting the main loop
        /// </summary>
        private void PreRoll() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // log 
                LogServiceConfig();
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
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }

        /// <summary>
        /// Log service attributes
        /// </summary>
        private void LogServiceConfig() {

            

            SvcLog.DebugFormat(Global.Messages.Service.ConfigurationItem, "MainLoopCycleTime", Global.MainLoopCycleTime.Value, Global.MainLoopCycleTime.Units);
        }

        /// <summary>
        /// Main Loop
        /// </summary>
        private void MainLoop() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // pre-loop logic
                PreRoll();
                MainLoopStartEvent.Set();   // release threads
                // looper
                int count = 0;
                while (!SvcShutdownEvent.WaitOne(Global.MainLoopCycleTime.Value)) {                  
                    SvcLog.DebugFormat(Global.Messages.Service.MainLoopIterate, ++count);
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
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                // shut 'er down?
                SvcShutdownEvent.Set();
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// MainLoop System Queue Activities
        /// </summary>
        private void DoSystemQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                if (SysQReset.WaitOne(0)) {                    
                    SysQ.BeginReceive();
                    QueueLog.InfoFormat(Global.Messages.Queue.Waiting, SysQ.QueueName);
                }
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }

        /// <summary>
        /// MainLoop Xml Queue Activities
        /// </summary>
        private void DoXmlQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                if (XmlQReset.WaitOne(0)) {                                       
                    XmlQ.BeginReceive();
                    QueueLog.InfoFormat(Global.Messages.Queue.Waiting, XmlQ.QueueName);
                }
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }

        /// <summary>
        /// Initialize the system message queue
        /// </summary>
        private void InitSysQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // aquire the system queue
                if (!MessageQueue.Exists(Global.AppSettings.IntegratrexSysQueue)) {
                    SvcLog.ErrorFormat(Global.Messages.Queue.DoesNotExist, Global.AppSettings.IntegratrexSysQueue);
                    return;
                }
                // got it
                SysQ = new MessageQueue(Global.AppSettings.IntegratrexSysQueue);
                SysQ.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                // hook up event
                SysQ.ReceiveCompleted += SysQ_ReceiveCompleted;
                // log
                QueueLog.InfoFormat(Global.Messages.Queue.Opened, SysQ.QueueName);
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                
            }
        }

        /// <summary>
        /// Initialize the XML message queue
        /// </summary>
        private void InitXmlQ() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // aquire the system queue
                if (!MessageQueue.Exists(Global.AppSettings.IntegratrexXmlQueue)) {
                    SvcLog.ErrorFormat(Global.Messages.Queue.DoesNotExist, Global.AppSettings.IntegratrexXmlQueue);
                    return;
                }
                // got it
                XmlQ = new MessageQueue(Global.AppSettings.IntegratrexXmlQueue);
                XmlQ.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                // hook up event
                XmlQ.ReceiveCompleted += XmlQ_ReceiveCompleted;
                // log
                QueueLog.InfoFormat(Global.Messages.Queue.Opened, XmlQ.QueueName);                
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
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
                QueueLog.InfoFormat(Global.Messages.Queue.MessageReceived, Q.QueueName, message);
                // check and action the message                    
                if (message.Equals(Global.SysQueue.STOP, StringComparison.OrdinalIgnoreCase)) {
                    SvcShutdownEvent.Set();
                }
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
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
                string message = Q.EndReceive(e.AsyncResult).Body.ToString();
                // log it
                QueueLog.InfoFormat(Global.Messages.Queue.MessageReceived, Q.QueueName, message);
                // do XML things
                
            }
            catch (MessageQueueException ex) {
                if ((int)ex.MessageQueueErrorCode == -1073741536) {  // queue is closed
                    QueueLog.ErrorFormat(Global.Messages.Queue.ClosedOnReceive, Global.AppSettings.IntegratrexXmlQueue);
                    return;
                }
                SvcLog.ErrorFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
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
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // stop listening to queues
                SysQ.Close();
                QueueLog.InfoFormat(Global.Messages.Queue.Closed, SysQ.QueueName);
                XmlQ.Close();
                QueueLog.InfoFormat(Global.Messages.Queue.Closed, XmlQ.QueueName);
                // stop all integrations
                m_Integratrex.StopAllIntegrations();
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
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
                // log
                SvcLog.InfoFormat(Global.Messages.Service.Stopping);
                // if the shutdown event has not been signaled, signal it!
                if (!SvcShutdownEvent.WaitOne(0)) {
                    SvcShutdownEvent.Set();
                }
                // log
                SvcLog.InfoFormat(Global.Messages.Service.Stopped);
            }
            catch (Exception ex) {
                SvcLog.ErrorFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
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
            // log
            SvcLog.InfoFormat(Global.Messages.Service.Paused);
        }

        /// <summary>
        /// Play
        /// </summary>
        protected override void OnContinue() {
            // log
            SvcLog.InfoFormat(Global.Messages.Service.Resume);
        }

        /// <summary>
        /// Shut it down
        /// </summary>
        protected override void OnShutdown() {
            SvcLog.InfoFormat(Global.Messages.Service.Shutdown);
            OnStop();
        }

    }   // end class Service
}
