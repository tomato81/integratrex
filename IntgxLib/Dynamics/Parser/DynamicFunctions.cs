using System;

namespace C2InfoSys.FileIntegratrex.Lib {
    
    /// <summary>
    /// Base class for dynamic text functions
    /// </summary>
    public abstract class DyFn {

        protected DyFn(string p_fnName) {
            m_fnName = p_fnName;
        }

        public abstract void AddParam(string p_name, object p_val);

        public abstract Type EvalToType();       

        
        public abstract string EvalStr();
        public abstract object EvalObj();
        

        protected string Name {
            get {
                return m_fnName;
            }
        }

        private string m_fnName;



    }   // DyFn


    /// <summary>
    /// Format Function
    /// </summary>
    public class FormatFn : DyFn {

        /// <summary>
        /// Constructor
        /// </summary>
        public FormatFn() : base("Format") {
            m_format = "{0}";
        }

        /// <summary>
        /// Add a parameter to the function
        /// </summary>
        /// <param name="p_name"></param>
        /// <param name="p_val"></param>
        public override void AddParam(string p_name, object p_val) {
            switch (p_name) {
                case "f": {
                        m_format = "{0:" + p_val.ToString() + "}";
                        break;
                    }
                case "v": {
                        m_value = p_val;
                        break;
                    }
                default: {
                        throw new Exception(string.Format("{0} is not a valid parameter for {1}", p_name, Name));
                    }
            }
        }
        
        /// <summary>
        /// Evaluate as an Object
        /// </summary>
        /// <returns></returns>
        public override object EvalObj() {
            return EvalStr();
        }

        public override Type EvalToType() {
            return typeof(string);
        }

        /// <summary>
        /// Evaluate as a String
        /// </summary>
        /// <returns></returns>
        public override string EvalStr() {
            string r = string.Format(m_format, m_value);
            return r;
        }
        

        // params
        private string m_format;
        private object m_value;

    }   // DateFn


    /// <summary>
    /// Date Function
    /// </summary>
    public class DateFn : DyFn {

        /// <summary>
        /// Constructor
        /// </summary>
        public DateFn() : base("Date") {
            m_format = "{0:MM/dd/yyyy}";
        }


        private bool m_evalStr = false;

        /// <summary>
        /// Add a parameter to the function
        /// </summary>
        /// <param name="p_name"></param>
        /// <param name="p_val"></param>
        public override void AddParam(string p_name, object p_val) {                       
            switch(p_name) {
                case "f": {
                        m_evalStr = true;
                        m_format = "{0:" + p_val.ToString() + "}";
                        break;
                    }
                default: {
                        throw new Exception(string.Format("{0} is not a valid parameter for {1}", p_name, Name));
                    }
            }
        }

        public override Type EvalToType() {
            if(m_evalStr) {
                return typeof(string);
            }
            return typeof(DateTime);
        }

        /// <summary>
        /// Evaluate
        /// </summary>
        /// <returns></returns>
        public override object EvalObj() {
            return DateTime.Now;
        }

        /// <summary>
        /// Evaluate
        /// </summary>
        /// <returns></returns>
        public override string EvalStr() {
            return string.Format(m_format, EvalObj());            
        }        

        // params
        private string m_format;                

    }   // DateFn

}
