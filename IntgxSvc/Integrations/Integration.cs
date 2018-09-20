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
using Newtonsoft.Json;

namespace C2InfoSys.FileIntegratrex.Svc {


    /* perhaps break out the integration logic into 2 classes
     * logic for the scan - anything that happens before any match
     * logic for matched files activities
     * 
     * strip logic from integrationmanager and put it in the scan object
     * use integration tracker for post scan activities
     * 
     * create scan object on each iteration 
     * create tracker only when required
     * 
     * this should elimiate the need to detach all the events from the tracker after the iteration
     */

    /// <summary>
    /// Tracks a single execution of an integration
    /// </summary>
    public class IntegrationTracker : IDisposable {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationTracker(IntegrationManager p_Manager) {
            // set locals
            m_Manager = p_Manager;            
            // set up working dirs
            string dtStamp = string.Format("{0:s}", DateTime.Now).Replace(" ", "").Replace(":", "").Replace(".", "").Replace("-", "");
            string work = Path.Combine(Global.AppSettings.IntegratrexWorkFolder, Integration.Desc, Global.WorkInstDir, dtStamp);
            // create some objects
            m_MatchedFiles = new List<MatchedFile>();
            m_WorkingDi = new DirectoryInfo(work);
            // hookup tracker events
            AttachEvents();
            // set context for dynamic text replacement
            Manager.SetAttrContext(m_MatchedFiles);
        }

        /// <summary>
        /// Attach integration object event sources to the handlers on the tracker
        /// </summary>
        private void AttachEvents() {
            // source            
            Manager.Source.Match += SourceScan_Match;
            Manager.Source.GotFile += Source_GotFile;
            Manager.Source.SourceTransform += Source_DoTransform;
            Manager.Source.FileRenamed += Source_FileRenamed;
            Manager.Source.GotFiles += Source_GotFiles;
            Manager.Source.DeleteFiles += Source_DeleteFiles;
            Manager.Source.DeletedFile += Source_DeletedFile;
            Manager.Source.DeletedFiles += Source_DeletedFiles;
            Manager.Source.OnValueRequired += ValueRequired;
            // on contact
            Manager.OnContact.OnValueRequired += ValueRequired;
            // responses
            foreach (IntegrationResponse R in Manager.Responses) {
                // attach response events
                R.TransformResponse += Response_TransformResponse;
                R.LocationCreated += Response_LocationCreated;
                R.FileExists += Response_FileExists;
                R.FileOverrwrite += Response_FileOverrwrite;
                R.ActionStarted += Response_ActionStarted;
                R.ActionComplete += Response_ActionComplete;
                R.FileActioned += Response_FileActioned;
                R.OnValueRequired += ValueRequired;
                R.Transformation.OnValueRequired += ValueRequired;
            }                                    
        }

        /// <summary>
        /// Detach integration object event sources to the handlers on the tracker
        /// </summary>
        private void DetachEvents() {
            // detach source
            Manager.Source.Match -= SourceScan_Match;
            Manager.Source.GotFile -= Source_GotFile;
            Manager.Source.SourceTransform -= Source_DoTransform;
            Manager.Source.FileRenamed -= Source_FileRenamed;
            Manager.Source.GotFiles -= Source_GotFiles;
            Manager.Source.DeleteFiles -= Source_DeleteFiles;
            Manager.Source.DeletedFile -= Source_DeletedFile;
            Manager.Source.DeletedFiles -= Source_DeletedFiles;
            Manager.Source.OnValueRequired -= ValueRequired;
            // on contact
            Manager.OnContact.OnValueRequired -= ValueRequired;
            // responses
            foreach (IntegrationResponse R in Manager.Responses) {
                // attach response events
                R.TransformResponse -= Response_TransformResponse;
                R.LocationCreated -= Response_LocationCreated;
                R.FileExists -= Response_FileExists;
                R.FileOverrwrite -= Response_FileOverrwrite;
                R.ActionStarted -= Response_ActionStarted;
                R.ActionComplete -= Response_ActionComplete;
                R.FileActioned -= Response_FileActioned;
                R.OnValueRequired -= ValueRequired;
                R.Transformation.OnValueRequired -= ValueRequired;
            }           
        }

        /// <summary>
        /// Apply transformations to the response file 
        /// </summary>
        /// <remarks>basically rename the file to be actioned at the response location</remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_TransformResponse(object sender, TransformResponseEventArgs e) {
            // set context
            Manager.SetAttrContext(e.MatchedFile);
            // get at the response
            IntegrationResponse Response = (IntegrationResponse)sender;
            // rename the response file?
            if (Response.Transformation.IsRename) {                
                e.MatchedFile.ResponseFileName = Response.Transformation.Rename();
                e.TransformApplied = true;
            }
            else {
                e.MatchedFile.ResponseFileName = e.MatchedFile.WorkingName;
            }                           
        }

