using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2InfoSys.FileIntegratrex.Lib {

    public enum TokenType {
        TEXT,
        VARIABLE,        
        FUNCTION,
        ENDFUNCTION,
        CONSTANT,
        SYMBOL,
        PARAM,
        EOF,
        NONE
    }   // TokenType

    /// <summary>
    /// Token
    /// </summary>
    public class Token {

        /// <summary>
        /// Constructor
        /// </summary>
        public Token() {
            m_TokenType = TokenType.NONE;
            m_cargo = string.Empty;
            m_verboseCargo = string.Empty;
        }

        /// <summary>
        /// Get a Token representing the EOF
        /// </summary>
        public static Token EOF() {
            Token T = new Token();
            T.TokenType = TokenType.EOF;
            T.AddChar(Character.None());
            T.m_verboseCargo = "{eof}";
            return T;
        }

        /// <summary>
        /// Add a character to the token
        /// </summary>
        /// <param name="C">character to add</param>
        /// <returns>length of the token</returns>
        public int AddChar(Character C) {
            if (m_cargo.Length == 0) {
                m_FirstChar = C;
            }
            m_cargo = m_cargo + C.Cargo.ToString();
            m_verboseCargo = m_verboseCargo + C.DescribeCargo();
            return m_cargo.Length;
        }

        // members
        private Character m_FirstChar;        
        private string m_verboseCargo;

        /// <summary>
        /// Token Content
        /// </summary>
        public string Cargo {
            get {
                return m_cargo;
            }
        }
        // member
        private string m_cargo;

        /// <summary>
        /// Set the Depth of this Token
        /// </summary>
        /// <param name="p_depth">the depth</param>
        public void SetDepth(int p_depth) {
            m_depth = p_depth;
        }
        // member 
        private int m_depth;
        // prop
        public int Depth {
            get {
                return m_depth;
            }
        }

        /// <summary>
        /// Token Type
        /// </summary>
        public TokenType TokenType {
            get {
                return m_TokenType;
            }
            set {
                if (m_TokenType != TokenType.NONE) {
                    throw new InvalidOperationException("TokenType is immutable once set.");
                }
                m_TokenType = value;                
            }
        }
        // member
        private TokenType m_TokenType;  

        /// <summary>
        /// To String!
        /// </summary>
        /// <returns></returns>
        public override string ToString() {            
            return string.Format("type:{0} token:{1}", m_TokenType, m_verboseCargo);
        }

    }   // Token


    /// <summary>
    /// Lexxer
    /// </summary>
    public class Lexxer {


        // control constants
        private const int PARSE_ERROR_SNIP_LENGTH = 14;

        // symbols
        private const char ASSIGNMENT = '=';
        private const char BEGIN_DYNAMIC = '?';
        private const char OPEN_SQUIG = '{';
        private const char CLOSE_SQUIG = '}';
        private const char OPEN_BRAC = '(';
        private const char CLOSE_BRAC = ')';
        private const char PARAM = '-';
        private const char DB_QUOTE = '"';

        // whitespace
        private const char SPACE = ' ';
        private const char NEWLINE = '\n';
        private const char CR = '\r';
        private const char TAB = '\t';

        // escape character
        private const char ESCAPE = '\\';        

        // define the language        
        private readonly char[] SYMBOLS = { ASSIGNMENT, BEGIN_DYNAMIC, OPEN_SQUIG, CLOSE_SQUIG, OPEN_BRAC, CLOSE_BRAC, PARAM, DB_QUOTE };
        private readonly char[] WHITESPACE = { SPACE, CR, NEWLINE, TAB };
        private readonly char[] LETTERS = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                                   'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        private readonly char[] NUMBERS = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };


        private readonly char[] VARIABLE_CHARS = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                                                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 
                                                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                                                '.', '-', '_' };

        private readonly char[] SYMBOL_CHARS = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                                                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 
                                                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                                                '+', '-' };

        /// <summary>
        /// The current token
        /// </summary>
        public Token Token {
            get {
                return m_Token;
            }
        }
        // member
        private Token m_Token;

        /// <summary>
        /// The Parse Log
        /// </summary>
        public string ParseLog {
            get {
                return m_ParseLog.ToString();
            }
        }
        // member
        private StringBuilder m_ParseLog;

        // other members
        private Scanner m_Scanner;        
        private Token m_PrevToken;
        private int m_index;        

        // same same        
        private int m_inFunction;   // read2
    
        /// <summary>
        /// Constructor
        /// </summary>
        public Lexxer(Scanner p_Scanner) {
            m_Scanner = p_Scanner;
            m_ParseLog = new StringBuilder();
            m_index = 0;            
            m_inFunction = 0;
        }

        /// <summary>
        /// Log a parse error
        /// </summary>
        /// <param name="p_message">message</param>
        /// <param name="p_OnChar">error on this Character</param>
        private void LogParseError(string p_message, Token p_Token, Character p_OnChar) {        
            string nearbyText = m_Scanner.GetSurrounding(p_OnChar, PARSE_ERROR_SNIP_LENGTH);
            string format = "{0} at line:{1} col:{2} near [...{3}...]";
            m_ParseLog.AppendLine(string.Format(format, p_message, p_OnChar.Position.Line, p_OnChar.Position.Col, nearbyText));             
        }         

        /// <summary>
        /// Read the next token
        /// </summary>
        /// <returns></returns>
        public bool Read() {            
            //bool escaped = false;
            bool dynamic = false;
            bool varOpenSquig = false;
            bool constOpenQuote = false;
            bool tokenStatus = false;            
            // going to build this token
            Token TheToken = new Token();
            // how many characters in the token
            int charCount = 0;
            // while our scanner can scan & the token is incomplete
            while (!tokenStatus && m_Scanner.Read()) {
                // get the next character
                Character C = m_Scanner.Char;                
                switch (TheToken.TokenType) {
                    case TokenType.NONE:    // the token type has not been identified                        
                        if (C.CharType == CharType.NONE) {  // end-of-file                            
                            if (m_inFunction > 0) {  // can't have an unclosed function at end-of-file
                                LogParseError("unclosed function", TheToken, m_Scanner.Char);
                            }
                            return false;                            
                        }
                        else if (SYMBOLS.Count(sym => C == sym) > 0) {  // determine token type
                            if (C == BEGIN_DYNAMIC) {   // this token is dynamic text                                
                                dynamic = true;
                                // peek at the next character
                                Character P = m_Scanner.Peek();
                                // what kind of dynamic text element?
                                if (P == OPEN_BRAC) {   // it is a ?(function)
                                    TheToken.TokenType = TokenType.FUNCTION;
                                    m_inFunction++; // adjust function depth context
                                    m_Scanner.Read();   // move cursor forward                                    
                                }   
                                else if (P == OPEN_SQUIG) { // it is a ?{variable}
                                    varOpenSquig = true;
                                    TheToken.TokenType = TokenType.VARIABLE;
                                    m_Scanner.Read();   // move cursor forward                                    
                                }
                                else if (LETTERS.Count(letter => P == letter) > 0) {    // it is a ?variable
                                    TheToken.TokenType = TokenType.VARIABLE;
                                }                                                                                 
                                else {  // it is invalid syntax
                                    LogParseError("invalid character after dynamic identifier", TheToken, P);
                                    tokenStatus = true;
                                }                          
                            }
                            else if (m_inFunction > 0) {                                
                                if (C == PARAM) {  // it is a ?(function -parameter)
                                    TheToken.TokenType = TokenType.PARAM;                                    
                                }
                                else if (C == DB_QUOTE) {   // it is a ?(function -parameter "constant value")
                                    constOpenQuote = true;
                                    TheToken.TokenType = TokenType.CONSTANT;                                    
                                }
                                else if (C == CLOSE_BRAC) {  // it is a ?(function) closing brace
                                    TheToken.TokenType = TokenType.ENDFUNCTION;
                                    m_inFunction--; // adjust function depth context      
                                    tokenStatus = true; // complete token
                                }
                                else {  // it is invalid syntax
                                    LogParseError("unexpected symbol in function context", TheToken, C);
                                    tokenStatus = true;
                                }
                            }
                            else {  // treat the symbol as normal text
                                TheToken.TokenType = TokenType.TEXT;
                                m_Scanner.Back(); 
                            }                         
                        }
                        else if (C == ESCAPE) { // escape character detected, consume next character as normal text
                            Character P = m_Scanner.Peek();
                            if (P.CharType == CharType.NONE) {
                                LogParseError("escape character cannot precede EOF", TheToken, C);
                                tokenStatus = true;
                            }
                            else {
                                TheToken.TokenType = TokenType.TEXT;
                                m_Scanner.Read();   // consume next character                            
                                TheToken.AddChar(m_Scanner.Char);  // and add it to the token                            
                            }
                        }
                        else if (m_inFunction > 0) {    // in a function looking for a token
                            if (WHITESPACE.Count(space => C == space) == 0) {    // only whitespace allowed in this context                                
                                LogParseError("unexpected symbol in function context", TheToken, C);
                                tokenStatus = true;
                            }
                        }
                        else {  // normal text
                            TheToken.TokenType = TokenType.TEXT;
                            TheToken.AddChar(C);  
                        } 
                        break;
                    case TokenType.TEXT:
                        Debug.Assert(m_inFunction == 0);    // should never be in a function while parsing a text token
                        if (C.CharType == CharType.NONE) {  // end-of-file                            
                            tokenStatus = true;
                        }
                        else if (C == ESCAPE) {
                            Character P = m_Scanner.Peek();
                            if (P.CharType == CharType.NONE) {
                                LogParseError("escape character cannot precede EOF", TheToken, P);
                                tokenStatus = true;
                            }
                            else {
                                m_Scanner.Read();   // consume next character                            
                                TheToken.AddChar(m_Scanner.Char);  // and add it to the token                            
                            }
                        }
                        else if (C == BEGIN_DYNAMIC) {  // end of text token - beginning a dynamic element
                            tokenStatus = true;
                            m_Scanner.Back();   // move the cursor back so the ? is consumed on the next iteration
                        }
                        else {
                            TheToken.AddChar(C);
                        }
                        break;
                    case TokenType.VARIABLE:                      
                        if (varOpenSquig) { // this var is braced with { squiggly brackets }
                            if (C.CharType == CharType.NONE) {  // end-of-file                            
                                LogParseError("unexpected end-of-file in braced variable", TheToken, C);
                                tokenStatus = true;
                            }
                            else if (WHITESPACE.Count(space => C == space) > 0) {
                                LogParseError("whitespace not allowed in a braced variable", TheToken, C);
                                tokenStatus = true;
                            }
                            else if (C == CLOSE_SQUIG) { // end of ?{variable} token
                                tokenStatus = true;                                
                            }
                        }
                        else {                          
                            if (WHITESPACE.Count(space => C == space) > 0) {    // end of ?variable token                      
                                tokenStatus = true;
                                m_Scanner.Back();   // move the cursor back so the whitespace is consumed on the next iteration                                
                            }
                        }
                        if (!tokenStatus) {
                            if (VARIABLE_CHARS.Count(letter => C == letter) == 0) {    // only letters may be part of a variable name
                                LogParseError("invalid character in variable name", TheToken, C);
                                tokenStatus = true;
                            }
                            else {  // add character to the token
                                TheToken.AddChar(C);
                            }
                        }
                        break;
                    case TokenType.FUNCTION:
                        if (C.CharType == CharType.NONE) {  // end-of-file                            
                            LogParseError("unexpected end-of-file in function call", TheToken, C);
                            tokenStatus = true;
                        }
                        else if (WHITESPACE.Count(space => C == space) > 0) {    // end of ?(function token                                                          
                            tokenStatus = true;
                        }
                        else if (C == CLOSE_BRAC) {  // end of ?(function) token                              
                            m_Scanner.Back();   // move the cursor back so function closing brace can be consumed by the next iteration
                            tokenStatus = true;
                        }
                        else if (LETTERS.Count(letter => C == letter) == 0) {    // only letters may be part of a function name
                            LogParseError("function identifiers may only contain letters", TheToken, C);
                            tokenStatus = true;
                        }
                        else {
                            TheToken.AddChar(C);
                        }      
                        break;
                    case TokenType.ENDFUNCTION:
                        LogParseError("invalid token type in parse context", TheToken, C);
                        tokenStatus = true;
                        break;
                    case TokenType.CONSTANT:                        
                        Debug.Assert(m_inFunction > 0);    // should always be a function when parsing a constant value
                        if (C.CharType == CharType.NONE) {  // end-of-file                            
                            LogParseError("unexpected eod-of-file when parsing constant value token", TheToken, C);
                            tokenStatus = true;
                        }
                        else if (C == ESCAPE) {  // next char is escaped
                            Character P = m_Scanner.Peek();
                            if (P.CharType == CharType.NONE) {
                                LogParseError("unexpected eod-of-file when parsing constant value token", TheToken, C);
                                tokenStatus = true;
                            }
                            else {                                
                                m_Scanner.Read();   // consume next character                            
                                TheToken.AddChar(m_Scanner.Char);  // and add it to the token                            
                            }
                        }
                        else {
                            if (C == DB_QUOTE) {    // end of constant value token
                                tokenStatus = true;
                            }
                            else {
                                TheToken.AddChar(C);
                            }
                        }                     
                        break;
                    case TokenType.SYMBOL:
                        throw new NotImplementedException("TokenType==SYMBOL");
                        //Debug.Assert(m_inFunction > 0);    // should always be a function when parsing a function parameter
                        //break;
                    case TokenType.PARAM:
                        Debug.Assert(m_inFunction > 0);    // should always be a function when parsing a function parameter
                        if (C.CharType == CharType.NONE) {  // end-of-file                            
                            LogParseError("unexpected eod-of-file when parsing function parameter", TheToken, C);
                            tokenStatus = true;
                        }
                        else if (WHITESPACE.Count(space => C == space) > 0 || C == ASSIGNMENT) {    // end of -param token                                                          
                            tokenStatus = true;
                        }
                        else if (LETTERS.Count(letter => C == letter) == 0) {    // only letters may be part of a ?(function -param)
                            LogParseError("function parameters may only contain letters", TheToken, C);
                            tokenStatus = true;
                        }
                        else {
                            TheToken.AddChar(C);
                        }
                        break;                    
                    default:
                        break;                           
                }                
                // next
                charCount++;
            }
            // set output
            m_Token = TheToken;
            // next token
            m_index++;
            // return it
            return tokenStatus;
        }


    }   // Lexxer
}
