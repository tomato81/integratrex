using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

// lib
using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {


    /// <summary>
    /// Integration Source Factory
    /// </summary>
    public class IntegrationSourceFactory
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationSourceFactory()
        {
        }

        /// <summary>
        /// Create an Integration Source object
        /// </summary>
        /// <param name="p_XSource">XSource</param>
        /// <returns></returns>
        public IntegrationSource Create(XSource p_XSource)
        {
            // interface to a source 
            IntegrationSource Source;
            
            // what is the source location??
            try {
                Type T = p_XSource.Item.GetType();
                switch (T.Name) {
                    case "XLocalSrc": {
                            Source = new LocalSrc(p_XSource, (XLocalSrc)p_XSource.Item);
                            break;
                        }
                    case "XNetworkSrc": {
                            Source = new NetworkSrc(p_XSource, (XNetworkSrc)p_XSource.Item);                            
                            break;
                        }
                    case "XWebSrc": {
                            throw new NotImplementedException();
                        }
                    case "XFTPSrc": {
                            throw new NotImplementedException();
                        }
                    case "XSFTPSrc": {
                            throw new NotImplementedException();
                        }
                    default: {
                            throw new InvalidOperationException();
                        }
                }
                // return a new integration source object with the correct source location
                return Source;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }   // IntegrationSourceFactory    

    /// <summary>
    /// On File Match Event Args
    /// </summary>
    public class OnFileMatchEventArgs : EventArgs {
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile"></param>
        public OnFileMatchEventArgs(MatchedFile p_MatchedFile) {
            MatchedFile = p_MatchedFile;
        }

        // the matched file
        public readonly MatchedFile MatchedFile;    

    }   // OnFileMatchEventArgs

    /// <summary>
    /// On Got File Event Args
    /// </summary>
    public class OnGotFileEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile"></param>
        public OnGotFileEventArgs(MatchedFile p_MatchedFile)
            : base() {
            m_MatchedFile = p_MatchedFile;
        }

        /// <summary>
        /// The Matched File
        /// </summary>
        public MatchedFile MatchedFile => m_MatchedFile;
        public readonly MatchedFile m_MatchedFile;

    }   // OnGotFileEventArgs

    /// <summary>
    /// Renamed a file event args
    /// </summary>
    public class OnRenamedFileEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile"></param>
        public OnRenamedFileEventArgs(MatchedFile p_MatchedFile, string p_originalName, string p_renamedTo)
            : base() {
            m_MatchedFile = p_MatchedFile;
            OriginalName = p_originalName;
            RenamedTo = p_renamedTo;
        }

        /// <summary>
        /// The Matched File
        /// </summary>
        public MatchedFile MatchedFile => m_MatchedFile;
        private readonly MatchedFile m_MatchedFile;

        /// <summary>
        /// Original File Name
        /// </summary>
        public readonly string OriginalName;

        /// <summary>
        /// Renamed File
        /// </summary>
        public readonly string RenamedTo;

    }   // OnRenamedFileEventArgs

    /// <summary>
    /// Transform Source Event Args
    /// </summary>
    public class TransformSourceEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile">the matched file</param>
        public TransformSourceEventArgs(List<MatchedFile> p_MatchedFiles)
            : base() {
            MatchedFiles = p_MatchedFiles;
        }

        /// <summary>
        /// The Matched File
        /// </summary>        
        public readonly List<MatchedFile> MatchedFiles;
        
        /// <summary>
        /// Flag if any transforms have been applied
        /// </summary>
        public bool HasTransforms = false;

    }   // TransformSourceEventArgs

    /// <summary>
    /// Integration Source Base
    /// </summary>
    public abstract class IntegrationSource : IntegrationObject, ISourceLocation {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Source">X object</param>
        /// <param name="p_SourceLocation">Source Location</param>
        public IntegrationSource(XSource p_Source) :
            base() {
            m_Source = p_Source;
        }


        /// <summary>
        /// On Matched File Contact
        /// </summary>
        public event EventHandler<OnFileMatchEventArgs> Match;

        /// <summary>
        /// After a file has been retrived from the source
        /// </summary>
        public event EventHandler<OnGotFileEventArgs> GotFile;



        public event EventHandler<TransformSourceEventArgs> DoTransform;

        // integration events
        public event EventHandler<IntegrationEventArgs> ScanSource;
        public event EventHandler<IntegrationEventArgs> GetFiles;
        public event EventHandler<IntegrationEventArgs> DeleteFiles;        
        public event EventHandler<IntegrationEventArgs> DoDeleteFolder;
        public event EventHandler<IntegrationEventArgs> DoPing;

        /// <summary>
        /// Fire the OnContact Event
        /// </summary>
        /// <param name="p_M"></param>
        protected void MatchEvent(MatchedFile p_M) {
            Match?.Invoke(this, new OnFileMatchEventArgs(p_M));
        }

        /// <summary>
        /// Fire the OnContact Event
        /// </summary>
        /// <param name="p_M"></param>
        protected void GotFileEvent(MatchedFile p_M) {
            GotFile?.Invoke(this, new OnGotFileEventArgs(p_M));
        }

        /// <summary>
        /// Transform Source Event
        /// </summary>
        /// <param name="p_Args">Event Args</param>
        protected void DoTransformEvent(TransformSourceEventArgs p_Args) {
            DoTransform?.Invoke(this, p_Args);
        }

        /// <summary>
        /// Scanning!
        /// </summary>
        protected void OnScanEvent() {
            ScanSource?.Invoke(this, IntegrationEventArgs.Empty);
        }

        protected void GetFileEvent() {
            GetFiles?.Invoke(this, IntegrationEventArgs.Empty);
        }

        protected void DeleteFileEvent() {
            DeleteFiles?.Invoke(this, IntegrationEventArgs.Empty);
        }

        protected void DeleteFolderEvent() {
            DoDeleteFolder?.Invoke(this, IntegrationEventArgs.Empty);
        }

        protected void PingEvent() {
            DoPing?.Invoke(this, IntegrationEventArgs.Empty);
        }

        public abstract void Scan(IPattern[] p_Pattern);
        public abstract void Get(List<MatchedFile> p_Mf);
        public abstract void Delete(List<MatchedFile> p_Mf);
        public abstract void Transform(List<MatchedFile> p_Mf);
        public abstract void DeleteFolder(string p_folder);
        public abstract void Ping();
        public abstract bool CanCalc();

        // XSource
        private XSource m_Source;

        /// <summary>
        /// Source Description
        /// </summary>
        public string Description {
            get {
                return m_Source.Desc;
            }
        }                         

    }   // IntegrationSource
    
}
