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
    /// Source Location Factory
    /// </summary>
    public class SourceLocationFactory {

        /// <summary>
        /// Constructor
        /// </summary>
        public SourceLocationFactory() {
        }

        /// <summary>
        /// Create a Source Location object
        /// </summary>
        /// <param name="p_XSource">XSource</param>
        /// <returns></returns>
        public ISourceLocation Create(XSource p_XSource) {
            try {
                Type T = p_XSource.Item.GetType();
                switch(T.Name) {
                    case "XLocalSrc": {
                        throw new NotImplementedException();
                    }
                    case "XNetworkSrc": {
                        return new NetworkSrc(p_XSource);
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
            }
            catch(Exception ex) {
                throw ex;
            }
        }

    }   // SourceLocationFactory

    /// <summary>
    /// Base class for all integration objects
    /// </summary>
    public class IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationObject() {
        }
       
        
    }   // IntegrationObject

    /// <summary>
    /// Integration Source Base
    /// </summary>
    public class IntegrationSource {

        // log        
        public ILog SvcLog;
        public ILog DebugLog;
        public ILog IntLog;

                

        /// <summary>
        /// Constructor
        /// </summary>
        protected IntegrationSource() {
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);
            DebugLog = LogManager.GetLogger(Global.DebugLogName);
        }

        protected IntegrationSource(string p_traceSourceName) :
            this() {
            IntLog = CreateLogger(p_traceSourceName);
        }

        private ILog CreateLogger(string p_name) {
            return LogManager.GetLogger(p_name);
        }        

    }   // IntegrationSource


    /// <summary>
    /// Network Source
    /// </summary>
    public class NetworkSrc: IntegrationSource, ISourceLocation {

        private XSource m_XSource;        
        private XNetworkSrc m_XNetworkSrc;

        /// <summary>
        /// Constructor
        /// </summary>
        public NetworkSrc(XSource p_XSource) :
            base(p_XSource.Desc) {
            

            // so i need to compile the dynamic text here
            // and store it ... somehow ... for it to be accessed during the integration function (scan, etc.) so it can be called

            m_XSource = p_XSource;
            m_XNetworkSrc = (XNetworkSrc)p_XSource.Item;                       

            // need to use reflection to identify the string properties of each object and parse them out
        }               

        /// <summary>
        /// Scan the source location
        /// </summary>
        /// <param name="p_Pattern">the file matching patterns</param>
        /// <returns>a list of matched files</returns>
        public MatchedFile[] Scan(IPattern[] p_Pattern) {                                            
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                IntLog.InfoFormat("Scanning {0}", m_XSource.Desc);

                // TODO: ADD DYNAMIC TEXT LOGIC 
                // .Folder needs to be processed!! 




                DirectoryInfo Di = new DirectoryInfo(m_XNetworkSrc.Folder);
                FileInfo[] Files = Di.GetFiles();
                HashSet<MatchedFile> Matches = new HashSet<MatchedFile>();
                // go thru each pattern
                foreach(IPattern P in p_Pattern) {                                                       
                    foreach(FileInfo Fi in Files) {
                        if(P.IsMatch(Fi.Name)) {
                            Matches.Add(new MatchedFile(Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc));
                        }
                    }
                }
                // out
                return Matches.ToArray<MatchedFile>();
            }
            catch(Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Get matched files from source location and write to the working directory
        /// </summary>
        /// <param name="p_Mf"></param>
        public void Get(MatchedFile[] p_Mf) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete matched files from the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        public void Delete(MatchedFile[] p_Mf) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Rename matched files in the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        /// <param name="p_rename"></param>
        public void Rename(MatchedFile[] p_Mf, string[] p_rename) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ping the source location to verify connectivity
        /// </summary>
        public void Ping() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete the deepest sub-directory from the given location
        /// </summary>
        /// <param name="p_folder"></param>
        public void DeleteFolder(string p_folder) {
            throw new NotImplementedException();
        }

    }   // end of class
}
