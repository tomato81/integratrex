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

        // XNetworkSrc object        
        private XNetworkSrc m_XNetworkSrc;

        /// <summary>
        /// Constructor
        /// </summary>
        public NetworkSrc(XSource p_XSource, XNetworkSrc p_XNetworkSrc) :
            base(p_XSource) {
            m_XNetworkSrc = p_XNetworkSrc;
            
            CompileDynamicText();
        }        

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
        public override void Scan(IPattern[] p_Pattern) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try { 
                throw new NotImplementedException();
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
            try {                
                throw new NotImplementedException();
            }
            catch(Exception ex) {
                ErrorEvent(ex);
            }
        }

        /// <summary>
        /// Delete matched files from the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        public override void Delete(List<MatchedFile> p_Mf) {
            try {
                
                throw new NotImplementedException();
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }            
        }

        /// <summary>
        /// Rename matched files in the source location
        /// </summary>
        /// <param name="p_Mf"></param>
        /// <param name="p_rename"></param>
        public override void Transform(List<MatchedFile> p_Mf) {
            try {
                
                throw new NotImplementedException();
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }            
        }

        /// <summary>
        /// Ping the source location to verify connectivity
        /// </summary>
        public override void Ping() {            
            try {
                
                throw new NotImplementedException();
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }
        }

        /// <summary>
        /// Delete the deepest sub-directory from the given location
        /// </summary>
        /// <param name="p_folder"></param>
        public override void DeleteFolder(string p_folder) {            
            try {
                
                throw new NotImplementedException();
            }
            catch (Exception ex) {
                ErrorEvent(ex);
            }
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