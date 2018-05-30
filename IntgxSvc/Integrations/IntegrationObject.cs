using System;
using System.Collections.Generic;

// lawg
using log4net;

// C2
using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Integration Error Event Args
    /// </summary>
    public class IntegrationErrorEventArgs : EventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Ex">an Exception</param>
        public IntegrationErrorEventArgs(Exception p_Exception) {
            IsException = true;
            Exception = p_Exception;
            Message = p_Exception.Message;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_errorMessage">error message</param>
        public IntegrationErrorEventArgs(string p_errorMessage) {
            IsException = false;
            Exception = null;                        
            Message = p_errorMessage;
        }

        /// <summary>
        /// Error Message
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Is this error an Exception?
        /// </summary>
        public readonly bool IsException;

        /// <summary>
        /// Exception 
        /// </summary>
        public readonly Exception Exception;
        

    }   // IntegrationErrorEventArgs

    /// <summary>
    /// Integration Log Event Args
    /// </summary>
    public class IntegrationLogEventArgs : EventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationLogEventArgs(string p_message) {
            Message = p_message;
        }

        /// <summary>
        /// Log Message
        /// </summary>
        public readonly string Message;

    }   // IntegrationLogEventArgs

    /// <summary>
    /// Integration Event Args
    /// </summary>
    public class IntegrationEventArgs : EventArgs {

        /// <summary>
        /// Empty!
        /// </summary>
        public static new IntegrationEventArgs Empty {
            get {
                return new IntegrationEventArgs();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationEventArgs() {
        }


    }   // IntegrationEventArgs

    /// <summary>
    /// Use to Identify Dynamic Text Functions
    /// </summary>
    public sealed class DynamicTextAttribute : System.Attribute {
    }   // DynamicTextAttribute

    /// <summary>
    /// An integration event that affects a single file
    /// </summary>
    public class IntegrationFileEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile">the matched file</param>
        public IntegrationFileEventArgs(MatchedFile p_File)
            : base() {
            File = p_File;
        }

        /// <summary>
        /// The Matched File
        /// </summary>        
        public readonly MatchedFile File;
    }

    /// <summary>
    /// An integration event that affects set of files
    /// </summary>
    public class IntegrationFilesEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFiles">the files</param>
        public IntegrationFilesEventArgs(List<MatchedFile> p_Files)
            : base() {
            Files = p_Files;
        }

        /// <summary>
        /// The Matched File
        /// </summary>        
        public readonly List<MatchedFile> Files;

    }

    /// <summary>
    /// Base class for all integration objects
    /// </summary>
    public abstract class IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        protected IntegrationObject() {
            // log refs 
            DebugLog = LogManager.GetLogger(Global.DebugLogName);
        }

        // debug log       
        public ILog DebugLog;

        /// <summary>
        /// Fires when a dynamic text replacement value is required
        /// </summary>
        public event EventHandler<OnValueRequiredEventArgs> OnValueRequired;

        /// <summary>
        /// Fires when an error occurs on the integration object
        /// </summary>
        public event EventHandler<IntegrationErrorEventArgs> OnError;

        /// <summary>
        /// The Integration Object has performed a loggable action
        /// </summary>
        public event EventHandler<IntegrationLogEventArgs> DoLog;

        
        /// <summary>
        /// Raise an Error Event
        /// </summary>
        /// <param name="p_errorFormat"></param>
        /// <param name="p_params"></param>
        protected void ErrorEvent(string p_errorFormat, params object[] p_params) {
            OnError?.Invoke(this, new IntegrationErrorEventArgs(string.Format(p_errorFormat, p_params)));
        }

        /// <summary>
        /// Raise an Error Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="p_args"></param>
        protected void ErrorEvent(string p_errorMessage) {
            OnError?.Invoke(this, new IntegrationErrorEventArgs(p_errorMessage));
        }

        /// <summary>
        /// Raise an Error Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="p_args"></param>
        protected void ErrorEvent(Exception ex) {
            OnError?.Invoke(this, new IntegrationErrorEventArgs(ex));
        }

        /// <summary>
        /// Log 
        /// </summary>
        /// <param name="p_format">string format</param>
        /// <param name="p_params">params</param>
        protected void LogEvent(string p_format, params object[] p_params) {
            // create message
            string message = string.Format(p_format, p_params);
            // raise it 
            DoLog?.Invoke(this, new IntegrationLogEventArgs(message));
        }

        /// <summary>
        /// Log 
        /// </summary>
        /// <param name="p_message">log message</param>
        protected void LogEvent(string p_message) {
            // raise it 
            DoLog?.Invoke(this, new IntegrationLogEventArgs(p_message));
        }

        /// <summary>
        /// Implement in child objects to compile dynamic text elements
        /// </summary>
        protected abstract void CompileDynamicText();

        /// <summary>
        /// Dynamic Text is required
        /// </summary>
        /// <param name="p_EventArgs"></param>
        protected void ValueRequired(object sender, OnValueRequiredEventArgs p_EventArgs) {
            try {
                if (OnValueRequired != null) {
                    OnValueRequired(sender, p_EventArgs);
                    return;
                }
                throw new NullReferenceException("IntegrationObject.OnValueRequired event handler is not attached");
            }
            catch (Exception ex) {
                // set the return val to an error
                p_EventArgs.Result = "{TEXT_ERROR}";
                // raise error event
                ErrorEvent(ex);
            }
        }

    }   // IntegrationObject

}
