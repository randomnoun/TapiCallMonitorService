# What is it ?

This project contains a Windows service which monitors a TAPI-compliant PBX 
and writes events to a SQL Server database.

# Why is it ?

This service was part of a hastily-constructed software solution that provided call notifications in the Windows taskbar. 

The basic workflow was that

* Incoming calls were centrally received by reception staff.
* Reception staff were given a list of possible clients based on the calling number, which was looked up in four or five different customer information systems
* Reception staff would select the client and other relevant information required to answer the call, and determine which account manager to forward the call to
* Reception staff would forward the call
* The account manager would receive a notification balloon on their desktop with the details of the incoming call

Note that this workflow is not implemented by this service, the description above is just to give a basic idea of how this service fits into the larger picture.

All this service does is monitor a PBX and dump call information into a database. 

If you're interested, the notification system was built on top of an Openfire XMPP (jabber) server and some custom XMPP client software.

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
| dtmTime                              |                                                   |                 |                |                   |
| lngTimeMsec                          |                                                   |                 |                |                   |
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
| tblPbxLog                            | OnNewCall | OnCallStateChanged | OnCallInfoChanged |
| dtmTime                              | NewCall   | CallStateChanged   | CallInfoChanged   |
| lngTimeMsec                          | :white_check_mark:         | x                  | x                 |
| txtEventName                         | x         | x                  | x                 |
| lngTapiLineId                        | x         | x                  | x                 |
| txtTapiLineName                      | x or -1   | x or -1            | x or -1           |
| txtTapiLineDeviceSpecificExtensionID | x or ""   | x or ""            | x or ""           |
| lngPhoneId                           | x         | x                  | x                 |
| txtPhoneName                         | x         | x                  | x                 |
| lngCallId                            | x         | x                  | x                 |
| lngCallRelatedId                     | ""        | ""                 | ""                |
| lngCallTrunkId                       | x         | x                  | x                 |
| txtDeviceSpecificData                | x         | x                  | x                 |
| lngCallState                         | x         | x                  | x                 |
| txtCallerId                          | x         | x                  | x                 |
| txtCallerName                        | x         | x                  | x                 |
| txtCalledId                          | x         | x                  | x                 |
| txtCalledName                        | x         | x                  | x                 |
| txtConnectedId                       | x         | x                  | x                 |
| txtConnectedName                     | x         | x                  | x                 |
| lngCallOrigin                        | 0         | 0                  | x                 |
| lngCallReason                        |           |                    |                   |
| lngChangeType                        |           |                    |                   |


## TAPI line events

| TAPI Line Events                     |             |               |               |
|--------------------------------------|-------------|---------------|---------------|
| tblPbxLog                            | OnLineAdded | OnLineChanged | OnLineRemoved |
| dtmTime                              | LineAdded   | LineChanged   | LineRemoved   |
| lngTimeMsec                          | x           | x             | x             |
| txtEventName                         | x           | x             | x             |
| lngTapiLineId                        | x           | x             | x             |
| txtTapiLineName                      | -1          | -1            | -1            |
| txtTapiLineDeviceSpecificExtensionID |             |               |               |
| lngPhoneId                           | -1          | -1            | -1            |
| txtPhoneName                         | -1          | -1            | -1            |
| lngCallId                            | -1          | -1            | -1            |
| lngCallRelatedId                     |             |               |               |
| lngCallTrunkId                       | -1          | -1            | -1            |
| txtDeviceSpecificData                |             |               |               |
| lngCallState                         |             |               |               |
| txtCallerId                          |             |               |               |
| txtCallerName                        |             |               |               |
| txtCalledId                          |             |               |               |
| txtCalledName                        |             |               |               |
| txtConnectedId                       | 0           | 0             | 0             |
| txtConnectedName                     | 0           | 0             | 0             |
| lngCallOrigin                        | 0           | 0             | 0             |
| lngCallReason                        |             |               |               |
| lngChangeType                        |             |               |               |


## TAPI phone events

| TAPI Phone Events                    |              |                |
|--------------------------------------|--------------|----------------|
| tblPbxLog                            | OnPhoneAdded | OnPhoneRemoved |
| dtmTime                              | PhoneAdded   | PhoneRemoved   |
| lngTimeMsec                          | x            | x              |
| txtEventName                         | x            | x              |
| lngTapiLineId                        | x            | x              |
| txtTapiLineName                      | x or -1      | x or -1        |
| txtTapiLineDeviceSpecificExtensionID | x or ""      | x or ""        |
| lngPhoneId                           | -1           | -1             |
| txtPhoneName                         | -1           | -1             |
| lngCallId                            | -1           | -1             |
| lngCallRelatedId                     |              |                |
| lngCallTrunkId                       | -1           | -1             |
| txtDeviceSpecificData                |              |                |
| lngCallState                         |              |                |
| txtCallerId                          |              |                |
| txtCallerName                        |              |                |
| txtCalledId                          |              |                |
| txtCalledName                        |              |                |
| txtConnectedId                       | 0            | 0              |
| txtConnectedName                     | 0            | 0              |
| lngCallOrigin                        | 0            | 0              |
| lngCallReason                        |              |                |
| lngChangeType                        |              |                |

