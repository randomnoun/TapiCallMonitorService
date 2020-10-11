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

