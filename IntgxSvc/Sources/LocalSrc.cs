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
            // method logic
            HashSet<MatchedFile> Matches = new HashSet<MatchedFile>(new MatchedFileComparer());
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                
                // scan logic - first things first - what is the scan location?
                string folder = Folder();                
                // local folder
                DirectoryInfo Di = new DirectoryInfo(folder);
                // scanning
                OnScanEvent(Di.FullName);
                FileInfo[] Files = Di.GetFiles();
                // go thru each pattern
                foreach (IPattern P in p_Pattern) {
                    foreach (FileInfo Fi in Files) {
                        if (P.IsMatch(Fi.Name)) {
                            MatchedFile Match = new MatchedFile(this, Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc);

                            
                            if (Matches.Add(new MatchedFile(this, Fi.Name, Fi.DirectoryName, Fi.Length, Fi.LastWriteTimeUtc))) {
                                // pew pew
                                MatchEvent(Match, folder); 
                            }
                            else {
                                ErrorEvent("Unexpected File Match. A subsequent pattern {0} has matched on: {1}", P.ToString(), Fi.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Get matched files from source location and write to the working directory
        /// </summary>
        /// <param name="p_Mf"></param>
        public override void Get(List<MatchedFile> p_Mf) {        
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();            
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // getting
                GetFilesEvent();
                // go thru matched files
                foreach(MatchedFile F in p_Mf) {
                    // source file
                    FileInfo SourceFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.OriginalName));
                    // the source file should definately exist
                    if (!SourceFi.Exists) {
                        ErrorEvent(new FileNotFoundException("File is missing from integration source", F.Name));
                        // should an error flag be set on the matched file object???
                    }
                    else {
                        // copy to working area
                        SourceFi.CopyTo(F.WorkingFi.FullName, false);
                        // got one
                        GotFileEvent(F);                        
                    }
                }
                // got all
                GotFilesEvent(p_Mf);
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Delete matched files from the source location
        /// </summary>
        /// <param name="p_Mf">matched files</param>
        public override void Delete(List<MatchedFile> p_Mf) {          
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);                
                // deleting
                DeleteFilesEvent();
                // go thru matched files
                foreach (MatchedFile F in p_Mf) {
                    // source file
                    FileInfo SourceFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.Name));
                    // the source file should definately exist
                    if (!SourceFi.Exists) {
                        ErrorEvent(new FileNotFoundException("File is missing from integration source", F.Name));                        
                    }
                    else {
                        try {
                            // delete from source                                                        
                            SourceFi.Delete();
                            DeletedFileEvent(F);
                        }
                        catch(UnauthorizedAccessException ex) {
                            ErrorEvent(ex);
                        }
                        catch(IOException ex) {
                            ErrorEvent(ex);
                        }                        
                    }
                }
                // deleted them all!
                DeletedFilesEvent(p_Mf);
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }
        }

        /// <summary>
        /// Transform files at the source location
        /// </summary>
        /// <param name="p_Mf">the matched files</param>        
        public override void Transform(List<MatchedFile> p_Mf) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                // transform event arguments
                TransformSourceEventArgs Args = new TransformSourceEventArgs(p_Mf);

                // perform transforms on the model
                DoTransformEvent(Args);
                // were any transforms performed?
                if(!Args.HasTransforms) {
                    return;
                }

                // go thru matched files (model) and perform **actual** transforms at the source location
                foreach (MatchedFile F in p_Mf) {
                    // name changed?
                    if(F.Name.Equals(F.OriginalName)) {
                        continue;
                    }                                        
                    // source file
                    FileInfo SourceFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.OriginalName));
                    // it better exist...
                    if(!SourceFi.Exists) {
                        ErrorEvent(new FileNotFoundException("Source file is unexpectedly missing", SourceFi.FullName));
                        continue;
                    }
                    // target file
                    FileInfo RenameFi = new FileInfo(string.Format("{0}\\{1}", F.Folder, F.Name));
                    // it better not exist...
                    if (RenameFi.Exists) {
                        ErrorEvent(string.Format("Source transform error: A file with the name {0} already exists at the source location {1}", RenameFi.Name, RenameFi.DirectoryName));
                        continue;
                    }
                    // if it is actually different 
                    if (!SourceFi.Name.Equals(RenameFi.Name)) {                      
                        // do the rename
                        SourceFi.MoveTo(RenameFi.FullName);
                        // raise the event
                        RenamedFileEvent(F.OriginalName, RenameFi.Name);
                    }
                }
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
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