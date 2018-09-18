using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

// lib
using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// Integration Response Factory
    /// </summary>
    public class IntegrationResponseFactory {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationResponseFactory() {
        }

        /// <summary>
        /// Create an Integration Response object
        /// </summary>
        /// <param name="p_XResponse">XResponse</param>
        /// <returns></returns>
        public IntegrationResponse Create(XResponse p_XResponse) {

            // interface to a response 
            IntegrationResponse Response;

            // what is the response location??
            try {
                Type T = p_XResponse.Target.Item.GetType();
                switch (T.Name) {
                    case "XLocalTgt": {
                        Response = new LocalResponse(p_XResponse, (XLocalTgt)p_XResponse.Target.Item);
                        break;
                    }                    
                    default: {
                        throw new InvalidOperationException();
                    }
                }
                // return a new integration response object with the correct response location
                return Response;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }   // IntegrationSourceFactory    

    /// <summary>
    /// Integration Response
    /// </summary>
    public abstract class IntegrationResponse : IntegrationObject, IResponse {

        // XObject
        private XResponse m_Response;

        private readonly ResponseTransform m_Transform;

        public ResponseTransform Transformation { get => m_Transform; }

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationResponse(XResponse p_Response) 
            : base() {
            m_Response = p_Response;
            m_Transform = new ResponseTransform(p_Response.Rename);
        }

        /// <summary>
        /// Response Description
        /// </summary>
        public string Description {
            get {
                return string.IsNullOrWhiteSpace(m_Response.Desc) ? "(none)" : m_Response.Desc;
            }
        }      

        /// <summary>
        /// String representaion of the action to take at the target
        /// </summary>
        public abstract string ActionDesc { get; }

        public event EventHandler<LocationCreatedEventArgs> LocationCreated;
        public event EventHandler<ActionStartedEventArgs> ActionStarted;
        public event EventHandler<FileExistsEventArgs> FileExists;
        public event EventHandler<FileActionedEventArgs> FileActioned;
        public event EventHandler<ActionCompleteEventArgs> ActionComplete;
        public event EventHandler<OverrwriteEventArgs> FileOverrwrite;
        public event EventHandler<IntegrationEventArgs> PingTarget;
        public event EventHandler<IntegrationEventArgs> ResponseSupressed;
        public event EventHandler<TransformResponseEventArgs> TransformResponse;        


        protected void LocationCreatedEvent(string p_location) {
            LocationCreated?.Invoke(this, new LocationCreatedEventArgs(p_location));
        }
        protected void FileExistsEvent(MatchedFile p_Mf, string p_target) {
            FileExists?.Invoke(this, new FileExistsEventArgs(p_Mf, p_target));
        }
        protected void ActionStartedEvent(List<MatchedFile> p_Mf) {
            ActionStarted?.Invoke(this, new ActionStartedEventArgs(p_Mf));
        }
        protected void FileActionedEvent(MatchedFile p_Mf, string p_action, string p_target) {
            FileActioned?.Invoke(this, new FileActionedEventArgs(p_Mf, p_action, p_target));
        }
        protected void ActionCompleteEvent(List<MatchedFile> p_Mf) {
            ActionComplete?.Invoke(this, new ActionCompleteEventArgs(p_Mf));
        }
        protected void FileOverrwriteEvent(string p_path) {
            FileOverrwrite?.Invoke(this, new OverrwriteEventArgs(p_path));
        }
        protected void ResponseSupressedEvent() {
            ResponseSupressed?.Invoke(this, IntegrationEventArgs.Empty);
        }
        protected void PingTargetEvent() {
            PingTarget?.Invoke(this, IntegrationEventArgs.Empty);
        }

        public abstract void Action(List<MatchedFile> p_Mf);

        public void Transform(MatchedFile p_Mf) {
            TransformResponse?.Invoke(this, new TransformResponseEventArgs(p_Mf));
        }

    }   // IntegrationResponse

    /// <summary>
    /// Rename a response file
    /// </summary>
    public class ResponseTransform : IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_XRenameResponse">XRenameResponse</param>
        public ResponseTransform(XRenameResponse p_XRenameResponse) {
            m_XRenameResponse = p_XRenameResponse;
            CompileDynamicText();
        }

        // X Object
        private readonly XRenameResponse m_XRenameResponse;

        /// <summary>
        /// Rename?
        /// </summary>
        public bool IsRename => m_XRenameResponse.Enable == XRenameResponseEnable.Y;        

        /// <summary>
        /// The X object
        /// </summary>
        private XRenameResponse X { get => m_XRenameResponse; }

        /// <summary>
        /// Create and Compile Dynamic Text
        /// </summary>
        protected override void CompileDynamicText() {
            m_Rename = new DynamicTextParser(X.Value is null ? string.Empty : X.Value);
            m_Rename.OnValueRequired += ValueRequired;
            m_Rename.Compile();
        }

        /// <summary>
        /// Get the file name for the response
        /// </summary>
        /// <returns></returns>
        [DynamicText]
        public string Rename() {
            return m_Rename.Run();
        }
        private DynamicTextParser m_Rename;

    }   // RenameResponse

    /// <summary>
    /// Transform Response Event Args
    /// </summary>
    public class TransformResponseEventArgs : IntegrationEventArgs {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_MatchedFile">the matched file</param>
        public TransformResponseEventArgs(MatchedFile p_MatchedFile)
            : base() {
            MatchedFile = p_MatchedFile;
        }

        /// <summary>
        /// The Matched File
        /// </summary>        
        public readonly MatchedFile MatchedFile;

        /// <summary>
        /// Flag if any transforms have been applied
        /// </summary>
        public bool TransformApplied = false;

    }   // TransformSourceEventArgs    

    /// <summary>
    /// On Overrwrite Event Args
    /// </summary>
    public class OverrwriteEventArgs : IntegrationEventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_path">the path being overrwritten</param>
        public OverrwriteEventArgs(string p_path)
            : base() {
            Path = p_path;
        }
        /// <summary>
        /// The Path being overrwritten
        /// </summary>
        public readonly string Path;
    }   // OverrwriteEventArgs

    /// <summary>
    /// On Location Created Event Args
    /// </summary>
    public class LocationCreatedEventArgs : IntegrationEventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Location">the location</param>
        public LocationCreatedEventArgs(string p_location)
            : base() {
            Location = p_location;
        }
        /// <summary>
        /// The Matched File
        /// </summary>
        public readonly string Location;
    }   // LocationCreatedEventArgs
    
    /// <summary>
    /// On File Exists Event Args
    /// </summary>
    public class FileExistsEventArgs : IntegrationEventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Mf">the file</param>
        /// <param name="p_target">the target location</param>
        public FileExistsEventArgs(MatchedFile p_Mf, string p_target)
            : base() {
            MatchedFile = p_Mf;
            Target = p_target;
        }
        /// <summary>
        /// The file
        /// </summary>
        public readonly MatchedFile MatchedFile;
        /// <summary>
        /// The target location
        /// </summary>
        public readonly string Target;
    }   // FileExistsEventArgs

    /// <summary>
    /// On File Actioned Event Args
    /// </summary>
    public class FileActionedEventArgs : IntegrationEventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Mf">the file</param>        
        public FileActionedEventArgs(MatchedFile p_Mf, string p_action, string p_target)
            : base() {
            MatchedFile = p_Mf;
            Action = p_action;
            Target = p_target;
        }
        /// <summary>
        /// The file
        /// </summary>
        public readonly MatchedFile MatchedFile;

        public readonly string Action;

        public readonly string Target;

    }   // FileActionedEventArgs

    /// <summary>
    /// On Action Started Event Args
    /// </summary>
    public class ActionStartedEventArgs : IntegrationEventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Mf">the file</param>        
        public ActionStartedEventArgs(List<MatchedFile> p_Mf)
            : base() {
            MatchedFiles = p_Mf;
        }
        /// <summary>
        /// The file
        /// </summary>
        public readonly List<MatchedFile> MatchedFiles;
    }   // ActionStartedEventArgs

    /// <summary>
    /// On Action Complete Event Args
    /// </summary>
    public class ActionCompleteEventArgs : IntegrationEventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Mf">the file</param>        
        public ActionCompleteEventArgs(List<MatchedFile> p_Mf)
            : base() {
            MatchedFiles = p_Mf;
        }
        /// <summary>
        /// The file
        /// </summary>
        public readonly List<MatchedFile> MatchedFiles;

    }   // ActionCompleteEventArgs

}
