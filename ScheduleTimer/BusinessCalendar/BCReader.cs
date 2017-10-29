using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace C2InfoSys.Schedule {

    /// <summary>
    /// Business Calendar Reader
    /// </summary>
    public class BCReader {

        // BusinessCalendar XML namespace
        string m_xmlns;

        /// <summary>
        /// Constructor
        /// </summary>
        public BCReader(string p_xmlns) {
            m_xmlns = p_xmlns;
        }

        /// <summary>
        /// Load all XML business calendars from a specific location
        /// </summary>
        /// <param name="p_path">location</param>
        /// <param name="p_recursive">recursive</param>
        /// <returns>array of BusinessCalendar</returns>
        public BusinessCalendar[] LoadFromFolder(string p_path, bool p_recursive) {
            return LoadFromFolder(new DirectoryInfo(p_path), p_recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Load all XML business calendars from a specific location
        /// </summary>
        /// <param name="p_Di">location</param>
        /// <param name="p_searchOption">recursive</param>
        /// <returns>array of BusinessCalendar</returns>
        public BusinessCalendar[] LoadFromFolder(DirectoryInfo p_Di, SearchOption p_searchOption) {
            try {
                // get ready
                FileInfo[] BcFiles = p_Di.GetFiles("*.xml", p_searchOption);
                BusinessCalendar[] Calendars = new BusinessCalendar[BcFiles.Length];
                XmlSerializer Xin = new XmlSerializer(typeof(BusinessCalendar), m_xmlns);
                // set
                int count = 0;
                // go
                foreach (FileInfo BcFi in BcFiles) {
                    try {
                        Calendars[count] = (BusinessCalendar)Xin.Deserialize(new StreamReader(BcFi.FullName));                         
                    }
                    catch {
                        continue;
                    }
                    // real count
                    count++;                                        
                }
                // fix the array length
                Array.Resize<BusinessCalendar>(ref Calendars, count);                
                // out
                return Calendars;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }   // BCReader
}
