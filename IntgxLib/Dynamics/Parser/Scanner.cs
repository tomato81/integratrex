using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2InfoSys.FileIntegratrex.Lib {

    /// <summary>
    /// Character Types
    /// </summary>
    public enum CharType {
        TEXT,        
        SPACE,
        TAB,
        CR,
        NEWLINE,                        
        NONE
    }   // enum CharType

    /// <summary>
    /// Use to scan characters from the source file
    /// </summary>
    public class Scanner {

        /// <summary>
        /// Constructor
        /// </summary>
        public Scanner(string p_source) {
            m_source = p_source;         

            m_Ms = new MemoryStream(Encoding.UTF8.GetBytes(p_source));

            

            m_Sr = new StreamReader(m_Ms);          
            m_buffer = new char[1];
            m_index = 0;
            m_Position = new Position(m_index, 0, 0);
            m_eof = false;
        }

        /// <summary>
        /// Scan next character
        /// </summary>
        /// <returns>a Character object</returns>
        public bool Read() {
            if (m_eof) {
                return false;
            }
            // set prev char ref, unless the EOF has been reached
            m_prevChar = m_theChar;
            // read next
            if (m_Sr.Read(m_buffer, 0, 1) < 1) {               
                m_theChar = Character.None();
                m_theChar.Position.Adjust(m_Position);
                m_eof = true;
            }
            else {
                // create the char
                m_theChar = new Character(m_buffer[0], m_Position);
                // increment stream index and character position
                m_index++;
                if (m_theChar.CharType == CharType.NEWLINE) {
                    m_Position.NewLine();
                }
                else {
                    m_Position.IncrementCol();
                }
            }            
            // out
            return true;
        }

        /// <summary>
        /// Can Read?
        /// </summary>
        public bool CanRead {
            get {
                try {
                    return !m_eof;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Peek at the next character without moving the scanner forward
        /// </summary>
        /// <returns></returns>
        public Character Peek() {
            try {
                int peek = m_Sr.Peek();
                if (peek < 0) {
                    return Character.None();
                }
                return new Character(Convert.ToChar(peek), m_Position.Index, m_Position.Line, m_Position.Col);
            }
            catch(Exception ex) {                
                throw ex;
            }
        }

        /// <summary>
        /// Move stream back
        /// </summary>
        /// <remarks>can only move back once</remarks>
        /// <returns>true if the pointer was moved back</returns>
        public bool Back() {            
            if(m_index == 0) {  // can't move back
                return false;
            }
            if (m_prevChar.CharType == CharType.NONE) { // can't move back
                return false;
            }
            // move stream pointer
            m_Ms.Seek(--m_index, SeekOrigin.Begin);
            m_Sr.DiscardBufferedData();
            // return the cursor to the previous position          
            m_Position.Adjust(m_theChar.Position);
            // adjust output
            m_theChar = m_prevChar;
            m_prevChar = Character.None();            
            // yes
            return true;
        }

        /// <summary>
        /// Get the context of a character by returning a part of the string near the character
        /// </summary>
        /// <param name="p_C"></param>
        /// <returns></returns>
        public string GetSurrounding(Character p_C, int p_numChars) {
            try {             
                int startIndex = p_C.Position.Index - p_numChars < 0 ? 0 : p_C.Position.Index - p_numChars;             
                int length = p_numChars * 2;
                if (startIndex + (p_numChars * 2) > m_source.Length) {
                    length = m_source.Length - startIndex;
                }
                string surrounding = m_source.Substring(startIndex, length);
                return surrounding;            
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        /// The current character
        /// </summary>
        public Character Char {
            get {
                return m_theChar;
            }
        }
        // member
        private Character m_theChar;

        /// <summary>
        /// Scanner index
        /// </summary>
        public int Index {
            get {
                return m_index;
            }
        }
        // member
        private int m_index;

        public Position Position {
            get {
                return m_Position;
            }
        }        
        // member
        private Position m_Position;        

        // other members
        private string m_source;
        private StringReader m_R;
        private StreamReader m_Sr;
        private MemoryStream m_Ms;
        private char[] m_buffer;
        private Character m_prevChar;
        private bool m_eof;

    }   // end class Scanner

    /// <summary>
    /// Position of a character
    /// </summary>
    public class Position {

        /// <summary>
        /// Constructor
        /// </summary>
        public Position() {
            Adjust(-1, -1, -1);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_index">absolute position in source file</param>
        /// <param name="p_line">line in source file</param>
        /// <param name="p_col">column in source file</param>
        public Position(int p_index, int p_line, int p_col) {
            Adjust(p_index, p_line, p_col);
        }

        /// <summary>
        /// Adjust the position
        /// </summary>
        /// <param name="p_index">absolute position in source file</param>
        /// <param name="p_line">line in source file</param>
        /// <param name="p_col">column in source file</param>
        public void Adjust(int p_index, int p_line, int p_col) {
            m_index = p_index;
            m_line = p_line;
            m_col = p_col;
        }

        /// <summary>
        /// Adjust the position
        /// </summary>
        /// <param name="p_Position">adjust to this position</param>
        public void Adjust(Position p_Position) {
            Adjust(p_Position.Index, p_Position.Line, p_Position.Col);
        }

        /// <summary>
        /// Increment position one column forward
        /// </summary>
        public void IncrementCol() {
            m_index++;
            m_col++;            
        }

        /// <summary>
        /// Move to new line
        /// </summary>
        public void NewLine() {
            m_index++;
            m_line++;
            m_col = 0;
        }

        /// <summary>
        /// Absolute position in source file
        /// </summary>
        public int Index {
            get {
                return m_index;
            }
        }
        // member 
        int m_index;

        /// <summary>
        /// Line of source file
        /// </summary>
        public int Line {
            get {
                return m_line;
            }
        }
        // member 
        int m_line;

        /// <summary>
        /// Column of source file
        /// </summary>
        public int Col {
            get {
                return m_col;
            }
        }
        // member 
        int m_col;      

        /// <summary>
        /// To String
        /// </summary>
        /// <returns>a string representation of the object</returns>
        public override string ToString() {
            string f = "Index:{0} Line:{1} Col:{2}";
            return string.Format(f, Index, Line, Col);           
        }

    }   // end class Position

    /// <summary>
    /// A character from the source file
    /// </summary>
    public class Character : IEquatable<Character>, IEquatable<char> {  

        /// <summary>
        /// Position
        /// </summary>
        public Position Position {
            get {
                return m_Position;
            }
        }
        // member
        private Position m_Position;

        /// <summary>
        /// The Character Type
        /// </summary>
        public CharType CharType {
            get {
                return m_charType;
            }
        }
        // member
        private CharType m_charType;

        /// <summary>
        /// Cargo
        /// </summary>
        public char Cargo {
            get {
                return m_cargo;
            }
        }
        // members
        private char m_cargo;        

        /// <summary>
        /// Constructor
        /// </summary>
        public Character(char p_cargo, int p_index, int p_line, int p_col) {
            m_cargo = p_cargo;
            m_Position = new Position(p_index, p_line, p_col);            
            m_charType = ParseCharType(m_cargo);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Character(char p_cargo, Position p_Position) 
            : this(p_cargo, p_Position.Index, p_Position.Line, p_Position.Col) {            
        }

        /// <summary>
        /// Return a nothing character
        /// </summary>
        /// <returns></returns>
        public static Character None() {
            return new Character(CharType.NONE);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        private Character(CharType T) {
            m_charType = T;
            m_Position = new Position();
        }

        /// <summary>
        /// Categorize a character
        /// </summary>
        /// <param name="c">the character</param>
        /// <returns>the char type</returns>
        private CharType ParseCharType(char c) {
            switch (m_cargo) {
                case ' ':
                    return CharType.SPACE;
                case '\r':
                    return CharType.CR;
                case '\n':
                    return CharType.NEWLINE;
                case '\t':
                    return CharType.TAB;                           
                default:
                    return CharType.TEXT;
            }
        }

        /// <summary>
        /// Get cargo and replace special characters with descriptive text
        /// </summary>
        /// <returns></returns>
        public string DescribeCargo() {
            string cargo;
            switch (m_charType) {
                case CharType.NONE:
                    cargo = "{none}";
                    break;
                case CharType.SPACE:
                    cargo = "{space}";
                    break;
                case CharType.CR:
                    cargo = "{cr}";
                    break;
                case CharType.NEWLINE:
                    cargo = "{lf}";
                    break;
                case CharType.TAB:
                    cargo = "{tab}";
                    break;                
                case CharType.TEXT:
                    cargo = m_cargo.ToString();
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return cargo;            
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            string toString = "line:{0} col:{1} cargo:{2}";
            string cargo;
            switch (m_charType) {
                case CharType.SPACE:
                    cargo = "{space}";
                    break;
                case CharType.CR:
                    cargo = "{cr}";
                    break;
                case CharType.NEWLINE:
                    cargo = "{lf}";
                    break;
                case CharType.TAB:
                    cargo = "{tab}";
                    break;                
                case CharType.TEXT:
                    cargo = m_cargo.ToString();
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return string.Format(toString, m_Position.Line, m_Position.Col, cargo);
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="p_rhs"></param>
        /// <returns></returns>
        public bool Equals(Character p_rhs) {
            return Cargo.Equals(p_rhs.Cargo);
        }
        public bool Equals(char p_rhs) {
            return Cargo.Equals(p_rhs);
        }

        /// <summary>
        /// !=
        /// </summary>        
        public static bool operator !=(Character p_lhs, Character p_rhs) {
            return !p_lhs.Equals(p_rhs);
        }
        public static bool operator !=(Character p_lhs, char p_rhs) {
            return !p_lhs.Equals(p_rhs);
        }

        /// <summary>
        /// ==
        /// </summary>        
        public static bool operator ==(Character p_lhs, Character p_rhs) {
            return p_lhs.Equals(p_rhs);
        }
        public static bool operator ==(Character p_lhs, char p_rhs) {
            return p_lhs.Equals(p_rhs);
        }

    }   // end class Character    

}
