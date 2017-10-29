using System;
using System.IO;
using System.Data;
using System.Configuration;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace C2InfoSys.FileIntegratrex.Svc {

    /// <summary>
    /// File transfer protocol gateway
    /// </summary>
    public class FtpGateway {

        // network credentials
        private NetworkCredential m_Credentials;

        // ftp url
        public string Url {
            get {
                return m_url;
            }
            set {
                m_url = value;
            }
        }
        // member
        private string m_url;

        // ftp user name
        public string User {
            get {
                return m_user;
            }
            set {
                m_user = value;
                CreateCredentials();
            }
        }
        // member
        private string m_user;

        // ftp password
        public string Password {
            get {
                return m_password;
            }
            set {
                m_password = value;
                CreateCredentials();
            }
        }
        // member
        private string m_password;

        // ftp buffer length
        public int BufferLength {
            get {
                return m_bufferLength;
            }
        }
        // set the buffer length
        public void SetBufferLength(int bufferLength) {
            // 262144 will be the minimum allowed length            
            m_bufferLength = bufferLength > 262144 ? bufferLength : 262144;
        }
        // member
        private int m_bufferLength = 262144;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="url"></param>
        public FtpGateway(string url, string user, string password) {
            // default buffer length
            m_bufferLength = 262144;
            // set locals
            m_url = url;
            m_user = user;
            m_password = password;
            // create credentials
            CreateCredentials();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="url"></param>
        public FtpGateway()
            : this(string.Empty, string.Empty, string.Empty) {
        }

        /// <summary>
        /// Create network credentials object with current username and password
        /// </summary>
        private void CreateCredentials() {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {

                // method code
                m_Credentials = new NetworkCredential(m_user, m_password);
            }
            catch (Exception ex) {

                throw ex;
            }
            finally {
    
            }
        }

        /// <summary>
        /// Upload text as a file to ftp server
        /// </summary>
        public void UploadFile(string remoteFilename, string fileContents) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {

                // method code
                byte[] buffer = new byte[fileContents.Length];
                // fill buffer
                Encoding.ASCII.GetBytes(fileContents, 0, fileContents.Length, buffer, 0);
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + remoteFilename);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.UploadFile;
                // get upload stream
                Stream Upload = Ftp.GetRequestStream();
                // upload file
                Upload.Write(buffer, 0, buffer.Length);
                Upload.Close();
            }
            catch (IOException ex) {
                throw ex;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Upload text as a file to ftp server
        /// </summary>
        /// <param name="remoteFilename">remote filename</param>
        /// <param name="localFile">local file object</param>
        public void UploadFile(string remoteFilename, FileInfo localFile) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // the file to upload should exist
                if (!localFile.Exists) {
                    throw new FileNotFoundException();
                }
                // read it
                FileStream Fin = localFile.OpenRead();
                // create buffer
                byte[] buffer = new byte[m_bufferLength];
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + remoteFilename);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.UploadFile;
                // get upload stream
                Stream Upload = Ftp.GetRequestStream();
                // read 
                while (Fin.Position < Fin.Length) {
                    // read some
                    int bytesRead = Fin.Read(buffer, 0, buffer.Length);
                    // write some
                    Upload.Write(buffer, 0, bytesRead);
                }
                // shut 'er down
                Fin.Close();
                Upload.Close();
            }
            catch (IOException ex) {
                throw ex;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Delete a remote file
        /// </summary>
        /// <param name="filename">filename to delete</param>
        /// <returns>server reply</returns>
        public string DeleteRemoteFile(string remoteFilename) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + remoteFilename);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.DeleteFile;
                // get response             
                FtpWebResponse Response = (FtpWebResponse)Ftp.GetResponse();
                // status description 
                string status = Response.StatusDescription.Replace("\r\n", string.Empty);
                // check code
                if (Response.StatusCode != FtpStatusCode.FileActionOK) {
                    throw new WebException(status);
                }
                // return status
                return status;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Download a remote file
        /// </summary>
        /// <param name="filename">filename to download</param>
        /// <returns>file contents</returns>
        public string DownloadRemoteFile(string remoteFilename) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // download byts
                byte[] fileContents = DownloadRemoteFileBytes(remoteFilename);
                // decode into string
                return Encoding.ASCII.GetString(fileContents);
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Download a remote file and save to the local disk
        /// </summary>
        /// <param name="remoteFileName">filename to download</param>
        /// <param name="localFile">local file object</param>
        /// <returns>bytes downloaded</returns>
        /// <remarks>local file will be replaced if it exists</remarks>
        public int DownloadRemoteFile(string p_remoteFile, FileInfo p_LocalFile) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + p_remoteFile);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.DownloadFile;
                // get response             
                FtpWebResponse Response = (FtpWebResponse)Ftp.GetResponse();
                // status description 
                string status = Response.StatusDescription.Replace("\r\n", string.Empty);
                // check response code
                if (Response.StatusCode != FtpStatusCode.OpeningData) {
                    throw new WebException(status);
                }
                // access download stream
                Stream Download = Response.GetResponseStream();
                // create read buffer
                byte[] buffer = new byte[m_bufferLength];
                int bufferPos = 0;
                int totalBytes = 0;
                // delete existing file
                if (p_LocalFile.Exists) {
                    p_LocalFile.Delete();
                }
                // so far so good, open the local file     
                FileStream Fout = new FileStream(p_LocalFile.FullName, FileMode.CreateNew);
                // read directory listing from stream
                while (Download.CanRead) {
                    buffer[bufferPos++] = (byte)Download.ReadByte();
                    // check buffer
                    if (bufferPos == m_bufferLength) {
                        // flush the buffer to file                      
                        Fout.Write(buffer, 0, bufferPos);
                        totalBytes = totalBytes + bufferPos;
                        // reset buffer
                        bufferPos = 0;
                    }
                }
                // flush the remaining buffer if needed
                if (bufferPos > 0) {
                    // flush the buffer to file                      
                    Fout.Write(buffer, 0, bufferPos);
                    totalBytes = totalBytes + bufferPos;
                    // reset buffer
                    bufferPos = 0;
                }
                // done and done
                Download.Close();
                Fout.Close();
                // return
                return totalBytes;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Download a remote file
        /// </summary>
        /// <param name="filename">filename to download</param>
        /// <returns>file contents</returns>
        public byte[] DownloadRemoteFileBytes(string remoteFilename) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + remoteFilename);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.DownloadFile;
                // get response             
                FtpWebResponse Response = (FtpWebResponse)Ftp.GetResponse();
                // status description 
                string status = Response.StatusDescription.Replace("\r\n", string.Empty);
                // check response code
                if (Response.StatusCode != FtpStatusCode.OpeningData) {
                    throw new WebException(status);
                }
                // access download stream
                Stream Download = Response.GetResponseStream();
                // create read buffer
                byte[] buffer = new byte[m_bufferLength];
                int bufferPos = 0;
                // read directory listing from stream
                while (Download.CanRead) {
                    buffer[bufferPos++] = (byte)Download.ReadByte();
                }
                // close download stream
                Download.Close();
                // exact array for file contents
                byte[] fileContents = new byte[bufferPos];
                // copy buffer contents
                Array.Copy(buffer, fileContents, bufferPos);
                // out
                return fileContents;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Wait for a remote file to exist on the server
        /// </summary>
        /// <param name="filename">filename to wait on</param>
        public void WaitForRemoteFile(string remoteFilename, int waitInterval, int timeout) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // file exists flag
                bool fileExists = false;
                // how long have we been waiting
                int waiting = 0;
                // loop
                while (!fileExists) {
                    if (IsFileOnServer(remoteFilename)) {
                        fileExists = true;
                    }
                    else {
                        // chill
                        Thread.Sleep(waitInterval);
                        // increment wait time
                        waiting += waitInterval;
                        // how long have we been waiting now?
                        if (waiting > timeout) {
                            // too long
                            throw new TimeoutException();
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// List Remote Files
        /// </summary>
        /// <param name="remoteFolder">ftp remote folder</param>
        public string ListRemoteFiles(string remoteFolder) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + remoteFolder);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.ListDirectory;
                // get response             
                FtpWebResponse Response = (FtpWebResponse)Ftp.GetResponse();
                // status description 
                string status = Response.StatusDescription.Replace("\r\n", string.Empty);
                // check response code               
                if (Response.StatusCode != FtpStatusCode.OpeningData) {
                    throw new WebException(status);
                }
                // get download stream
                Stream Download = Response.GetResponseStream();
                // create read buffer
                byte[] buffer = new byte[m_bufferLength];
                int bufferPos = 0;
                // read directory listing from stream
                while (Download.CanRead) {
                    buffer[bufferPos++] = (byte)Download.ReadByte();
                }
                // decode buffer
                string directoryList = Encoding.ASCII.GetString(buffer, 0, bufferPos);
                // out
                return directoryList;
            }
            catch (WebException ex) {
                // check for 550 - file not found
                if (ex.Message.Contains("550")) {
                    return string.Empty;
                }
                // log ex
                throw ex;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

        /// <summary>
        /// Check the ftp server for the existance of a file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool IsFileOnServer(string remoteFilename) {
            // this method info
            MethodBase ThisMethod = MethodBase.GetCurrentMethod();
            try {
                // logging                
                // create request      
                FtpWebRequest Ftp = (FtpWebRequest)FtpWebRequest.Create(m_url + "/" + remoteFilename);
                Ftp.Credentials = m_Credentials;
                Ftp.KeepAlive = false;
                // set method
                Ftp.Method = WebRequestMethods.Ftp.ListDirectory;
                // get response             
                FtpWebResponse Response = (FtpWebResponse)Ftp.GetResponse();
                // status description 
                string status = Response.StatusDescription.Replace("\r\n", string.Empty);
                // check response code               
                if (Response.StatusCode != FtpStatusCode.OpeningData) {
                    throw new WebException(status);
                }
                // get download stream
                Stream Download = Response.GetResponseStream();
                // create read buffer
                byte[] buffer = new byte[m_bufferLength];
                int bufferPos = 0;
                // read directory listing from stream
                while (Download.CanRead) {
                    buffer[bufferPos++] = (byte)Download.ReadByte();
                }
                // decode buffer
                string directoryList = Encoding.ASCII.GetString(buffer, 0, bufferPos);
                // set to lower case
                directoryList = directoryList.ToLower();
                // check server message                      
                if (!directoryList.Contains("no such file or directory")) {
                    // it exists
                    return true;
                }
                // it does not exists
                return false;
            }
            catch (WebException ex) {
                // check for 550 - file not found
                if (ex.Message.Contains("550")) {
                    return false;
                }
                // log ex
                throw ex;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
            }
        }

    }   // end of class
}
