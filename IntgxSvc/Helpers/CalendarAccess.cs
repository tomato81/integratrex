using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// C2
using C2InfoSys.Schedule;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Provide Access to BusinessCalendars
    /// </summary>
    public sealed class CalendarAccess {

         // log
        private TraceSource SvcLog;

        /// <summary>
        /// Business Calendars
        /// </summary>
        public Dictionary<string, BusinessCalendar> Calendars {
            get {
                return m_CalendarDict;
            }
        }
        private Dictionary<string, BusinessCalendar> m_CalendarDict;
        private BusinessCalendar[] m_Calendars;

        /// <summary>
        /// Calendar Count
        /// </summary>
        public int Count {
            get {
                if (m_Calendars == null) {
                    return 0;
                }
                return m_Calendars.Length;
            }
        }        

        /// <summary>
        /// Construtor
        /// </summary>
        private CalendarAccess() {
            // setup trace source
            SvcLog = new TraceSource(Global.ServiceLogName);
        }

        /// <summary>
        /// Load Business Calendars
        /// </summary>
        public void LoadCalendars(string p_calendarPath, bool p_recursive, string p_xmlns) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {                
                SvcLog.TraceEvent(TraceEventType.Verbose, 0, Global.Messages.Debug.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic
                BCReader Reader = new BCReader(p_xmlns);
                m_Calendars = Reader.LoadFromFolder(p_calendarPath, p_recursive);
                m_CalendarDict = new Dictionary<string, BusinessCalendar>(m_Calendars.Length);
                // build dictionary
                foreach (BusinessCalendar BC in m_Calendars) {
                    m_CalendarDict.Add(BC.Code, BC);
                }
            }
            catch (Exception ex) {
                SvcLog.TraceEvent(TraceEventType.Critical, 0, Global.Messages.Error.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                SvcLog.TraceEvent(TraceEventType.Verbose, 0, Global.Messages.Debug.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }  
        }

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static CalendarAccess Instance {
            get {
                if (m_Instance == null) {
                    lock (padlock) {
                        m_Instance = new CalendarAccess();
                    }
                }
                return m_Instance;
            }
        }
        private static volatile CalendarAccess m_Instance;
        private static object padlock = new object();      


    }   // Calendar Access
}
