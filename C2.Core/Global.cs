using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2InfoSys.Core
{
    public static class Global
    {

        /// <summary>
        /// Log Message Templates
        /// </summary>
        public static class Messages {
            public static string EnterMethod = "Enter Method: {0}.{1}";
            public static string ExitMethod = "Exit Method: {0}.{1}";

            /// <summary>
            /// {1=Class}.{2=Method}; {0=Exception}; Message: {3=Message}
            /// </summary>
            public static string Exception = "{1}.{2}; {0}; Message: {3}";
        }   // Messages

    }   // Global
}
