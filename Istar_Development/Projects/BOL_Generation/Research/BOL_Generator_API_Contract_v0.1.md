# BOL Generator API Contract

Version: 0.1

## Overview

This document captures the verified Contract-Based REST API for the BOL
Generator project.

## Endpoint

**PUT**

`/entity/iStarBOL/25.200.001/BOLShipmentInquiry?$expand=BOLShipmentInquiryDetails`

## Request

``` json
{
  "Customer_order_NBR": {
    "value": "0215-4050763-0579"
  }
}
```

## Response Structure

``` text
BOLShipmentInquiry
├── Customer_order_NBR
└── BOLShipmentInquiryDetails[]
    ├── ShipmentNbr
    ├── CustomerOrderNbr
    ├── BOLNumber
    ├── Customer
    ├── CustomerName
    ├── Branch
    ├── BranchName
    ├── AddressLine1
    ├── AddressLine2
    ├── AddressLine3
    ├── City
    ├── State
    ├── PostalCode
    ├── Country
    ├── Location
    ├── LocationName
    ├── ShipmentDate
    ├── Packages
    ├── PackageWeight
    ├── ShippedQuantity
    ├── ShippedWeight
    ├── ShippedVolume
    ├── ShipVia
    ├── ShippingTerms
    ├── FreightPrice
    ├── FreightCost
    ├── FreightCurrency
    ├── Status
    ├── WarehouseID
    ├── Operation
    ├── Description
    ├── CreatedBy
    ├── CreatedOn
    ├── LastModifiedBy
    ├── LastModifiedOn
    └── Type
```

## Field Mapping

  -----------------------------------------------------------------------------
  JSON Field              Source                        Notes
  ----------------------- ----------------------------- -----------------------
  Customer_order_NBR      GI Parameter                  Request lookup

  ShipmentNbr             SOShipment.ShipmentNbr        Shipment key

  CustomerOrderNbr        SOShipment.CustomerOrderNbr   Verified lookup key

  BOLNumber               UsrTCCustomField3             Alias; currently empty

  Packages                SOShipment.PackageCount       Cartons

  ShippedWeight           SOShipment.ShipmentWeight     Preferred shipment
                                                        weight

  PackageWeight           SOShipment.PackageWeight      Needs validation

  ShipVia                 SOShipment.ShipVia            May be blank

  Status                  SOShipment.Status             Currently Open
  -----------------------------------------------------------------------------

## Sample Response

``` json
{
  "BOLShipmentInquiryDetails": [
    {
      "ShipmentNbr":{"value":"0000787"},
      "CustomerOrderNbr":{"value":"10001915244-0587"},
      "BOLNumber":{},
      "CustomerName":{"value":"TARGET"},
      "LocationName":{"value":"TARGET DC 0587"},
      "AddressLine1":{"value":"12905 E L AVE"},
      "City":{"value":"GALESBURG"},
      "State":{"value":"MI"},
      "PostalCode":{"value":"49053"},
      "Packages":{"value":77},
      "ShippedWeight":{"value":91.950492},
      "PackageWeight":{"value":-1035.362100},
      "Status":{"value":"Open"}
    }
  ],
  "Customer_order_NBR":{"value":"10001915244-0587"}
}
```

## Verified Lookups

  Customer Order      Shipment
  ------------------- ----------
  10001915244-0587    0000787
  0215-4050763-0560   0000786
  0215-4050763-0579   0000781

## Remaining Work

-   Validate BOLNumber usage.
-   Validate PackageWeight.
-   Complete Excel template mapping.
-   Implement Node.js client.
-   Generate Master and Individual BOL PDFs.
