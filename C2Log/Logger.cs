using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace C2InfoSys.C2Log
{
    /// <summary>
    /// Logger
    /// </summary>
    public class Logger {

        /// <summary>
        /// Constructor
        /// </summary>
        public Logger(string p_name, SourceLevels p_level) {
            m_Ts = new TraceSource(p_name, p_level);
        }


        /// <summary>
        /// Log
        /// </summary>
        /// <param name="p_message"></param>
        public void Log(string p_message) {


                       

            
        }


        // members
        private TraceSource m_Ts;


    }   // end of class
}
