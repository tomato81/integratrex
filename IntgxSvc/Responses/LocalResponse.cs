using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C2InfoSys.FileIntegratrex.Lib;
using System.IO;
using System.Reflection;

namespace C2InfoSys.FileIntegratrex.Svc {

    public class LocalResponse : IntegrationResponse {

        // XNetworkSrc object        
        private XLocalTgt m_LocalTgt;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Response"></param>
        /// <param name="p_LocalTgt"></param>
        public LocalResponse(XResponse p_Response, XLocalTgt p_LocalTgt)
            : base(p_Response) {
            m_LocalTgt = p_LocalTgt;            
            CompileDynamicText();
        }

        /// <summary>
        /// Create and Compile Dynamic Text
        /// </summary>
        protected override void CompileDynamicText() {
            m_Folder = new DynamicTextParser(m_LocalTgt.Path.Value);
            m_Folder.OnValueRequired += ValueRequired;
            m_Folder.Compile();
        }

        [DynamicText]
        public string Folder() {
            return m_Folder.Run();
        }
        private DynamicTextParser m_Folder;

        /// <summary>
        /// String representaion of the action to take at the target
        /// </summary>
        public override string ActionDesc => Enum.GetName(typeof(XLocalTgtAction), m_LocalTgt.Action);

        /// <summary>
        /// Action!
        /// </summary>
        /// <param name="p_Mf">Matched Files</param>
        public override void Action(List<MatchedFile> p_Mf) {
            ActionStartedEvent();                      
            switch (m_LocalTgt.Action) {
                case XLocalTgtAction.None:
                    break;
                case XLocalTgtAction.Copy:
                    Copy(p_Mf);
                    break;
                default:
                    break;
            }
            ActionCompleteEvent(p_Mf);
        }

        /// <summary>
        /// Copy Action
        /// </summary>
        /// <param name="p_Mf">Matched Files</param>
        private void Copy(List<MatchedFile> p_Mf) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            // method logic            
            try {
                DebugLog.DebugFormat(Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // what is the target location?
                string folder = Folder();
                // verify format of folder name??                             
                // local folder
                DirectoryInfo Di = new DirectoryInfo(folder);
                // maybe create the directory
                if (!Di.Exists && m_LocalTgt.CreateDirectory == XLocalTgtCreateDirectory.Y) {
                    Di.Create();
                    LocationCreatedEvent(Di.FullName);
                }                
                // maybe overrwrite files
                bool overwrite = m_LocalTgt.Overwrite == XLocalTgtOverwrite.Y;
                // go thru files
                foreach (MatchedFile Mf in p_Mf) {
                    // supress response on this file?
                    if(Mf.Supress) {
                        ResponseSupressedEvent();
                        continue;
                    }                    

                    string copyToPath = Path.Combine(folder, Mf.WorkingName);                    
                    if(File.Exists(copyToPath)) {
                        FileExistsEvent(Mf, folder);
                        if (overwrite) {
                            FileOverrwriteEvent(copyToPath);
                        }
                    }
                    try {
                        Mf.WorkingFi.CopyTo(copyToPath, overwrite);
                        FileActionedEvent(Mf, ActionDesc, copyToPath);
                    }
                    catch(IOException ex) {
                        ErrorEvent(ex);
                    }
                    catch(Exception ex) {
                        ErrorEvent(ex);
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



    }   // LocalResponse
}
