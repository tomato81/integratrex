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
    /// Local Source
    /// </summary>
    public class LocalSrc : IntegrationSource {

        /// <summary>
        /// Constructor
        /// </summary>
        public LocalSrc(XSource p_XSource, XLocalSrc p_XLocalSrc) :
            base(p_XSource) {
            m_XLocalSrc = p_XLocalSrc;
            CompileDynamicText();
        }

        // XNetworkSrc object        
        private XLocalSrc m_XLocalSrc;

        /// <summary>
        /// Create and Compile Dynamic Text
        /// </summary>
        protected override void CompileDynamicText() {
            m_Folder = new DynamicTextParser(m_XLocalSrc.Path.Value);
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
        public override void Scan(IPattern[] p_Pattern) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();

            HashSet<MatchedFile> Matches = new HashSet<MatchedFile>();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                // scanning
                OnScanEvent();
                // scan logic
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
                                MatchEvent(Match);  
                            }
                        }
                    }
                }

            }
            catch (Exception ex) {
                ErrorEvent(new IntegrationErrorEventArgs(ex));
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Get matched files from source location and write to the working directory
        /// </summary>
        /// <param name="p_Mf"></param>
        public override void Get(List<MatchedFile> p_Mf) {        
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                // getting
                GetFileEvent();

                // go thru matched files
                foreach(MatchedFile F in p_Mf) {
                    // source file
                    FileInfo SourceFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.OriginalName));
                    // the source file should definately exist
                    if (!SourceFi.Exists) {
                        ErrorEvent(new IntegrationErrorEventArgs(new FileNotFoundException("File is missing from integration source", F.Name)));
                    }
                    else {
                        // copy to working area
                        SourceFi.CopyTo(F.WorkingFi.FullName, false);
                        // got one
                        GotFileEvent(F);
                    }
                }
            }
            catch (Exception ex) {
                ErrorEvent(new IntegrationErrorEventArgs(ex));
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }

        }

        /// <summary>
        /// Delete matched files from the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        public override void Delete(List<MatchedFile> p_Mf) {
            DeleteFileEvent();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Rename matched files in the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        /// <param name="p_rename"></param>
        public override void Transform(List<MatchedFile> p_Mf) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);


                TransformSourceEventArgs Args = new TransformSourceEventArgs(p_Mf);

                // perform transforms on the model
                DoTransformEvent(Args);

                // but were there any?
                if(!Args.HasTransforms) {
                    return;
                }

                // go thru matched files and perform actual transforms at the source
                foreach (MatchedFile F in p_Mf) {
                    // name changed?
                    if(F.Name.Equals(F.OriginalName)) {
                        continue;
                    }                                        
                    // source file
                    FileInfo SourceFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.OriginalName));
                    // it better exist...
                    if(!SourceFi.Exists) {
                        ErrorEvent(new IntegrationErrorEventArgs(
                            new FileNotFoundException("Source file is unexpectedly missing", SourceFi.FullName)));
                        continue;
                    }
                    // target file
                    FileInfo RenameFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.Name));
                    // it better not exist...
                    if (RenameFi.Exists) {
                        ErrorEvent(new IntegrationErrorEventArgs(
                            new Exception(string.Format("Source transform error: A file with the name {0} already exists at the source location {1}", RenameFi.Name, RenameFi.DirectoryName))));
                        continue;
                    }
                    // do the rename
                    SourceFi.MoveTo(RenameFi.FullName);
                }
            }
            catch (Exception ex) {
                ErrorEvent(new IntegrationErrorEventArgs(ex));
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Ping the source location to verify connectivity
        /// </summary>
        public override void Ping() {
            PingEvent();

            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete the deepest sub-directory from the given location
        /// </summary>
        /// <param name="p_folder"></param>
        public override void DeleteFolder(string p_folder) {

            DeleteFolderEvent();

            throw new NotImplementedException();
        }

        /// <summary>
        /// Can MD5 or SHA1 be calculated at the source?
        /// </summary>
        /// <returns></returns>
        public override bool CanCalc() {
            return true;
        }

    }   // end of class
}