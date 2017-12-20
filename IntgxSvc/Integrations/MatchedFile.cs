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
    public class MatchedFile : IntegrationObject, IEqualityComparer<MatchedFile> {

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
        /// Transformed file name at the integration source without the file extension
        /// </summary>
        public string NameNoExt {
            get {
                return RemoveExtension(m_fileName);
            }
        }
        /// <summary>
        /// Transformed file extension at the integration source
        /// </summary>
        public string Ext {
            get {
                return GetFileExtension(m_fileName);
            }
        }
        // member
        private string m_fileName;

        /// <summary>
        /// Working Name
        /// </summary>
        public string WorkingName {
            get {
                return m_WorkingFi.Name;
            }
        }
        /// <summary>
        /// Working file name
        /// </summary>
        public string WorkingNameNoExt {
            get {
                return RemoveExtension(m_WorkingFi.Name);
            }
        }
        /// <summary>
        /// Working file extension
        /// </summary>
        public string WorkingNameExt {
            get {
                return m_WorkingFi.Extension;
            }
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

        public ISourceLocation Source { get => m_Source; set => m_Source = value; }

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
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(MatchedFile x, MatchedFile y) {
            return x.OriginalName.Equals(y.OriginalName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Get Hash Code
        /// </summary>        
        /// <returns>a hash code</returns>
        public new int GetHashCode() {
            return OriginalName.GetHashCode();
        }

        /// <summary>
        /// Get Hash Code
        /// </summary>
        /// <param name="obj">an object</param>
        /// <returns>a hash code</returns>
        public int GetHashCode(MatchedFile obj) {
            return obj.OriginalName.GetHashCode();
        }

        // properties
        public string ToJson() {
            return JsonConvert.SerializeObject(this);
        }

        public static MatchedFile FromJson(string p_json) {
            return JsonConvert.DeserializeObject<MatchedFile>(p_json);
        }
        
    }   // MatchedFile



}
