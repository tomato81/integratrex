<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
 xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
 xmlns:c2="c2infosys.com/Integratrex/Service.Config.xsd"
 exclude-result-prefixes="c2">


  <xsl:template match="/">


    <html>
      <body>
        <xsl:for-each select="c2:Integrations/c2:Integration">
          <table>
            <tr>
              <th colspan="2">Integration</th>
            </tr>
            <tr>
              <td>
                <xsl:value-of select="@Desc" />
              </td>
              <td>
                <xsl:value-of select="@Enable" />
              </td>
            </tr>


            <xsl:for-each select="c2:Schedule">

              <tr>
                <th colspan="2">Schedule</th>
              </tr>

              <xsl:for-each select="c2:Calendar">
                <tr>
                  <td>Schedule Type</td>
                  <td>Calendar</td>
                </tr>
                <tr>
                  <td>Occurs</td>
                  <td>
                    <xsl:value-of select="@Occurs" />
                  </td>
                </tr>
                <tr>
                  <td>Time of Day</td>
                  <td>
                    <xsl:value-of select="@TimeOfDay" />
                  </td>
                </tr>
                <tr>
                  <td>Day</td>
                  <td>
                    <xsl:value-of select="@Day" />
                  </td>
                </tr>
                <tr>
                  <td>Business Day</td>
                  <td>
                    <xsl:value-of select="@BusinessDay" />
                  </td>
                </tr>
                <tr>
                  <td>Business Calendar</td>
                  <td>
                    <xsl:value-of select="@BusinessCalendar" />
                  </td>
                </tr>
                <tr>
                </tr>
              </xsl:for-each>
              <xsl:for-each select="c2:Continuous">
                <tr>
                  <td>Schedule Type</td>
                  <td>Continuous</td>
                </tr>
                <tr>
                  <td>Interval (seconds)</td>
                  <td>
                    <xsl:value-of select="@Interval" />
                  </td>
                </tr>
                <tr>
                  <td>Daily Response Limit</td>
                  <td>
                    <xsl:value-of select="@DailyResponseLimit" />
                  </td>
                </tr>
              </xsl:for-each>
            </xsl:for-each>


            <xsl:for-each select="c2:Source">
              <tr>
                <th colspan="2">Integration Source</th>
              </tr>
              <xsl:for-each select="c2:SFTP">
                <tr>
                  <td>SFTP</td>
                  <td></td>
                </tr>
                <tr>
                  <td>URI</td>
                  <td>
                    <xsl:value-of select="@URI" />
                  </td>
                </tr>
                <tr>
                  <td>Port</td>
                  <td>
                    <xsl:value-of select="@Port" />
                  </td>
                </tr>
                <tr>
                  <td>Remote Folder</td>
                  <td>
                    <xsl:value-of select="@Folder" />
                  </td>
                </tr>
                <xsl:for-each select="c2:Credentials">
                  <tr>
                    <th>Credentials</th>
                    <th></th>
                  </tr>
                  <tr>
                    <th>Username</th>
                    <th>Password</th>
                  </tr>
                  <tr>
                    <td>
                      <xsl:value-of select="@User" />
                    </td>
                    <td>
                      <xsl:value-of select="@Password" />
                    </td>
                  </tr>
                </xsl:for-each>
              </xsl:for-each>

              <xsl:for-each select="c2:FTP">
                <tr>
                  <td>FTP</td>
                  <td></td>
                </tr>
                <tr>
                  <td>URI</td>
                  <td>
                    <xsl:value-of select="@URI" />
                  </td>
                </tr>
                <tr>
                  <td>Port</td>
                  <td>
                    <xsl:value-of select="@Port" />
                  </td>
                </tr>
                <tr>
                  <td>Remote Folder</td>
                  <td>
                    <xsl:value-of select="@Folder" />
                  </td>
                </tr>
                <xsl:for-each select="c2:Credentials">
                  <tr>
                    <th>Credentials</th>
                    <th></th>
                  </tr>
                  <tr>
                    <th>Username</th>
                    <th>Password</th>
                  </tr>
                  <tr>
                    <td>
                      <xsl:value-of select="@User" />
                    </td>
                    <td>
                      <xsl:value-of select="@Password" />
                    </td>
                  </tr>
                </xsl:for-each>
              </xsl:for-each>

              <xsl:for-each select="c2:Network">
                <tr>
                  <td>Network Folder</td>
                  <td></td>
                </tr>
                <tr>
                  <td>Folder</td>
                  <td>
                    <xsl:value-of select="@Folder" />
                  </td>
                </tr>
              </xsl:for-each>

              <xsl:for-each select="c2:Local">
                <tr>
                  <td>Local Folder</td>
                  <td></td>
                </tr>
                <tr>
                  <td>Folder</td>
                  <td>
                    <xsl:value-of select="@Folder" />
                  </td>
                </tr>
              </xsl:for-each>

              <xsl:for-each select="c2:Web">
                <tr>
                  <td>Web Address</td>
                  <td></td>
                </tr>
                <tr>
                  <td>URI</td>
                  <td>
                    <xsl:value-of select="@URI" />
                  </td>
                </tr>
                <tr>
                  <td>Port</td>
                  <td>
                    <xsl:value-of select="@Port" />
                  </td>
                </tr>
              </xsl:for-each>

            </xsl:for-each>

            <tr>
              <th colspan="2">Matching Patterns</th>
            </tr>
            <xsl:for-each select="c2:Patterns/c2:Pattern">
              <tr>
                <td>
                  <xsl:value-of select="@Desc" />
                </td>
                <td>
                  <xsl:value-of select="@Type" />
                </td>
                <td>
                  <xsl:value-of select="text()" />
                </td>
              </tr>
            </xsl:for-each>

            <tr>
              <th colspan="2">Integration Responses</th>
            </tr>
            <xsl:for-each select="c2:Responses/c2:Response">
              <tr>
                <th>
                  <xsl:value-of select="@Desc" />
                </th>
                <th>
                  <xsl:value-of select="@Enable" />
                </th>
              </tr>

              <xsl:for-each select="c2:Transform">
                <xsl:for-each select="c2:TextFile">
                  <tr>
                    <th colspan="2">TextFile Transform</th>
                    <th>
                    </th>
                  </tr>
                  <tr>
                    <td>Original Encoding</td>
                    <td>
                      <xsl:value-of select="@OriginalEncoding" />
                    </td>
                  </tr>
                  <tr>
                    <td>Target Encoding</td>
                    <td>
                      <xsl:value-of select="@TargetEncoding" />
                    </td>
                  </tr>
                  <tr>
                    <td>Process Line Breaks</td>
                    <td>
                      <xsl:value-of select="@LineBreaks" />
                    </td>
                  </tr>
                </xsl:for-each>
              </xsl:for-each>

              <xsl:for-each select="c2:Target">
                <xsl:for-each select="c2:SFTP">
                  <tr>
                    <td>SFTP</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td>URI</td>
                    <td>
                      <xsl:value-of select="@URI" />
                    </td>
                  </tr>
                  <tr>
                    <td>Port</td>
                    <td>
                      <xsl:value-of select="@Port" />
                    </td>
                  </tr>
                  <tr>
                    <td>Remote Folder</td>
                    <td>
                      <xsl:value-of select="@Folder" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Overwrite?</td>
                    <td>
                      <xsl:value-of select="@Overwrite" />
                    </td>
                  </tr>
                  <xsl:for-each select="c2:Credentials">
                    <tr>
                      <th>Credentials</th>
                      <th></th>
                    </tr>
                    <tr>
                      <th>Username</th>
                      <th>Password</th>
                    </tr>
                    <tr>
                      <td>
                        <xsl:value-of select="@User" />
                      </td>
                      <td>
                        <xsl:value-of select="@Password" />
                      </td>
                    </tr>
                  </xsl:for-each>
                </xsl:for-each>

                <xsl:for-each select="c2:FTP">
                  <tr>
                    <td>FTP</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td>URI</td>
                    <td>
                      <xsl:value-of select="@URI" />
                    </td>
                  </tr>
                  <tr>
                    <td>Port</td>
                    <td>
                      <xsl:value-of select="@Port" />
                    </td>
                  </tr>
                  <tr>
                    <td>Remote Folder</td>
                    <td>
                      <xsl:value-of select="@Folder" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Overwrite?</td>
                    <td>
                      <xsl:value-of select="@Overwrite" />
                    </td>
                  </tr>
                  <xsl:for-each select="c2:Credentials">
                    <tr>
                      <th>Credentials</th>
                      <th></th>
                    </tr>
                    <tr>
                      <th>Username</th>
                      <th>Password</th>
                    </tr>
                    <tr>
                      <td>
                        <xsl:value-of select="@User" />
                      </td>
                      <td>
                        <xsl:value-of select="@Password" />
                      </td>
                    </tr>
                  </xsl:for-each>
                </xsl:for-each>

                <xsl:for-each select="c2:Network">
                  <tr>
                    <td>Network Folder</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td>Folder</td>
                    <td>
                      <xsl:value-of select="@Folder" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Overwrite?</td>
                    <td>
                      <xsl:value-of select="@Overwrite" />
                    </td>
                  </tr>
                </xsl:for-each>

                <xsl:for-each select="c2:Local">
                  <tr>
                    <td>Local Folder</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td>Folder</td>
                    <td>
                      <xsl:value-of select="@Folder" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Overwrite?</td>
                    <td>
                      <xsl:value-of select="@Overwrite" />
                    </td>
                  </tr>
                </xsl:for-each>

                <xsl:for-each select="c2:Program">
                  <tr>
                    <td>Program</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td>Path</td>
                    <td>
                      <xsl:value-of select="@Path" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Args</td>
                    <td>
                      <xsl:value-of select="@Args" />
                    </td>
                  </tr>
                  <xsl:for-each select="c2:Parameters">
                    <tr>
                      <th>Parameters</th>
                      <th></th>
                    </tr>
                    <xsl:for-each select="c2:Parameter">
                      <tr>
                        <td>
                          <xsl:value-of select="@Name" />
                        </td>
                        <td>
                          <xsl:value-of select="text()" />
                        </td>
                      </tr>
                    </xsl:for-each>
                  </xsl:for-each>
                </xsl:for-each>

                <xsl:for-each select="c2:Database">
                  <tr>
                    <td>Database</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td>DBMS</td>
                    <td>
                      <xsl:value-of select="@DBMS" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Server</td>
                    <td>
                      <xsl:value-of select="@Server" />
                    </td>
                  </tr>
                  <tr>
                    <td>Db</td>
                    <td>
                      <xsl:value-of select="@Db" />
                    </td>
                  </tr>
                  <xsl:for-each select="c2:Credentials">
                    <tr>
                      <th>Credentials</th>
                      <th></th>
                    </tr>
                    <tr>
                      <th>Username</th>
                      <th>Password</th>
                    </tr>
                    <tr>
                      <td>
                        <xsl:value-of select="@User" />
                      </td>
                      <td>
                        <xsl:value-of select="@Password" />
                      </td>
                    </tr>
                  </xsl:for-each>
                  <xsl:for-each select="c2:Parameters">
                    <tr>
                      <th>Parameters</th>
                      <th></th>
                    </tr>
                    <xsl:for-each select="c2:Parameter">
                      <tr>
                        <td>
                          <xsl:value-of select="@Name" />
                        </td>
                        <td>
                          <xsl:value-of select="text()" />
                        </td>
                      </tr>
                    </xsl:for-each>
                  </xsl:for-each>
                </xsl:for-each>

                <xsl:for-each select="c2:Email">
                  <tr>
                    <td>Email</td>
                    <td></td>
                  </tr>
                  <tr>
                    <td colspan="3">Receipients</td>
                  </tr>
                  <tr>
                    <td>To</td>
                    <td>CC</td>
                    <td>BCC</td>
                  </tr>
                  <tr>
                    <td>
                      <xsl:value-of select="@To" />
                    </td>
                    <td>
                      <xsl:value-of select="@CC" />
                    </td>
                    <td>
                      <xsl:value-of select="@BCC" />
                    </td>
                  </tr>
                  <tr>
                    <td>Action</td>
                    <td>
                      <xsl:value-of select="@Action" />
                    </td>
                  </tr>
                  <tr>
                    <td>Subject Line</td>
                    <td>
                      <xsl:value-of select="@Subject" />
                    </td>
                  </tr>
                  <tr>
                    <td colspan="3">Email Body</td>
                  </tr>
                  <tr>
                    <td colspan="3">
                      <p>
                        <xsl:value-of select="text()" />
                      </p>
                    </td>
                  </tr>
                </xsl:for-each>
              </xsl:for-each>

            </xsl:for-each>

          </table>
        </xsl:for-each>

      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>
