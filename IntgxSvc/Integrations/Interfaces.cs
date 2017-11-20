using System;
using System.IO;
using System.Collections.Generic;

using C2InfoSys.FileIntegratrex.Lib;
using System.Diagnostics;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// File matching pattern
    /// </summary>
    public interface IPattern {
        bool IsMatch(string p_fileName);
        XPatternType PatternType { get; }

    }   // IPattern


    /// <summary>
    /// Integration Source Location
    /// </summary>
    public interface ISourceLocation {
        string Description { get; }        
        MatchedFile[] Scan(IPattern[] p_Pattern);
        void Get(MatchedFile[] p_Mf);
        void Delete(MatchedFile[] p_Mf);        
        void Rename(MatchedFile[] p_Mf, string[] p_rename);
        void DeleteFolder(string p_folder);
        void Ping();              

    }   // ISourceLocation


    /// <summary>
    /// Integration Response
    /// </summary>
    public interface IResponse {
        bool IsSupress(MatchedFile p_Mf);   // hm
        void Transform(MatchedFile[] p_Mf);
        void Action();
    }

    /// <summary>
    /// Integration Response Target
    /// </summary>
    public interface IResponseTarget {
        void Ping();
    }   // IResponseTarget

}