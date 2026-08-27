# Scan Speed Optimization Options

**Created:** August 27, 2026  
**Priority:** Reduce the time between scanning a barcode and receiving confirmation in Acumatica Pick, Pack, and Ship.

## Purpose

This document is the working register for every practical approach considered for improving scan speed. Each option must be measured against the same functional requirements: correct quantities, package assignments, lot/serial and location validation, concurrent users, recovery from failure, command states, and label generation.

The latest controlled capture averaged approximately 956 ms of Server Time, 844 ms of Server CPU, and 259 ms of SQL Time per scan. Future work should therefore consider both application-server processing and database activity. Server Time, Server CPU, and SQL Time are overlapping measurements and must not be added together.

## Evaluation rules

Every experiment should:

1. Use the same large shipment and a normal-sized shipment.
2. Capture at least three warmed scans before and after the change.
3. Record Server Time, Server CPU, SQL Time, SQL calls, SQL rows, exceptions, and dotTrace method timings where appropriate.
4. Preserve standard Acumatica behavior or clearly identify any workflow change.
5. Include recovery, concurrency, package, quantity, removal, label, and final-confirmation testing.
6. Remain disabled or fail safely when its assumptions are not satisfied.

## Peiyu's proposed approaches

Peiyu's August 2026 recommendations add four useful lines of investigation:

1. Reduce the number of actions performed by the operator.
2. Reduce scanner and browser delay before or after the server callback.
3. Remove expensive custom attributes, events, lookups, and integrations from each scan.
4. Verify application configuration and deployment practices.

The barcode-lookup recommendation maps to Option 10 and has already been implemented. SQL index review maps to Option 12. Customization isolation maps to Option 15. The remaining recommendations are recorded as Options 16 through 24 below.

## Option 1: Memory-only deferred scan cache

### Concept

Hold scanned items in application memory and postpone the normal Acumatica packing updates until the user completes the batch.

```text
Scan barcode
-> validate against a memory snapshot
-> add quantity to an in-memory batch
-> immediately acknowledge the scan

Finish batch
-> reload current Acumatica state
-> revalidate every scan
-> apply packing changes
-> save and print
```

### Potential benefit

- Individual scans could avoid most database writes, split reloads, formulas, parent updates, saves, and command-state recalculation.
- The scanner could receive near-immediate acknowledgment if validation is performed against an already-loaded shipment snapshot.
- Expensive processing would be consolidated into one final operation.

### Risks

- Unsaved scans could be lost during an IIS recycle, application restart, publish, crash, session expiration, or load-balancer transition.
- Two users could act on outdated quantities or pack the same inventory.
- Lot/serial, location, overpacking, removal, and package validation could be delayed until the end.
- Labels and command states would not necessarily reflect the physical work as it occurs.
- A final validation failure could reject work after the shipment has already been physically packed.
- A long-lived application-memory cache could become stale or leak data between requests if it is not scoped correctly.

### Current assessment

**Highest theoretical per-scan benefit, highest operational risk.** This should not be a production implementation unless the business explicitly accepts deferred packing semantics and possible loss of uncommitted scans. It is best treated as a proof of concept or an offline-scanning design.

## Option 2: Durable scan-staging table with final batch commit

### Concept

Write a small staging record for each scan instead of immediately executing the complete packing workflow. At completion, process all staged scans through standard Acumatica logic.

```text
Scan
-> lightweight validation
-> INSERT a staging record
-> acknowledge scan

Finish batch
-> reload authoritative shipment/package state
-> revalidate staged records
-> apply standard packing changes
-> save, print, and mark staging records complete
```

### Required staging data

- Company, branch, shipment, and package identifiers
- Inventory, subitem, location, lot/serial, quantity, and UOM
- User, scanner/session, sequence number, and timestamp
- Processing status, error text, and retry count
- An idempotency key to prevent duplicate application
- A version or timestamp used to detect shipment changes

### Potential benefit

- A small insert should be cheaper than the full packing workflow.
- Scans survive application restarts and session loss.
- Expensive formulas and parent updates can be consolidated at final commit.

### Risks

- This still uses both the Acumatica application server and SQL Server for every scan.
- Final processing may become slow and may reject previously acknowledged scans.
- Concurrency, cancellation, recovery, duplicate processing, and partial failure require a complete design.
- Standard labels and package state would be delayed unless additional logic is created.

### Current assessment

**Safer than memory-only caching but still a new business workflow.** Consider only if sub-second scans remain insufficient and the business accepts batch confirmation.

