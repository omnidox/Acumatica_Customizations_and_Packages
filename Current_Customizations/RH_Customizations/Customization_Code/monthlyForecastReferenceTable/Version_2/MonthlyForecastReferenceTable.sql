USE [AcumaticaDB]
GO

/****** Object:  Table [dbo].[UsrMonthlyForecast]    Script Date: 6/22/2026 11:05:00 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UsrMonthlyForecast](
	[CompanyID] [int] NOT NULL,
	[InventoryID] [int] NOT NULL,
	[FinPeriodID] [char](6) NOT NULL,
	[ForecastQty] [decimal](25, 6) NOT NULL,
	[NoteID] [uniqueidentifier] NULL,
	[CreatedByID] [uniqueidentifier] NULL,
	[CreatedByScreenID] [char](8) NULL,
	[CreatedDateTime] [datetime2](7) NULL,
	[LastModifiedByID] [uniqueidentifier] NULL,
	[LastModifiedByScreenID] [char](8) NULL,
	[LastModifiedDateTime] [datetime2](7) NULL,
	[Tstamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_UsrMonthlyForecast] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[InventoryID] ASC,
	[FinPeriodID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[UsrMonthlyForecast] ADD  CONSTRAINT [DF_UsrMonthlyForecast_ForecastQty]  DEFAULT ((0)) FOR [ForecastQty]
GO

