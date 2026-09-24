# IstarEDIShipViaResolver, Version 1

Acumatica source for selecting `SOOrder.ShipVia` from TrueCommerce sales-order
UDF1/UDF2. Version 1 keeps customer mappings in the C# dictionary in
`ShipViaMappings.cs`. No mapping table or maintenance screen is required.

The project references installed Acumatica 25.201 and TrueCommerce TCAddon
9.0.1.137 assemblies. It does not modify either vendor assembly.

## Behavior

- The dictionary is keyed by Acumatica `Customer.AcctCD` (Customer ID), using
  IDs from `Customers 20260923.xlsx`. This avoids display-name differences.
  The Kohl's department-store, Sterling, and Nexcom rules apply to all
  corresponding records in the workbook per the current business decision.
  Other unmapped customer IDs retain Acumatica's normal Ship Via.
- When both UDFs contain useful values, both must match. A blank or `N/A`
  UDF is treated as missing; the remaining value may resolve a unique match.
- Set a Ship Via only when all matching routes point to one active Acumatica
  carrier. Otherwise, leave Acumatica's value in use and display a warning.
- On a later unresolved input, restore Acumatica's default only if the current
  value is the Ship Via previously assigned by this resolver.
- `Manual Ship Via` suppresses automatic changes. Direct UI edits of Ship Via
  also enable that checkbox. API and Excel import edits do not.
- TrueCommerce's existing outbound map remains responsible for ASN routing
  and SCAC; this customization does not populate UDF1/UDF2 for outbound use.

## Intentionally omitted draft rows

- J.C.Penney has no clear Ship Via in the supplied table.
- Amazon's `UPS 3 UPS GROUND` routing description is malformed.
- `FDSP` has conflicting SCACs between the customer and summary tables.
- `USFC` has a conflicting routing description between those tables.

These cases use Acumatica's default Ship Via until their mapping is confirmed.

## Customer record scope

Kohl's department stores: `10282`, `DKOHLS`, `DKOHLX` (not Kohl's.com).
Sterling: `JSTERM`, `JSTERX`. Nexcom: `27230`, `CNA096`, `CNA099`,
`JNX995`. Each group shares its customer-specific routing rules.

The Stanley/Candela Boscov's rows still need a decision separate from R&S
customer `38588`. Saks Fifth Avenue and Macy's/Macy's.com have no clear
one-to-one customer record in this workbook. These records keep Acumatica's
normal Ship Via until their customer IDs and routing applicability are
confirmed. Amazon Marketplace IDs are not included in the Amazon.com rule.

## Package work before deployment

1. Add the graph extension, DAC extension, and `ShipViaMappings.cs` to an
   Acumatica customization project with `TCAddon.dll` available.
2. Add the two `SOOrder` columns described in `schema.sql` through the
   customization package schema workflow.
3. Place `UsrManualShipVia` next to Ship Via on SO301000.
4. Confirm customer ID scope and available Ship Via codes against
   Acumatica's customer and carrier records.
5. Exercise creation and later updates through an actual TrueCommerce 850
   import. The TCAddon DLL does not reveal when inbound UDFs are written.

To change a routing rule in this version, edit `ShipViaMappings.cs` and
rebuild/publish the customization. A mapping table and screen can be added
later if frequent updates make that worthwhile.
