USE [QtracDB-610]
GO

/****** Object:  Table [dbo].[BranchSTAverageServiceTime]    Script Date: 03/31/2017 15:28:20 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BranchSTAverageServiceTime](
	[AverageStorageID] [int] IDENTITY(1,1) NOT NULL,
	[ServiceTypeName] [nvarchar](50) NULL,
	[AverageServiceTime] [int] NULL,
	[Branch] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[Company] [nvarchar](50) NULL,
) ON [PRIMARY]

GO


