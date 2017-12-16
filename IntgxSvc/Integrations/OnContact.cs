using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// On Contact
    /// </summary>
    public class OnContact : IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_XOnContact"></param>
        public OnContact(XOnContact p_XOnContact) {
            m_XOnContact = p_XOnContact;
            m_SupressDuplicates = new SupressDuplicates(p_XOnContact.SupressDuplicates);
        }

        // members
        private readonly XOnContact m_XOnContact;
        private readonly SupressDuplicates m_SupressDuplicates;

        /// <summary>
        /// Supress Duplicates
        /// </summary>
        public SupressDuplicates SupressDuplicates { get => m_SupressDuplicates; }

        /// <summary>
        /// The X object
        /// </summary>
        private XOnContact X { get => m_XOnContact; }


        // functions
        public readonly Func<MatchedFile, string> GetSHA1;
        public readonly Func<MatchedFile, string> GetMD5;

        /// <summary>
        /// Get the SHA1 Hash of the Matched file - reading from the source location
        /// </summary>
        /// <param name="p_Mf">Matched File</param>
        private string _GetSHA1(MatchedFile p_Mf) {
            try {
                string sha1 = string.Empty;
                FileInfo Fi = new FileInfo(string.Format("{0}\\{1}", p_Mf.Folder, p_Mf.OriginalName));
                using (FileStream Fin = new FileStream(Fi.FullName, FileMode.Open)) {
                    using (SHA1Managed SHA1 = new SHA1Managed()) {
                        byte[] hash = SHA1.ComputeHash(Fin);
                        StringBuilder Sb = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash) {
                            Sb.AppendFormat("{0:X2}", b);
                        }
                        sha1 = Sb.ToString();
                    }
                }
                return sha1;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Get the MD5 Hash of the Matched file - reading from the source location
        /// </summary>
        /// <param name="p_Mf">Matched File</param>
        private string _GetMD5(MatchedFile p_Mf) {
            try {
                string md5 = string.Empty;
                FileInfo Fi = new FileInfo(string.Format("{0}\\{1}", p_Mf.Folder, p_Mf.OriginalName));
                using (MD5 MDFive = MD5.Create()) {
                    using (FileStream Fin = new FileStream(Fi.FullName, FileMode.Open)) {
                        byte[] hash = MDFive.ComputeHash(Fin);
                        StringBuilder Sb = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash) {
                            Sb.AppendFormat("{0:X2}", b);
                        }
                        md5 = Sb.ToString();
                    }
                }
                return md5;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Rename the orignal file at the source location?
        /// </summary>
        public bool RenameOrignal {
            get {
                return X.Rename.Original == XRenameOnContactOriginal.Y;
            }
        }

        /// <summary>
        /// Rename the working copy of the file?
        /// </summary>
        public bool RenameWorkingCopy {
            get {
                return X.Rename.WorkingCopy == XRenameOnContactWorkingCopy.Y;
            }
        }

        /// <summary>
        /// Create and Compile Dynamic Text
        /// </summary>
        protected override void CompileDynamicText() {
            m_Rename = new DynamicTextParser(X.Rename.Value);
            m_Rename.OnValueRequired += ValueRequired;
            m_Rename.Compile();           
        }

        [DynamicText]
        public string Rename() {
            return m_Rename.Run();
        }
        private DynamicTextParser m_Rename;

    }   // OnContact

    /// <summary>
    /// Helps check for duplicate files in source locations
    /// </summary>
    public class SupressDuplicates { 

        // members
        private readonly XSupressDuplicates m_XSupressDuplicates;
        private HashSet<string> m_MD5;
        private HashSet<string> m_Names;
        private Dictionary<string, int> m_Sizes;
        private Dictionary<string, DateTime> m_LastModified;
        private Dictionary<string, string> m_NameMD5;

        /// <summary>
        /// The X object
        /// </summary>
        private XSupressDuplicates X { get => m_XSupressDuplicates; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SupressDuplicates(XSupressDuplicates p_XSupressDuplicates)
            : base()  {
            m_XSupressDuplicates = p_XSupressDuplicates;
        }      

        /// <summary>
        /// Supressing Duplicate Matched Files?
        /// </summary>
        public bool Enabled {
            get {
                return X.Enable == XSupressDuplicatesEnable.Y;
            }
        }

        /// <summary>
        /// Can a duplicate be identified at the integration source?
        /// </summary>
        /// <returns></returns>
        public bool CanIDAtSource {
            get {
                if((
                    X.MatchBy.FileName == XMatchByFileName.Y && 
                    (X.MatchBy.FileSize == XMatchByFileSize.Y ||
                    X.MatchBy.LastModifiedDate == XMatchByLastModifiedDate.Y)
                    ) && !(
                        X.MatchBy.MD5 == XMatchByMD5.Y ||
                        X.MatchBy.SHA1 == XMatchBySHA1.Y
                        )) {
                    return true;
                }
                return false;
            }
        } 

        /// <summary>
        /// Check a matched file against against stored lists
        /// </summary>
        /// <param name="p_Mf">matched file object</param>
        public bool IsDuplicate(MatchedFile p_Mf) {
            try {
                if (X.Enable == XSupressDuplicatesEnable.N) {
                    return false;
                }
                if (X.MatchBy.FileName == XMatchByFileName.Y) {
                    // check file name
                    if (m_Names.Contains(p_Mf.OriginalName)) {
                        // filesize and last modified date can only be matched in addition to filename
                        if (X.MatchBy.FileSize == XMatchByFileSize.Y) {
                            if (m_Sizes[p_Mf.OriginalName] != p_Mf.Size) {
                                // no size match
                                return false;
                            }
                        }
                        if (X.MatchBy.LastModifiedDate == XMatchByLastModifiedDate.Y) {
                            if (m_LastModified[p_Mf.OriginalName] != p_Mf.LastModified) {
                                // no date match
                                return false;
                            }
                        }
                        if (X.MatchBy.MD5 == XMatchByMD5.Y) {
                            if (m_NameMD5[p_Mf.OriginalName] != p_Mf.MD5) {
                                // no MD5 match
                                return false;
                            }
                        }
                        // matches on all specified conditions
                        return true;
                    }
                    // no filename match
                    return false;
                }
                else {
                    // filesize and last modified date can only be matched in addition to filename
                    if (X.MatchBy.FileSize == XMatchByFileSize.Y) {
                        throw new InvalidOperationException();
                    }
                    if (X.MatchBy.LastModifiedDate == XMatchByLastModifiedDate.Y) {
                        throw new InvalidOperationException();
                    }
                    // match by MD5 only
                    if (X.MatchBy.MD5 == XMatchByMD5.Y) {                        
                        return m_MD5.Contains(p_Mf.MD5);
                    }
                }                
                // NOTE: log something - supress duplciates is activated, but no detection conditions have been specified
                return false;
            }
            catch (Exception ex) {
                throw ex;
            }           
        }

    }   // SupressDuplicates

}
