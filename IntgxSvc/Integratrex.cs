using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

// Other
using log4net;


// C2
using C2InfoSys.Schedule;
using C2InfoSys.FileIntegratrex.Lib;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// File Integratrex Service
    /// </summary>
    public class Integratrex : IDisposable {

        // log
        private ILog SvcLog;
        private ILog SysLog;
        private ILog XmlLog;
        private ILog DebugLog;

        // members
        private XIntegrations m_Integrations;
        private List<IntegrationManager> m_Managers;
        private Dictionary<string, IntegrationManager> m_ManagerDict;

        // threading
        private ManualResetEvent m_IntegrationInterruptEvent;

        /// <summary>
        /// Constructor
        /// </summary>
        public Integratrex(ManualResetEvent p_IntegrationInterruptEvent) {
            m_IntegrationInterruptEvent = p_IntegrationInterruptEvent;            
            m_Managers = new List<IntegrationManager>();
            m_ManagerDict = new Dictionary<string, IntegrationManager>();

            // setup trace sources
            DebugLog = LogManager.GetLogger(Global.DebugLogName);
            SvcLog = LogManager.GetLogger(Global.ServiceLogName);                        
        }      

        /// <summary>
        /// Read Integrations
        /// </summary>
        public void ReadIntegrationConfig(string p_configPath, string p_xmlns) {            
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                // method logic
                m_Managers.Clear();
                m_ManagerDict.Clear();
                // read config file
                ConfigReader R = new ConfigReader();
                m_Integrations = R.ReadIntegrationsXml(p_configPath, p_xmlns);
                // initialize integrations
                foreach (XIntegration Integration in m_Integrations.Integration) {                                    
                    // check the integration has a unique name
                    if (m_ManagerDict.ContainsKey(Integration.Desc)) {                      
                        SvcLog.WarnFormat(Global.ErrMessage.ERR1001, Integration.Desc);
                        continue;
                    }
                    // get a manager
                    IntegrationManager Mgr = new IntegrationManager(Integration, m_IntegrationInterruptEvent);    
                    // add to list and dictionary
                    m_ManagerDict.Add(Integration.Desc, Mgr);
                    m_Managers.Add(Mgr);
                    // create the integration scehdule
                    Mgr.CreateSchedule();
                }                                                           
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }         
        }

        /// <summary>
        /// Start Schedules
        /// </summary>
        public void StartAllIntegrations() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                foreach (IntegrationManager Mgr in m_Managers) {
                    Mgr.StartSchedule();
                }
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }         
        }

        /// <summary>
        /// Stop Schedules
        /// </summary>
        public void StopAllIntegrations() {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                DebugLog.DebugFormat(Global.Messages.EnterMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
                foreach (IntegrationManager Mgr in m_Managers) {
                    Mgr.StopSchedule();
                }
            }
            catch (Exception ex) {
                SvcLog.FatalFormat(Global.Messages.Exception, ex.GetType().ToString(), ThisMethod.DeclaringType.Name, ThisMethod.Name, ex.Message);
                throw ex;
            }
            finally {
                DebugLog.DebugFormat(Global.Messages.ExitMethod, ThisMethod.DeclaringType.Name, ThisMethod.Name);
            }  
        }

        /// <summary>
        /// Dispose!
        /// </summary>
        void IDisposable.Dispose() {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {                
            }
            catch (Exception ex) {                
                throw ex;
            }
        }

    }   // Integratrex
}
