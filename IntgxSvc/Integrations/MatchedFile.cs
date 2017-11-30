using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Serialization;

using C2InfoSys.FileIntegratrex.Lib;
using Newtonsoft.Json;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Matched File
    /// </summary>
    public class MatchedFile : IEqualityComparer<MatchedFile> {

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
        /// Original file name at the integration source
        /// </summary>
        public string OrigName {
            get {
                return m_origName;
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
        }
        private string m_fileName;

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
        public long FileSize {
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
        public bool Supress {
            get {
                return m_supress;
            }
        }

        public ISourceLocation Source { get => m_Source; set => m_Source = value; }

        private bool m_supress;
        private FileInfo m_WorkingFi;
        private XIntegration m_Integration;
        private ISourceLocation m_Source;
        private XPattern m_Pattern;

        /// <summary>
        /// Set Working Directory
        /// </summary>
        /// <param name="p_WorkingDi">working directory</param>
        public void SetWorkingDi(DirectoryInfo p_WorkingDi) {
            m_WorkingFi = new FileInfo(string.Format("{0}\\{1}", p_WorkingDi.FullName, OrigName));
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(MatchedFile x, MatchedFile y) {
            return x.OrigName.Equals(y.OrigName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Get Hash Code
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(MatchedFile obj) {
            return obj.OrigName.GetHashCode();
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
