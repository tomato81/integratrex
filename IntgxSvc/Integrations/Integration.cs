using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

// lawg
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;

// C2
using C2InfoSys.Schedule;
using C2InfoSys.FileIntegratrex.Lib;
using System.Threading;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Tracks a single execution of an integration
    /// </summary>
    public class IntegrationTracker {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationTracker(XIntegration p_Integration, IntegrationAttributes p_Attrs) {
            // set locals
            m_Attrs = p_Attrs;
            m_Integration = p_Integration;       
            // set up working dirs
            string dtStamp = string.Format("{0:s}", DateTime.Now).Replace(" ", "").Replace(":", "").Replace(".", "").Replace("-", "");
            string work = Path.Combine(Global.AppSettings.IntegratrexWorkFolder, m_Integration.Desc, Global.WorkInstDir, dtStamp);            
            // create some objects
            m_WorkingDi = new DirectoryInfo(work);            
        }
        
        /// <summary>
        /// Get at the integration attributes
        /// </summary>
        public IntegrationAttributes Attrs {
            get {
                return m_Attrs;
            }
        }

        // members
        private IntegrationAttributes m_Attrs;
        private XIntegration m_Integration;
        private DateTime m_RunDate;
        private MatchedFile[] m_MatchedFiles;
        private int m_matchCount;
        private DirectoryInfo m_WorkingDi;  // a time stamped folder used for a particular execution of an integration (typically only created if the scan returns results)               
    }  

    /// <summary>
    /// Manages the execution of an Integration - directly corresponds to an <Integration> ... </Integration> in the configuration file
    /// </summary>
    public class IntegrationManager {

        // logs
        private ILog SvcLog;    // service log
        private ILog IntLog;    // log of ALL integrations          
        private ILog DebugLog;  // debug log
        private ILog IntInstLog;    // log of THIS integration

        // integration log listener
        //private TraceListener IntegrationInstLog;   // this is for THIS integration        

        // thread safety
        private object m_Padlock = new object();
        private int m_inRunMethod = 0;  
        private ManualResetEvent m_IntegrationInterruptEvent;

        // members
        private IntegrationAttributes m_Attrs;
        private XIntegration m_Integration;
        private ScheduleTimer m_Timer;
        private IntegrationSource m_Source;
        private IPattern[] m_Patterns;
        private DirectoryInfo m_IntegrationDi;  // this folder stores any support files necessrary for this integration to run (e.g. psftp scripts)
        private DirectoryInfo m_WorkingDi;  // root folder for this integration's timestamped integration instance folders        

        /// <summary>
        /// Integration Function
        /// </summary>
        public Action RunAction {
            get {
                return m_RunAction;
            }
        }
        // member
        private Action m_RunAction;

        /// <summary>
        /// Log Level
        /// </summary>
        public SourceLevels LogLevel {
            get {
                try {
                    string logLevelStr = Enum.GetName(typeof(XLogLevel), m_Integration.Log.Level);
                    return (SourceLevels)Enum.Parse(typeof(SourceLevels), logLevelStr);
                }
                catch {
                    return SourceLevels.All; 
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_IntegrationConfig"></param>
        public IntegrationManager(XIntegration p_Integration, ManualResetEvent p_IntegrationInterruptEvent) {                          
            // set locals
            m_IntegrationInterruptEvent = p_IntegrationInterruptEvent;
            m_Integration = p_Integration;
            m_Attrs = new IntegrationAttributes(p_Integration);
            // intialize sub-systems
            InitializeMgr();
            InitializeLog();
            InitializePatterns();
            InitializeSource();
            // setup timer
            m_RunAction = new Action(Run);
            m_Timer = new ScheduleTimer();            
        }

        private void InitializeMgr() {
            

            
        }

        /// <summary>
        /// Initialize the Integration Instance log
        /// </summary>
        private void InitializeLog() {
            // setup log for this integration
            string dtStamp = string.Format("{0:s}", DateTime.Now).Replace(" ", "").Replace(":", "").Replace(".", "").Replace("-", "");            
            // integration log file
            FileInfo LogFi = new FileInfo(Path.Combine(Global.AppSettings.IntegratrexWorkFolder, m_Integration.Desc, Global.WorkLogDir, string.Concat(dtStamp, ".log")));
            if (!LogFi.Directory.Exists) {
                LogFi.Directory.Create();
            }


            
            IAppender A = Global.CreateFileAppender(m_Integration.Desc, LogFi.FullName);
            IntInstLog = Global.CreateLogger(m_Integration.Desc);
            Global.AddAppender(m_Integration.Desc, A);
            

            
            IAppender I = LogManager.GetRepository().GetAppenders().Where(x => x.Name.Equals(Global.IntegrationLogName, StringComparison.Ordinal)).First();


            Global.AddAppender(m_Integration.Desc, I);

            IntInstLog.Info("integration instance log intialized");
            
                   
            // setup trace sources
            DebugLog = LogManager.GetLogger(Global.DebugLogName);
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);
            IntLog = LogManager.GetLogger(Global.IntegrationLogName);            

            
    
        }

        /// <summary>
        /// Extract Attributes from an XObject
        /// </summary>
        /// <param name="p_Obj">the XObject</param>
        /// <returns>property dictionary</returns>
        public Dictionary<string, object> ExtractAttrs(XObject p_XObj) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // empty dictionary
                Dictionary<string, object> Attrs = new Dictionary<string, object>();
                // reflect
                Type T = p_XObj.GetType();
                PropertyInfo[] Props = T.GetProperties();
                // get 'em
                foreach(PropertyInfo P in Props) {
                    if (P.GetType().Equals(typeof(string))) {
                        if (!P.GetType().IsValueType) {
                            continue;
                        }
                    }
                    string key = string.Format("{0}.{1}", T.Name, P.Name);
                    object Val = P.GetValue(p_XObj);
                    Attrs.Add(key, Val);
                }
                // return the completed attribute dictionary
                return Attrs;
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Initialize Patterns
        /// </summary>
        private void InitializePatterns() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // create the array
                m_Patterns = new IPattern[m_Integration.Patterns.Pattern.Count()];
                // source!
                PatternFactory F = new PatternFactory();
                // go thru the patterns
                for(int i = 0; i < m_Patterns.Count(); i++) {
                    m_Patterns[i] = F.Create(m_Integration.Patterns.Pattern[i]);
                }                
                // log
                IntLog.InfoFormat("File matching patterns intialized:{0}", m_Patterns.Count());
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
        }    

        /// <summary>
        /// Setup the Source Object
        /// </summary>
        private void InitializeSource() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // source!
                IntegrationSourceFactory F = new IntegrationSourceFactory();
                m_Source = F.Create(m_Integration.Source);
                
                //Source.TraceSource.Listeners.Add(IntegrationInstLog);
                // log
                IntLog.InfoFormat("Integration source intialized:{0}", m_Integration.Source.Desc);
            }
            catch(Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Start the integration schedule
        /// </summary>
        public void StartSchedule() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {                
                // start schedule
                m_Timer.Start();
                IntLog.InfoFormat("Schedule started:{0}", m_Integration.Desc);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
        }

        /// <summary>
        /// Stop the integration schedule
        /// </summary>
        public void StopSchedule() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                m_Timer.Stop();
                IntLog.InfoFormat("Schedule stopped:{0}", m_Integration.Desc);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }            
        }        

        /// <summary>
        /// Create a ScheduleTimer object from schedule information stored in a XIntegration object
        /// </summary>
        public void CreateSchedule() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {                
                // method logic                           
                m_Timer.ClearJobs();
                EventQueue TimerEvents = new EventQueue();
                // handle continuous
                if (m_Integration.Schedule.Continuous != null) {
                    SimpleInterval IntSched = null;
                    int seconds = int.Parse(m_Integration.Schedule.Continuous.Interval);
                    TimeSpan Interval = new TimeSpan(0, 0, seconds);
                    if (!string.IsNullOrEmpty(m_Integration.Schedule.Continuous.BusinessCalendar)) {
                        // get at the business calendar
                        BusinessCalendar BC = CalendarAccess.Instance.Calendars[m_Integration.Schedule.Continuous.BusinessCalendar];
                        // create the business calendar aware interval schedule
                        IntSched = new SimpleIntervalEx(DateTime.Now, Interval, BC);
                    }
                    else {
                        // create the interval schedule
                        IntSched = new SimpleInterval(DateTime.Now, Interval);
                    }
                    // add to the queue
                    TimerEvents.Add(IntSched);                    
                }
                // handle calendar schedules
                if (m_Integration.Schedule.Calendar != null) {                    
                    foreach (XCalendar Cal in m_Integration.Schedule.Calendar) {
                        ScheduledTime TimeSched = null;
                        EventTimeBase timeBase = ScheduleHelper.TranslateXCalendarOccurs(Cal.Occurs);
                        string offset = ScheduleHelper.GetOffset(Cal.Occurs, Cal.Offset, Cal.TimeOfDay);
                        if (!string.IsNullOrEmpty(Cal.BusinessCalendar)) {
                            // get at the business calendar
                            BusinessCalendar BC = CalendarAccess.Instance.Calendars[Cal.BusinessCalendar];
                            // create the business calendar aware interval schedule
                            TimeSched = new ScheduledTimeEx(timeBase, offset, BC, BusinessDayAdjust.RunNormal);
                        }
                        else {
                            // create the scheduled time
                            TimeSched = new ScheduledTime(timeBase, offset);
                        }
                        // add to the queue
                        TimerEvents.Add(TimeSched);
                    }
                }
                // add the schedule to the timer
                m_Timer.AddJob(TimerEvents, m_RunAction, new object[0]);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Run thread monitor
        /// </summary>
        private void Monitor() {
            try {
                // monitor Run thread activity here
            }
            catch (Exception ex) { 
            }
        }        

        /// <summary>
        /// Run the Integration
        /// </summary>
        public void Run() {
            // do not allow method re-entry from multiple threads
            if (Interlocked.CompareExchange(ref m_inRunMethod, 1, 0) == 1) {
                SvcLog.WarnFormat("Integration Run action re-entry detected on Integration:{0}", m_Integration.Desc);
                return;
            }            
            // Run()
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // log debug
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                

                // check if blocking
                if (!m_IntegrationInterruptEvent.WaitOne(Global.IntegrationInterruptWait)) {
                    SvcLog.WarnFormat("Integration interrupt event is set. {0} is blocked until released.", m_Integration.Desc);
                    if (!m_IntegrationInterruptEvent.WaitOne(Global.IntegrationInterruptTimeout)) {
                        SvcLog.ErrorFormat("Integration interrupt event block was not released in a timely manner. Aborting Run action for {0}", m_Integration.Desc);
                        return;
                    }
                }

                // reset attributes
                m_Attrs.Reset();

                // tracker jacker
                IntegrationTracker T = new IntegrationTracker(m_Integration, m_Attrs);

                // log integration
                IntLog.InfoFormat("Run Integration:{0}", m_Integration.Desc);

                // matched files
                MatchedFile[] MatchedFiles;

                // steps               
                ScanSource(T);
                GetFiles(T);
                WorkingTransform(T);
                SourceTransform(T);
                RunResponses(T);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                m_inRunMethod = 0;
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }            
        }
        
        /// <summary>
        /// Scan the integration source
        /// </summary>
        private void ScanSource(IntegrationTracker p_T) {
            // scan the source and return a list of matches
            // dont do anything else at this point
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);


                // method logic
                IntLog.InfoFormat("Integration:{0} Scan Source:{1}", m_Integration.Desc, m_Integration.Source.Desc);




                MatchedFile[] MatchedFiles = m_Source.Location.Scan(m_Patterns, p_T);
                

                
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
        /// Retrive files from the integration source 
        /// </summary>
        private void GetFiles(IntegrationTracker p_T) {            
            // copy files from the source to the working directory
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic
                IntLog.InfoFormat("Get Files:{0}", m_Integration.Desc);
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
        /// Apply transforms to files in the working folder
        /// </summary>
        private void WorkingTransform(IntegrationTracker p_T) {
            // make any necessary alterations to files in the working directory
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic
                IntLog.InfoFormat("Working Transform:{0}", m_Integration.Desc);
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
        /// Apply transforms to files at the source location
        /// </summary>
        private void SourceTransform(IntegrationTracker p_T) {
            // make any necessary alterations the the source (delete, rename, etc.)
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic
                IntLog.InfoFormat("Source Transform:{0}", m_Integration.Desc);
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
        /// Run integration response actions
        /// </summary>
        private void RunResponses(IntegrationTracker p_T) {
            // run each response in order            
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic
                IntLog.InfoFormat("Run Responses:{0}", m_Integration.Desc);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            } 
        }

    }   // IntegrationManager


    /// <summary>
    /// Manage the attributes associated with each integration and provide a way to access them
    /// </summary>
    public class IntegrationAttributes {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationAttributes(XIntegration p_Integration) {
            Integration(p_Integration);
        }        

        /// <summary>
        /// Add an integration to the attribute collection
        /// </summary>
        /// <param name="p_Integration"></param>
        public void Integration(XIntegration p_Integration) {
            // get attributes
            Dictionary<string,object> IntegrationAttrs = ExtractAttrs(p_Integration);
            Dictionary<string,object> SourceAttrs = ExtractAttrs(p_Integration.Source);
            // merge them and output
            m_IntegrationAttrs = IntegrationAttrs.Concat(SourceAttrs).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        private Dictionary<string, object> m_IntegrationAttrs;


        /// <summary>
        /// Reset the object for a new Run
        /// </summary>
        public void Reset() {
            // clear any integration specific attributes
        }


        public void MatchedFiles(MatchedFile[] p_MatchedFiles) {

        }

        /// <summary>
        /// Get an updated key-val set of integration attributes
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetAttrs() {
            return m_IntegrationAttrs;
        }

        /// <summary>
        /// Extract Attributes from an XObject
        /// </summary>
        /// <param name="p_Obj">the XObject</param>
        /// <returns>property dictionary</returns>
        private Dictionary<string, object> ExtractAttrs(XObject p_XObj) {      
            // empty dictionary
            Dictionary<string, object> Attrs = new Dictionary<string, object>();
            // reflect
            Type T = p_XObj.GetType();
            PropertyInfo[] Props = T.GetProperties();
            // get the type name - X
            string tname = T.Name[0] == 'X' ? T.Name.Substring(1, T.Name.Length - 1) : T.Name;
            // get 'em
            foreach (PropertyInfo P in Props) {
                if (P.GetType().Equals(typeof(string))) {
                    if (!P.GetType().IsValueType) {
                        continue;
                    }
                }
                string key = string.Format("{0}.{1}", tname, P.Name);
                object Val = P.GetValue(p_XObj);
                Attrs.Add(key, Val);
            }
            // return the completed attribute dictionary
            return Attrs;           
        }


    }   // IntegrationAttributes

    /// <summary>
    /// Base class for all integration objects
    /// </summary>
    public class IntegrationObject {

        // log        
        public ILog SvcLog;
        public ILog DebugLog;
        public ILog IntLog;

        /// <summary>
        /// Constructor
        /// </summary>
        protected IntegrationObject(object p_IntegrationObj) {

            // log refs
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);
            DebugLog = LogManager.GetLogger(Global.DebugLogName);


            ExtractObjAttrs(p_IntegrationObj);
        }

        /// <summary>
        /// Does the referenced property require dynamic text processing?
        /// </summary>
        /// <param name="p_Info">the property</param>
        /// <returns>true false</returns>
        protected bool IsDynamic(PropertyInfo p_Info) {
            return m_DynamicText.ContainsKey(p_Info.Name);
        }
        protected bool IsDynamic(string p_propertyName) {
            return m_DynamicText.ContainsKey(p_propertyName);
        }
        /// <summary>
        /// Compiled Dynamic Text
        /// </summary>
        protected Dictionary<string, DynamicTextParser> DynamicText {
            get {
                return m_DynamicText;
            }
        }
        // member
        private Dictionary<string, DynamicTextParser> m_DynamicText = new Dictionary<string, DynamicTextParser>();

        /// <summary>
        /// Find all attributes of the integration object, and compile any dynamic text
        /// </summary>
        protected void ExtractObjAttrs(object p_IntegrationObj) {
            try {
                Type T = p_IntegrationObj.GetType();
                m_ObjectProps = T.GetProperties();
                foreach (PropertyInfo P in m_ObjectProps) {
                    if (P.PropertyType == typeof(string)) {
                        string text = P.GetValue(p_IntegrationObj).ToString();
                        DynamicTextParser DyText = new DynamicTextParser(text);
                        if (DyText.Compile()) {
                            m_DynamicText.Add(P.Name, DyText);
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Adds a new dynamic element to the collection
        /// </summary>
        /// <param name="p_key"></param>
        /// <param name="p_text"></param>
        protected void AddDynamicText(string p_key, string p_text) {
            if (m_DynamicText.ContainsKey(p_key)) {
                throw new Exception(string.Format("a dynamic text element with the key {0} already exists", p_key));
            }
            DynamicTextParser DyText = new DynamicTextParser(p_text);
            if (DyText.Compile()) {
                m_DynamicText.Add(p_key, DyText);
            }
        }

        protected PropertyInfo[] ObjectProps {
            get {
                return m_ObjectProps;
            }
        }
        // member
        private PropertyInfo[] m_ObjectProps;

        /// <summary>
        /// Create Logger
        /// </summary>
        /// <param name="p_name"></param>
        /// <returns></returns>
        protected ILog CreateLogger(string p_name) {
            return LogManager.GetLogger(p_name);
        }

    }   // IntegrationObject

}
