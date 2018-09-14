using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using C2InfoSys.FileIntegratrex.Lib;

using Newtonsoft.Json;


namespace C2InfoSys.FileIntegratrex.Svc {       

    /// <summary>
    /// Matched File
    /// </summary>
    public class MatchedFile : IntegrationObject, IEquatable<MatchedFile> {

        /// <summary>
        /// Constructor
        /// </summary>
        public MatchedFile(ISourceLocation p_Source, string p_origName, string p_folder, long p_size, DateTime p_lastModified) {
            m_Source = p_Source;
            m_fileName = p_origName;
            m_origName = p_origName;
            m_folder = p_folder;
            m_size = p_size;
            m_lastModified = p_lastModified;
            m_lastModifiedUTC = p_lastModified.ToFileTimeUtc();
            Deleted = false;            
        }

        /// <summary>
        /// Working File
        /// </summary>
        public FileInfo WorkingFi {
            get {
                return m_WorkingFi;
            }
        }

        /// <summary>
        /// Get File Extension from File Name
        /// </summary>
        /// <param name="p_name">the file name</param>
        /// <returns>file extension</returns>
        private string GetFileExtension(string p_name) {
            if (!p_name.Contains(".")) {
                return string.Empty;
            } else {
                return p_name.Substring(p_name.LastIndexOf('.'));
            }            
        }

        /// <summary>
        /// Get File Name Without Extension
        /// </summary>
        /// <param name="p_name">the file name</param>
        /// <returns>file extension</returns>
        private string RemoveExtension(string p_name) {
            if (!p_name.Contains(".")) {
                return p_name;
            }
            else {
                return p_name.Substring(0, p_name.LastIndexOf('.'));
            }
        }

        /// <summary>
        /// Original file name at the integration source
        /// </summary>
        public string OriginalName {
            get {
                return m_origName;
            }
        }

        /// <summary>
        /// Transformed file name at the integration source
        /// </summary>
        public string OriginalNameNoExt {
            get {
                return RemoveExtension(m_origName);
            }
        }

        /// <summary>
        /// Transformed file name extension at the integration source
        /// </summary>        
        public string OriginalNameExt {
            get {
                return GetFileExtension(m_origName);
            }
        }
        private string m_origName;

        /// <summary>
        /// Transformed file name at the integration source
        /// </summary>
        public string Name {
            get {
                return m_fileName;
            }
            set {
                m_fileName = value;
            }
        }

        /// <summary>
        /// Transformed file name at the integration source - No Extension
        /// </summary>        
        public string NameNoExt {
            get {
                return RemoveExtension(m_fileName);
            }
        }

        /// <summary>
        /// Transformed File Name at the integration source - Extension Only
        /// </summary>        
        public string Ext {
            get {
                return GetFileExtension(m_fileName);
            }
        }
        // member
        private string m_fileName;

        /// <summary>
        /// Working File Name
        /// </summary>        
        public string WorkingName {
            get {
                return m_WorkingFi.Name;
            }
        }

        /// <summary>
        /// Working File Name - No Extension
        /// </summary>
        public string WorkingNameNoExt {
            get {
                return RemoveExtension(m_WorkingFi.Name);
            }
        }

        /// <summary>
        /// Working File Name - Extension Only
        /// </summary>        
        public string WorkingNameExt {
            get {
                return m_WorkingFi.Extension;
            }
        }

        /// <summary>
        /// Name of file at response location
        /// </summary>
        /// <remarks>this can change as the integration processes the list of responses</remarks>
        public string ResponseFileName {
            get; set;
        }

        /// <summary>
        /// Source Folder or Remote Folder
        /// </summary>
        public string Folder {
            get {
                return m_folder;
            }
        }
        private string m_folder;

        /// <summary>
        /// File Size
        /// </summary>
        public long Size {
            get {
                return m_size;
            }
        }        
        private long m_size;

        /// <summary>
        /// MD5 checksum
        /// </summary>
        public string MD5 {
            get {
                return m_MD5;
            }
            set {
                m_MD5 = value;
            }
        }
        private string m_MD5 = string.Empty;

