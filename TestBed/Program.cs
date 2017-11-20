using System;
using System.IO;
using System.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;

// C2 
using C2InfoSys.Schedule;
using C2InfoSys.FileIntegratrex.Lib;
using C2InfoSys.FileIntegratrex.Svc;


namespace C2InfoSys.FileIntegratrex.TestBed {
    
    public delegate void VoidDelegateVoid();


    class C { 
        
        public static Func<int> getter; 
        public static Action<int> setter; 



        public static void M() { 
            int x = 5; 
            getter = () => x; 
            setter = y => { x = y; }; 
        }    
    
    
    
    }


    /// <summary>
    /// Test Bed
    /// </summary>
    public class Program {


        /// <summary>
        /// App Entry
        /// </summary>
        /// <param name="args">args</param>
        static void Main(string[] args) {
            try {


                /*
                C.M();
                C.setter(3);
                int d = C.getter();
                Console.WriteLine(d.ToString());                    
                */





                //TestBCReader();

                TestIntegrationsXml();
                //TestBusinessCalendarXml();
                //TestBusinessCalendarEdits();
                //TestOptions();
                //ParseDynamicText();
                //TestDynamicTextParser();
                //TestScheduleTimer();

                //TestBusinessCalendar();


                //TestScheduledTime();


            }
            catch (Exception ex) {
                do {
                    Console.WriteLine(ex.Message);
                    ex = ex.InnerException;
                } while (ex != null);
            }           
            Console.ReadKey();        
        }

        private static void TestBCReader() {

            string bcfolder = @"C:\Users\bedey\Google Drive\C2 Info Systems\File Integratrex\IntegratrexSln\IntgxWeb\Calendar";
            string xmlns = @"c2infosys.com/Common/BusinessCalendar.xsd";


            // method logic
            CalendarAccess.Instance.LoadCalendars(bcfolder, false, xmlns);

        }

        private static void TestDynamicTextParser() {
            try {

                // http://parsingintro.sourceforge.net/


                FileInfo Fi = new FileInfo(@"..\..\NoDyText.txt");
                StreamReader Fin = new StreamReader(Fi.OpenRead());

                string incoming = Fin.ReadToEnd();


                //Tree<int> T = new Tree<int>();

                //string dytext = "?(Format -v=\"03/29/2016\" -f \"MM/dd/YYYY hh:mm:ss\")";

                // a character scanner
                Scanner S = new Scanner(incoming);
                // a tokenizer
                Lexxer L = new Lexxer(S);
                // read the tokens
                List<Token> Tokens = new List<Token>();
                while (L.Read()) {
                    Tokens.Add(L.Token);
                }

                // lex log                
                Console.WriteLine(L.ParseLog);

                // a compiler
                Compiler C = new Compiler(Tokens);
                C.BuildTree();

                

                Console.WriteLine();
                Console.WriteLine(C.Tree.ToString());


                // build an attribtute dictionary -- need to switch this to string - object ... 
                Dictionary<string, object> Attrs = new Dictionary<string, object>();

                Attrs.Add("Integration.Name", "INT4556660");
                Attrs.Add("Integration.Desc", "Trades to Bank");
                Attrs.Add("Integration.ContactDate", DateTime.Now);

                Attrs.Add("Files.Count", 3);
                Attrs.Add("Files.All", "test1.txt, test2.txt, test3.txt");

                Attrs.Add("Pattern.Type", "Simple");
                Attrs.Add("Pattern.Name", "*.txt");

                DynamicTextParser RT = new DynamicTextParser(incoming);

                RT.Compile();

                string result = RT.Run(Attrs);


                Console.WriteLine();
                Console.WriteLine(result);


                //Expression constString = Expression.Constant("hi there", typeof(string));


                //Console.WriteLine(constString);

            }
            catch (Exception ex) {

                Console.WriteLine(ex.Message);

            }
            finally {



                Console.ReadKey();
            }



        }


        private static void TestScheduledTime() {

            try {


                // load up a business calendar
                string xmlConfigPath = @"C:\Users\bedey\Google Drive\C2 Info Systems\File Integratrex\IntegratrexSln\IntgxWeb\Calendar\CA-ON.xml";
                StreamReader Fin = new StreamReader(xmlConfigPath);
                XmlSerializer Xin = new XmlSerializer(typeof(BusinessCalendar), "c2infosys.com/Common/BusinessCalendar.xsd");
                BusinessCalendar BC = new BusinessCalendar();
                BC = (BusinessCalendar)Xin.Deserialize(Fin);
                BC.Init();   




                Barker B = new Barker(3);

                ScheduleTimerTest T = new ScheduleTimerTest();


//                ScheduledTimeEx St_Weekly = new ScheduledTimeEx("Weekly", "5, 12:59 PM", BC, CalendarBusinessDay.Prior);

                ScheduledTimeEx St_Daily = new ScheduledTimeEx("Daily", "1:59 PM", BC, BusinessDayAdjust.RunPrior);



                VoidDelegateVoid BarkDelegate = new VoidDelegateVoid(B.Bark);



                T.Timer.AddJob(St_Daily, BarkDelegate, new object[0]);

                T.Start();

                System.Threading.Thread.Sleep(1000000);


            }
            catch (Exception ex) {
                throw ex;
            }      
        }

