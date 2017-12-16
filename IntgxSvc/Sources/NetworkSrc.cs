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
    /// Network Source
    /// </summary>
    public class NetworkSrc : IntegrationSource, ISourceLocation {

        /// <summary>
        /// Constructor
        /// </summary>
        public NetworkSrc(XSource p_XSource, XNetworkSrc p_XNetworkSrc) :
            base(p_XSource) {
            m_XNetworkSrc = p_XNetworkSrc;            
        }

        // XNetworkSrc object        
        private XNetworkSrc m_XNetworkSrc;

        /// <summary>
        /// Create and Compile Dynamic Text
        /// </summary>
        protected override void CompileDynamicText() {
            m_Folder = new DynamicTextParser(m_XNetworkSrc.Path.Value);
            m_Folder.OnValueRequired += ValueRequired;
            m_Folder.Compile();
        }


        [DynamicText]
        public string Folder() {   
            return m_Folder.Run();                   
        }
        private DynamicTextParser m_Folder;                     

        /// <summary>
        /// Scan the source location
        /// </summary>
        /// <param name="p_Pattern">the file matching patterns</param>
        /// <returns>a list of matched files</returns>
        public void Scan(IPattern[] p_Pattern) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();

            HashSet<MatchedFile> Matches = new HashSet<MatchedFile>();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                Log.InfoFormat("Scanning {0}", Description);

                string folder = Folder();

                DirectoryInfo Di = new DirectoryInfo(folder);
                FileInfo[] Files = Di.GetFiles();

                // go thru each patternp_T.Log.InfoFormat("Contact {0}", Fi.FullName);
                foreach (IPattern P in p_Pattern) {
                    foreach (FileInfo Fi in Files) {                        
                        if (P.IsMatch(Fi.Name)) {
                            MatchedFile Match = new MatchedFile(this, Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc);
                            if (Matches.Add(new MatchedFile(this, Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc))) {
                                // pew pew
                                OnContactEvent(Match);
                            }
                        }
                    }
                }

            }
            catch (DirectoryNotFoundException ex) {
                IntLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            catch (DriveNotFoundException ex) {
                IntLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            catch (IOException ex) {
                IntLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            catch (Exception ex) {
                IntLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
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
        public void Get(List<MatchedFile> p_Mf) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete matched files from the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        public void Delete(List<MatchedFile> p_Mf) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Rename matched files in the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        /// <param name="p_rename"></param>
        public void Rename(List<MatchedFile> p_Mf, string[] p_rename) {
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

        /// <summary>
        /// Can MD5 or SHA1 be calculated at the source?
        /// </summary>
        /// <returns></returns>
        public bool CanCalc() {
            return true;
        }

    }   // end of class
}