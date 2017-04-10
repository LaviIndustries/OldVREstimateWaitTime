USE [QtracDB-610]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

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

GO

