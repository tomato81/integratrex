using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Xml.Serialization;


namespace C2InfoSys.FileIntegratrex.Lib {

    /// <summary>
    /// Use to read integratrex config xml
    /// </summary>
    public class ConfigReader {


        /// <summary>
        /// Constructor
        /// </summary>
        public ConfigReader() {

        }

        /// <summary>
        /// Read Integratrex XML configuration file
        /// </summary>
        /// <param name="p_IntegrationsXmlPath">file path</param>
        /// <param name="p_xmlNamespace">XML namespace</param>
        /// <returns>XIntegrations object</returns>
        public XIntegrations ReadIntegrationsXml(string p_IntegrationsXmlPath, string p_xmlNamespace) {
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                return ReadIntegrationsXml(new FileInfo(p_IntegrationsXmlPath), p_xmlNamespace);
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                // empty
            } 
        }

        /// <summary>
        /// Read Integratrex XML configuration file
        /// </summary>
        /// <param name="p_ConfigXmlFi">file info</param>
        /// <returns>XIntegrations object</returns>
        public XIntegrations ReadIntegrationsXml(FileInfo p_IntegrationsXmlFi, string p_xmlNamespace) {


            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                


                // read configuration
                StreamReader Fin = new StreamReader(p_IntegrationsXmlFi.FullName);
                XmlSerializer Xin = new XmlSerializer(typeof(XIntegrations), p_xmlNamespace);

                XIntegrations Integrations = new XIntegrations();
                Integrations = (XIntegrations)Xin.Deserialize(Fin);

                // log em
                foreach (XIntegration I in Integrations.Integration) {

   

                    
                }

                return Integrations;

            }
            catch (Exception ex) {
                
                throw ex;
            }
            finally {
                // empty
            } 

            
        }




    }   // ConfigReader
}
