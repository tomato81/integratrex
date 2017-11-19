using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2InfoSys.FileIntegratrex.Lib
{
    public class DynamicTextParser {

        /// <summary>
        /// Constructor
        /// </summary>
        public DynamicTextParser(string p_text) {
            SetDynamicText(p_text);
        }

        /// <summary>
        /// Run it
        /// </summary>
        /// <param name="p_Attrs">attributes</param>
        /// <returns>evaluated dynamic text</returns>
        public string Run(Dictionary<string, object> p_Attrs) {
            // function stack
            Stack<DyFn> FnStack = new Stack<DyFn>();
            // evaluate the tree
            List<object> Output = Evaluate(m_Root, p_Attrs, FnStack);

            

            StringBuilder Sb = new StringBuilder();
            foreach (object output in Output) {
                Sb.Append(output.ToString());
            }
            return Sb.ToString();

        }

        /// <summary>
        /// Parse a dymanic text string in a Tree
        /// </summary>
        /// <param name="p_keyVals"></param>
        /// <returns></returns>
        private List<object> Evaluate(MyTree<Token> p_Branch, Dictionary<string, object> p_Attrs, Stack<DyFn> p_FnStack) {

            if(!m_compiled) {
                throw new InvalidOperationException();
            }


            //StringBuilder Sb = new StringBuilder();

            List<object> Output = new List<object>();


            // evaluate the dynamic string against the input dictionary

            // when evaluating branches... only the top level branch needs to return a string ... this will help when evaluating function params (especially dates... which need to retain their objectness as params)


            try {
                
                foreach (MyTree<Token> B in p_Branch) {
                    // does this branch have any depth?
                    if (B.Count > 0) {
                        switch (B.Value.TokenType) {
                            case TokenType.FUNCTION: {
                                    p_FnStack.Push(ParseFn(B.Value));
                                    // is the return of this useful??? i dont think so... it should just complete the params of the function on the stack
                                    if (Evaluate(B, p_Attrs, p_FnStack).Count > 0) {
                                        throw new Exception("evaluation of function parameters returned a non-zero length result list");
                                    }    
                                    DyFn F = p_FnStack.Pop();
                                    
                                    if(F.EvalToType() == typeof(string)) {
                                        Output.Add(F.EvalStr());
                                    }
                                    else {
                                        Output.Add(F.EvalObj());
                                    }                                  

                                    

                                        break;
                                }
                            case TokenType.PARAM: {
                                    if(p_FnStack.Count < 1) {
                                        throw new Exception();
                                    }

                                    
                                    string param = B.Value.Cargo;   // this is the -?? or whatever

                                    // this should return a list with exactly 1 entry
                                    List<object> FnParam = Evaluate(B, p_Attrs, p_FnStack);   // the evaluation of the branch below the param is the paramater value

                                    if(FnParam.Count !=  1) {
                                        throw new Exception("evaluation of parameter value result list length does not equal 1"); 
                                    }


                                    p_FnStack.Peek().AddParam(param, FnParam[0]);
                                    

                                    


                                    break;
                                }
                            case TokenType.TEXT:                                
                            case TokenType.VARIABLE:                              
                            case TokenType.ENDFUNCTION:                                
                            case TokenType.CONSTANT:                                
                            case TokenType.SYMBOL:                                                                      
                            case TokenType.EOF:                                
                            case TokenType.NONE:
                                throw new Exception(string.Format("unexpected token type {0}", B.Value.TokenType));
                            default:
                                throw new Exception(string.Format("unhandled token type {0}", B.Value.TokenType));
                        }
                    }
                    else {
                        switch (B.Value.TokenType) {
                            case TokenType.FUNCTION: {

                                    DyFn F = ParseFn(B.Value);

                                    if (F.EvalToType() == typeof(string)) {
                                        Output.Add(F.EvalStr());
                                    }
                                    else {
                                        Output.Add(F.EvalObj());
                                    }
                                    break;
                                }
                            case TokenType.CONSTANT:
                            case TokenType.TEXT: {
                                    //Sb.Append(B.Value.Cargo);
                                    Output.Add(B.Value.Cargo);
                                    break;
                                }
                            case TokenType.VARIABLE: {
                                    //Sb.Append(p_Attrs[B.Value.Cargo].ToString());
                                    Output.Add(p_Attrs[B.Value.Cargo]);
                                    break;
                                }
                            case TokenType.PARAM:
                            case TokenType.ENDFUNCTION:                            
                            case TokenType.SYMBOL:
                            case TokenType.EOF:
                            case TokenType.NONE:
                                throw new Exception(string.Format("unexpected token type {0}", B.Value.TokenType));
                            default:
                                throw new Exception(string.Format("unhandled token type {0}", B.Value.TokenType));
                        }
                    }
                }

                return Output;

                /*
                StringBuilder Sb = new StringBuilder();
                foreach(string output in Output) {
                    Sb.Append(output);
                }
                return Sb.ToString();
                */
            }
            catch (Exception ex) {
                throw ex;
            }

            
        }


        private DyFn ParseFn(Token p_FnToken) {
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
        public void Compile() {
            try {
                if(m_compiled) {
                    throw new InvalidOperationException();
                }
                if(m_text.Length == 0) {
                    throw new InvalidOperationException();
                }                
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
            }
            catch (Exception ex) {
                throw ex;
            }
            
        }
       


    }   // end of class
}
