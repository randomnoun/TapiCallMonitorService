using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using JulMar.Atapi;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Win32;

using System.Runtime.CompilerServices;

namespace TapiCallMonitorService
{
    public partial class TapiCallMonitorService : ServiceBase
    {
        TapiManager tapiManager = null;
        Boolean paused = false;
        Boolean registryOK = false;
        // this needs to be sourced from the registry, really
        string logFilename = null; // "C:\\data\\production\\Administration\\data\\TapiCallMonitorService.log";
        string connectionString = null; // "Server=SERVER;Database=pbx-prd;User Id=randomnoun_pbx_prd;Password=randomnoun_pbx_prd;";
        string versionString = "v0.3"; // goes into txtCallerName column on server start/stop
        StreamWriter logWriter; 

        private void readRegistryKeys() {
            const string machineRoot = "HKEY_LOCAL_MACHINE";
            const string subkey = "SOFTWARE\\Randomnoun\\TapiCallMonitorService";
            const string keyName = machineRoot + "\\" + subkey;
            logFilename = (string)Registry.GetValue(keyName, "LogFilename", null);
            connectionString = (string)Registry.GetValue(keyName, "ConnectionString", null);

        }

        public TapiCallMonitorService()
        {
            readRegistryKeys();

            InitializeComponent();
            paused = false;
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("TapiCallMonitor"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "TapiCallMonitor", "TapiCallMonitorLog");
            }
            eventLog1.Source = "TapiCallMonitor";
            eventLog1.Log = "TapiCallMonitorLog";
        }

        protected override void OnStart(string[] args)
        {
            if (logFilename==null) {
                // could just disable writing to log if value is missing
                eventLog1.WriteEntry("Missing registry value 'LogFilename' in key 'HKEY_LOCAL_MACHINE\\SOFTWARE\\Randomnoun\\TapiCallMonitorService'", EventLogEntryType.Error);
                paused = true;
                ExitCode = 1;  
                throw new Exception("Missing registry value 'LogFilename' in key 'HKEY_LOCAL_MACHINE\\SOFTWARE\\Randomnoun\\TapiCallMonitorService'");
            }

            if (connectionString==null) {
                // could just disable writing to database if value is missing
                eventLog1.WriteEntry("Missing registry value 'ConnectionString' in key 'HKEY_LOCAL_MACHINE\\SOFTWARE\\Randomnoun\\TapiCallMonitorService'", EventLogEntryType.Error);
                paused = true;
                ExitCode = 1;  
                throw new Exception("Missing registry value 'ConnectionString' in key 'HKEY_LOCAL_MACHINE\\SOFTWARE\\Randomnoun\\TapiCallMonitorService'");
            }


            // we're going to keep this thing open whilst the service is running
            logWriter = new StreamWriter(logFilename, true); // true = append

            if (tapiManager == null)
            {
                tapiManager = new TapiManager("TapiCallMonitorService");
            }
            if (tapiManager.Initialize() == false)
            {
                LogWarn("No Tapi devices found. TapiCallMonitorService paused on startup.");
                paused = true;
                return;
            }

            long countMonitorOK = 0;
            long countMonitorFail = 0;
            tapiManager.LineAdded += OnLineAdded;
            tapiManager.LineChanged += OnLineChanged;
            tapiManager.LineRemoved += OnLineRemoved;
            tapiManager.PhoneAdded += OnPhoneAdded;
            tapiManager.PhoneRemoved += OnPhoneRemoved;
            
            foreach (TapiLine line in tapiManager.Lines)
            {
                try 
                {
                    line.NewCall += OnNewCall;
                    line.CallStateChanged += OnCallStateChanged;
                    line.CallInfoChanged += OnCallInfoChanged;
                    line.Monitor();
                    countMonitorOK++;
                    LogInfoFile("TapiCallMonitorService monitoring " + line.Id + ":" + line.Name );
                }
                catch (TapiException ex)
                {
                    countMonitorFail++;
                    // this happens a fair bit, so don't dump it into the event log
                    LogErrorFile("TapiCallMonitorService exception on startup monitoring line " + line.Id + ":" + line.Name + " message=" + ex.Message.Trim());
                }
            }
            LogInfo("TapiCallMonitorService started (countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + "). Connection string is " + connectionString);
            addPbxLogRecord("OnStart", -1, "", "", -1, "", -1, -1, -1, "", -1, "SERVICE STARTED", "(" + versionString + ", countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + ")", "", "", "", "", 0, 0, 0);
            if (paused) {
                LogWarn("TapiCallMonitorService paused on startup");
            } else {
                LogInfo("TapiCallMonitorService startup record saved");
                LogInfoFile(":COLUMNS:dtmTime:lngTimeMsec:txtEventName:lngTapiLineId:txtTapiLineName:txtTapiLineDeviceSpecificExtensionID:" +
                      "lngPhoneId:txtPhoneName:" +
                      "lngCallId:lngCallRelatedId:lngCallTrunkId:txtDeviceSpecificData:" +
                      "lngCallState:txtCallerId:txtCallerName:txtCalledId:txtCalledName:txtConnectedId:txtConnectedName:" +
                      "lngCallOrigin:lngCallReason:lngChangeType");

            }


        }

