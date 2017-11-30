using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Security.Cryptography;

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

        /// <summary>
        /// Set Logger
        /// </summary>
        /// <param name="p_Logger">the logger</param>
        public void SetLogger(ILog p_Logger) {
            IntInstLog = p_Logger;
        }

        /// <summary>
        /// Log this integration
        /// </summary>
        public ILog Log {
            get {
                return IntInstLog;
            }
        }

        /// <summary>
        /// Create the integration working directory
        /// </summary>
        /// <returns></returns>
        public void CreateWorkingDi() {
            try {
                if(!m_WorkingDi.Exists) {
                    m_WorkingDi.Create();
                }

                // update MatchedFiles



            }
            catch(Exception ex) {
                throw ex;
            }
        }        

        /// <summary>
        /// Matched Files
        /// </summary>
        public MatchedFile[] MatchedFiles { get => m_MatchedFiles; set => m_MatchedFiles = value; }
        public DirectoryInfo WorkingDi { get => m_WorkingDi; }
        public XIntegration Integration { get => m_Integration; }


        /// <summary>
        /// Number of Matched Files 
        /// </summary>
        public int MatchCount {
            get {
                return MatchedFiles == null ? 0 : MatchedFiles.Length;
            }
        }

        // members
        private IntegrationAttributes m_Attrs;
        private XIntegration m_Integration;
        private DateTime m_RunDate;
        private MatchedFile[] m_MatchedFiles;
        
        private DirectoryInfo m_WorkingDi;  // a time stamped folder used for a particular execution of an integration (typically only created if the scan returns results)               

        // log
        private ILog IntInstLog;    // log of THIS integration
    }  

    /// <summary>
    /// Manages the execution of an Integration - directly corresponds to an <Integration> ... </Integration> in the configuration file
    /// </summary>
    public class IntegrationManager : IDisposable {

        // all active integration manager
        public static Dictionary<string, IntegrationManager> m_IntegrationManagers = new Dictionary<string, IntegrationManager>();
        /// <summary>
        /// Get the integration manager for a named integration
        /// </summary>
        /// <param name="p_integration">integration name</param>
        /// <returns>integration manager</returns>
        public static IntegrationManager GetIntegrationManager(string p_integration) {
            if(!m_IntegrationManagers.ContainsKey(p_integration)) {
                throw new KeyNotFoundException();
            }
            return m_IntegrationManagers[p_integration];
        }

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

        private MatchHistory m_MatchHistory;

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
            // add this integration to the static list
            if (m_IntegrationManagers.ContainsKey(p_Integration.Desc)) {
                throw new Exception("this shouldn't be ... why does this integration already exist?");
            }
            m_IntegrationManagers.Add(m_Integration.Desc, this);
            // intialize sub-systems
            InitializeMgr();
            InitializeLog();
            InitializePatterns();
            InitializeSource();
            // setup timer
            m_RunAction = new Action(Run);
            m_Timer = new ScheduleTimer();            
        }

        /// <summary>
        /// Initialization code
        /// </summary>
        private void InitializeMgr() {
            m_MatchHistory = new MatchHistory(m_Integration.OnContact.SupressDuplicates);
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
                IntInstLog.InfoFormat("Integration source intialized:{0}", m_Integration.Source.Desc);
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
                IntInstLog.InfoFormat("Schedule started:{0}", m_Integration.Desc);
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
                IntInstLog.InfoFormat("Schedule stopped:{0}", m_Integration.Desc);
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
                T.SetLogger(IntInstLog);

                // log integration
                IntInstLog.InfoFormat("Run Integration:{0}", m_Integration.Desc);

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
                IntInstLog.InfoFormat("Integration:{0} Scan Source:{1}", m_Integration.Desc, m_Integration.Source.Desc);

                                
                m_Source.Location.Scan(m_Patterns, p_T);

                
                

                
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
                IntInstLog.InfoFormat("Get Files:{0}", m_Integration.Desc);

                // were files matched?
                if (p_T.MatchCount == 0) {
                    return;
                }

                Debug.Assert(!p_T.WorkingDi.Exists);

                // create the working directory
                p_T.CreateWorkingDi();
                // apply working folder to matched files
                foreach(MatchedFile M in p_T.MatchedFiles) {
                    M.SetWorkingDi(p_T.WorkingDi);
                }
                // get files into the working directory
                m_Source.Location.Get(p_T.MatchedFiles, p_T);
                
                // examine files
                foreach (MatchedFile M in p_T.MatchedFiles) {
                    if (p_T.Integration.OnContact.CalculateSHA1 == XOnContactCalculateSHA1.Y) {
                        M.SHA1 = GetSHA1(M);
                    }
                    if (p_T.Integration.OnContact.CalculateMD5 == XOnContactCalculateMD5.Y) {
                        M.MD5 = GetMD5(M);
                    }
                }

                // add to match history
                m_MatchHistory.Add(p_T.MatchedFiles);

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
                IntInstLog.InfoFormat("Working Transform:{0}", m_Integration.Desc);

                

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
                IntInstLog.InfoFormat("Source Transform:{0}", m_Integration.Desc);
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
                IntInstLog.InfoFormat("Run Responses:{0}", m_Integration.Desc);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            } 
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                    m_IntegrationManagers.Remove(m_Integration.Desc);
                }
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IntegrationManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


        /// <summary>
        /// Get the SHA1 Hash of the Matched file - reading from the source location
        /// </summary>
        /// <param name="p_Mf">Matched File</param>
        private string GetSHA1(MatchedFile p_Mf) {
            try {
                string sha1 = string.Empty;
                FileInfo Fi = new FileInfo(string.Format("{0}\\{1}", p_Mf.Folder, p_Mf.OrigName));
                using (FileStream Fin = new FileStream(Fi.FullName, FileMode.Open)) {
                    using (SHA1Managed SHA1 = new SHA1Managed()) {
                        byte[] hash = SHA1.ComputeHash(Fin);
                        StringBuilder Sb = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash) {
                            Sb.AppendFormat("{0:X2}", b);
                        }
                        sha1 = Sb.ToString();
                    }
                }
                return sha1;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Get the MD5 Hash of the Matched file - reading from the source location
        /// </summary>
        /// <param name="p_Mf">Matched File</param>
        private string GetMD5(MatchedFile p_Mf) {
            try {
                string md5 = string.Empty;

                

                FileInfo Fi = new FileInfo(string.Format("{0}\\{1}", p_Mf.Folder, p_Mf.OrigName));
                using (MD5 MDFive = MD5.Create()) {
                    using (FileStream Fin = new FileStream(Fi.FullName, FileMode.Open)) {
                        byte[] hash = MDFive.ComputeHash(Fin);
                        StringBuilder Sb = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash) {
                            Sb.AppendFormat("{0:X2}", b);
                        }
                        md5 = Sb.ToString();
                    }
                }
                return md5;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }   // IntegrationManager

    /// <summary>
    /// File Match History
    /// </summary>
    public class MatchHistory {


        private XSupressDuplicates m_SupressDuplicates;

        /// <summary>
        /// Constructor
        /// </summary>
        public MatchHistory(XSupressDuplicates p_SupressDuplicates) {
            m_SupressDuplicates = p_SupressDuplicates;
        }

        /// <summary>
        /// Are duplicates being supressed?
        /// </summary>
        public bool SupressDuplciates {
            get {
                return m_SupressDuplicates.Enable == XSupressDuplicatesEnable.Y;
            }
        }

        /// <summary>
        /// Active methods used to identify duplicates
        /// </summary>
        private bool MD5 {
            get {
                return m_SupressDuplicates.MatchBy.MD5 == XMatchByMD5.Y;
            }
        }
        private bool SHA1 {
            get {
                return m_SupressDuplicates.MatchBy.SHA1 == XMatchBySHA1.Y;
            }
        }
        private bool FileName {
            get {
                return m_SupressDuplicates.MatchBy.FileName == XMatchByFileName.Y;
            }
        }
        private bool FileSize {
            get {
                return m_SupressDuplicates.MatchBy.FileSize == XMatchByFileSize.Y;
            }
        }
        private bool LastModifiedDate {
            get {
                return m_SupressDuplicates.MatchBy.LastModifiedDate == XMatchByLastModifiedDate.Y;                
            }
        }

        /// <summary>
        /// Is the Matched File a duplicate?
        /// </summary>
        /// <param name="p_M"></param>
        /// <returns></returns>
        public bool IsDuplicate(MatchedFile p_M) {
            try {
                // matched flags
                bool fileName = false;
                bool fileSize = false;
                bool fileDt = false;
                bool fileMD5 = false;
                bool fileSHA1 = false;
                
                // check for matches
                if (m_FileNames.Contains(p_M.OrigName)) {
                    fileName = true;
                    if (m_FileNameSize[p_M.OrigName].Contains(p_M.FileSize)) {
                        fileSize = true;
                    }
                    if (m_FileNameLastMod[p_M.OrigName].Contains(p_M.LastModifiedUTC)) {
                        fileDt = true;
                    }
                }
                if(m_MD5.Contains(p_M.MD5)) {
                    fileMD5 = true;
                }
                if (m_SHA1.Contains(p_M.SHA1)) {
                    fileSHA1 = true;
                }

                // is it a match?
                bool match = ((FileName ? fileName : true) && (FileSize ? fileSize : true) && (LastModifiedDate ? fileDt : true))
                    && (MD5 == fileMD5)
                    && (SHA1 == fileSHA1);
                // out
                return match;
            }
            catch(Exception ex) {
                throw ex;
            }
        }

        private HashSet<string> m_MD5 = new HashSet<string>();
        private HashSet<string> m_SHA1 = new HashSet<string>();
        private HashSet<string> m_FileNames = new HashSet<string>();
        //private HashSet<string> m_SHA1 = new HashSet<string>();

        private Dictionary<string, HashSet<long>> m_FileNameSize = new Dictionary<string, HashSet<long>>();
        private Dictionary<string, HashSet<long>> m_FileNameLastMod = new Dictionary<string, HashSet<long>>();

        /// <summary>
        /// Add a Matched File to the duplicate list
        /// </summary>
        /// <param name="p_Mf">a Matched File</param>
        public void Add(MatchedFile p_Mf) {
            // add the file name
            m_FileNames.Add(p_Mf.OrigName);
            // add the file hashs
            if (p_Mf.MD5.Length > 0) {
                m_MD5.Add(p_Mf.MD5);
            }
            if (p_Mf.SHA1.Length > 0) {
                m_SHA1.Add(p_Mf.SHA1);
            }           
            // add the file size            
            if(!m_FileNameSize.ContainsKey(p_Mf.OrigName)) {
                m_FileNameSize.Add(p_Mf.OrigName, new HashSet<long>());
            }
            m_FileNameSize[p_Mf.OrigName].Add(p_Mf.FileSize);
            // add the last modified date
            if (!m_FileNameLastMod.ContainsKey(p_Mf.OrigName)) {
                m_FileNameLastMod.Add(p_Mf.OrigName, new HashSet<long>());
            }            
            m_FileNameLastMod[p_Mf.OrigName].Add(p_Mf.LastModifiedUTC);
        }

        /// <summary>
        /// Add an array of Matched Files to the duplicate list
        /// </summary>
        /// <param name="p_Mf">an array of Matched Files</param>
        public void Add(MatchedFile[] p_Mf) {
            foreach(MatchedFile M in p_Mf) {
                Add(M);
            }
        }


    }   // MatchHistory


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
        /// Create Logger
        /// </summary>
        /// <param name="p_name"></param>
        /// <returns></returns>
        protected void SetLogger(ILog p_Log) {
            Log = p_Log;
        }
        protected ILog Log;

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
                            DebugLog.DebugFormat("Extracted Property {0}.{1}{2}Compiling:{3}{4}", T.Name, P.Name, Environment.NewLine, Environment.NewLine, text);                            
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

        /// <summary>
        /// Object Properties
        /// </summary>
        protected PropertyInfo[] ObjectProps {
            get {
                return m_ObjectProps;
            }
        }
        // member
        private PropertyInfo[] m_ObjectProps;

    }   // IntegrationObject

}