## Option 3: Hybrid memory cache with durable checkpoints

### Concept

Keep the active batch in memory for fast validation while periodically or asynchronously writing durable checkpoints.

### Potential benefit

- Faster than synchronous staging for most scans.
- Less data-loss exposure than a purely memory-only design.

### Risks

- The user may receive acknowledgment before a checkpoint is durable.
- Recovery and reconciliation become complex.
- Multiple application servers require a shared or distributed cache.
- More moving parts make correctness and support harder than either Option 1 or Option 2.

### Current assessment

**High complexity.** Do not pursue before measuring a durable staging prototype.

## Option 4: Client-side scan batching

### Concept

Collect several barcode scans in the browser or mobile client and submit them to Acumatica in one request.

### Potential benefit

- Reduces HTTP callback, page lifecycle, session, grid-binding, and response-rendering overhead per item.
- May retain standard server processing while amortizing request overhead across several scans.

### Risks

- Validation feedback is delayed until the batch is submitted.
- A single bad barcode requires clear per-line error handling.
- Rapid scans must be queued reliably while a batch is in flight.
- Standard Pick, Pack, and Ship UI behavior may require substantial customization.

### Current assessment

**Potentially high impact where callback overhead is significant.** Profile the full request timeline before designing it.

## Option 5: Continue safe request-scoped reuse

### Concept

Reuse expensive data only within one HTTP request and invalidate it after any mutation that could make it stale.

### Work already completed

- Cached `pickedForPack()` results within the request.
- Preserved a post-confirmation refresh after `PackSplit()`.
- Eliminated repeated per-split quantity-threshold lookups.
- Removed repeated shipment-line loads and the Package Content LINQ fallback.

### Remaining possibility

Add narrowly scoped caches only where dotTrace proves that repeated, state-safe reads remain material.

### Current assessment

**Lowest-risk application approach, but current opportunities have diminishing returns.** Version 3 conditional split reuse reduced internal rows and selects without materially improving elapsed scan time, so Version 1 remains the production design.

## Option 6: Reduce `CanPack` and command-state evaluation

### Concept

Replace full collection processing with a state-safe existence check or reuse an already-current collection.

### Risks

- Unsaved package changes may be newer than the database.
- Paperless, wave/batch, unassigned, inserted, deleted, and reassigned splits can change the required behavior.
- Incorrect results could enable or disable package commands at the wrong time.

### Current assessment

**Previously researched.** Conditional reuse reduced two split loads to one but did not materially improve scan time. Do not deploy without a new measurement showing meaningful benefit.

## Option 7: Reduce grid synchronization and callback rendering

### Concept

Reduce data controls, grid refreshes, serialization, or selects performed before and after the barcode callback.

### Constraints

- `gridPacked` currently requires `SyncPosition="true"` for reliable current-row behavior and `ReopenLineQty`.
- Removing required synchronization may cause commands to act on the wrong row.

### Possible research

- Identify controls rebound after every scan even when their data did not change.
- Measure response serialization and `CollectDataControls` costs.
- Refresh only affected views when Acumatica provides a supported mechanism.

### Current assessment

**Potentially meaningful but UI-state sensitive.** Do not change ASPX synchronization without complete row-selection and command testing.

## Option 8: Reduce in-memory work inside `GetSplits()`

### Concept

Avoid repeatedly materializing, converting, sorting, or merging the complete shipment-split collection.

### Risks

Correct behavior depends on:

- Assigned and unassigned splits
- `SOShipLine` and `INLocation` joined records
- `processedSeparator` behavior
- Shipment-location ordering
- Formulas and cache state
- Exact row ordering expected by packing logic

### Current assessment

**Potentially high impact but invasive.** An earlier replacement did not improve the active path and was withdrawn. Reconsider only if method-level profiling proves that this remains the dominant cost and a state-equivalent implementation can be demonstrated.

## Option 9: Package-state bulk prefetch

### Concept

Load package-content state for all packages once, compute package summaries in memory, and invalidate affected entries after packing mutations.

### Potential benefit

- Could replace repeated package-specific queries with one bulk query per valid phase.

### Current assessment

The measured package-split query consumed only a small amount of SQL time. **Low-priority micro-optimization** unless dotTrace shows material application CPU in package-state evaluation.

## Option 10: Barcode and reference lookup caching

### Concept

Cache positive and negative barcode, `InventoryItem`, and `INItemXRef` results at the narrowest safe scope.

### Work already completed

