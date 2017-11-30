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
using System.Diagnostics;

using System.Text;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Network Source
    /// </summary>
    public class LocalSrc : IntegrationObject, ISourceLocation {

        // XNetworkSrc object        
        private XLocalSrc m_XLocalSrc;

        /// <summary>
        /// Constructor
        /// </summary>
        public LocalSrc(string p_sourceDesc, XSourceLocation p_XSourceLocation) :
            base(p_XSourceLocation) {


            m_description = p_sourceDesc;
            m_XLocalSrc = (XLocalSrc)p_XSourceLocation;
        }

        /// <summary>
        /// Source Description
        /// </summary>
        public string Description {
            get {
                return m_description;
            }
        }
        // member
        private string m_description;

        /// <summary>
        /// Scan the source location
        /// </summary>
        /// <param name="p_Pattern">the file matching patterns</param>
        /// <returns>a list of matched files</returns>
        public MatchedFile[] Scan(IPattern[] p_Pattern, IntegrationTracker p_T) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();

            HashSet<MatchedFile> Matches = new HashSet<MatchedFile>();
            try {


                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                p_T.Log.InfoFormat("Scanning {0}", Description);

                string folder = IsDynamic("Folder") ? DynamicText["Folder"].Run(p_T.Attrs.GetAttrs()) : m_XLocalSrc.Folder;               
                

                DirectoryInfo Di = new DirectoryInfo(folder);
                FileInfo[] Files = Di.GetFiles();

                // go thru each pattern
                foreach (IPattern P in p_Pattern) {
                    foreach (FileInfo Fi in Files) {
                        if (P.IsMatch(Fi.Name)) {
                            p_T.Log.InfoFormat("Matched {0} at {1} using Pattern {2}", Fi.Name, Fi.DirectoryName, P.ToString());
                            Matches.Add(new MatchedFile(this, Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc));                                                           
                        }
                    }
                }               

                // add the matches to the tracker
                p_T.MatchedFiles = Matches.ToArray();

            }
            catch (DirectoryNotFoundException ex) {
                p_T.Log.WarnFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            catch (DriveNotFoundException ex) {
                p_T.Log.WarnFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            catch (IOException ex) {
                p_T.Log.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
            }
            catch (Exception ex) {
                p_T.Log.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
            // out
            return p_T.MatchedFiles;
        }

        

        /// <summary>
        /// Get matched files from source location and write to the working directory
        /// </summary>
        /// <param name="p_Mf"></param>
        public void Get(MatchedFile[] p_Mf, IntegrationTracker p_T) {            
            foreach(MatchedFile M in p_Mf) {       
                // get at the source file
                FileInfo Fi = new FileInfo(string.Format("{0}\\{1}", M.Folder, M.Name));
                Debug.Assert(Fi.Exists);
                Debug.Assert(!M.WorkingFi.Exists);
                // copy to the working folder
                Fi.CopyTo(M.WorkingFi.FullName);
            }
        }

        /// <summary>
        /// Delete matched files from the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        public void Delete(MatchedFile[] p_Mf, IntegrationTracker p_T) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Rename matched files in the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        /// <param name="p_rename"></param>
        public void Rename(MatchedFile[] p_Mf, string[] p_rename, IntegrationTracker p_T) {
            throw new NotImplementedException();
        }        

        /// <summary>
        /// Ping the source location to verify connectivity
        /// </summary>
        public void Ping(IntegrationTracker p_T) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            HashSet<MatchedFile> Matches = new HashSet<MatchedFile>();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                string folder = IsDynamic("Folder") ? DynamicText["Folder"].Run(p_T.Attrs.GetAttrs()) : m_XLocalSrc.Folder;                
                DirectoryInfo Di = new DirectoryInfo(folder);
                if(Di.Exists) {
                    // return true?
                }  
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            } 
        }

        /// <summary>
        /// Delete the deepest sub-directory from the given location
        /// </summary>
        /// <param name="p_folder"></param>
        public void DeleteFolder(string p_folder, IntegrationTracker p_T) {
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