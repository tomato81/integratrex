using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

// lawg
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {


    /// <summary>
    /// Integration Source Factory
    /// </summary>
    public class IntegrationSourceFactory
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public IntegrationSourceFactory()
        {
        }

        /// <summary>
        /// Create an Integration Source object
        /// </summary>
        /// <param name="p_XSource">XSource</param>
        /// <returns></returns>
        public IntegrationSource Create(XSource p_XSource)
        {
            // interface to a source location
            ISourceLocation SourceLocation;
            // what is the source location??
            try {
                Type T = p_XSource.Item.GetType();
                switch (T.Name) {
                    case "XLocalSrc": {
                            SourceLocation = new LocalSrc(p_XSource.Desc, p_XSource.Item);
                            break;
                        }
                    case "XNetworkSrc": {
                            SourceLocation = new NetworkSrc(p_XSource.Desc, p_XSource.Item);                            
                            break;
                        }
                    case "XWebSrc": {
                            throw new NotImplementedException();
                        }
                    case "XFTPSrc": {
                            throw new NotImplementedException();
                        }
                    case "XSFTPSrc": {
                            throw new NotImplementedException();
                        }
                    default: {
                            throw new InvalidOperationException();
                        }
                }
                // return a new integration source object with the correct source location
                return new IntegrationSource(p_XSource, SourceLocation);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }   // IntegrationSourceFactory    

    /// <summary>
    /// Integration Source Base
    /// </summary>
    public class IntegrationSource : IntegrationObject {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Source"></param>
        /// <param name="p_SourceLocation"></param>
        public IntegrationSource(XSource p_Source, ISourceLocation p_SourceLocation) :
            base(p_Source) {
            m_Source = p_Source;
            m_SourceLocation = p_SourceLocation;
        }
        
        /// <summary>
        /// Source Location
        /// </summary>
        public ISourceLocation Location {
            get {
                return m_SourceLocation;
            }
        }
        // member
        private ISourceLocation m_SourceLocation;

        // XSource
        private XSource m_Source;        

        /// <summary>
        /// Constructor
        /// </summary>
        protected IntegrationSource(XSource p_Source) 
            : base(p_Source) {
            // things
        }                   

    }   // IntegrationSource
    
}