The expensive Master Pack resolver installed by `WMS.PackModeLogicExt.DecorateScanState` was replaced by the production customization:

```text
PackModeBarcodeLookupOptimization.cs
Class: IStar.ScanPerformance.PackModeBarcodeLookupOptimization
```

The original handler worked approximately as follows:

```text
Load shipment splits
-> inspect a split
-> query InventoryItem and INItemXRef
-> repeat until a barcode match is found
```

The replacement performs:

```text
Query INItemXRef once for the scanned barcode
-> identify candidate InventoryItem records
-> run a limited shipment-existence query for the InventoryID
-> return the matching barcode and item
```

It preserves the original fallback that accepts `InventoryCD` when no `INItemXRef` record matches. This optimization is separate from `PackModePickedForPackRequestCache.cs`: the barcode file removes repeated barcode-reference work, while the request-cache file reduces repeated shipment-split processing.

### Remaining possibility

Use short-lived negative-result caching if profiling shows repeated missing-barcode lookups.

### Current assessment

**Primary issue already resolved.** Revisit only with new profiler evidence.

## Option 11: Defer nonessential label work

### Concept

Queue label generation after the packing transaction and return scan confirmation without waiting for rendering or printer communication.

### Potential benefit

- Removes report generation, external service, and printer latency from the scan response when those operations currently block it.

### Risks

- A scan may succeed while label generation fails later.
- Requires durable queueing, retry, duplicate prevention, and visible error handling.
- Package and UCC context must remain explicit.

### Current assessment

**Worth profiling separately.** Do not combine with the known selected-package UCC defect until that issue is understood.

## Option 12: SQL query and index tuning

### Concept

Use Query Store and execution plans to identify scans, joins, missing indexes, blocking, parameter sensitivity, or excessive reads.

### Potential actions

- Verify existing indexes match shipment, line, split, package, inventory, and barcode predicates.
- Review actual execution plans for the highest-duration queries.
- Check blocking, waits, statistics, and plan regressions.
- Add a supported custom index only when measurements prove a meaningful gain.

### Current assessment

SQL currently accounts for a minority of elapsed scan time, and major N+1 query patterns have been removed. **Useful for targeted findings, not the leading general strategy.** Custom indexes add upgrade and maintenance risk.

## Option 13: Application and database infrastructure tuning

### Possible areas

- Application-server CPU speed, memory pressure, garbage collection, and IIS recycling
- SQL Server CPU, memory, storage latency, tempdb, statistics, and blocking
- Network latency between IIS and SQL Server
- Antivirus exclusions supported by company policy
- Application-pool and Acumatica configuration supported by the installed version
- Load balancing and session affinity in multi-server deployments

### Current assessment

**Measure before changing.** Infrastructure tuning can improve all requests but will not correct inefficient application logic by itself.

## Option 14: Reduce shipment size or partition the workflow

### Concept

Avoid placing approximately 1,800 splits into one active scanning document. Divide work into smaller shipments, worksheets, waves, packages, or operational batches where business rules permit.

### Potential benefit

- Reduces the cost of every operation that scales with shipment lines or splits.
- Uses standard application behavior rather than overriding core methods.

### Risks

- Changes warehouse planning, documentation, shipping, labels, and customer-facing processes.

### Current assessment

**Potentially the highest structural benefit** for unusually large documents, but it is a business-process change rather than a code optimization.

## Option 15: Feature and customization isolation

### Concept

Disable unused features or customizations in a controlled test and compare identical warmed scans.

### Work already completed

- Advanced Picking was disabled and its unused command path disappeared, but steady scan time did not materially improve.
- Multiple unrelated customizations were ruled out during LINQ fallback isolation.

### Current assessment

Continue only when profiler stacks identify a specific feature or customization. Broad deactivation without a controlled comparison is not sufficient evidence.

## Option 16: Enable Use Default Quantity

### Concept

Configure the applicable scan mode so that each item scan automatically applies a default quantity, normally one, without requiring a separate quantity entry or confirmation.

```text
Without default quantity:
Scan item
-> enter or confirm quantity
-> confirm

With default quantity:
Scan item
-> quantity increments automatically
```

### Potential benefit

- Reduces the number of physical actions required per item.
- May materially improve operator throughput even if the Acumatica callback itself is unchanged.

### Required testing

- Quantities greater than one and accidental duplicate scans
- Lot-, serial-, catch-weight-, and variable-quantity items
- Removal and unpacking
- The exact Pick and Pack modes used by the warehouse