        /// <summary>
        /// Test a business calendar schedule
        /// </summary>
        private static void TestBusinessCalendar() {
            try {                                        
                // load up a business calendar
                string xmlConfigPath = @"C:\Users\bedey\Google Drive\C2 Info Systems\File Integratrex\IntegratrexSln\IntgxWeb\Calendar\CA-ON.xml";                
                StreamReader Fin = new StreamReader(xmlConfigPath);
                XmlSerializer Xin = new XmlSerializer(typeof(BusinessCalendar), "c2infosys.com/Common/BusinessCalendar.xsd");
                BusinessCalendar B = new BusinessCalendar();
                B = (BusinessCalendar)Xin.Deserialize(Fin);
                B.Init();                               

                DateTime Dt = new DateTime(2016, 1, 1);
                Day D = B.NextBusinessDay(Dt);

                Console.WriteLine(D.ToString());
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            finally {
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Test the scheduled timer... 
        /// </summary>
        private static void TestScheduleTimer() {
            try {

                Barker B = new Barker(3);

                ScheduleTimerTest T = new ScheduleTimerTest();

                SimpleInterval Interval = new SimpleInterval(DateTime.Now, new TimeSpan(0, 0, 1));





                VoidDelegateVoid BarkDelegate = new VoidDelegateVoid(B.Bark);



                T.Timer.AddJob(Interval, BarkDelegate, new object[0]);

                T.Start();

                System.Threading.Thread.Sleep(1000000);


            }
            catch (Exception ex) {
                throw ex;
            }           
        }




      
        



        private static void TestBusinessCalendarEdits() {

            BusinessCalendar B = new BusinessCalendar("CA-ON2", "Canada - Ontario");

            
            Year _2016 = B.AddYear(2016);
            

            //foreach (Month M in _2015.Month) {
            //    Console.WriteLine(M.ToString());
            //    foreach (Day D in M.Day) {
            //        Console.WriteLine(D.ToString());
            //    }
            //}

            //Console.ReadKey();

            string xmlConfigOutPath = @"C:\Users\bedey\Google Drive\C2 Info Systems\File Integratrex\IntegratrexSln\IntgxWeb\Calendar\CA_ON2.xml";

            StreamWriter Fout = new StreamWriter(xmlConfigOutPath);
            XmlSerializer Xout = new XmlSerializer(typeof(BusinessCalendar), "c2infosys.com/Common/BusinessCalendar.xsd");
            Xout.Serialize(Fout, B);   
        }

        /// <summary>
        /// Test the Xml serialization/deserialization of the BusinessCalendar
        /// </summary>
        private static void TestBusinessCalendarXml() {

            string xmlConfigPath = @"C:\Users\bedey\Google Drive\C2 Info Systems\File Integratrex\IntegratrexSln\IntgxWeb\Calendar\US-NY.xml";
            string xmlConfigOutPath = @"C:\Users\bedey\Google Drive\C2 Info Systems\File Integratrex\IntegratrexSln\IntgxWeb\Calendar\US-NY.Out.xml";

            StreamReader Fin = new StreamReader(xmlConfigPath);
            XmlSerializer Xin = new XmlSerializer(typeof(BusinessCalendar), "c2infosys.com/Common/BusinessCalendar.xsd");

            BusinessCalendar B = new BusinessCalendar();
            B = (BusinessCalendar)Xin.Deserialize(Fin);

            B.Init();

            foreach (Day D in B.Year[0].QuarterEnds) {
                Console.WriteLine(D.ToString());
            }

            Year _2014 = B.Year[0];

            StreamWriter Fout = new StreamWriter(xmlConfigOutPath);
            XmlSerializer Xout = new XmlSerializer(typeof(BusinessCalendar), "c2infosys.com/Common/BusinessCalendar.xsd");
            Xout.Serialize(Fout, B);

        }

        /// <summary>
        /// Test the integrations object, xml serialization, deserialization
        /// </summary>
        private static void TestIntegrationsXml() {

            string xmlConfigPath = @"C:\source\integratrex\IntgxWeb\Config\Service.Config.xml";
            string xmlConfigOutPath = @"C:\source\integratrex\IntgxWeb\Config\Service.Config.Out.xml";
            
            StreamReader Fin = new StreamReader(xmlConfigPath);
            XmlSerializer Xin = new XmlSerializer(typeof(XIntegrations), "c2infosys.com/Integratrex/Service.Config.xsd");
            
            XIntegrations IntegratrexConfig = new XIntegrations();
            IntegratrexConfig = (XIntegrations)Xin.Deserialize(Fin);

            foreach (XIntegration I in IntegratrexConfig.Integration) {

                


                Console.WriteLine(I.Desc);


                Console.WriteLine(I.Source.Desc);
                Console.WriteLine(I.Source.Item.GetType().ToString());




                foreach(XResponse R in I.Responses.Response) {

                    Console.WriteLine(string.Format("\t{0}", R.Desc));
                }




            }


            StreamWriter Fout = new StreamWriter(xmlConfigOutPath);
            XmlSerializer Xout = new XmlSerializer(typeof(XIntegrations), "c2infosys.com/Integratrex/Service.Config.xsd");
            Xout.Serialize(Fout, IntegratrexConfig);                            

        }

        /// <summary>
        /// Test the message queue
        /// </summary>
        private static void TestMessageQueue() {
            MessageQueue TheQueue = null;
            string queuePath = @".\private$\Integratrex";
            if (!MessageQueue.Exists(queuePath)) {
                try {
                    TheQueue = MessageQueue.Create(queuePath);
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
            else {
                TheQueue = new MessageQueue(queuePath);

            }
            // send a message
            TheQueue.Send("hi", "Testing123");
        }

    }   // end of class

}

