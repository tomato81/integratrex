using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

// lawg
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
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
    public class OnGotFileEventArgs : EventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile"></param>
        public OnGotFileEventArgs(MatchedFile p_MatchedFile) {
            MatchedFile = p_MatchedFile;
        }

        // the matched file
        public readonly MatchedFile MatchedFile;

    }   // OnGotFileEventArgs

    /// <summary>
    /// Integration Source Base
    /// </summary>
    public abstract class IntegrationSource : IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Source">X object</param>
        /// <param name="p_SourceLocation">Source Location</param>
        public IntegrationSource(XSource p_Source, ISourceLocation p_SourceLocation) :
            base() {
            m_Source = p_Source;
            m_SourceLocation = p_SourceLocation;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Source">X object</param>
        protected IntegrationSource(XSource p_Source)
            : base() {
            // things
        }

        /// <summary>
        /// On Matched File Contact
        /// </summary>
        public event EventHandler<OnFileMatchEventArgs> OnFileMatch;

        /// <summary>
        /// After a file has been retrived from the source
        /// </summary>
        public event EventHandler<OnGotFileEventArgs> OnGotFile;

        /// <summary>
        /// Fire the OnContact Event
        /// </summary>
        /// <param name="p_M"></param>
        protected void OnContactEvent(MatchedFile p_M) {
            OnFileMatch?.Invoke(this, new OnFileMatchEventArgs(p_M));
        }

        /// <summary>
        /// Fire the OnContact Event
        /// </summary>
        /// <param name="p_M"></param>
        protected void OnGotFileEvent(MatchedFile p_M) {
            OnGotFile?.Invoke(this, new OnGotFileEventArgs(p_M));
        }

        /// <summary>
        /// Source Location
        /// </summary>
        public ISourceLocation Location {
            get {
                return m_SourceLocation;
            }
        }
        // member
        private ISourceLocation m_SourceLocation;

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