        protected override void OnStop()
        {
             tapiManager.Shutdown();
             tapiManager = null;
             addPbxLogRecord("OnStop", -1, "", "", -1, "", -1, -1, -1, "", -2, "SERVICE STOPPED", "(" + versionString + ")", "", "", "", "", 0, 0, 0);
             LogInfo("TapiCallMonitorService stopped");

             logWriter.Close();
        }

        protected override void OnPause()
        {
            paused = true;
            addPbxLogRecord("OnPause", -1, "", "", -1, "", -1, -1, -1, "", -3, "SERVICE PAUSED", "(" + versionString + ")", "", "", "", "", 0, 0, 0);
            LogInfo("TapiCallMonitorService paused");
        }

        protected override void OnContinue()
        {
            paused = false;
            addPbxLogRecord("OnContinue", -1, "", "", -1, "", -1, -1, -1, "", -4, "SERVICE CONTINUED", "(" + versionString + ")", "", "", "", "", 0, 0, 0);
            LogInfo("TapiCallMonitorService continued");
        }

        // addPbxLogRecord("NewCall", line.Name, line.DeviceSpecificExtensionID, (long)call.CallState, call.CallerId, call.CallerName, call.CalledId, call.CalledName, call.CallOrigin, call.CallReason);

        // this needs to be synchronized to prevent file open errors

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void addPbxLogRecord(string txtEventName, long lngTapiLineId, string txtTapiLineName, string txtTapiLineDeviceSpecificExtensionID, 
            long lngPhoneId, string txtPhoneName, long lngCallId, long lngCallRelatedId, long lngCallTrunkId, string txtDeviceSpecificData, 
             long lngCallState, string txtCallerId, string txtCallerName, string txtCalledId, string txtCalledName, string txtConnectedId, string txtConnectedName,
            long lngCallOrigin, long lngCallReason, long lngChangeType)
        {
            if (!paused)
            {
                try
                {
                    // 2013 is Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\myFolder\myAccessFile.accdb;Persist Security Info=False;

                    // using (var conn = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\BC207\test.accdb"))


                    /* SqlConnections are SQLServer only, according to the 5 minutes research and three responses at
                     * http://stackoverflow.com/questions/7764707/sql-connection-string-for-microsoft-access-2010-accdb
                     * */
                    // using (SqlConnection connection = new SqlConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=C:\\Administration\\Amanda Sun\\tblPbxLog.mdb;"))
                    /*
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                    builder.UserID = "randomnoun_pbx_prd";
                    builder.Password = "randomnoun_pbx_prd";
                    builder["Server"] = "192.168.0.1000";
                    builder.InitialCatalog = "pbx-prd";
                    //builder["Connect Timeout"] = 1000; // ?
                    // builder["Trusted_Connection"] = true;

                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString)) // "Data Source=MSSQL1;Initial Catalog=pbx-prd;Integrated Security=true;"
                     */

                    // LogInfoFile(":PBX:" + lngCallState + ":" + txtCallerId + ":" + txtCallerName + ":" + txtCalledId + ":" + txtCalledName);
                    // let's see what these things are first before we start recording them into the database
                    
                    DateTime now = DateTime.Now;    
                    LogInfoFile(":PBX:" + now + ":" + now.Millisecond + ":" + txtEventName + ":" + lngTapiLineId + ":" + txtTapiLineName + ":" + txtTapiLineDeviceSpecificExtensionID + ":" +
                      lngPhoneId + ":" + txtPhoneName + ":" +
                      lngCallId + ":" + lngCallRelatedId + ":" + lngCallTrunkId + ":" + txtDeviceSpecificData + ":" + 
                      lngCallState + ":" + txtCallerId + ":" + txtCallerName + ":" + txtCalledId + ":" + txtCalledName + ":" + txtConnectedId + ":" + txtConnectedName + ":" +
                      lngCallOrigin + ":" + lngCallReason + ":" + lngChangeType );

                    using (SqlConnection connection = new SqlConnection(connectionString)) 
                    {
                        
                        // Console.WriteLine("Now is " + now);
                        SqlCommand cmd = new SqlCommand("INSERT INTO tblPbxLog (dtmTime, lngTimeMsec, txtEventName, lngTapiLineId, txtTapiLineName, txtTapiLineDeviceSpecificExtensionID, " +
                            " lngPhoneId, txtPhoneName, " +
                            " lngCallId, lngCallRelatedId, lngCallTrunkId, txtDeviceSpecificData, " +
                            " lngCallState, txtCallerId, txtCallerName, txtCalledId, txtCalledName, txtConnectedId, txtConnectedName, " +
                            " lngCallOrigin, lngCallReason, lngChangeType) " +
                            " VALUES (@dtmTime, @lngTimeMsec, @txtEventName, @lngTapiLineId, @txtTapiLineName, @txtTapiLineDeviceSpecificExtensionID, " +
                            "  @lngPhoneId, @txtPhoneName, " +
                            "  @lngCallId, @lngCallRelatedId, @lngCallTrunkId, @txtDeviceSpecificData, " +
                            "  @lngCallState, @txtCallerId, @txtCallerName, @txtCalledId, @txtCalledName, @txtConnectedId, @txtConnectedName," + 
                            "  @lngCallOrigin, @lngCallReason, @lngChangeType)");
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = connection;
            
                        cmd.Parameters.AddWithValue("@dtmTime", now);
                        cmd.Parameters.AddWithValue("@lngTimeMsec", now.Millisecond);
                        cmd.Parameters.AddWithValue("@txtEventName", txtEventName);
                        cmd.Parameters.AddWithValue("@lngTapiLineId", lngTapiLineId);
                        cmd.Parameters.AddWithValue("@txtTapiLineName", txtTapiLineName);
                        cmd.Parameters.AddWithValue("@txtTapiLineDeviceSpecificExtensionID", txtTapiLineDeviceSpecificExtensionID);
                        cmd.Parameters.AddWithValue("@lngPhoneId", lngPhoneId);
                        cmd.Parameters.AddWithValue("@txtPhoneName", txtPhoneName);
                        cmd.Parameters.AddWithValue("@lngCallId", lngCallId);
                        cmd.Parameters.AddWithValue("@lngCallRelatedId", lngCallRelatedId);
                        cmd.Parameters.AddWithValue("@lngCallTrunkId", lngCallTrunkId);
                        cmd.Parameters.AddWithValue("@txtDeviceSpecificData", txtDeviceSpecificData);
                        cmd.Parameters.AddWithValue("@lngCallState", lngCallState);
                        cmd.Parameters.AddWithValue("@txtCallerId", txtCallerId);
                        cmd.Parameters.AddWithValue("@txtCallerName", txtCallerName);
                        cmd.Parameters.AddWithValue("@txtCalledId", txtCalledId);
                        cmd.Parameters.AddWithValue("@txtCalledName", txtCalledName);
                        cmd.Parameters.AddWithValue("@txtConnectedId", txtConnectedId);
                        cmd.Parameters.AddWithValue("@txtConnectedName", txtConnectedName);
                        cmd.Parameters.AddWithValue("@lngCallOrigin", lngCallOrigin);
                        cmd.Parameters.AddWithValue("@lngCallReason", lngCallReason);
                        cmd.Parameters.AddWithValue("@lngChangeType", lngChangeType);

                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                catch (Exception e)
                {
                    LogError("Paused service; exception saving PBX record. Message='" + e.Message + "'\nStacktrace='" + e.StackTrace + "'");
                    paused = true;
                    // possibly send some kind of alert.
                }
            }
        }


        private void OnNewCall(object sender, NewCallEventArgs e)
        {

            try {
                TapiLine line = (TapiLine)sender;
                TapiCall call = e.Call;
                TapiPhone phone = line.GetAssociatedPhone();
                
                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // addPbxLogRecord((long) call.CallState, call.CallerId, call.CallerName, call.CalledId, call.CalledName);
                addPbxLogRecord("NewCall", line.Id, line.Name, line.DeviceSpecificExtensionID, phone == null ? -1 : phone.Id, phone == null ? "" : phone.Name, call.Id, call.RelatedId, call.TrunkId, deviceSpecificData, (long)call.CallState, call.CallerId, call.CallerName, call.CalledId, call.CalledName, call.ConnectedId, call.ConnectedName, (long)call.CallOrigin, (long)call.CallReason, 0);
            } catch (Exception ex) {
                LogError("Paused service; exception in OnNewCall handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }
        }

        private void OnCallStateChanged(object sender, CallStateEventArgs e)
        {
            try {
                TapiLine line = (TapiLine)sender;
                TapiCall call = e.Call;
                TapiPhone phone = line.GetAssociatedPhone();

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData);
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                addPbxLogRecord("CallStateChanged", line.Id, line.Name, line.DeviceSpecificExtensionID, phone == null ? -1 : phone.Id, phone == null ? "" : phone.Name, call.Id, call.RelatedId, call.TrunkId, deviceSpecificData, (long)call.CallState, call.CallerId, call.CallerName, call.CalledId, call.CalledName, call.ConnectedId, call.ConnectedName, (long)call.CallOrigin, (long)call.CallReason, 0);
            } catch (Exception ex) {
                LogError("Paused service; exception in OnCallStateChanged handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }

        }

        private void OnCallInfoChanged(object sender, CallInfoChangeEventArgs e)
        {
            try {
                TapiLine line = (TapiLine)sender;
                TapiCall call = e.Call;
                TapiPhone phone = line.GetAssociatedPhone();

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // log all changes
                addPbxLogRecord("CallInfoChanged", line.Id, line.Name, line.DeviceSpecificExtensionID, phone == null ? -1 : phone.Id, phone == null ? "" : phone.Name, call.Id, call.RelatedId, call.TrunkId, deviceSpecificData, (long)call.CallState, call.CallerId, call.CallerName, call.CalledId, call.CalledName, call.ConnectedId, call.ConnectedName, (long)call.CallOrigin, (long)call.CallReason, (long)e.Change);
            } catch (Exception ex) {
                LogError("Paused service; exception in OnCallInfoChanged handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }


        }

        private void OnLineAdded(object sender, LineAddedEventArgs e)
        {
            try
            {
                TapiLine line = e.Line;

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // log all changes
                addPbxLogRecord("LineAdded", line.Id, line.Name, line.DeviceSpecificExtensionID, 
                    -1, "", -1, -1, -1, "", -1, /*"SERVICE STARTED"*/ "", /*"(" + versionString + ", countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + ")"*/ "", "", "", "", "", 0, 0, 0);
                // start monitoring this line
                try
                {
                    line.NewCall += OnNewCall;
                    line.CallStateChanged += OnCallStateChanged;
                    line.CallInfoChanged += OnCallInfoChanged;
                    line.Monitor();
                    //countMonitorOK++;
                    LogInfoFile("TapiCallMonitorService monitoring new line " + line.Id + ":" + line.Name);
                }
                catch (TapiException ex)
                {
                    // countMonitorFail++;
                    // this happens a fair bit, so don't dump it into the event log
                    LogErrorFile("TapiCallMonitorService exception on monitoring new line " + line.Id + ":" + line.Name + " message=" + ex.Message.Trim());
                }


            }
            catch (Exception ex)
            {
                LogError("Paused service; exception in OnLineAdded handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }

        }

        private void OnLineChanged(object sender, LineInfoChangeEventArgs e)
        {
            try
            {
                TapiLine line = e.Line;

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // log all changes
                addPbxLogRecord("LineChanged", line.Id, line.Name, line.DeviceSpecificExtensionID,
                    -1, "", -1, -1, -1, "", -1, /*"SERVICE STARTED"*/ "", /*"(" + versionString + ", countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + ")"*/ "", "", "", "", "", 0, 0, 0);
            }
            catch (Exception ex)
            {
                LogError("Paused service; exception in OnLineChanged handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }

        }

        private void OnLineRemoved(object sender, LineRemovedEventArgs e)
        {
            try
            {
                TapiLine line = e.Line;

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // log all changes
                addPbxLogRecord("LineRemoved", line.Id, line.Name, line.DeviceSpecificExtensionID,
                    -1, "", -1, -1, -1, "", -1, /*"SERVICE STARTED"*/ "", /*"(" + versionString + ", countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + ")"*/ "", "", "", "", "", 0, 0, 0);
            }
            catch (Exception ex)
            {
                LogError("Paused service; exception in OnLineRemoved handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }

        }

        private void OnPhoneAdded(object sender, PhoneAddedEventArgs e)
        {
            try
            {
                TapiPhone phone = e.Phone; // line.GetAssociatedPhone();
                TapiLine line = phone.GetAssociatedLine(); 

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // log all changes
                addPbxLogRecord("PhoneAdded", line.Id, line.Name, line.DeviceSpecificExtensionID,
                    phone == null ? -1 : phone.Id, phone == null ? "" : phone.Name, -1, -1, -1, "", -1, /*"SERVICE STARTED"*/ "", /*"(" + versionString + ", countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + ")"*/ "", "", "", "", "", 0, 0, 0);
            }
            catch (Exception ex)
            {
                LogError("Paused service; exception in OnPhoneAdded handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }

        }

        private void OnPhoneRemoved(object sender, PhoneRemovedEventArgs e)
        {
            try
            {
                TapiPhone phone = e.Phone; // line.GetAssociatedPhone();
                TapiLine line = phone.GetAssociatedLine();

                // devicespecificdata is in here somewhere
                // see http://stackoverflow.com/questions/1003275/how-to-convert-byte-to-string for possible encoding methods
                // string deviceSpecificData = call.DeviceSpecificData == null ? "" : Encoding.UTF8.GetString(call.DeviceSpecificData); 
                string deviceSpecificData = ""; // the call.DeviceSpecificData call sometimes throws 'Value cannot be null' exceptions (TapiCall:910) , so since I'm not using it, just going to set it to ""

                // log all changes
                addPbxLogRecord("PhoneRemoved", line.Id, line.Name, line.DeviceSpecificExtensionID,
                    phone == null ? -1 : phone.Id, phone == null ? "" : phone.Name, -1, -1, -1, "", -1, /*"SERVICE STARTED"*/ "", /*"(" + versionString + ", countMonitorOK=" + countMonitorOK + ", countMonitorFail=" + countMonitorFail + ")"*/ "", "", "", "", "", 0, 0, 0);
            }
            catch (Exception ex)
            {
                LogError("Paused service; exception in OnPhoneRemoved handler. Message='" + ex.Message + "'\nStacktrace='" + ex.StackTrace + "'");
                paused = true;
            }

        }

        // ******************** things

        // not sure if this is needed here any more
        delegate void StringCallback(string p);

        private void LogError(string p) {
            eventLog1.WriteEntry(p, EventLogEntryType.Error);
            LogErrorFile(p);
        }

        private void LogErrorFile(string p) {
            logWriter.WriteLine(DateTime.Now + ":ERROR:" + p);
        }

        private void LogInfo(string p) {
            eventLog1.WriteEntry(p);
            LogInfoFile(p);
        }

        private void LogInfoFile(string p) {
            logWriter.WriteLine(DateTime.Now + ":INFO:" + p);
        }

        private void LogWarn(string p) {
            eventLog1.WriteEntry(p, EventLogEntryType.Warning);
            logWriter.WriteLine(DateTime.Now + ":WARN:" + p);
        }



    }
}
