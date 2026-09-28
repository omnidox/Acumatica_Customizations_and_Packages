# Planner Shortage Report – Data Guardrails (exploration phase)

Status: agreed working rules while the final report is designed in Excel with Velixo ACU.QUERY.
Adopted 2026-09-28. Review when the report is finalised and feeds move to Generic Inquiries.

## Rules

1. **One sheet per table; never join inside a query.**
   Kit recipes, annual forecast, open SO lines, shipments, stock, PO lines and vendor terms each get their own sheet.
   Connect them in Excel by item, customer or order key.
   *Why:* these tables have different grains. Joining them in a query repeats rows and double-counts forecast and on-hand quantities.

2. **Pull whole tables; filter in Excel.**
   Query filters may restrict only status (open, not cancelled, active) and date range, never customer or kit.
   *Why:* the all-account purchase view, the account view and what-if analysis must all work from the same data. Shared stock must be counted once.

3. **Explicit row limits, visible row counts.**
   Every ACU.QUERY sets a row limit well above the expected size (e.g. 200,000 for open SO lines), and each sheet shows the rows returned.
   *Why:* if the count equals the limit, the data was cut off without any warning.

4. **Select only the columns in use.**
   *Why:* refresh time grows with columns. Adding a column later is easy.

5. **Record each business rule next to the formula that applies it.**
   Examples: which date puts an order into a month, whether on-hold / pending-approval / blanket POs count as supply, whether credit-hold orders count as demand, how shipments consume the forecast, which warehouses count, which forecast type is used.
   Mark each one as *confirmed (by whom, date)* or *assumption*.
   *Why:* these notes become the GI specification and the report's audit trail.

6. **Production access is a separate decision.**
   Line, stock and kit objects need the Acumatica role `ODatav4 User` (grants broad read access through DAC-based OData).
   In production, assign it to a dedicated reporting/Velixo user, not a person's account. Needs owner approval.

## Related known facts

- Use `QtyOnHand` plus the report's own demand feeds. Do **not** subtract open orders from `QtyAvail`, which already deducts booked sales orders (double count).
- Volume test (2026-09-28, snapshot data): all 124,962 open SO lines load via ACU.QUERY in about 27 s. Excel `GROUPBY` totals them to 1,313 customer × kit × month rows in under 1 s. Re-test at production volume. Filter shipment history by date.
- Proof-of-concept workbook and evidence: `Velixo_POC/`.

## When to move a feed to a Generic Inquiry

Only when at least one applies:
- the query becomes too slow at production volume;
- other people or tools need the same totals outside the workbook;
- the definition should be owned and locked in Acumatica.
