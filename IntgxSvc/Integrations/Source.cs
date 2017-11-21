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
            // interface to a source location
            ISourceLocation SourceLocation;
            // what is the source location??
            try {
                Type T = p_XSource.Item.GetType();
                switch (T.Name) {
                    case "XLocalSrc": {
                            throw new NotImplementedException();
                        }
                    case "XNetworkSrc": {
                            SourceLocation = new NetworkSrc(p_XSource.Desc, p_XSource.Item);                            
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
                return new IntegrationSource(p_XSource, SourceLocation);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }   // IntegrationSourceFactory

    /// <summary>
    /// Base class for all integration objects
    /// </summary>
    public class IntegrationObject {

        // log        
        public ILog SvcLog;
        public ILog DebugLog;
        public ILog IntLog;

        /// <summary>
        /// Constructor
        /// </summary>
        protected IntegrationObject(object p_IntegrationObj) {

            // log refs
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);
            DebugLog = LogManager.GetLogger(Global.DebugLogName);


            ExtractObjAttrs(p_IntegrationObj);
        }

        /// <summary>
        /// Does the referenced property require dynamic text processing?
        /// </summary>
        /// <param name="p_Info">the property</param>
        /// <returns>true false</returns>
        protected bool IsDynamic(PropertyInfo p_Info) {
            return m_DynamicText.ContainsKey(p_Info.Name);
        }
        protected bool IsDynamic(string p_propertyName) {
            return m_DynamicText.ContainsKey(p_propertyName);
        }
        /// <summary>
        /// Compiled Dynamic Text
        /// </summary>
        protected Dictionary<string, DynamicTextParser> DynamicText {
            get {
                return m_DynamicText;
            }
        }
        // member
        private Dictionary<string, DynamicTextParser> m_DynamicText = new Dictionary<string, DynamicTextParser>();

        /// <summary>
        /// Find all attributes of the integration object, and compile any dynamic text
        /// </summary>
        protected void ExtractObjAttrs(object p_IntegrationObj) {
            try {
                Type T = p_IntegrationObj.GetType();
                m_ObjectProps = T.GetProperties();
                foreach (PropertyInfo P in m_ObjectProps) {
                    if (P.PropertyType == typeof(string)) {
                        string text = P.GetValue(p_IntegrationObj).ToString();
                        DynamicTextParser DyText = new DynamicTextParser(text);
                        if (DyText.Compile()) {
                            m_DynamicText.Add(P.Name, DyText);
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /*
        public void SetIntegrationAttrs(Dictionary<string, object> p_Attrs) {
            m_Attrs = p_Attrs;
        }
        protected Dictionary<string, object> Attrs {
            get {
                return m_Attrs;
            }
        }
        private Dictionary<string, object> m_Attrs;
        */


        /// <summary>
        /// Adds a new dynamic element to the collection
        /// </summary>
        /// <param name="p_key"></param>
        /// <param name="p_text"></param>
        protected void AddDynamicText(string p_key, string p_text) {
            if(m_DynamicText.ContainsKey(p_key)) {
                throw new Exception(string.Format("a dynamic text element with the key {0} already exists", p_key));
            }
            DynamicTextParser DyText = new DynamicTextParser(p_text);
            if (DyText.Compile()) {
                m_DynamicText.Add(p_key, DyText);
            }
        }

        protected PropertyInfo[] ObjectProps {
            get {
                return m_ObjectProps;
            }
        }
        // member
        private PropertyInfo[] m_ObjectProps;

        /// <summary>
        /// Create Logger
        /// </summary>
        /// <param name="p_name"></param>
        /// <returns></returns>
        protected ILog CreateLogger(string p_name)
        {
            return LogManager.GetLogger(p_name);
        }

    }   // IntegrationObject

    /// <summary>
    /// Integration Source Base
    /// </summary>
    public class IntegrationSource : IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Source"></param>
        /// <param name="p_SourceLocation"></param>
        public IntegrationSource(XSource p_Source, ISourceLocation p_SourceLocation) :
            base(p_Source) {
            m_Source = p_Source;
            m_SourceLocation = p_SourceLocation;
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

        private XSource m_Source;
        

        /// <summary>
        /// Constructor
        /// </summary>
        protected IntegrationSource(XSource p_Source) 
            : base(p_Source) {
            // log of this integration
            IntLog = CreateLogger(p_Source.Desc);
        }                   

    }   // IntegrationSource


    /// <summary>
    /// Network Source
    /// </summary>
    public class NetworkSrc: IntegrationObject, ISourceLocation
    {
                
        // XNetworkSrc object        
        private XNetworkSrc m_XNetworkSrc;        

        /// <summary>
        /// Constructor
        /// </summary>
        public NetworkSrc(string p_sourceDesc, XSourceLocation p_XSourceLocation) :
            base(p_XSourceLocation) {

            // so i need to compile the dynamic text here
            // and store it ... somehow ... for it to be accessed during the integration function (scan, etc.) so it can be called
            // i think i will need a separate object to manage the dynamic text of the integration objects
            // some object to read and organize all of the integration attributes and make them available to the parser

            m_description = p_sourceDesc;
            m_XNetworkSrc = (XNetworkSrc)p_XSourceLocation;
            

            //Attrs.Add("Source.Desc", m_description);



            IntLog = CreateLogger(p_sourceDesc);

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
        public MatchedFile[] Scan(IPattern[] p_Pattern) {                                            
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);

                IntLog.InfoFormat("Scanning {0}", Description);

                // TODO: ADD DYNAMIC TEXT LOGIC 
                // .Folder needs to be processed!! 


                // problem is the Attrs we set in the integration manager is on the integrationsource object, this is the attrs from the location xnetworkfolder
                // really gotta figure out where this lives, probabaly on the manager/tracker
                string folder = IsDynamic("Folder") ? DynamicText["Folder"].Run(Attrs) : m_XNetworkSrc.Folder;

                







                DirectoryInfo Di = new DirectoryInfo(folder);
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