### Current assessment

**High-value, low-cost operational test** if users currently perform a separate quantity confirmation. This improves workflow throughput rather than necessarily reducing Server Time.

## Option 17: Preserve scan context with a soft reset

### Concept

After confirmation, use the standard equivalent of `ScanMode.Reset(fullReset: false)` only where appropriate so item-specific state is cleared while shipment, mode, and package context remain available for the next scan.

### Potential benefit

- Avoids making the user or state machine re-establish context unnecessarily.
- May reduce state reconstruction when a full reset is currently occurring.

### Risks

- `Reset(false)` does not itself guarantee that no page reload or callback occurs.
- Incorrect use can retain stale lot, location, quantity, removal, or package state.
- Standard Acumatica Pack mode already performs deliberate state transitions after confirmation.

### Current assessment

**Inspect before modifying.** Use dotTrace and decompiled call paths to prove that an unnecessary full reset occurs. Do not force soft reset globally.

## Option 18: Audit custom attributes and scan event handlers

### Concept

Search DAC extensions and graph extensions on `SOShipLine`, `SOShipLineSplit`, `SOShipment`, `INItemXRef`, `PickPackShip`, and related scan DACs for database or business work executed by frequently evaluated attributes and events.

High-risk examples include:

- `PXFormula` evaluators that query the database
- `PXDBScalar` fields on frequently loaded DACs
- `PXDefault` or selector logic that performs repeated searches
- `RowSelected`, `RowUpdated`, `RowInserted`, `FieldUpdated`, `FieldVerifying`, and `FieldDefaulting` handlers that execute selects, calculations, or external checks

### Work already completed

- The Advanced Labels split formula was prevented from calling `SOShipLine.PK.Find()` per split, eliminating up to approximately 1,814 lookups per scan.
- `PickPackShipShipmentRowSelectedOptimization.cs` replaced a repeated full `SOShipLine` load and eliminated query `B5446270`.

### Important qualification

`RowSelected` can run frequently, but it is too broad to claim that it always executes for every cached row on every postback. Frequency depends on the views, controls, rows, and callback path. Measurement should identify the actual handler and call count.

### Current assessment

**Proven high-value category.** Continue only from Request Profiler or dotTrace evidence; do not broadly remove standard attributes or events.

## Option 19: Scanner prefix, suffix, and focus configuration

### Concept

Configure handheld scanners to send one standard carriage return or Enter suffix after the barcode so Acumatica submits it immediately without a manual keypress.

### Potential benefit

- Reduces operator delay before the HTTP request begins.
- Improves perceived responsiveness and scan cadence.

### Required checks

- Exactly one compatible suffix is sent.
- CR and LF do not create duplicate submissions.
- The full barcode is transmitted before Enter.
- Browser focus remains in the barcode field.
- Configuration matches Acumatica's client-side barcode handler.

### Current assessment

**Easy operational test.** It will not normally reduce Request Profiler Server Time because it affects the period before the request reaches Acumatica.

## Option 20: Browser audio caching and playback

### Concept

Ensure success and error `.wav` assets are cached by the browser and that custom scripts do not synchronously wait for download or playback before accepting the next scan.

### Investigation

Use the browser Network and Performance panels to determine:

- Whether sound files are requested after every scan
- Whether responses come from memory or disk cache
- Whether warehouse Wi-Fi adds meaningful latency
- Whether scan input is blocked until playback finishes

The previously observed `wms_error.wav` request showed that an error sound was loaded; it did not prove that audio caused the server-side delay.

### Current assessment

**Low-risk client-side investigation.** Likely a small improvement unless caching is disabled or playback is synchronous.

## Option 21: Defer carrier, freight, and address-validation hooks

### Concept

Confirm that EasyPost, Pacejet, UPS, FedEx, address-validation, rate, freight, and package-weight integrations do not make synchronous external calls during each item scan. Where business rules allow, run them only at package or shipment completion.

### Evidence required

- dotTrace methods and namespaces during `scan`
- Request Profiler event and exception traces
- HTTP/network calls from IIS or the browser
- Carrier-related SQL and graph-extension call stacks

The carrier `RowSelected` code previously reviewed changed UI state and selected carrier data, but did not prove that a remote carrier request occurred.

### Current assessment

**Potentially high impact if an external call exists; otherwise no benefit.** Do not move carrier logic without confirming its current trigger and required transaction timing.

## Option 22: Defer TrueCommerce, EDI, and other integration hooks

### Concept

