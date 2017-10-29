using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Helps check for duplicate files in source locations
    /// </summary>
    public class SupressDuplicates : XSupressDuplicates {
        
        // members
        HashSet<string> m_MD5;
        HashSet<string> m_Names;
        Dictionary<string, int> m_Sizes;
        Dictionary<string, DateTime> m_LastModified;
        Dictionary<string, string> m_NameMD5;

        /// <summary>
        /// Constructor
        /// </summary>
        public SupressDuplicates() {                                  
        }      

        /// <summary>
        /// Check a matched file against against stored lists
        /// </summary>
        /// <param name="p_Mf">matched file object</param>
        public bool IsDuplicate(MatchedFile p_Mf) {
            try {
                if (Enable == XSupressDuplicatesEnable.N) {
                    return false;
                }
                if (MatchBy.FileName == XMatchByFileName.Y) {
                    // check file name
                    if (m_Names.Contains(p_Mf.OrigName)) {
                        // filesize and last modified date can only be matched in addition to filename
                        if (MatchBy.FileSize == XMatchByFileSize.Y) {
                            if (m_Sizes[p_Mf.OrigName] != p_Mf.FileSize) {
                                // no size match
                                return false;
                            }
                        }
                        if (MatchBy.LastModifiedDate == XMatchByLastModifiedDate.Y) {
                            if (m_LastModified[p_Mf.OrigName] != p_Mf.LastModified) {
                                // no date match
                                return false;
                            }
                        }
                        if (MatchBy.MD5 == XMatchByMD5.Y) {
                            if (m_NameMD5[p_Mf.OrigName] != p_Mf.MD5) {
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
                    if (MatchBy.FileSize == XMatchByFileSize.Y) {
                        throw new InvalidOperationException();
                    }
                    if (MatchBy.LastModifiedDate == XMatchByLastModifiedDate.Y) {
                        throw new InvalidOperationException();
                    }
                    // match by MD5 only
                    if (MatchBy.MD5 == XMatchByMD5.Y) {                        
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

    }   // end of class
}