        /// <summary>
        /// The response has been actioned on a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_FileActioned(object sender, FileActionedEventArgs e) {
            // set context
            Manager.SetAttrContext(e.MatchedFile);
            // do things
            IntegrationResponse R = (IntegrationResponse)sender;
            IntInstLog.InfoFormat(Global.Messages.Integration.ResponseFileActioned, e.Action, e.MatchedFile.WorkingFi.FullName, e.Target);
        }

        /// <summary>
        /// The response action has completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_ActionComplete(object sender, ActionCompleteEventArgs e) {            
            // do things
            IntegrationResponse R = (IntegrationResponse)sender;            
            IntInstLog.InfoFormat(Global.Messages.Integration.ResponseActionComplete, R.Description);
        }

        /// <summary>
        /// The response action has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_ActionStarted(object sender, ActionStartedEventArgs e) {           
            IntegrationResponse R = (IntegrationResponse)sender;           
            IntInstLog.InfoFormat(Global.Messages.Integration.ResponseActionStarted, R.Description);
        }

        /// <summary>
        /// A file at the response location has been overwritten
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_FileOverrwrite(object sender, OverrwriteEventArgs e) {
            IntInstLog.InfoFormat(Global.Messages.Integration.OverrwriteFileAtTarget, e.Path);
        }

        /// <summary>
        /// The file already exists at the target location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_FileExists(object sender, FileExistsEventArgs e) {
            IntInstLog.InfoFormat(Global.Messages.Integration.FileExistsAtTarget, e.MatchedFile.WorkingName, e.Target);
        }

        /// <summary>
        /// A location has been created at the target
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Response_LocationCreated(object sender, LocationCreatedEventArgs e) {
            IntInstLog.InfoFormat(Global.Messages.Integration.ResponseLocationCreated, e.Location);
        }               

        /// <summary>
        /// A file has been renamed at the integration source
        /// </summary>
        /// <param name="sender">object that raised the event</param>
        /// <param name="e">Event Args</param>
        private void Source_FileRenamed(object sender, FileRenamedEventArgs e) {               
            IntInstLog.InfoFormat(Global.Messages.Integration.SourceFileRenamed, e.RenamedFrom, e.RenamedTo);
        }

        /// <summary>
        /// Transform Files at Source
        /// </summary>
        /// <param name="sender">object that raised the event</param>
        /// <param name="e">event args</param>
        private void Source_DoTransform(object sender, TransformSourceEventArgs e) {            
            // what we do?
            if(Manager.OnContact.RenameOriginal) {
                // go thru the files
                foreach (MatchedFile F in e.Files) {
                    // set context
                    Manager.SetAttrContext(F);
                    // do rename
                    F.Name = Manager.OnContact.Rename();
                }
                // transforms have been performed
                e.HasTransforms = true;
            }                        
        }

        /// <summary>
        /// Delete Files
        /// </summary>
        /// <param name="sender">object that raised the event</param>
        /// <param name="e">event args</param>
        private void Source_DeleteFiles(object sender, IntegrationEventArgs e) {
            IntInstLog.InfoFormat(Global.Messages.Integration.SourceDeleteFiles, Manager.Source.Description);
        }

        /// <summary>
        /// Deleted a File
        /// </summary>
        /// <param name="sender">object that raised the event</param>
        /// <param name="e">event args</param>
        private void Source_DeletedFile(object sender, IntegrationFileEventArgs e) {
            e.File.Deleted = true;
            IntInstLog.InfoFormat("Deleted {0} from {1}", e.File.Name, Manager.Source.Description);
        }

        /// <summary>
        /// Deleted all Files
        /// </summary>
        /// <param name="sender">object that raised the event</param>
        /// <param name="e">event args</param>
        private void Source_DeletedFiles(object sender, IntegrationFilesEventArgs e) {
            IntInstLog.InfoFormat("all matched files delete from {0}", Manager.Source.Description);
        }               

        /// <summary>
        /// Integration Value is Required!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValueRequired(object sender, OnValueRequiredEventArgs e) {
            e.Result = Manager.Attributes.GetReplacementValue(e.Name);                               
            IntInstLog.DebugFormat("Dynamic context required [Key={0}] [Substitution={1}]", e.Name, e.Result);
        }

        /// <summary>
        /// Matched Files
        /// </summary>
        public List<MatchedFile> MatchedFiles { get => m_MatchedFiles; }
        public DirectoryInfo WorkingDi { get => m_WorkingDi; }
        public XIntegration Integration { get => m_Manager.Integration; }
        public IntegrationAttributes Attrs { get => m_Manager.Attributes; }
        public IntegrationManager Manager { get => m_Manager; }

        /// <summary>
        /// Number of Matched Files 
        /// </summary>
        public int MatchCount {
            get {
                return MatchedFiles.Count;
            }
        }

        // members
        private DateTime m_RunDate;
        private readonly List<MatchedFile> m_MatchedFiles;
        private readonly DirectoryInfo m_WorkingDi;  // a time stamped folder used for a particular execution of an integration (typically only created if the scan returns results)               
        private IPattern[] m_Patterns;
        private readonly IntegrationManager m_Manager;

        // async result tracking
        private Dictionary<int, IAsyncResult> m_MD5Results = new Dictionary<int, IAsyncResult>();
        private Dictionary<int, IAsyncResult> m_SHA1Results = new Dictionary<int, IAsyncResult>();

        // logs
        private ILog SvcLog;    // service log          
        private ILog DebugLog;  // debug log
        private ILog IntInstLog;    // log of THIS integration            

        /// <summary>
        /// Log of this integration
        /// </summary>
        public ILog Log {
            get {
                return IntInstLog;
            }
        }   

        /// <summary>
        /// Set Logger
        /// </summary>
        /// <param name="p_Logger">the logger</param>
        public void SetLogger(ILog p_SvcLog, ILog p_DebugLog, ILog p_IntInstLog) {
            SvcLog = p_SvcLog;
            DebugLog = p_DebugLog;
            IntInstLog = p_IntInstLog;
        }        

        /// <summary>
        /// Create the integration working directory
        /// </summary>
        /// <returns></returns>
        public void CreateWorkingDi() {
            try {
                if(!m_WorkingDi.Exists) {
                    m_WorkingDi.Create();
                    IntInstLog.InfoFormat(Global.Messages.Integration.WorkingDirectoryCreated, m_WorkingDi);
                }              
            }
            catch(Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Integration Source -> File Match!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceScan_Match(object sender, OnFileMatchEventArgs e) {
            // log
            IntInstLog.InfoFormat(Global.Messages.Integration.SourceFileMatched, e.MatchedFile.OriginalName, e.Location, e.Pattern.ToString());
            // set integration attributes File context
            Manager.SetAttrContext(e.MatchedFile);
            // add to the match list
            MatchedFiles.Add(e.MatchedFile);            
            // can duplicates be identified at the integration source?
            if(Manager.OnContact.SupressDuplicates.CanIDAtSource) {
                if(Manager.OnContact.SupressDuplicates.IsDuplicate(e.MatchedFile)) {
                    e.MatchedFile.Supress = true;
                    return; // not much else to do
                }
            }
            // set the working directory
            e.MatchedFile.SetWorkingDi(WorkingDi);
            // will the name of the working file be different from the name at the source?
            if(Manager.OnContact.RenameWorkingCopy) {                
                e.MatchedFile.SetWorkingFileName(Manager.OnContact.Rename());            
            } else {
                e.MatchedFile.SetWorkingFileName(e.MatchedFile.OriginalName);
            }           
        }        

        /// <summary>
        /// Integration Source -> Got File!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_GotFile(object sender, GotFileEventArgs e) {            
            // log
            IntInstLog.InfoFormat("[{0}] > [{1}] - Got File", e.MatchedFile.OriginalName, e.MatchedFile.WorkingFi.FullName);
            // set context
            Manager.SetAttrContext(e.MatchedFile);
            // check that the file can be opened for read
            try {
                e.MatchedFile.WorkingFi.OpenRead().Close();
            }
            catch(Exception ex) {
                IntInstLog.ErrorFormat("Source_GotFile: Could not open working copy of file {0} Message:{1}", e.MatchedFile.WorkingName, ex.Message);
                return;
            }
            // calculate hashs
            if (Integration.OnContact.CalculateSHA1 == XOnContactCalculateSHA1.Y) {                    
                m_SHA1Results.Add(e.MatchedFile.GetHashCode(), Manager.OnContact.GetSHA1.BeginInvoke(e.MatchedFile, null, null));
            }
            if (Integration.OnContact.CalculateMD5 == XOnContactCalculateMD5.Y) {                
                m_MD5Results.Add(e.MatchedFile.GetHashCode(), Manager.OnContact.GetMD5.BeginInvoke(e.MatchedFile, null, null));                
            }

            // maybe other things??
            
        }

        /// <summary>
        /// All files have been retrived from the integration source
        /// </summary>
        /// <param name="sender">object that raised the event</param>
        /// <param name="e">event args</param>
        private void Source_GotFiles(object sender, GotFilesEventArgs e) {
            // go thru files
            foreach(MatchedFile M in e.MatchedFiles) {
                if(!M.WorkingFi.Exists) {
                    IntInstLog.ErrorFormat("working file {0} is unexpectedly missing", M.WorkingName);
                    continue;
                }
                // examine files
                if (Integration.OnContact.CalculateSHA1 == XOnContactCalculateSHA1.Y) {                    
                    IAsyncResult SHA1Result = m_SHA1Results[M.GetHashCode()];                    
                    M.SHA1 = Manager.OnContact.GetSHA1.EndInvoke(SHA1Result);
                }
                if (Integration.OnContact.CalculateMD5 == XOnContactCalculateMD5.Y) {                    
                    IAsyncResult MD5Result = m_MD5Results[M.GetHashCode()];                    
                    M.MD5 = Manager.OnContact.GetMD5.EndInvoke(MD5Result);
                }            
            }            
        }

        /// <summary>
        /// Top level integration function - Scane the integration source for files matching the patterns
        /// </summary>
        /// <remarks>
        /// +ScanSource
        /// GetFiles
        /// SupressDuplicates
        /// WorkingTransform
        /// SourceTransform
        /// RunResponses
        /// UpdateMatchHistory
        /// </remarks>
        public void ScanSource() {
            // scan the source and return a list of matches
            // dont do anything else at this point
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                
                // scan the source location for files patching the passed patterns
                Manager.Source.Scan(Manager.Patterns);                  
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
        /// Top level integration function - Retrive files from the integration source
        /// </summary>
        /// <remarks>
        /// *ScanSource
        /// +GetFiles
        /// SupressDuplicates
        /// WorkingTransform
        /// SourceTransform
        /// RunResponses
        /// UpdateMatchHistory
        /// </remarks>
        public void GetFiles() {
            // copy files from the source to the working directory
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);             
                // were files matched?
                if (MatchCount == 0) {
                    return;
                }
                // it better not exist
                Debug.Assert(!WorkingDi.Exists);
                // create the working directory
                CreateWorkingDi();
                // apply working folder to matched files
                foreach (MatchedFile M in MatchedFiles) {
                    M.SetWorkingDi(WorkingDi);
                }
                // get files into the working directory
                Manager.Source.Get(MatchedFiles);              
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
        /// Top level integration function - Identify and supress duplicate files
        /// </summary>
        /// <remarks>
        /// *ScanSource
        /// *GetFiles
        /// +SupressDuplicates
        /// WorkingTransform
        /// SourceTransform
        /// RunResponses
        /// UpdateMatchHistory
        /// </remarks>
        public void SupressDuplicates() {
            // make any necessary alterations to files in the working directory
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // are we supressing?
                if(!Manager.MatchHistory.SupressDuplciates) {
                    return; // nope
                }
                // check each file vs. the match history
                foreach(MatchedFile F in MatchedFiles) {
                    if(Manager.MatchHistory.IsDuplicate(F)) {
                        F.Supress = true;
                        IntInstLog.InfoFormat("[{0}] - supressing file from previous run", F.WorkingFi.FullName);
                    }
                }
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
        /// Top level integration function - Apply transforms to files in the working folder
        /// </summary>
        /// <remarks>
        /// *ScanSource
        /// *GetFiles
        /// *SupressDuplicates
        /// +WorkingTransform
        /// SourceTransform
        /// RunResponses
        /// UpdateMatchHistory
        /// </remarks>
        public void WorkingTransform() {
            // make any necessary alterations to files in the working directory
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                
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
        /// Top level integration function - Apply transforms to files at the source location
        /// </summary>
        /// <remarks>
        /// *ScanSource
        /// *GetFiles
        /// *SupressDuplicates
        /// *WorkingTransform
        /// +SourceTransform
        /// RunResponses
        /// UpdateMatchHistory
        /// </remarks>
        public void SourceTransform() {
            // make any necessary alterations the the source (delete, rename, etc.)
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                                
                // delete?
                if (Manager.OnContact.DeleteFromSource) {
                    Manager.Source.Delete(MatchedFiles);
                }
                else {
                    // transform!
                    Manager.Source.Transform(MatchedFiles);
                }                
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
        /// Top level integration function - Run all integration responses
        /// </summary>
        /// <remarks>
        /// *ScanSource
        /// *GetFiles
        /// *SupressDuplicates
        /// *WorkingTransform
        /// *SourceTransform
        /// +RunResponses
        /// UpdateMatchHistory
        /// </remarks>
        public void RunResponses() {
            // run each response in order            
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                foreach(IntegrationResponse R in Manager.Responses) {                                   
                    foreach(MatchedFile F in MatchedFiles) {                        
                        R.Transform(F);
                    }                    
                    R.Action(MatchedFiles);                    
                }
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
        /// Top level integration function - Update the file match history
        /// </summary>
        /// <remarks>
        /// *ScanSource
        /// *GetFiles
        /// *SupressDuplicates
        /// *WorkingTransform
        /// *SourceTransform
        /// *RunResponses
        /// +UpdateMatchHistory
        /// </remarks>
        public void UpdateMatchHistory() {
            // run each response in order            
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // add em'
                foreach(MatchedFile F in MatchedFiles) {
                    Manager.SetAttrContext(F);
                    // if the file was not supressed
                    if(!F.Supress) {
                        Manager.MatchHistory.Add(F);
                    }
                }
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                }
                // detach!
                DetachEvents();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IntegrationTracker() {
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

    }   // IntegrationTracker

    /// <summary>
    /// Manages the execution of an Integration
    /// </summary>
    /// <remarks>directly corresponds to an <Integration> ... </Integration> in the configuration file</remarks>
    public class IntegrationManager : IDisposable {

        // all active integration managers
        public static Dictionary<string, IntegrationManager> m_IntegrationManagers = new Dictionary<string, IntegrationManager>();

        /// <summary>
        /// Get the integration manager for a named integration
        /// </summary>
        /// <param name="p_integration">integration name</param>
        /// <returns>an integration manager</returns>
        public static IntegrationManager GetIntegrationManager(string p_integration) {
            if (!m_IntegrationManagers.ContainsKey(p_integration)) {
                throw new KeyNotFoundException();
            }
            return m_IntegrationManagers[p_integration];
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_IntegrationConfig"></param>
        public IntegrationManager(XIntegration p_Integration, ManualResetEvent p_IntegrationInterruptEvent) {
            // static things
            if (m_IntegrationManagers.ContainsKey(p_Integration.Desc)) {
                throw new Exception("this shouldn't be ... why does this integration already exist?");
            }            
            // set locals
            m_IntegrationInterruptEvent = p_IntegrationInterruptEvent;
            m_Integration = p_Integration;          
            m_MatchHistory = new MatchHistory(p_Integration.OnContact.SupressDuplicates, new FileInfo(Path.Combine(Global.AppSettings.IntegratrexWorkFolder, p_Integration.Desc, Global.WorkSupprtDir, string.Concat("match", ".hist"))));
            m_Attrs = new IntegrationAttributes(p_Integration);            
            m_IntegrationManagers.Add(p_Integration.Desc, this);            
            m_Patterns = new IPattern[p_Integration.Patterns.Count()];
            m_OnContact = new OnContact(p_Integration.OnContact);
            // some events
            m_OnContact.OnError += OnContact_OnError;
            m_OnContact.DoLog += IntegrationObject_WriteLog;
            // intialize sub-systems        
            InitializeLog();
            InitializePatterns();
            InitializeSource();
            InitializeResponses();
            // setup timer            
            m_RunAction = new Action(Run);
            m_Timer = new ScheduleTimer();            
        }

        /// <summary>
        /// Write a log message rasied by an integration object to the integration instance log
        /// </summary>
        /// <param name="sender">raised by</param>
        /// <param name="e">event args</param>
        private void IntegrationObject_WriteLog(object sender, IntegrationLogEventArgs e) {
            IntInstLog.Info(e.Message);
        }

        /// <summary>
        /// OnContact Error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContact_OnError(object sender, IntegrationErrorEventArgs e) {
            IntInstLog.ErrorFormat("{0} [On Contact] - {1}", m_Integration.Desc, e.Exception.Message);
        }

        // integration log listener
        //private TraceListener IntegrationInstLog;   // this is for THIS integration        

        // thread safety
        private object m_Padlock = new object();
        private int m_inRunMethod = 0;
        private ManualResetEvent m_IntegrationInterruptEvent;

        // readonly members
        private readonly IntegrationAttributes m_Attrs;
        private readonly XIntegration m_Integration;
        private readonly ScheduleTimer m_Timer;        
        private readonly IPattern[] m_Patterns;
        private readonly DirectoryInfo m_IntegrationDi;  // this folder stores any support files necessary for this integration to run (e.g. psftp scripts)
        private readonly DirectoryInfo m_WorkingDi;  // root folder for this integration's timestamped integration instance folders        
        private readonly MatchHistory m_MatchHistory;        
        private readonly Action m_RunAction;
        private readonly OnContact m_OnContact;

        // other members
        private IntegrationSource m_Source;
        private List<IntegrationResponse> m_Responses = new List<IntegrationResponse>();

        // logs
        private ILog SvcLog;    // service log
        private ILog IntLog;    // log of ALL integrations
        private ILog DebugLog;  // debug log
        private ILog IntInstLog;    // log of THIS integration

        /// <summary>
        /// On Contact
        /// </summary>
        public OnContact OnContact { get => m_OnContact; }

        /// <summary>
        /// Integration Source
        /// </summary>
        public IntegrationSource Source { get => m_Source; }

        /// <summary>
        /// Integration Responses
        /// </summary>
        public List<IntegrationResponse> Responses { get => m_Responses; }

        /// <summary>
        /// XIntegration Object
        /// </summary>
        public XIntegration Integration { get => m_Integration; }

        /// <summary>
        /// Integration Attributes
        /// </summary>
        public IntegrationAttributes Attributes { get => m_Attrs; }

        /// <summary>
        /// Patterns
        /// </summary>
        public IPattern[] Patterns { get => m_Patterns;  }

        /// <summary>
        /// Integration Function
        /// </summary>
        public Action RunAction { get => m_RunAction; }        

        /// <summary>
        /// File Match History
        /// </summary>
        public MatchHistory MatchHistory { get => m_MatchHistory; }

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
        /// Set dynamic text replacement context
        /// </summary>
        /// <param name="p_File">the current working file</param>
        /// <param name="p_Files">the current working set of files</param>
        public void SetAttrContext(MatchedFile p_File, List<MatchedFile> p_Files) {
            Attributes.File = p_File;
            Attributes.Files = p_Files;
            //IntInstLog.DebugFormat(Global.Messages.Activity.DynamicContextSwitch, "N/A", Attributes.Files.Count, Attributes.File.WorkingName);
        }

        /// <summary>
        /// Set dynamic text replacement context
        /// </summary>        
        /// <param name="p_Files">the current working set of files</param>
        public void SetAttrContext(List<MatchedFile> p_Files) {            
            Attributes.Files = p_Files;
            //IntInstLog.DebugFormat(Global.Messages.Activity.DynamicContextSwitch, "N/A", Attributes.Files.Count, Attributes.File.WorkingName);
        }

        /// <summary>
        /// Set dynamic text replacement context
        /// </summary>
        /// <param name="p_File">the current working file</param>        
        public void SetAttrContext(MatchedFile p_File) {
            Attributes.File = p_File;
            //IntInstLog.DebugFormat(Global.Messages.Activity.DynamicContextSwitch, "N/A", Attributes.Files.Count, Attributes.File.WorkingName);
        }

        /// <summary>
        /// Get a new Integration Tracker
        /// </summary>
        /// <returns></returns>
        private IntegrationTracker NewTracker() {
            IntegrationTracker T = new IntegrationTracker(this);
            T.SetLogger(DebugLog, SvcLog, IntInstLog);
            return T;
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

            IntInstLog.InfoFormat("{0} - Instance log intialized", m_Integration.Desc);
            
                   
            // setup trace sources
            DebugLog = LogManager.GetLogger(Global.DebugLogName);
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);
            IntLog = LogManager.GetLogger(Global.IntegrationLogName);            

            
    
        }

        /// <summary>
        /// Initialize Patterns
        /// </summary>
        private void InitializePatterns() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {                
                // source!
                PatternFactory F = new PatternFactory();
                // go thru the patterns
                for(int i = 0; i < m_Patterns.Count(); i++) {
                    m_Patterns[i] = F.Create(m_Integration.Patterns[i]);                    
                }                
                // log
                IntLog.InfoFormat("File matching patterns intialized:{0}", m_Patterns.Count());
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
        }    

        /// <summary>
        /// Setup the Source Object
        /// </summary>
        private void InitializeSource() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // source factory
                IntegrationSourceFactory F = new IntegrationSourceFactory();
                m_Source = F.Create(m_Integration.Source);
                // events!
                m_Source.ScanSource += Source_ScanSource;
                m_Source.OnError += Source_OnError;
                m_Source.OnWarn += Source_OnWarn;
                // log
                IntInstLog.InfoFormat("Integration source intialized:{0}", m_Source.Description);
            }
            catch(Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Warning
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_OnWarn(object sender, IntegrationWarningEventArgs e) {
            try {
                IntegrationSource Obj = (IntegrationSource)sender;
                IntInstLog.WarnFormat("{0} [{1}]", Obj.Description, e.Message);
            }
            catch (InvalidCastException ex) {
                DebugLog.DebugFormat("Source_OnError expected IntegrationSource as sender. Message: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Setup the Response List
        /// </summary>
        private void InitializeResponses() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // response factory
                IntegrationResponseFactory F = new IntegrationResponseFactory();
                // create all the resposnse
                foreach(XResponse R in m_Integration.Responses) {
                    IntegrationResponse Response = F.Create(R);
                    // attach events
                    Response.OnError += Response_OnError;                  
                    // add it to this list
                    Responses.Add(Response);
                    // log
                    IntInstLog.InfoFormat("Integration response intialized:{0}", Response.Description);
                }
                // log
                IntInstLog.InfoFormat("All integration responses intialized. Response Count=[{0}]", m_Responses.Count);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Log Dat
        /// </summary>
        /// <param name="sender">an IntegrationResponse</param>
        /// <param name="e">event args</param>
        private void Response_OnError(object sender, IntegrationErrorEventArgs e) {
            try {
                IntegrationResponse Obj = (IntegrationResponse)sender;              
                IntInstLog.ErrorFormat("{0} [{1}]", Obj.Description, e.Message);
            }
            catch(InvalidCastException ex) {
                DebugLog.DebugFormat("Response_OnError expected IntegrationResponse as sender. Message: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Log Dat
        /// </summary>
        /// <param name="sender">an IntegrationSource</param>
        /// <param name="e">event args</param>
        private void Source_OnError(object sender, IntegrationErrorEventArgs e) {
            try {
                IntegrationSource Obj = (IntegrationSource)sender;
                IntInstLog.ErrorFormat("{0} [{1}]", Obj.Description, e.Message);
            }
            catch (InvalidCastException ex) {
                DebugLog.DebugFormat("Source_OnError expected IntegrationSource as sender. Message: {0}", ex.Message);
            }               
        }

        /// <summary>
        /// A scan at the source location has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_ScanSource(object sender, ScanSourceEventArgs e) {
            IntInstLog.InfoFormat("{0} [{1}] - Scan", Source.Description, e.Location);
        }

        /// <summary>
        /// Start the integration schedule
        /// </summary>
        public void StartSchedule() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {                
                // start schedule                
                m_Timer.Start();
                IntInstLog.InfoFormat("{0} - Schedule Started", m_Integration.Desc);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
        }

        /// <summary>
        /// Stop the integration schedule
        /// </summary>
        public void StopSchedule() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                m_Timer.Stop();

                // save match history to file

                



                IntInstLog.InfoFormat("{0} - Schedule Stopped", m_Integration.Desc);
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
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
                SvcLog.WarnFormat("{0} - Run canceled. A run is in progress.", m_Integration.Desc);
                return;
            }            
            // Run()
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // log debug
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                

                // check if blocking
                if (!m_IntegrationInterruptEvent.WaitOne(Global.IntegrationInterruptWait)) {
                    SvcLog.WarnFormat("Integration interrupt event is set. {0} is blocked until released.", m_Integration.Desc);
                    if (!m_IntegrationInterruptEvent.WaitOne(Global.IntegrationInterruptTimeout)) {
                        SvcLog.ErrorFormat("Integration interrupt event block was not released in a timely manner. Aborting Run action for {0}", m_Integration.Desc);
                        return;
                    }
                }

                // log integration
                IntInstLog.InfoFormat("{0} - Run Integration", m_Integration.Desc);

                // reset attributes
                m_Attrs.Reset();

                // integration activities
                using (IntegrationTracker T = NewTracker()) {
                    // integrate
                    T.ScanSource();
                    T.GetFiles();
                    T.SupressDuplicates();
                    T.WorkingTransform();
                    T.SourceTransform();
                    T.RunResponses();
                    T.UpdateMatchHistory();
                }
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                m_inRunMethod = 0;
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
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

    }   // IntegrationManager

    /// <summary>
    /// File Match History
    /// </summary>
    public class MatchHistory {


        private XSupressDuplicates m_SupressDuplicates;
        private FileInfo m_MatchHistoryFi;

        /// <summary>
        /// Constructor
        /// </summary>
        public MatchHistory(XSupressDuplicates p_SupressDuplicates, FileInfo p_MatchHistoryFi) {
            m_MatchHistoryFi = p_MatchHistoryFi;
            m_SupressDuplicates = p_SupressDuplicates;
            // read the history in
            ReadMatchHistoryFile();
        }
        
        /// <summary>
        /// Read the match history from file
        /// </summary>
        private void ReadMatchHistoryFile() {
            try {
                if(!m_MatchHistoryFi.Exists) {
                    return;
                }
                using(StreamReader Fin = new StreamReader(m_MatchHistoryFi.FullName, Encoding.ASCII)) {
                    while(!Fin.EndOfStream) {                     
                        MatchHistoryRecord R = JsonConvert.DeserializeObject<MatchHistoryRecord>(Fin.ReadLine());
                        Add(R.Name, R.MD5, R.SHA1, R.Size, R.LastModifiedUTC);                      
                    }
                }               
            }
            catch(Exception ex) {
                throw ex;
            }
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
        /// <param name="p_M">the Matched File</param>
        /// <returns>duplicate flag</returns>
        public bool IsDuplicate(MatchedFile p_M) {
            try {         
                // matched flags
                bool fileName = false;
                bool fileSize = false;
                bool fileDt = false;
                bool fileMD5 = false;
                bool fileSHA1 = false;                
                // check for matches
                if (m_FileNames.Contains(p_M.OriginalName)) {
                    fileName = true;
                    if (m_FileNameSize[p_M.OriginalName].Contains(p_M.Size)) {
                        fileSize = true;
                    }
                    if (m_FileNameLastMod[p_M.OriginalName].Contains(p_M.LastModifiedUTC)) {
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

        private Dictionary<string, HashSet<long>> m_FileNameSize = new Dictionary<string, HashSet<long>>();
        private Dictionary<string, HashSet<long>> m_FileNameLastMod = new Dictionary<string, HashSet<long>>();

        /// <summary>
        /// Add a Matched File to the duplicate list
        /// </summary>
        /// <param name="p_Mf">a Matched File</param>
        public void Add(MatchedFile p_Mf) {
            // don't add duplicate files to the list
            if(IsDuplicate(p_Mf)) {
                return;
            }
            // write to file
            using (StreamWriter Fout = new StreamWriter(m_MatchHistoryFi.FullName, true, Encoding.ASCII)) {                
                Fout.WriteLine(JsonConvert.SerializeObject(new MatchHistoryRecord { Name = p_Mf.OriginalName, MD5 = p_Mf.MD5, SHA1 = p_Mf.SHA1, Size = p_Mf.Size, LastModifiedUTC = p_Mf.LastModifiedUTC }));
            }
            // write to memory
            Add(p_Mf.OriginalName, p_Mf.MD5, p_Mf.SHA1, p_Mf.Size, p_Mf.LastModifiedUTC);  
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

        /// <summary>
        /// The guts of Add
        /// </summary>
        /// <param name="p_name">original name at scan location</param>
        /// <param name="p_MD5">MD5 checksum</param>
        /// <param name="p_SHA1">SHA1 checksum</param>
        /// <param name="p_size">file size</param>
        /// <param name="p_lastModifiedUTC">last modified date UTC</param>
        private void Add(string p_name, string p_MD5, string p_SHA1, long p_size, long p_lastModifiedUTC) {
            // add the file name
            m_FileNames.Add(p_name);
            // add the file hashs
            if (p_MD5.Length > 0) {
                m_MD5.Add(p_MD5);
            }
            if (p_SHA1.Length > 0) {
                m_SHA1.Add(p_SHA1);
            }
            // add the file size            
            if (!m_FileNameSize.ContainsKey(p_name)) {
                m_FileNameSize.Add(p_name, new HashSet<long>());
            }
            m_FileNameSize[p_name].Add(p_size);
            // add the last modified date
            if (!m_FileNameLastMod.ContainsKey(p_name)) {
                m_FileNameLastMod.Add(p_name, new HashSet<long>());
            }
            m_FileNameLastMod[p_name].Add(p_lastModifiedUTC);
        }

        /// <summary>
        /// Add a list of Matched Files to the duplicate list
        /// </summary>
        /// <param name="p_Mf">a list of Matched Files</param>
        public void Add(List<MatchedFile> p_Mf) {
            foreach (MatchedFile M in p_Mf) {
                Add(M);
            }
        }

        /// <summary>
        /// To Serialize
        /// </summary>
        private class MatchHistoryRecord {
            public string Name { get; set; }
            public string MD5 { get; set; }
            public string SHA1 { get; set; }
            public long Size { get; set; }
            public long LastModifiedUTC { get; set; }
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
            ExtractProperties();
        }

        /// <summary>
        /// Extract Properties
        /// </summary>
        private void ExtractProperties() {
            Type MatchedFileT = typeof(MatchedFile);
            FileProps = MatchedFileT.GetProperties();
        }

        /// <summary>
        /// Replace a Token with the corresponding value
        /// </summary>
        /// <param name="p_name"></param>
        /// <returns></returns>
        public object GetReplacementValue(string p_name) {
            try {              
                string[] elements = p_name.Split('.');      
                if(elements.Length == 1) {
                    return elements[0];
                }
                else if(elements.Length == 2) {
                    // to upper the class! 
                    elements[0] = elements[0].ToUpperInvariant();
                    // should replace the case values with constants
                    switch (elements[0]) {
                        case AttrClass.FILE: {
                            return GetFileAttr(elements[1]);                                
                        }
                        case AttrClass.FILES: {
                            return "";
                        }
                        default: {
                            // error handling here.
                            return p_name;  
                        }
                    }
                }
                else {
                    throw new Exception(string.Format("{0} is not a valid token for replacement", p_name));
                }      
            }
            catch(Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Get a file attribute
        /// </summary>
        /// <param name="p_attr">file attribute</param>
        /// <returns></returns>
        private object GetFileAttr(string p_attr) {
            /*
             * VALID FILE ATTRIBUTES:
             * OriginalName
             * WorkingName
             * Size
             * LastModified
             */

            Type MatchedFileT = typeof(MatchedFile);
            PropertyInfo P = MatchedFileT.GetProperty(p_attr);
            return P.GetValue(File);            
        }



        private PropertyInfo[] FileProps;

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


        public List<MatchedFile> Files { get; set; }
        public MatchedFile File { get; set; }

        /// <summary>
        /// Extract Attributes from an XObject
        /// </summary>
        /// <param name="p_Obj">the XObject</param>
        /// <returns>property dictionary</returns>
        private Dictionary<string, object> ExtractAttrs(object p_XObj) {      
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

        private class AttrClass {

            public const string FILE = "FILE";
            public const string FILES = "FILES";

        }

    }   // IntegrationAttributes  

}
