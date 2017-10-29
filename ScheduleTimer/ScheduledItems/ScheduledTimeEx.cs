/***************************************************************************
 * Copyright Andy Brummer 2004-2005
 * 
 * This code is provided "as is", with absolutely no warranty expressed
 * or implied. Any use is at your own risk.
 *
 * This code may be used in compiled form in any way you desire. This
 * file may be redistributed unmodified by any means provided it is
 * not sold for profit without the authors written consent, and
 * providing that this notice and the authors name is included. If
 * the source code in  this file is used in any commercial application
 * then a simple email would be nice.
 * 
 **************************************************************************/

using System;
using System.Collections;
using System.Collections.Specialized;

namespace C2InfoSys.Schedule
{

    /// <summary>
    /// How to adjust schedule on non-business days
    /// </summary>
    public enum BusinessDayAdjust {
        RunNormal,
        DoNotRun,
        RunPrior,
        RunNext
    }    
	
    [Serializable]
    public class ScheduledTimeEx : ScheduledTime {

        // use this business calendar
        BusinessCalendar m_BC;

        // specify treatment of scheduled instances occuring on non-business days
        BusinessDayAdjust m_adjustment;        

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Base"></param>
        /// <param name="Offset"></param>
        /// <param name="BC"></param>
        public ScheduledTimeEx(EventTimeBase p_Base, TimeSpan p_Offset, BusinessCalendar p_BC, BusinessDayAdjust p_adjustment)
            : base(p_Base, p_Offset) {
            m_BC = p_BC;
            m_adjustment = p_adjustment;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="StrBase"></param>
        /// <param name="StrOffset"></param>
        /// <param name="BC"></param>
        public ScheduledTimeEx(EventTimeBase p_Base, string p_offset, BusinessCalendar p_BC, BusinessDayAdjust p_adjustment)
            : base(p_Base, p_offset) {
            m_BC = p_BC;
            m_adjustment = p_adjustment;
        }  

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="StrBase"></param>
        /// <param name="StrOffset"></param>
        /// <param name="BC"></param>
        public ScheduledTimeEx(string p_base, string p_offset, BusinessCalendar p_BC, BusinessDayAdjust p_adjustment)
            : base(p_base, p_offset) {
            m_BC = p_BC;
            m_adjustment = p_adjustment;
		}  

        /// <summary>
        /// Get next scheduled run time skipping non-business days
        /// </summary>
        /// <param name="time"></param>
        /// <param name="AllowExact"></param>
        /// <returns></returns>
        public override DateTime NextRunTime(DateTime p_Time, bool p_allowExact) {
            DateTime Next = base.NextRunTime(p_Time, p_allowExact);
            // if we are ignoring the business calendar, do not run bc logic, just return the next run time
            if (m_adjustment == BusinessDayAdjust.RunNormal) {
                return Next;
            }                               
            // get business calendar Day, and today's Day
            Day D = m_BC.GetDay(Next);
            Day Today = m_BC.Today();            

            // apply logic
            if (D.BusinessDay == YesNo.N) {                
                switch (_Base) {
                    case EventTimeBase.BySecond:
                    case EventTimeBase.ByMinute:
                    case EventTimeBase.Hourly:
                    case EventTimeBase.Daily:
                        // get the next business day and apply the offset, this is the return value
                        D = m_BC.NextBusinessDay(Next);
                        Next = D.Date.Add(_Offset);    
                        break;
                    case EventTimeBase.Weekly:
                    case EventTimeBase.Monthly: {
                        switch (m_adjustment) {
                            case BusinessDayAdjust.DoNotRun:
                                // get the next scheduled time using the next business day as a reference point
                                Next = NextRunTime(m_BC.NextBusinessDay(Today.Date).Date, p_allowExact);
                                break;
                            case BusinessDayAdjust.RunPrior:
                                // move the scheduled time to the prior business day                                
                                D = m_BC.PrevBusinessDay(Next);
                                Next = D.Date.Add(Next.TimeOfDay);
                                // if the prior business day is before today, get the next scheduled time using the next business day as a reference point
                                if (D.CompareTo(Today) == -1) {                                    
                                    Next = NextRunTime(m_BC.NextBusinessDay(Today.Date).Date, p_allowExact);                                    
                                }                                                              
                                break;
                            case BusinessDayAdjust.RunNext:
                                // move the scheduled time to the next business day
                                D = m_BC.NextBusinessDay(Next);
                                break;
                            default:
                                break;
                        }
                        break;
                    }                        
                    default:
                        break;
                }                     
            }         
            return Next;
        }

    }   // end of class
		
}