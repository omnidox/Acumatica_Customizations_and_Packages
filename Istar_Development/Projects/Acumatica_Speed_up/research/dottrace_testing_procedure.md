# dotTrace Testing Procedure for Pick, Pack, and Ship

**Created:** August 6, 2026

## Objective

Identify the methods consuming the remaining application-server CPU during a Pick, Pack, and Ship scan on shipment `0000787`. `ProfilerLog_013` averaged approximately 1.37 seconds of server time and 1.16 seconds of IIS CPU per scan.

This procedure is intended for a development or staging server. Do not profile a shared production worker process without operational approval.

## Important corrections to the original procedure

- Leave `<compilation debug="false">` unchanged for the baseline. Enabling debug compilation can change JIT optimization and invalidate the performance comparison.
- PDB files are optional for source-line navigation, not required to identify managed method names. Configure matching PDB/source locations in dotTrace Viewer instead of copying files into the Acumatica `bin` directory.
- Begin with **Sampling**. JetBrains recommends Sampling as the initial, low-overhead method for finding slow methods.
- **Tracing and Line-by-Line cannot be used when attaching dotTrace to an existing process.** They also add enough overhead to distort a short scan.
- Keep Acumatica Request Profiler disabled during the dotTrace capture so its SQL logging and stack collection are not included in the CPU profile.

## Test preparation

1. Use the same Acumatica build, customization set, shipment, user, browser, and scan workflow used for `ProfilerLog_013`.
2. Confirm all six performance customization files are published.
3. Complete one warm-up scan before collecting data. This reduces JIT compilation, first-use cache initialization, and page-load noise.
4. Stop or avoid unrelated processing jobs and ensure no other user is exercising the same IIS worker during the capture.
5. Record:
   - Acumatica build and tenant
   - Server name
   - Application-pool name
   - Shipment and package used
   - Barcode and operation tested
   - Test time and observed browser duration
6. Keep the functional result of the scan: packed quantity, selected package, message, and label outcome.

## Identify the correct IIS worker process

Open an elevated Command Prompt on the IIS server and run:

```bat
%windir%\System32\inetsrv\appcmd.exe list wp
```

The output maps each `w3wp.exe` process ID to its application pool. Record the PID for the Acumatica instance. Do not identify the process by username alone because multiple pools can use similar identities.

If no worker process is listed for the pool, open the Acumatica site once to start it and rerun the command.

## Pass 1: Sampling capture

1. Run dotTrace as Administrator on the IIS server.
2. Select **Attach to Process** or **Running Process**.
3. If necessary, select **Show All Processes**.
4. Select the recorded `w3wp.exe` PID and confirm the application-pool mapping again.
5. Choose **Sampling**.
6. If the option is available, clear **Collect profiling data from start**. Attach first, then manually start collection immediately before the scan.
7. Start collecting.
8. Perform exactly one controlled `scan` callback on `SO302020`.
9. Wait until the browser response, quantity update, command-state update, and any expected label action complete.
10. Immediately select **Get Snapshot and Wait** to stop collection and generate the snapshot.
11. Detach from the process. Detaching should leave the worker process running.
12. Save the `.dtp` snapshot outside dotTrace's temporary storage with a descriptive name, for example:

```text
SO302020_0000787_scan_sampling_2026-08-06_01.dtp
```

Repeat this process for three separate scans, producing three snapshots. Separate captures make it easier to distinguish repeatable hot spots from one-off JIT, garbage-collection, or operating-system noise.

## Analyze each Sampling snapshot

1. Open the snapshot in dotTrace Viewer.
2. Use **Call Tree** and **Hot Spots**.
3. Locate the request path under methods such as:

```text
PX.BarcodeProcessing.BarcodeDrivenStateMachine.scan
PX.BarcodeProcessing.BarcodeDrivenStateMachine.ProcessSingleBarcode
PX.Data.PXAction.Press
```

4. Scope the Call Tree to the scan subtree.
5. Record both total time and own time for the most expensive methods.
6. Search for and record material time under:

```text
PX.Data.PXView.Select
PX.Data.PXView.MergeCache
PX.Data.PXCache
PickPackShip.GetSplits
PickPackShip.PackMode
ActualizeCommandActions
IsPackageEmpty
IStar.ScanPerformance
WMS.PackModeLogicExt
```

