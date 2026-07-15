USE [AcumaticaDB]
GO

/****** Object:  Table [dbo].[UsrAnnualForecast]    Script Date: 7/15/2026 11:49:49 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UsrAnnualForecast](
	[CompanyID] [int] NOT NULL,
	[CustomerID] [int] NOT NULL,
	[InventoryID] [int] NOT NULL,
	[ForecastYear] [char](4) NOT NULL,
	[ForecastType] [char](2) NOT NULL,
	[JanQty] [int] NOT NULL,
	[FebQty] [int] NOT NULL,
	[MarQty] [int] NOT NULL,
	[AprQty] [int] NOT NULL,
	[MayQty] [int] NOT NULL,
	[JunQty] [int] NOT NULL,
	[JulQty] [int] NOT NULL,
	[AugQty] [int] NOT NULL,
	[SepQty] [int] NOT NULL,
	[OctQty] [int] NOT NULL,
	[NovQty] [int] NOT NULL,
	[DecQty] [int] NOT NULL,
	[NoteID] [uniqueidentifier] NULL,
	[CreatedByID] [uniqueidentifier] NULL,
	[CreatedByScreenID] [char](8) NULL,
	[CreatedDateTime] [datetime2](7) NULL,
	[LastModifiedByID] [uniqueidentifier] NULL,
	[LastModifiedByScreenID] [char](8) NULL,
	[LastModifiedDateTime] [datetime2](7) NULL,
	[Tstamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_UsrAnnualForecast] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[CustomerID] ASC,
	[InventoryID] ASC,
	[ForecastYear] ASC,
	[ForecastType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_JanQty]  DEFAULT ((0)) FOR [JanQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_FebQty]  DEFAULT ((0)) FOR [FebQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_MarQty]  DEFAULT ((0)) FOR [MarQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_AprQty]  DEFAULT ((0)) FOR [AprQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_MayQty]  DEFAULT ((0)) FOR [MayQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_JunQty]  DEFAULT ((0)) FOR [JunQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_JulQty]  DEFAULT ((0)) FOR [JulQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_AugQty]  DEFAULT ((0)) FOR [AugQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_SepQty]  DEFAULT ((0)) FOR [SepQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_OctQty]  DEFAULT ((0)) FOR [OctQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_NovQty]  DEFAULT ((0)) FOR [NovQty]
GO

ALTER TABLE [dbo].[UsrAnnualForecast] ADD  CONSTRAINT [DF_UsrAnnualForecast_DecQty]  DEFAULT ((0)) FOR [DecQty]
GO

