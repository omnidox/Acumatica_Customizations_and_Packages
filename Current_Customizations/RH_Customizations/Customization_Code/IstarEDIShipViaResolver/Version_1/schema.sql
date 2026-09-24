-- Reference schema for the two order-level fields only. The routing
-- dictionary is in ShipViaMappings.cs and needs no mapping table.
-- Add these columns through the Acumatica customization package schema
-- workflow; do not run this file directly against production.

ALTER TABLE dbo.SOOrder ADD
    UsrManualShipVia bit NOT NULL
        CONSTRAINT DF_SOOrder_UsrManualShipVia DEFAULT (0),
    UsrAutoShipVia nvarchar(15) NULL;
