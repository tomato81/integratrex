using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Matched File
    /// </summary>
    public class MatchedFile : IEqualityComparer<MatchedFile> {

        /// <summary>
        /// Constructor
        /// </summary>
        public MatchedFile(string p_origName, string p_folder, long p_size, DateTime p_lastModified) {
            m_origName = p_origName;
            m_folder = p_folder;
            m_size = p_size;
            m_lastModified = p_lastModified;
        }

        /// <summary>
        /// File name at the integration source
        /// </summary>
        public string OrigName {
            get {
                return m_origName;
            }
        }        
        private string m_origName;

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
        }
        private string m_MD5;

        /// <summary>
        /// SHA1
        /// </summary>
        public string SHA1 {
            get {
                return m_sha1;
            }
        }
        private string m_sha1;

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
        /// Supress response on this file?
        /// </summary>
        public bool Supress {
            get {
                return m_supress;
            }
        }
        private bool m_supress;



        private FileInfo m_WorkingFi;
        private XIntegration m_Integration;
        private ISourceLocation m_Source;
        private XPattern m_Pattern;

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






    }   // MatchedFile



}