        /// <summary>
        /// SHA1
        /// </summary>
        public string SHA1 {
            get {
                return m_sha1;
            }
            set {
                m_sha1 = value;
            }
        }
        private string m_sha1 = string.Empty;

        /// <summary>
        /// Last Modified Date
        /// </summary>
        public DateTime LastModified {
            get {
                return m_lastModified;
            }
        }
        private DateTime m_lastModified;

        /// <summary>
        /// Last Modified UTC File Time
        /// </summary>
        public long LastModifiedUTC {
            get {
                return m_lastModifiedUTC;
            }
        }
        private long m_lastModifiedUTC;

        /// <summary>
        /// Supress response on this file?
        /// </summary>        
        public bool Supress { get; set; }

        /// <summary>
        /// Has the file been deleted from the source?
        /// </summary>        
        public bool Deleted { get; set; }

        /// <summary>
        /// All responses completed on this file
        /// </summary>
        public List<IntegrationResponse> CompleteResponses = new List<IntegrationResponse>();

        /// <summary>
        /// Count of responses completed on this file
        /// </summary>
        public int ResponseCount { get => CompleteResponses.Count; }
        
        /// <summary>
        /// Source Location
        /// </summary>
        public ISourceLocation Source { get => m_Source; set => m_Source = value; }

        // members
        private bool m_supress;
        private DirectoryInfo m_WorkingDi;
        private FileInfo m_WorkingFi;
        private XIntegration m_Integration;
        private ISourceLocation m_Source;
        private XPattern m_Pattern;

        /// <summary>
        /// Set Working Directory
        /// </summary>
        /// <param name="p_WorkingDi">working directory</param>
        public void SetWorkingDi(DirectoryInfo p_WorkingDi) {
            m_WorkingDi = p_WorkingDi;
        }

        /// <summary>
        /// Set the Working File Name
        /// </summary>
        /// <param name="p_fileName">the file name</param>
        public void SetWorkingFileName(string p_fileName) {
            if(m_WorkingDi == null) {
                throw new NullReferenceException("in SetWorkingFileName-MatchedFile.WorkingDi");
            }
            m_WorkingFi = new FileInfo(string.Format("{0}\\{1}", m_WorkingDi.FullName, p_fileName));
        }

        /// <summary>
        /// Compile!
        /// </summary>
        protected override void CompileDynamicText() {            
            // nothing
        }

        /// <summary>
        /// Get Hash Code
        /// </summary>        
        /// <returns>the hash code</returns>
        public new int GetHashCode() {
            return OriginalName.GetHashCode();
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="p_Other"></param>
        /// <returns>true if Matched Files are the same</returns>
        public bool Equals(MatchedFile p_Other) {
            return new MatchedFileComparer().Equals(this, p_Other);
        }

    }   // MatchedFile

    /// <summary>
    /// Use to compare matched files on file name
    /// </summary>
    public class MatchedFileComparer : IEqualityComparer<MatchedFile> {

        /// <summary>
        /// Constructor
        /// </summary>
        public MatchedFileComparer() :
            this(StringComparison.OrdinalIgnoreCase) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_stringComparison">String Comparison Type</param>
        public MatchedFileComparer(StringComparison p_stringComparison) {
            m_stringComparison = p_stringComparison;
        }

        // members
        private StringComparison m_stringComparison;

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="p_Lhs">left hand side</param>
        /// <param name="p_Rhs">right hand side</param>
        /// <returns>true if Matched Files are the same</returns>
        public bool Equals(MatchedFile p_Lhs, MatchedFile p_Rhs) {
            return (p_Lhs.OriginalName.Equals(p_Rhs.OriginalName, m_stringComparison));
        }

        /// <summary>
        /// Get Hash Code
        /// </summary>
        /// <param name="p_Mf">Matched File</param>
        /// <returns>hash code</returns>
        public int GetHashCode(MatchedFile p_Mf) {
            return p_Mf.GetHashCode();
        }        

    }   // MatchedFileComparer


}
