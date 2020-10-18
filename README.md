
# What is it ?

This project contains a Windows service which monitors a TAPI-compliant PBX 
and writes events to a SQL Server database.

# Why is it ?

This service was part of a hastily-constructed software solution that provided call notifications in the Windows taskbar. 

**All this service does is monitor a PBX and dump call information into a database.** 

You can then monitor that table and use it as the basis for a call monitoring and notification app, or convert it into gantt diagrams if you like.

# How is it ?
To install this software, you will need to

* Install the TAPI driver for your PBX. 
   * If you're using Asterix, there's [an open source one](https://www.voip-info.org/asterisk-tapi/), otherwise check the driver download page for your particular PBX device.
   
* Install a SQLServer-compatible database. Microsoft has a free one which is good enough, which you can install with either
   *  [Visual Studio 2019 Community edition](https://visualstudio.microsoft.com/downloads/) if you want to compile the service from source, or 
   * [SQL Server 2019 Express](https://www.microsoft.com/en-gb/sql-server/sql-server-downloads) if you don't
   
* Set up the database 
   * Details are in [README-SQLSERVER.md](README-SQLSERVER.md)

* The connection settings to the database are stored in the registry undor the following keys:
   * HKEY_LOCAL_MACHINE\SOFTWARE\Randomnoun\TapiCallMonitorService\LogFilename
   * HKEY_LOCAL_MACHINE\SOFTWARE\Randomnoun\TapiCallMonitorService\ConnectionString

* A sample .reg file which contains the connection string format can be found in [sample-reg-setup.reg](sample-reg-setup.reg)



* Copy the exe which I also haven't added to the repos yet somewhere and install it as a service somehow.

# The log

The service writes every TAPI event to a single log table ( tblPbxLog ), and a log file.

The table and logfile are only appended to, it is up to other systems to archive these records or rotate the logs 
(you probably want to stop the service whilst that's happening). 

There are four main types of events which are logged:
* Windows service events ( Start, Stop, Pause, Continue )
* TAPI call events ( NewCall, CallStateChanged, CallInfoChanged )
* TAPI line events ( LineAdded, LineChanged, LineRemoved )
* TAPI phone events ( PhoneAdded, PhoneRemoved )
 
The exact nature of the TAPI events depends on the type of PBX that you're using. 
Although the TAPI fields are standard, the order of those events and the data in the event fields changes between PBX vendors. 
The TapiCallMonitorService does not try to interpret the values in the TAPI events at all. 

You probably just want to run it for a few days, and perform a few activities like forwarding calls etc to see what appears in the logs.

Not all events populate all fields. Here are the fields which are populated.
Generally speaking if a field is not populated by an event it is either set to an empty string or the numeric value -1.

The lngCallState field is a bitmask, to interpret this field, use the values defined in tapi.h ( https://docs.microsoft.com/en-us/windows/win32/tapi/linecallstate--constants )

## Windows Serice Events

The txtCallerName contains the version number of the service (currently 0.4), and during startup also includes the number of lines successfully being monitored.
( Some PBXes expose a large number of lines over TAPI, only a subset of which are active ).


| Windows Service Events               |                                                   |                 |                |                   |
|--------------------------------------|---------------------------------------------------|-----------------|----------------|-------------------|
| **tblPbxLog**                        | **OnStart**                                       | **OnStop**      | **OnPause**    | **OnContinue**    |
| dtmTime                              | :clock2:                                                  |  :clock2:               |   :clock2:             |   :clock2:                |
| lngTimeMsec                          | :clock2:                                                  | :clock2:                |   :clock2:             |  :clock2:                 |
| txtEventName                         | OnStart                                           | OnStop          | OnPause        | OnContinue        |
| lngTapiLineId                        | -1                                                | -1              | -1             | -1                |
| txtTapiLineName                      |                                                   |                 |                |                   |
| txtTapiLineDeviceSpecificExtensionID |                                                   |                 |                |                   |
| lngPhoneId                           | -1                                                | -1              | -1             | -1                |
| txtPhoneName                         |                                                   |                 |                |                   |
| lngCallId                            | -1                                                | -1              | -1             | -1                |
| lngCallRelatedId                     | -1                                                | -1              | -1             | -1                |
| lngCallTrunkId                       | -1                                                | -1              | -1             | -1                |
| txtDeviceSpecificData                |                                                   |                 |                |                   |
| lngCallState                         | -1                                                | -2              | -3             | -4                |
| txtCallerId                          | SERVICE STARTED                                   | SERVICE STOPPED | SERVICE PAUSED | SERVICE CONTINUED |
| txtCallerName                        | (0.4,<br>countMonitorOK=n,<br>countMonitorFail=n) | (0.4)           | (0.4)          | (0.4)             |
| txtCalledId                          |                                                   |                 |                |                   |
| txtCalledName                        |                                                   |                 |                |                   |
| txtConnectedId                       |                                                   |                 |                |                   |
| txtConnectedName                     |                                                   |                 |                |                   |
| lngCallOrigin                        | 0                                                 | 0               | 0              | 0                 |
| lngCallReason                        | 0                                                 | 0               | 0              | 0                 |
| lngChangeType                        | 0                                                 | 0               | 0              | 0                 |


## TAPI call events

| TAPI Call Events                     |           |                    |                   |
|--------------------------------------|-----------|--------------------|-------------------|
| **tblPbxLog**                            | **OnNewCall** | **OnCallStateChanged** | **OnCallInfoChanged** |
| dtmTime                              | :clock2:                         |  :clock2:                        |  :clock2:                        |
| lngTimeMsec                          | :clock2:                         |  :clock2:                        |  :clock2:                        |
| txtEventName                         | NewCall                  | CallStateChanged         | CallInfoChanged          |
| lngTapiLineId                        | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtTapiLineName                      | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtTapiLineDeviceSpecificExtensionID | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| lngPhoneId                           | :white_check_mark: or -1 | :white_check_mark: or -1 | :white_check_mark: or -1 |
| txtPhoneName                         | :white_check_mark: or "" | :white_check_mark: or "" | :white_check_mark: or "" |
| lngCallId                            | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| lngCallRelatedId                     | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| lngCallTrunkId                       | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtDeviceSpecificData                | ""                       | ""                       | ""                       |
| lngCallState                         | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtCallerId                          | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtCallerName                        | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtCalledId                          | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtCalledName                        | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtConnectedId                       | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| txtConnectedName                     | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| lngCallOrigin                        | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| lngCallReason                        | :white_check_mark:       | :white_check_mark:       | :white_check_mark:       |
| lngChangeType                        | 0                        | 0                        | :white_check_mark:       |

## TAPI line events

| TAPI Line Events                     |             |               |               |
|--------------------------------------|-------------|---------------|---------------|
| **tblPbxLog**                            | **OnLineAdded**        | **OnLineChanged**      | **OnLineRemoved**      |
| dtmTime                              |  :clock2:                  |  :clock2:                  |  :clock2:                  |
| lngTimeMsec                          |  :clock2:                  |  :clock2:                  |  :clock2:                  |
| txtEventName                         | LineAdded          | LineChanged        | LineRemoved        |
| lngTapiLineId                        | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| txtTapiLineName                      | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| txtTapiLineDeviceSpecificExtensionID | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| lngPhoneId                           | -1                 | -1                 | -1                 |
| txtPhoneName                         |                    |                    |                    |
| lngCallId                            | -1                 | -1                 | -1                 |
| lngCallRelatedId                     | -1                 | -1                 | -1                 |
| lngCallTrunkId                       | -1                 | -1                 | -1                 |
| txtDeviceSpecificData                |                    |                    |                    |
| lngCallState                         | -1                 | -1                 | -1                 |
| txtCallerId                          |                    |                    |                    |
| txtCallerName                        |                    |                    |                    |
| txtCalledId                          |                    |                    |                    |
| txtCalledName                        |                    |                    |                    |
| txtConnectedId                       |                    |                    |                    |
| txtConnectedName                     |                    |                    |                    |
| lngCallOrigin                        | 0                  | 0                  | 0                  |
| lngCallReason                        | 0                  | 0                  | 0                  |
| lngChangeType                        | 0                  | 0                  | 0                  |


## TAPI phone events

| TAPI Phone Events                    |              |                |
|--------------------------------------|--------------|----------------|
| **tblPbxLog**                            | **OnPhoneAdded**             | **OnPhoneRemoved**           |
| dtmTime                              |  :clock2:                        |   :clock2:                       |
| lngTimeMsec                          |  :clock2:                        |   :clock2:                       |
| txtEventName                         | PhoneAdded               | PhoneRemoved             |
| lngTapiLineId                        | :white_check_mark:       | :white_check_mark:       |
| txtTapiLineName                      | :white_check_mark:       | :white_check_mark:       |
| txtTapiLineDeviceSpecificExtensionID | :white_check_mark:       | :white_check_mark:       |
| lngPhoneId                           | :white_check_mark: or -1 | :white_check_mark: or -1 |
| txtPhoneName                         | :white_check_mark: or "" | :white_check_mark: or "" |
| lngCallId                            | -1                       | -1                       |
| lngCallRelatedId                     | -1                       | -1                       |
| lngCallTrunkId                       | -1                       | -1                       |
| txtDeviceSpecificData                |                          |                          |
| lngCallState                         | -1                       | -1                       |
| txtCallerId                          |                          |                          |
| txtCallerName                        |                          |                          |
| txtCalledId                          |                          |                          |
| txtCalledName                        |                          |                          |
| txtConnectedId                       |                          |                          |
| txtConnectedName                     |                          |                          |
| lngCallOrigin                        | 0                        | 0                        |
| lngCallReason                        | 0                        | 0                        |
| lngChangeType                        | 0                        | 0                        |

# Doesn't do much does it ?

That's the idea. 

This service was part of a hastily-constructed software solution that provided call notifications in the Windows taskbar. 

The basic workflow was that

* Incoming calls were centrally received by reception staff.
* Reception staff were given a list of possible clients based on the calling number, which was looked up in four or five different customer information systems
* Reception staff would select the client and other relevant information required to answer the call, and determine which account manager to forward the call to
* Reception staff would forward the call
* The account manager would receive a notification balloon on their desktop with the details of the incoming call

Note that this workflow is not implemented by this service, the description above is just to give a basic idea of how this service fits into the larger picture.

**All this service does is monitor a PBX and dump call information into a database.** 

If you're interested, the rest of the notification system was built on top of an Openfire XMPP (jabber) server and some custom XMPP client software.