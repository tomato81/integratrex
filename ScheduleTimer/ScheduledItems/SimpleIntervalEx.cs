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
using System.Diagnostics;

namespace C2InfoSys.Schedule
{
	/// <summary>
	/// an extension of SimpleInterval that supports business calendars
	/// </summary>
	[Serializable]
    public class SimpleIntervalEx : SimpleInterval
	{
        public SimpleIntervalEx(DateTime StartTime, TimeSpan Interval, BusinessCalendar p_BC) : base(StartTime, Interval)
		{			
            m_BC = p_BC;
		}
        public SimpleIntervalEx(DateTime StartTime, TimeSpan Interval, int count, BusinessCalendar p_BC) : base(StartTime, Interval, count)
		{			
            m_BC = p_BC;
		}
        public SimpleIntervalEx(DateTime StartTime, TimeSpan Interval, DateTime EndTime, BusinessCalendar p_BC) : base(StartTime, Interval, EndTime)
		{			
            m_BC = p_BC;
		}

		public override DateTime NextRunTime(DateTime time, bool AllowExact)
		{
			DateTime Next = NextRunTimeInt(time, AllowExact);          

            

            // get business calendar Day, and today's Day
            Day D = m_BC.GetDay(Next);
            // is the next run time on a business day?
            if (D.BusinessDay == YesNo.N) {
                // get the next business day and transpose the time of day from the Next run time
                D = m_BC.NextBusinessDay(Next);
                Next = D.Date.Add(new TimeSpan(Next.Hour, Next.Minute, Next.Second));
            }
			Debug.WriteLine(time);
            Debug.WriteLine(Next);
			Debug.WriteLine(_EndTime);
            return (Next >= _EndTime) ? DateTime.MaxValue : Next;
		}
    
        // the business calendar
        BusinessCalendar m_BC;
	}
}