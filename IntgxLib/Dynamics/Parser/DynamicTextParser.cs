using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2InfoSys.FileIntegratrex.Lib
{

    /// <summary>
    /// Value Required Event Args
    /// </summary>
    public class OnValueRequiredEventArgs : EventArgs {

        /// <summary>
        /// Value Requried Event Args
        /// </summary>
        public OnValueRequiredEventArgs(string p_name) :
            this(p_name, string.Empty) {            
        }

        /// <summary>
        /// Value Requried Event Args
        /// </summary>
        public OnValueRequiredEventArgs(string p_name, string p_tag) {
            m_name = p_name;
            Tag = p_tag;
        }

        /// <summary>
        /// UDF
        /// </summary>
        public readonly string Tag;

        /// <summary>
        /// Result
        /// </summary>
        public object Result { get; set; }        

        /// <summary>
        /// Variable Name
        /// </summary>
        public string Name {
            get {
                return m_name;
            }
        }       
        // member
        private readonly string m_name;        

    }

    /// <summary>
    /// Raised when the OnValueRequired event is not handled by the client
    /// </summary>
    public class OnValueRequiredNotHandledException : Exception {

        /// <summary>
        /// Constructor
        /// </summary>
        public OnValueRequiredNotHandledException() :
            base("OnValueRequired event is not handled") {

        }

    }   //  OnValueRequiredNotHandledException


    /// <summary>
    /// Dynamic Text Parser
    /// </summary>
    public class DynamicTextParser {

        /// <summary>
        /// Constructor
        /// </summary>
        public DynamicTextParser(string p_text) {
            SetDynamicText(p_text);
        }

        /// <summary>
        /// Value Required Event
        /// </summary>
        /// <remarks>This handler must be implemented by the client</remarks>
        public event EventHandler<OnValueRequiredEventArgs> OnValueRequired;
        /// <summary>
        /// Get a Attribute Value
        /// </summary>
        /// <param name="p_name"></param>
        /// <returns></returns>
        private object GetValue(string p_name) {
            if(OnValueRequired == null) {
                throw new OnValueRequiredNotHandledException();
            }
            OnValueRequiredEventArgs Args = new OnValueRequiredEventArgs(p_name);
            OnValueRequired?.Invoke(this, Args);
            return Args.Result;
        }

        /// <summary>
        /// Run it
        /// </summary>        
        /// <returns>evaluated dynamic text</returns>
        public string Run(){
            // function stack
            Stack<DyFn> FnStack = new Stack<DyFn>();
            // evaluate the tree                   
            List<object> Output = Evaluate(m_Root, FnStack);
            // build up the processed string 
            StringBuilder Sb = new StringBuilder();
            foreach (object output in Output) {
                Sb.Append(output.ToString());
            }
            return Sb.ToString();
        }

        /// <summary>
        /// Parse a dymanic text string in a Tree
        /// </summary>        
        /// <returns></returns>
        private List<object> Evaluate(MyTree<Token> p_Branch, Stack<DyFn> p_FnStack) {

            if (!m_compiled) {
                throw new InvalidOperationException();
            }

            List<object> Output = new List<object>();


            //evaluate the dynamic string against the input dictionary
            //when evaluating branches... only the top level branch needs 
            //to return a string ... this will help when evaluating 
            //function params (especially dates... which need to retain 
            //their objectness as params)


            try {
                foreach (MyTree<Token> B in p_Branch) {
                    switch (B.Value.TokenType) {
                        case TokenType.FUNCTION: {
                                p_FnStack.Push(GetFn(B.Value));
                                if (Evaluate(B, p_FnStack).Count > 0) {    //  the return of this isn't useful. it completes the params of the function on the stack
                                    throw new Exception("evaluation of function parameters returned a non-zero length result list");
                                }
                                DyFn F = p_FnStack.Pop();
                                if (F.EvalToType() == typeof(string)) {
                                    Output.Add(F.EvalStr());
                                }
                                else {
                                    Output.Add(F.EvalObj());
                                }
                                break;
                            }
                        case TokenType.PARAM: {
                                if (p_FnStack.Count < 1) {
                                    throw new Exception();
                                }
                                string param = B.Value.Cargo;   // this is the -?? or -whatever    
                                List<object> FnParam = Evaluate(B, p_FnStack);   // the evaluation of the branch below the param is the paramater value. this should return a list with exactly 1 entry
                                if (FnParam.Count != 1) {
                                    throw new Exception("evaluation of parameter value result list length does not equal 1");
                                }
                                p_FnStack.Peek().AddParam(param, FnParam[0]);
                                break;
                            }
                        case TokenType.CONSTANT:
                        case TokenType.TEXT: {
                                Output.Add(B.Value.Cargo);
                                break;
                            }
                        case TokenType.VARIABLE: {                             
                                Output.Add(GetValue(B.Value.Cargo));
                                break;
                            }
                        case TokenType.ENDFUNCTION:
                        case TokenType.SYMBOL:
                        case TokenType.EOF:
                        case TokenType.NONE:
                            throw new Exception(string.Format("unexpected token type {0}", B.Value.TokenType));
                        default:
                            throw new Exception(string.Format("unhandled token type {0}", B.Value.TokenType));
                    }
                }
                // done
                return Output;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Get Function from the Token
        /// </summary>
        /// <param name="p_FnToken"></param>
        /// <returns></returns>
        private DyFn GetFn(Token p_FnToken) {
            if(p_FnToken.TokenType != TokenType.FUNCTION) {
                throw new Exception();
            }          
            switch(p_FnToken.Cargo) {
                case "Date": {
                    return new DateFn();                        
                }
                case "Format": {
                    return new FormatFn();                        
                }
                case "GUID": {
                    return new GuidFn();
                }
                default: {
                    throw new Exception(string.Format("{0} is not a function", p_FnToken.Cargo));
                }
            }            
        }

        /// <summary>
        /// Set the dynamic text
        /// </summary>
        /// <param name="p_text">dynamic text string</param>
        public void SetDynamicText(string p_text) {
            m_text = p_text;
        }

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
        private string m_text = string.Empty;
        private bool m_compiled = false;

        /// <summary>
        /// Compile the Dynamic Text
        /// </summary>
        /// <param name="p_text">dynamic text string</param>
        public void Compile(string p_text) {
            SetDynamicText(p_text);
            Compile();
        }                

        /// <summary>
        /// Compile
        /// </summary>
        /// <returns>false if there are no dynamic elements in the source string</returns>
        public bool Compile() {
            try {
                if(m_compiled) {
                    throw new InvalidOperationException();
                }
                //if(m_text.Length == 0) {
                //    throw new InvalidOperationException();  // maybe 0 length is fine
                //}                
                // a character scanner
                Scanner S = new Scanner(m_text);
                // a tokenizer
                Lexxer L = new Lexxer(S);
                // read the tokens
                List<Token> Tokens = new List<Token>();
                while (L.Read()) {
                    Tokens.Add(L.Token);
                }              
                // a compiler
                Compiler C = new Compiler(Tokens);
                C.BuildTree();
                // assign local tree
                m_Root = C.Tree;              
                // compiled
                m_compiled = true;
                // returns false if there is nothing dynamic about this text
                return !(Tokens.Count == 1 && Tokens[0].TokenType == TokenType.TEXT);                
            }
            catch (Exception ex) {
                throw ex;
            }            
        }    

    }   // end of class
}
