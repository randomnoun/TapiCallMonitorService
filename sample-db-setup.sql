-- uncomment to set current database
--
-- USE [pbx-dev]
-- GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [tblPbxLog](
	[lngId] [int] IDENTITY(1,1) NOT NULL,
	[dtmTime] [datetime] NULL,
	[lngTimeMsec] [int] NULL,
	[txtEventName] [nvarchar](200) NULL,
	[lngTapiLineId] [int] NULL,
	[txtTapiLineName] [nvarchar](200) NULL,
	[txtTapiLineDeviceSpecificExtensionID] [nvarchar](max) NULL,
	[lngPhoneId] [int] NULL,
	[txtPhoneName] [nvarchar](200) NULL,
	[lngCallId] [int] NULL,
	[lngCallRelatedId] [int] NULL,
	[lngCallTrunkId] [int] NULL,
	[txtDeviceSpecificData] [nvarchar](max) NULL,
	[lngCallState] [int] NULL,
	[txtCallerId] [nvarchar](200) NULL,
	[txtCallerName] [nvarchar](200) NULL,
	[txtCalledId] [nvarchar](200) NULL,
	[txtCalledName] [nvarchar](200) NULL,
	[txtConnectedId] [nvarchar](200) NULL,
	[txtConnectedName] [nvarchar](200) NULL,
	[lngCallOrigin] [int] NULL,
	[lngCallReason] [int] NULL,
	[lngChangeType] [int] NULL,
	[lngCorrespondenceId] [int] NULL,
 CONSTRAINT [PK_tblPbxLog] PRIMARY KEY CLUSTERED (
	[lngId] ASC
 ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO



