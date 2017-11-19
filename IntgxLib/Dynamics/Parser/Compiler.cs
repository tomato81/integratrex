using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2InfoSys.FileIntegratrex.Lib {

    /// <summary>
    /// Compiler
    /// </summary>
    public class Compiler {

        /// <summary>
        /// Complied Token Tree
        /// </summary>
        public MyTree<Token> Tree {
            get {
                return m_Root;
            }
        }
        // member
        private MyTree<Token> m_Root = new MyTree<Token>(); 

        // other members
        private List<Token> m_Tokens;        

        /// <summary>
        /// The Parse Log
        /// </summary>
        public string ParseLog {
            get {
                return m_ParseLog.ToString();
            }
        }
        // member
        private StringBuilder m_ParseLog = new StringBuilder();

        /// <summary>
        /// Log a parse error
        /// </summary>
        /// <param name="p_message">message</param>        
        private void LogParseError(string p_message, Token p_Token) {
            string format = "Compile Error: {0} - {1}";
            m_ParseLog.AppendLine(string.Format(format, p_Token, p_message));            
        }   

        /// <summary>
        /// Constructor
        /// </summary>
        public Compiler(List<Token> p_Tokens) {
            m_Tokens = p_Tokens;
        }

        /// <summary>
        /// Run it
        /// </summary>
        public void BuildTree() {
            try {
 
                // vars
                Stack<MyTree<Token>> FnStack = new Stack<MyTree<Token>>();
                Stack<MyTree<Token>> ParamStack = new Stack<MyTree<Token>>();            
                                                                        
                // go thru tokens and build execution tree
                foreach (Token Tok in m_Tokens) {
                    switch (Tok.TokenType) {
                        case TokenType.TEXT:
                            MyTree<Token> TextNode = new MyTree<Token>(Tok);
                            m_Root.Add(TextNode);
                            break;
                        case TokenType.VARIABLE:                            
                            if (ParamStack.Count == 0) {
                                m_Root.Add(new MyTree<Token>(Tok));
                            }
                            else {
                                ParamStack.Pop().Add(new MyTree<Token>(Tok));                      
                            }                         
                            break;
                        case TokenType.FUNCTION:
                            MyTree<Token> FnNode = new MyTree<Token>(Tok);
                            FnStack.Push(FnNode);
                            if (ParamStack.Count == 0) {
                                m_Root.Add(FnNode);                                
                            }
                            else {
                                ParamStack.Pop().Add(FnNode);
                            } 
                            break;
                        case TokenType.ENDFUNCTION:
                            FnStack.Pop();
                            break;
                        case TokenType.CONSTANT:
                            if (ParamStack.Count == 0) {
                                LogParseError("Hit constant token in improper context", Tok);
                            }
                            ParamStack.Pop().Add(new MyTree<Token>(Tok));
                            break;
                        case TokenType.SYMBOL:
                            break;
                        case TokenType.PARAM:
                            if (FnStack.Count == 0) {
                                LogParseError("Hit param token outside of function context", Tok);
                            }
                            MyTree<Token> ParamToken = new MyTree<Token>(Tok);
                            ParamStack.Push(ParamToken);
                            FnStack.Peek().Add(ParamToken);
                            break;
                        case TokenType.EOF:
                            break;
                        case TokenType.NONE:
                            break;
                        default:
                            break;
                    }              
                }

            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }


    }   // end class Compiler

    /// <summary>
    /// My Tree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyTree<T> : List<MyTree<T>> {

        // members
        public T Value { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_T">cargo</param>
        public MyTree(T p_T)
            : base() {
            Value = p_T;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MyTree()
            : base() {
        }

        /// <summary>
        /// To String
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return ToString(0);
        }

        private string ToString(int p_tabs) {
            try {
                string tabs = new string('\t', p_tabs);
                StringBuilder Sb = new StringBuilder();
                foreach (MyTree<T> B in this) {
                    Sb.Append(tabs);
                    Sb.AppendLine(B.Value.ToString());
                    if (B.Count > 0) {
                        Sb.Append(B.ToString(p_tabs + 1));
                    }
                }
                return Sb.ToString();
            }
            catch (Exception ex) {
                throw ex;
            }
        }


    }   // end class MyTree

}
