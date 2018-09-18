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
        public IntegrationSourceFactory() {
        }

        /// <summary>
        /// Create an Integration Source object
        /// </summary>
        /// <param name="p_XSource">XSource</param>
        /// <returns></returns>
        public IntegrationSource Create(XSource p_XSource) {

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
    /// Scan Source Event Args
    /// </summary>
    public class ScanSourceEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        public ScanSourceEventArgs(string p_location) {
            Location = p_location;
        }

        /// <summary>
        /// Scan location as a string
        /// </summary>
        public readonly string Location;

    }   // ScanSourceEventArgs

    /// <summary>
    /// On File Match Event Args
    /// </summary>
    public class OnFileMatchEventArgs : IntegrationEventArgs {
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile"></param>
        public OnFileMatchEventArgs(MatchedFile p_MatchedFile, string p_location, IPattern p_Pattern) {
            MatchedFile = p_MatchedFile;            
            Location = p_location;
            Pattern = p_Pattern;
        }

        /// <summary>
        /// The match matter
        /// </summary>
        public readonly IPattern Pattern;

        /// <summary>
        /// The Matched File
        /// </summary>
        public readonly MatchedFile MatchedFile;

        /// <summary>
        /// The Location as a string
        /// </summary>
        public readonly string Location;

    }   // OnFileMatchEventArgs

    /// <summary>
    /// On Got File Event Args
    /// </summary>
    public class GotFileEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile"></param>
        public GotFileEventArgs(MatchedFile p_MatchedFile)
            : base() {
            MatchedFile = p_MatchedFile;
        }

        /// <summary>
        /// The Matched File
        /// </summary>
        public readonly MatchedFile MatchedFile;
        

    }   // OnGotFileEventArgs


    /// <summary>
    /// On Got Files Event Args
    /// </summary>
    public class GotFilesEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFiles">Matched Files</param>
        public GotFilesEventArgs(List<MatchedFile> p_MatchedFiles)
            : base() {
            MatchedFiles = p_MatchedFiles;
        }

        /// <summary>
        /// The Downloaded Files
        /// </summary>
        public readonly List<MatchedFile> MatchedFiles;


    }   // OnGotFileEventArgs

    /// <summary>
    /// Renamed a file event args
    /// </summary>
    public class FileRenamedEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_renamedFrom">the original file name</param>
        /// <param name="p_renamedTo">the new file name</param>
        public FileRenamedEventArgs(string p_renamedFrom, string p_renamedTo)
            : base() {
            // set locals
            RenamedFrom = p_renamedFrom;
            RenamedTo = p_renamedTo;
        }

        /// <summary>
        /// Original File Name
        /// </summary>
        public readonly string RenamedFrom;

        /// <summary>
        /// Renamed File
        /// </summary>
        public readonly string RenamedTo;

    }   // OnRenamedFileEventArgs

    /// <summary>
    /// Transform Source Event Args
    /// </summary>
    public class TransformSourceEventArgs : IntegrationFilesEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile">the matched files</param>
        public TransformSourceEventArgs(List<MatchedFile> p_MatchedFiles)
            : base(p_MatchedFiles) {
        }
        
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
        
        /*
         * when other sources are implented it may be a good idea to implement this attribute to
         * control how the manager/tracker calls methods. In some cases it may be best to act on 
         * one file at a time (for context setting) but for some source locations it may be beneficial
         * to operate on batches of files all at once (i.e. 
        public abstract bool SupportsBatchOps { get; }
        */

        /// <summary>
        /// On Matched File Contact
        /// </summary>
        public event EventHandler<OnFileMatchEventArgs> Match;

        /// <summary>
        /// After a file has been retrived from the source
        /// </summary>
        public event EventHandler<GotFileEventArgs> GotFile;

        /// <summary>
        /// After all files have been retrived from the source
        /// </summary>
        public event EventHandler<GotFilesEventArgs> GotFiles;

        public event EventHandler<TransformSourceEventArgs> SourceTransform;

        public event EventHandler<FileRenamedEventArgs> FileRenamed;

        // integration events
        public event EventHandler<ScanSourceEventArgs> ScanSource;
        public event EventHandler<IntegrationFilesEventArgs> GetFiles;     

        public event EventHandler<IntegrationEventArgs> DeleteFiles;
        public event EventHandler<IntegrationFileEventArgs> DeletedFile;
        public event EventHandler<IntegrationFilesEventArgs> DeletedFiles;

        public event EventHandler<IntegrationEventArgs> DeletedFolder;
        public event EventHandler<IntegrationEventArgs> PingSource;

        

        /// <summary>
        /// Fire the OnContact Event
        /// </summary>
        /// <param name="p_M">the matched file</param>
        /// <param name="p_location">the location as a string</param>
        protected void MatchEvent(MatchedFile p_M, string p_location, IPattern p_Pattern) {
            Match?.Invoke(this, new OnFileMatchEventArgs(p_M, p_location, p_Pattern));
        }

        /// <summary>
        /// Fire the GotFileEvent Event
        /// </summary>
        /// <param name="p_M">downloaded file</param>
        protected void GotFileEvent(MatchedFile p_M) {
            GotFile?.Invoke(this, new GotFileEventArgs(p_M));
        }

        /// <summary>
        /// Fire the Got
        /// </summary>
        /// <param name="p_M">downloaded files</param>
        protected void GotFilesEvent(List<MatchedFile> p_M) {
            GotFiles?.Invoke(this, new GotFilesEventArgs(p_M));
        }

        /// <summary>
        /// Transform Source Event
        /// </summary>
        /// <param name="p_Args">Event Args</param>
        protected void SourceTransformEvent(TransformSourceEventArgs p_Args) {
            SourceTransform?.Invoke(this, p_Args);
        }

        /// <summary>
        /// Renamed File Event
        /// </summary>
        /// <param name="p_renamedFrom">original name</param>
        /// <param name="p_renamedTo">new name</param>
        protected void RenamedFileEvent(string p_renamedFrom, string p_renamedTo) {
            FileRenamed?.Invoke(this, new FileRenamedEventArgs(p_renamedFrom, p_renamedTo));
        }

        /// <summary>
        /// Scanning!
        /// </summary>
        /// <param name="p_location">scan location as string</param>
        protected void ScanEvent(string p_location) {
            ScanSource?.Invoke(this, new ScanSourceEventArgs(p_location));
        }

        protected void GetFilesEvent(List<MatchedFile> p_Files) {
            GetFiles?.Invoke(this, new IntegrationFilesEventArgs(p_Files));
        }

        protected void DeleteFilesEvent() {
            DeleteFiles?.Invoke(this, IntegrationEventArgs.Empty);
        }

        protected void DeletedFileEvent(MatchedFile p_File) {
            DeletedFile?.Invoke(this, new IntegrationFileEventArgs(p_File));
        }

        protected void DeletedFilesEvent(List<MatchedFile> p_Files) {
            DeletedFiles?.Invoke(this, new IntegrationFilesEventArgs(p_Files));            
        }

        protected void DeleteFolderEvent() {
            DeletedFolder?.Invoke(this, IntegrationEventArgs.Empty);
        }

        protected void PingEvent() {
            PingSource?.Invoke(this, IntegrationEventArgs.Empty);
        }

        // integration source activities to be implemented in derived class
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
                return string.IsNullOrWhiteSpace(m_Source.Desc) ? "(none)" : m_Source.Desc;
            }
        }

        /// <summary>
        /// To String
        /// </summary>
        /// <returns>a string representation of the object</returns>
        public override string ToString() {
            return Description;
        }

    }   // IntegrationSource
    
}