7. Use back traces or an inverted call tree to determine which caller caused each expensive framework method.
8. Do not treat Sampling output as an exact method-call count; Sampling estimates where CPU time is spent.
9. Consider a method actionable only when it appears materially in multiple snapshots or clearly dominates a single isolated scan subtree.

## Pass 2: Timeline capture, only if needed

Use Timeline only when Sampling does not explain the elapsed time or when garbage collection, locking, I/O, task scheduling, or thread waits are suspected.

1. Attach to the same verified `w3wp.exe` PID using **Timeline**.
2. Timeline uses ETW on Windows and requires the JetBrains ETW host service with administrative privileges.
3. Collect one controlled scan and generate a `.dtt` snapshot.
4. In Timeline Viewer, apply the **Incoming HTTP Requests** interval filter and select the request corresponding to the `SO302020` scan.
5. Select the exact request interval and relevant thread-pool thread.
6. Review:
   - Running, waiting, and ready thread states
   - Garbage-collection pauses and allocation activity
   - SQL request intervals
   - File I/O
   - Lock contention
   - Tasks and asynchronous continuations
7. Scope Call Tree and Hot Spots to that interval rather than analyzing the entire worker-process lifetime.

## Symbols and source code

Managed method names should normally be visible without changing the Acumatica site. If source-line navigation for a custom assembly is required:

1. Use a PDB produced from the exact assembly build being profiled.
2. In dotTrace Viewer, configure the PDB or source path through the symbol/source settings or the **Browse for PDB** action.
3. Do not copy mismatched PDB files into the Acumatica website.
4. Do not enable debug compilation merely to improve profiler display. If a later line-level investigation is unavoidable, reproduce it separately in an isolated development instance and do not compare its timing directly with the optimized baseline.

## Evidence to retain

For each capture, retain:

- The `.dtp` or `.dtt` snapshot
- IIS application-pool name and worker PID
- Capture timestamp
- Shipment, package, barcode, and operation
- Browser-observed duration
- Functional result and label result
- Screenshot or export of the scoped Hot Spots and Call Tree
- Top methods with total time, own time, and caller path
- Any GC, lock, I/O, or waiting evidence

Profiling snapshots may expose internal assembly, method, server, and file-path information. Store and share them according to company security policy.

## Decision rule

Do not create another performance customization solely because a method executes frequently. Proceed only when the scoped dotTrace snapshots show that the method consumes meaningful CPU or elapsed time and a state-safe optimization exists.

Examples:

- If the two full split loads dominate CPU, investigate only approaches that preserve dirty cached quantities before and after `PackSplit()`.
- If `IsPackageEmpty` is negligible, do not implement package-state caching.
- If garbage collection dominates, investigate allocation sources before changing GC settings.
- If SQL intervals dominate, use Query Store and execution plans rather than changing application caching blindly.

## Verified references

- [JetBrains: Start Profiling Session](https://www.jetbrains.com/help/profiler/Starting_Local_Profiling_Session.html)
- [JetBrains: Profiling Types](https://www.jetbrains.com/help/profiler/Basic_Concepts.html)
- [JetBrains: Profile a Web Application on IIS](https://www.jetbrains.com/help/profiler/Profile_ASP_Web_Site.html)
- [JetBrains: Control a Profiling Session](https://www.jetbrains.com/help/profiler/Profiling_Guidelines__Launching_and_Controlling_the_Profiling_Process.html)
- [JetBrains: Source View and PDB Configuration](https://www.jetbrains.com/help/profiler/Source_View.html)
- [JetBrains: Incoming HTTP Request Interval Filters](https://www.jetbrains.com/help/profiler/Interval_Filters.html)
- [JetBrains: Call Tree Analysis](https://www.jetbrains.com/help/profiler/Call_Tree.html)
- [Microsoft: Identify the IIS Application Pool for w3wp.exe](https://learn.microsoft.com/en-us/troubleshoot/developer/webapps/iis/site-behavior-performance/troubleshoot-high-cpu-in-iis-app-pool)
- [Microsoft: AppCmd Worker-Process Commands](https://learn.microsoft.com/en-us/iis/get-started/getting-started-with-iis/getting-started-with-appcmdexe)