Determine whether line or package mutations trigger EDI staging, synchronization, webhooks, or other integration work on every scan. Consider asynchronous or confirmation-time processing where transaction ordering permits it.

### Evidence required

- TrueCommerce or integration namespaces in dotTrace
- Staging-table SQL associated with the scan request
- `SOShipLine`, split, package, or shipment event subscribers in customization code
- External service calls or queued work created per scan

TrueCommerce was previously disabled while isolating the LINQ fallback and did not cause that fallback. That test does not prove that it has no other per-scan overhead.

### Current assessment

**Plausible but unproven.** Asynchronous processing must preserve data integrity, retries, sequencing, and downstream expectations.

## Option 23: Verify production compilation settings

### Concept

Confirm that the effective Acumatica site configuration uses an appropriate non-debug compilation setting, such as:

```xml
<compilation debug="false" targetFramework="4.8" />
```

### Potential benefit

- Prevents debug-oriented compilation and runtime behavior from distorting performance tests.
- May improve application optimization, resource use, and cold-start behavior.

### Risks and qualification

- Changing the effective setting restarts or recompiles the application and must be coordinated.
- The claimed universal 200-400% CPU penalty is not established and should not be used as an expected result.
- The setting must be verified in the effective deployed configuration, not assumed from a template file.

### Current assessment

**Important configuration check and testing prerequisite.** Measure warmed scans before and after any approved change.

## Option 24: Precompile custom extension libraries

### Concept

Package selected custom graph extensions and DAC definitions in compiled extension DLLs rather than maintaining them as customization-project source files.

### Potential benefit

- More controlled deployment and source organization
- Possible improvement to publication, startup, or first-use compilation behavior

### Important qualification

Customization-project C# is compiled when the project is published or runtime code is rebuilt; it is not normally interpreted from source on every scan. A precompiled DLL therefore does not automatically make warmed method execution faster, and .NET still performs JIT compilation as needed.

### Current assessment

**Low priority for steady-state scan speed.** Consider as a deployment architecture decision, not a leading per-scan optimization.

## Recommended investigation order

1. Treat **Option 1** as the starting architectural concept and document the business consequences of deferred persistence.
2. Test **Option 16, Use Default Quantity**, because it may improve operator throughput without code changes.
3. Verify **Option 23, `debug="false"`**, and **Option 19, scanner suffix and focus**, as low-cost configuration checks.
4. If deferred processing is acceptable, prototype **Option 2** before any memory-only production design.
5. Use Request Profiler and dotTrace to identify specific work under **Options 18, 21, and 22**: custom attributes/events, carrier hooks, and EDI or integration hooks.
6. Inspect **Option 20**, audio download and playback, in the browser Network and Performance panels.
7. Review **Option 17**, soft reset behavior, only if profiling proves unnecessary full state reconstruction.
8. Measure full callback overhead to determine whether **Option 4** could reduce per-item page lifecycle costs through batching.
9. Pursue **Options 5, 7, 9, 10, or 11** only when a new capture identifies a state-safe material cost.
10. Review **Option 12** only for queries supported by execution-plan evidence.
11. Discuss **Option 14** with warehouse stakeholders if sub-second scans remain insufficient on extremely large shipments.
12. Treat **Option 24**, precompiled extension libraries, as a deployment consideration rather than an expected warmed-scan improvement.
13. Avoid **Options 3, 6, and 8** unless safer alternatives cannot meet the requirement and the measured benefit justifies their risk.

## Decision log

| Date | Option | Decision | Evidence or next action |
|---|---|---|---|
| August 27, 2026 | Memory-only deferred scan cache | Research only | Define acceptable data-loss, concurrency, validation, label, and recovery behavior before prototyping |
| August 27, 2026 | Durable staging table | Candidate alternative | Estimate the cost of one lightweight staging insert and design final revalidation semantics |
| August 27, 2026 | Version 1 request cache | Retain in production | Safer than conditional Version 3 and produced nearly identical user-visible scan time |
| August 27, 2026 | Use Default Quantity | Proposed operational test | Confirm current warehouse quantity workflow and test exceptional inventory types |
| August 27, 2026 | Debug compilation setting | Verify | Inspect the effective deployed configuration before changing or restarting the application |
| August 27, 2026 | Scanner suffix and browser audio | Proposed client tests | Measure pre-request submission delay and browser network/playback behavior |
| August 27, 2026 | Carrier, EDI, and custom event hooks | Evidence required | Search current profiler and dotTrace stacks before changing integration timing |
