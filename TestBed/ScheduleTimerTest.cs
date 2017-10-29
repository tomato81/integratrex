using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C2InfoSys.Schedule;

namespace C2InfoSys.FileIntegratrex.TestBed {

    /// <summary>
    /// Schedule Timer Test 
    /// </summary>
    public class ScheduleTimerTest {

        /// <summary>
        /// The Timer
        /// </summary>
        public ScheduleTimer Timer {
            get {
                return m_Timer;
            }
        }
        // member
        private ScheduleTimer m_Timer;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ScheduleTimerTest() {
            m_Timer = new ScheduleTimer();
            m_Timer.Error += m_Timer_Error;
        }       

        /// <summary>
        /// Start!
        /// </summary>
        public void Start() {
            m_Timer.Start();
        }

        /// <summary>
        /// Stop!
        /// </summary>
        public void Stop() {
            m_Timer.Stop();
        }

        /// <summary>
        /// Error!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Args"></param>
        private void m_Timer_Error(object sender, ExceptionEventArgs Args) {
               throw new NotImplementedException();
        }

    }   // end of class


    /// <summary>
    /// Barks
    /// </summary>
    public class Barker {

        // members
        private int m_numOfBarks;
        private string[] m_barks = new string[] { "ARF", "BARK", "RARF", "WOOF", "GRAR", "RRRR", "ROOF" };
        private Random Rand;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_barks">bark this many times</param>
        public Barker(int p_barks) {            
            Rand = new Random(DateTime.Now.Second);
            m_numOfBarks = p_barks;
        }

        /// <summary>
        /// Get a random bark
        /// </summary>
        /// <returns></returns>
        private string RandBark() {
            return m_barks[Rand.Next(0, m_barks.Length)];
        }

        /// <summary>
        /// Arf!
        /// </summary>
        public void Bark() {
            StringBuilder BarkBuilder = new StringBuilder();
            for (int i = 0; i < m_numOfBarks; i++) {
                BarkBuilder.Append(RandBark() + " ");
            }          
            Console.WriteLine(BarkBuilder.ToString().Trim());                       
        }

    }

}
