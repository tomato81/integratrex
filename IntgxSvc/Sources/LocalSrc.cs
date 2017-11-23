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
    public class LocalSrc : IntegrationObject, ISourceLocation {

        // XNetworkSrc object        
        private XLocalSrc m_XLocalSrc;

        /// <summary>
        /// Constructor
        /// </summary>
        public LocalSrc(string p_sourceDesc, XSourceLocation p_XSourceLocation) :
            base(p_XSourceLocation) {
            // integration log
            IntLog = CreateLogger(p_sourceDesc);

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

                IntLog.InfoFormat("Scanning {0}", Description);

                string folder = IsDynamic("Folder") ? DynamicText["Folder"].Run(p_T.Attrs.GetAttrs()) : m_XLocalSrc.Folder;




                DirectoryInfo Di = new DirectoryInfo(folder);
                FileInfo[] Files = Di.GetFiles();

                // go thru each pattern
                foreach (IPattern P in p_Pattern) {
                    foreach (FileInfo Fi in Files) {
                        if (P.IsMatch(Fi.Name)) {
                            Matches.Add(new MatchedFile(Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc));
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
            // out
            return Matches.ToArray<MatchedFile>();
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