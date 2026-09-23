# IstarEDIShipViaResolver — Version 1 design notes

## Confirmed behavior

- Target: `SOOrder.ShipVia`, which stores a configured `Carrier.CarrierID`.
- Inputs: customer, optional customer location, TrueCommerce `TCSOOrderExt.UsrTCCustomField1` (shipping method), and `UsrTCCustomField2` (SCAC, possibly blank).
- Resolve a unique active mapping when an order is created and when either UDF is changed by a user. Re-resolve when customer or location changes, unless a manual override is active.
- When a mapping resolves, apply the Ship Via through the Acumatica cache field update path so standard shipping, tax, freight, and package behavior runs.
- When no unique mapping resolves, leave Acumatica's normal Ship Via default or existing value in place; do not guess, block saving, or place the order on hold. A nonblocking warning or status may identify the unresolved mapping.
- An explicit user choice of Ship Via takes precedence over later automatic mapping. If the user releases the override, resolve again from the current inputs.

## Default and override precedence

1. User-selected Ship Via while manual override is enabled.
2. Unique active EDI mapping for the current customer/location and UDF values.
3. Acumatica's standard customer-location/default Ship Via behavior.

Acumatica `Location.CCarrierID` is a customer-location Ship Via default. Therefore, a nonblank `SOOrder.ShipVia` may be only a default, not a deliberate manual choice. The resolver must be allowed to replace such a default when a valid EDI mapping exists.

## Proposed user control

Add an order-header checkbox labeled **Manual Ship Via** (default off). When enabled, mapping input changes do not replace `SOOrder.ShipVia`. The user can select any valid Ship Via. Clearing the checkbox reruns the resolver; if no mapping resolves, normal Acumatica default behavior applies.

For convenience, a direct user edit of Ship Via should enable the checkbox automatically. This requires identifying user edits separately from Acumatica defaulting, EDI/API writes, and resolver updates. Do not rely on `ShipVia != null` or the Ship Via field event alone to infer a manual edit. Confirm import timing and event context before implementing automatic detection. The explicit checkbox remains the reliable override mechanism.

## Mapping behavior

- Normalize case and surrounding whitespace in UDF1/UDF2.
- Allow a blank UDF2 when the customer and UDF1 identify exactly one mapping.
- Multiple candidates with different target Ship Via values are unresolved; do not choose by arbitrary ordering.
- Validate that the target `Carrier` exists and is active.
- Keep mapping records configurable so changes do not require code changes.

## Implementation questions to resolve from an imported order

- Does TrueCommerce populate header UDFs during order insertion, later in the same save, or through a subsequent update?
- Does TrueCommerce use Acumatica cache field updates for the UDFs, or another write path?
- Does TrueCommerce ever set `SOOrder.ShipVia` itself?
- Which active entry points can change UDFs: UI, REST/import, processing screens, or direct database updates?

These determine which field/row events invoke the resolver and whether an additional after-import reconciliation step is needed. The final design should preserve the manual override across all entry points.
