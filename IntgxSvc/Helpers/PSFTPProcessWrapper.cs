using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Configuration;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// PSFTP Process Wrapper
    /// </summary>
    public class PSFTPProcessWrapper {

        // constants
        private readonly string c_psftp = ConfigurationManager.AppSettings["SFTPClient"];
        private readonly string c_args = ConfigurationManager.AppSettings["SFTPClientArgs"];
        private readonly string c_ppkArgs = ConfigurationManager.AppSettings["SFTPClientArgsPPK"];
        private readonly int c_timeout = int.Parse(ConfigurationManager.AppSettings["SFTPTimeout"]);

        // members
        private string m_host;
        private string m_port;
        private string m_user;
        private string m_ppk;
        private string m_pw;

        // output
        public string[] Output {
            get {
                return m_output;
            }
        }
        // member
        private string[] m_output = null;
        private int m_outLength = 0;

        // errors
        public string Error {
            get {
                return m_err;
            }
        }
        // member
        private string m_err = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        public PSFTPProcessWrapper(string p_host, int p_port, string p_user, string p_pw) :
            this(p_host, p_port, p_user, p_pw, string.Empty) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PSFTPProcessWrapper(string p_host, int p_port, string p_user, string p_pw, string p_ppk) {
            // set members
            m_host = p_host;
            m_port = p_port.ToString();
            m_user = p_user;
            m_pw = p_pw;
            m_ppk = p_ppk;
        }

        /// <summary>
        /// Connect to server and run script
        /// </summary>
        /// <param name="p_ScriptFi">script location</param>
        /// <returns>exit code</returns>
        public int RunScript(FileInfo p_ScriptFi) {
            // reset output vars
            m_err = string.Empty;
            m_outLength = 0;
            m_output = null;
            // process exit code
            int exitcode = int.MinValue;
            // scripts args
            string args;
            bool sentKeyCache = false;

            // use ppk template, or normal template
            if (m_ppk.Length > 0) {
                args = c_ppkArgs.Replace("%user%", m_user).Replace("%hostname%", m_host).Replace("%port%", m_port).Replace("%ppk%", m_ppk).Replace("%password%", m_pw).Replace("%script%", p_ScriptFi.FullName);
            }
            else {
                args = c_args.Replace("%user%", m_user).Replace("%hostname%", m_host).Replace("%port%", m_port).Replace("%password%", m_pw).Replace("%script%", p_ScriptFi.FullName);
            }

            // execute
            ProcessStartInfo StartInfo = new ProcessStartInfo(c_psftp, args);
            StartInfo.UseShellExecute = false;
            StartInfo.CreateNoWindow = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            StartInfo.RedirectStandardInput = true;

            // Run Process
            using (Process Proc = new Process()) {

                Proc.StartInfo = StartInfo;

                List<string> SOut = new List<string>();
                StringBuilder SErr = new StringBuilder();

                using (AutoResetEvent OutWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent ErrWaitHandle = new AutoResetEvent(false)) {
                    Proc.OutputDataReceived += (sender, e) => {
                        if (e.Data == null) {
                            OutWaitHandle.Set();
                        }
                        else {
                            SOut.Add(e.Data);
                        }
                    };
                    Proc.ErrorDataReceived += (sender, e) => {
                        if (e.Data == null) {
                            ErrWaitHandle.Set();
                        }
                        else {
                            // try and clear a cache error
                            if (e.Data.Contains("Store key in cache? (y/n)") && !sentKeyCache) {
                                Proc.StandardInput.WriteLine("n");
                                sentKeyCache = true;
                            }
                            if (e.Data.Contains("The server's host key is not cached in the registry") && !sentKeyCache) {
                                Proc.StandardInput.WriteLine("n");
                                sentKeyCache = true;
                            }
                            SErr.AppendLine(e.Data);
                        }
                    };

                    Proc.Start();

                    Proc.BeginOutputReadLine();
                    Proc.BeginErrorReadLine();

                    if (Proc.WaitForExit(c_timeout) &&
                        OutWaitHandle.WaitOne(c_timeout) &&
                        ErrWaitHandle.WaitOne(c_timeout)) {
                        // set exit code                        
                        exitcode = Proc.ExitCode;
                    }
                    else {
                        // add error records
                        SErr.AppendLine("Timed out.");
                        // set exit code
                        exitcode = -999;
                    }
                    // set outputs
                    m_outLength = SOut.Count;
                    m_output = SOut.ToArray();
                    m_err = SErr.ToString();
                }
            }
            // out
            return exitcode;
        }

    }   //  end class PSFTPProcessWrapper
}
