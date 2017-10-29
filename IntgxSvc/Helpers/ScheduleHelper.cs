using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// C2
using C2InfoSys.Schedule;
using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Some helper methods for dealing with schedules
    /// </summary>
    public class ScheduleHelper {
        
        /// <summary>
        /// Construtor
        /// </summary>
        private ScheduleHelper() {
        }

        /// <summary>
        /// GetOffset
        /// </summary>
        /// <returns></returns>
        public static string GetOffset(XCalendarOccurs p_occurs, string p_offset, string p_timeOfDay) {
            switch (p_occurs) {
                case XCalendarOccurs.Second:
                    return p_offset;
                case XCalendarOccurs.Minute:
                    return p_offset;
                case XCalendarOccurs.Hour:
                    return p_offset;
                case XCalendarOccurs.Day:
                    return p_timeOfDay;
                case XCalendarOccurs.Week:
                    return string.Format("{0},{1}", p_offset, p_timeOfDay);                        
                case XCalendarOccurs.Month:
                    return string.Format("{0},{1}", p_offset, p_timeOfDay);
                default:
                    throw new InvalidOperationException();
            }                    
        }

        /// <summary>
        /// Translate Integratrex Config to Scheduled Timer
        /// </summary>
        /// <param name="p_occurs">XCalendarOccurs</param>
        /// <returns>EventTimeBase</returns>
        public static EventTimeBase TranslateXCalendarOccurs(XCalendarOccurs p_occurs) {
            switch (p_occurs) {
                case XCalendarOccurs.Second:
                    return EventTimeBase.BySecond;
                case XCalendarOccurs.Minute:
                    return EventTimeBase.ByMinute;
                case XCalendarOccurs.Hour:
                    return EventTimeBase.Hourly;
                case XCalendarOccurs.Day:
                    return EventTimeBase.Daily;
                case XCalendarOccurs.Week:
                    return EventTimeBase.Weekly;
                case XCalendarOccurs.Month:
                    return EventTimeBase.Monthly;
                default:
                    throw new InvalidOperationException();
            }
        }


    }   // ScheduleHelper
}
