using System;
using System.Text.RegularExpressions;

using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// File matching Pattern Factory
    /// </summary>
    public class PatternFactory {

        /// <summary>
        /// Constructor
        /// </summary>
        public PatternFactory() {
        }

        /// <summary>
        /// Create a pattern object
        /// </summary>
        /// <param name="p_XPattern">X Pattern</param>
        /// <returns>a pattern object</returns>
        public IPattern Create(XPattern p_XPattern) {         
            switch(p_XPattern.Type) {
                case XPatternType.RegEx:
                    return new RegExPattern(p_XPattern);                    
                case XPatternType.Simple:
                    return new SimplePattern(p_XPattern);
                case XPatternType.Exact:                    
                case XPatternType.ExactIgnoreCase:
                    return new ExactPattern(p_XPattern);
                default:
                    throw new InvalidOperationException();
            }
        }

    }   // PatternFactory

    public abstract class Pattern : IPattern {

        // members
        protected XPattern m_Pattern;        

        /// <summary>
        /// Constructor
        /// </summary>
        public Pattern(XPattern p_P) {
            m_Pattern = p_P;            
        }

        /// <summary>
        /// Does the filename match the pattern?
        /// </summary>
        /// <param name="p_filename">match this filename against the pattern</param>
        /// <returns>boolean</returns>
        public abstract bool IsMatch(string p_filename);

        /// <summary>
        /// Pattern Type
        /// </summary>
        public XPatternType PatternType {
            get {
                return m_Pattern.Type;
            }
        }

        /// <summary>
        /// Describes the pattern
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format("{0} {1} {2}", Enum.GetName(typeof(XPatternType), PatternType), m_Pattern.Desc, m_Pattern.Text);
        }

    }

    /// <summary>
    /// Exact File Matching Pattern
    /// </summary>
    public class ExactPattern : Pattern {

        // members
        private StringComparison m_StringComparison;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExactPattern(XPattern p_P) : base(p_P) {                       
            m_StringComparison = p_P.Type == XPatternType.ExactIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        /// <summary>
        /// Does the filename match the pattern?
        /// </summary>
        /// <param name="p_filename">match this filename against the pattern</param>
        /// <returns>boolean</returns>
        public override bool IsMatch(string p_filename) {
            return m_Pattern.Text[0].Equals(p_filename, m_StringComparison);
        }

    }   // SimplePattern

    /// <summary>
    /// Simple File Matching Pattern
    /// </summary>
    public class SimplePattern : Pattern {

        /// <summary>
        /// Constructor
        /// </summary>
        public SimplePattern(XPattern p_P) 
            : base(p_P) {            
        }

        /// <summary>
        /// Does the filename match the pattern?
        /// </summary>
        /// <param name="p_filename">match this filename against the pattern</param>
        /// <returns>boolean</returns>
        public override bool IsMatch(string p_filename) {
            if(Operators.LikeString(p_filename, m_Pattern.Text[0], CompareMethod.Text)) {
                return true;
            }
            return false;
        }

    }   // SimplePattern

    /// <summary>
    /// Regular Expression File Matching Pattern
    /// </summary>
    public class RegExPattern : Pattern {

        // members
        private Regex m_RegEx;        

        /// <summary>
        /// Constructor
        /// </summary>
        public RegExPattern(XPattern p_P) 
            : base(p_P) {            
            m_RegEx = new Regex(p_P.Text[0]);
        }

        /// <summary>
        /// Does the filename match the pattern?
        /// </summary>
        /// <param name="p_filename">match this filename against the pattern</param>
        /// <returns>boolean</returns>
        public override bool IsMatch(string p_filename) {
            Match M = m_RegEx.Match(p_filename);
            if(M.Success && M.Value.Equals(p_filename, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            return false;
        }

    }   // RegExPattern
}
