USE [QtracDB-610]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AccumulateTimeStorage]') AND type in (N'U'))
DROP TABLE [dbo].[AccumulateTimeStorage]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BranchSTAverageServiceTime]') AND type in (N'U'))
DROP TABLE [dbo].[BranchSTAverageServiceTime]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WaitTimeList]') AND type in (N'U'))
DROP TABLE [dbo].[WaitTimeList]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WaitTimeListShaddow]') AND type in (N'U'))
DROP TABLE [dbo].[WaitTimeListShaddow]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HistoricalEstimateTimeStorage]') AND type in (N'U'))
DROP TABLE [dbo].[HistoricalEstimateTimeStorage]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HistoricalEstimateTimeStorage-AllEstimateTCollection]') AND type in (N'U'))
DROP TABLE [dbo].[HistoricalEstimateTimeStorage-AllEstimateTCollection]

-- New [AccumulateTimeStorage] table
CREATE TABLE [dbo].[AccumulateTimeStorage](
	[UserName] [nvarchar](50) NULL,
	[ServiceType] [nvarchar](50) NULL,
	[AccumulateTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL
) ON [PRIMARY]

-- New [BranchSTAverageServiceTime] table
CREATE TABLE [dbo].[BranchSTAverageServiceTime](
	[AverageStorageID] [int] IDENTITY(1,1) NOT NULL,
	[ServiceTypeName] [nvarchar](50) NULL,
	[AverageServiceTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL
) ON [PRIMARY]

-- New [WaitTimeList] table
CREATE TABLE [dbo].[WaitTimeList](
	[QueueId] [int] NULL,
	[TicketId] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[ServingUser] [nvarchar](50) NULL,
	[ServiceTypeName] [nvarchar](50) NULL,
	[EventQueueTimeInsert] [datetime] NULL,
	[EventName] [nvarchar](50) NULL,
	[Appointment] [datetime] NULL,
	[EstimateWaitTime] [int] NULL,
	[EstimateServiceTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL
) ON [PRIMARY]

-- New [WaitTimeListShaddow] table
CREATE TABLE [dbo].[WaitTimeListShaddow](
	[QueueId] [int] NULL,
	[TicketId] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[ServingUser] [nvarchar](50) NULL,
	[ServiceTypeName] [nvarchar](50) NULL,
	[EventQueueTimeInsert] [datetime] NULL,
	[EventName] [nvarchar](50) NULL,
	[Appointment] [datetime] NULL,
	[EstimateWaitTime] [int] NULL,
	[EstimateServiceTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL
) ON [PRIMARY]

-- New [HistoricalEstimateTimeStorage] table
CREATE TABLE [dbo].[HistoricalEstimateTimeStorage](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[ServingUser] [nvarchar](50) NULL,
	[ServiceTypeName] [nvarchar](50) NULL,
	[EstimateWaitTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL,
 CONSTRAINT [PK_HistoricalEstimateTimeStorage] PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

-- New [HistoricalEstimateTimeStorage] table
CREATE TABLE [dbo].[HistoricalEstimateTimeStorage-AllEstimateTCollection](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[TimeStemp] [datetime] NULL,
	[ServingUser] [nvarchar](50) NULL,
	[ServiceTypeName] [nvarchar](50) NULL,
	[EstimateWaitTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL,
 CONSTRAINT [PK_HistoricalEstimateTimeStorage-AllEstimateTCollection] PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


