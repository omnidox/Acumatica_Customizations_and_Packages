# dotTrace Testing Procedure for Pick, Pack, and Ship

**Created:** August 6, 2026

**Updated:** August 7, 2026 at 3:01 PM EDT

## Objective

Provide a repeatable procedure for profiling an Acumatica IIS request with JetBrains dotTrace, including process identification, controlled capture, request isolation, call-tree analysis, and safe recovery.

This procedure is intended for a development or staging server. Do not profile a shared production worker process without operational approval.

## Test environment

The following configuration was confirmed on the test computer:

- IIS server: `PCWC-LEGION`
- IIS site: `Default Web Site`
- Acumatica application: `AcumaticaERP`
- Application pool: `DefaultAppPool`
Worker-process IDs change after IIS recycling, application restarts, customization publication, and computer restarts. Always rerun `appcmd.exe list wp` immediately before attaching dotTrace.

## Install dotTrace

dotTrace must run on the computer hosting the Acumatica IIS worker process. Installing it only on a workstation that opens Acumatica in a browser is not sufficient.

1. Obtain IT approval before installing or attaching a profiler to a shared server.
2. Open the official [JetBrains dotTrace download page](https://www.jetbrains.com/profiler/download/).
3. Download either the **Windows x64 Portable** package or the Windows 64-bit standalone installer. The portable package is suitable for this test and requires no installation. JetBrains Toolbox is also supported, but is unnecessary when only dotTrace is required.
4. If using the portable package, extract the ZIP to a permanent folder such as `C:\Tools\JetBrains\dotTrace`. Do not run dotTrace from inside the ZIP.
5. Do not select or download the SDK, self-profiling API, ReSharper, command-line tools, or full dotUltimate suite unless those products are separately required.
6. Run dotTrace as administrator. For the installer edition, right-click the installer and select **Run as administrator** first.
7. Do not enable unrelated Visual Studio integrations unless they are needed.
8. At first launch, click **Start trial** to begin the 30-day evaluation, or sign in with an existing licensed JetBrains account. A license key or license server is not required for the trial.
9. Choose the **General** preset. Unity and Unreal Engine presets are not applicable.
10. Verify that the Home window exposes **New Process Run** and **Running Process** options.

```text
New Process Run
Running Process
Open Snapshot
```

## Confirm the IIS application pool

In IIS Manager:

1. Expand **Sites -> Default Web Site**.
2. Select **AcumaticaERP**.
3. Select **Basic Settings** or **Advanced Settings**.
4. Confirm that **Application Pool** is `DefaultAppPool`.

This has been confirmed for the current test environment. Repeat the check if the IIS application is moved or reconfigured.

## Important corrections to the original procedure

- Leave `<compilation debug="false">` unchanged for the baseline. Enabling debug compilation can change JIT optimization and invalidate the performance comparison.
- PDB files are optional for source-line navigation, not required to identify managed method names. Configure matching PDB/source locations in dotTrace Viewer instead of copying files into the Acumatica `bin` directory.
- Begin with **Sampling**. JetBrains recommends Sampling as the initial, low-overhead method for finding slow methods.
- **Tracing and Line-by-Line cannot be used when attaching dotTrace to an existing process.** They also add enough overhead to distort a short scan.
- Keep Acumatica Request Profiler disabled during the dotTrace capture so its SQL logging and stack collection are not included in the CPU profile.

## Test preparation

1. Use the same Acumatica build, all-enabled customization set, shipment, user, browser, and scan workflow used for the latest comparison testing.
2. Record the published customization set and keep it unchanged throughout a comparison series.
3. Complete at least one warm-up scan before collecting data. After an IIS recycle, worker termination, application restart, or customization publication, complete two warm-up scans because schema, query, formula, and JIT initialization can dominate the first request.
4. Stop or avoid unrelated processing jobs and ensure no other user is exercising the same IIS worker during the capture.
5. Record:
   - Acumatica build and tenant
   - Server name
   - Application-pool name
   - Shipment and package used
   - Barcode and operation tested
   - Test time and observed browser duration
   - Relevant Acumatica feature settings
   - Whether Acumatica Request Profiler is running
6. Keep the functional result of the scan: packed quantity, selected package, message, and label outcome.

## Identify the correct IIS worker process

Open an elevated Command Prompt on the IIS server and run:

```bat
%windir%\System32\inetsrv\appcmd.exe list wp
```

The output maps each `w3wp.exe` process ID to its application pool. Record the PID for the Acumatica instance. Do not identify the process by username alone because multiple pools can use similar identities.

If no worker process is listed for the pool, open the Acumatica site once to start it and rerun the command.

Expected output format for this environment:

```text
WP "<current PID>" (applicationPool:DefaultAppPool)
```

Confirm that the PID shown in dotTrace exactly matches the current command output. Do not rely on the previously observed PID `14592`.

### Recover an accidentally terminated IIS worker

If `w3wp.exe` is accidentally terminated, IIS normally creates a replacement when Acumatica receives another request:

1. Browse to the `AcumaticaERP` application and wait several seconds.
2. Rerun `appcmd.exe list wp` and record the new PID.
3. If the worker does not return, open **IIS Manager -> Application Pools**, select `DefaultAppPool`, and click **Start**.
4. If the pool is already started but no worker appears, recycle it once, browse to Acumatica, and rerun `appcmd`.
5. Log back in and perform another warm-up scan before profiling. Terminating the worker loses its in-memory application state and any unsaved work.

Never use **End Process**, **Kill Process**, or Task Manager to finish a profiling session. Use dotTrace **Detach** so the IIS worker remains running.

## Pass 1: Sampling capture

1. Run dotTrace as Administrator on the IIS server.
2. On the Home screen, select **Running Process**. Some versions may label this **Attach to Process**.
3. If `w3wp.exe` is not visible, select **Show All Processes** to grant the required administrative access.
4. Select the `w3wp.exe` PID that exactly matches the latest `appcmd.exe list wp` result for `DefaultAppPool`.
5. Choose **Sampling** as the profiling type.
6. Set **Control profiling** to **Manually**.
7. Set **Time measurement** to **Real time (performance counter)** when available. JetBrains identifies it as the recommended real-time measurement. **Real time (CPU instruction)** is an acceptable fallback.
8. Leave **Use safer sampling** cleared for the baseline. Enable it only if normal Sampling causes instability, because it can reduce sampling detail.
9. Clear **Collect profiling data from start**. This allows attachment and warm-up before the measurement interval begins.
10. Stop at this configuration screen during the first attempt and verify the PID and all settings before attaching.
11. Click the main **Start** button to attach to the selected process. The green plus beside a process selects or attaches to it; it is not a terminate control.
12. Wait for the dotTrace Controller window, then click its **Start** control to begin collecting data.
13. Perform exactly one controlled `scan` callback on `SO302020`.
14. Wait until the browser response, quantity update, command-state update, and any expected label action complete.
15. Immediately stop recording and generate the snapshot. Depending on the dotTrace version, this control may appear as **Get Snapshot and Wait**, **Stop Recording**, or **Drop** followed by snapshot creation. Do not press **Kill**.
16. Select **Detach** in dotTrace. Do not close or kill `w3wp.exe`.
17. Save the `.dtp` snapshot outside dotTrace's temporary storage with a descriptive name, for example:

```text
SO302020_0000787_scan_sampling_2026-08-07_01.dtp
```

Repeat this process for three separate scans, producing three snapshots. Separate captures make it easier to distinguish repeatable hot spots from one-off JIT, garbage-collection, or operating-system noise.

## Analyze each Sampling snapshot

1. Open the snapshot in dotTrace Viewer and immediately use **File -> Save Snapshot As** to retain it outside temporary storage.
2. Use **Call Tree** and **Hot Spots**. The initial display covers the entire IIS worker, including unrelated Acumatica background threads; it is not yet scoped to the scan.
3. In **Search Functions**, search for these terms individually, beginning with `ProcessSingleBarcode`:

```text
ProcessSingleBarcode
BarcodeDrivenStateMachine
PickPackShip
```

4. Select the matching scan method and use the available command resembling **Show in Call Tree**, **Focus on Method**, or **Set as Root**. Expand its descendants to isolate the scan subtree.
5. Locate the request path under methods such as:

```text
PX.BarcodeProcessing.BarcodeDrivenStateMachine.scan
PX.BarcodeProcessing.BarcodeDrivenStateMachine.ProcessSingleBarcode
PX.Data.PXAction.Press
```

6. Display **Total time** as well as **Own time** and record both for the most expensive descendants.
7. Search for and record material time under:

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

8. Use back traces or an inverted call tree to determine which caller caused each expensive framework method.
9. Do not interpret the initial all-process time as the duration of one scan. dotTrace can display accumulated activity across many IIS threads, so the value may greatly exceed the browser-observed scan time.
10. Do not treat Sampling output as an exact method-call count; Sampling estimates where CPU time is spent.
11. Capture a screenshot of the isolated scan method and its most expensive descendants.
12. If none of the scan search terms appears, repeat the capture with collection started immediately before the barcode submission and stopped immediately after all screen and label activity finishes.
13. Consider a method actionable only when it appears materially in multiple snapshots or clearly dominates a single isolated scan subtree.

## Pass 2: Full-request Timeline capture

Timeline is required when the objective is to reconcile browser/server time with work outside `ProcessSingleBarcode`, including ASP.NET request handling, page lifecycle, grid synchronization, callback rendering, waiting, garbage collection, and SQL intervals.

### Configure and capture

1. Rerun `appcmd.exe list wp` and verify the current `DefaultAppPool` PID.
2. Run dotTrace as Administrator and select that exact running `w3wp.exe` process.
3. Choose **Timeline**.
4. Set **Control profiling** to **Manually** and clear **Collect profiling data from start**.
5. Leave native-allocation collection disabled. For the first pass, also disable optional debug-output, background symbol-download, and TPL-event collection unless specifically needed; these increase snapshot size and may add overhead.
6. Use the default Timeline sampling rate. Some versions display `1000 samples/sec`; retain the default unless a controlled comparison requires otherwise.
7. Attach, wait for the controller, and prepare Acumatica on the Pack screen before recording.
8. Click **Start recording** immediately before one scan.
9. Perform exactly one barcode scan and wait for the complete callback, displayed quantities, command states, sounds, and label activity.
10. Stop recording immediately and create the snapshot. Detach from IIS; never press **Kill**.
11. Save the dotTrace performance snapshot (`.dtp`) outside temporary storage using a descriptive filename, for example:

```text
SO302020_0000787_scan_timeline_2026-08-07_01.dtp
```

### Isolate the incoming scan request

1. Open the snapshot in Timeline Viewer.
2. In the left **Filters** panel, expand **Interval Filters** and select **Incoming HTTP Requests**.
3. In **Incoming HTTP Requests: URL**, select the `SO302020.aspx` request. It may appear similar to:

```text
/AcumaticaERP/(W(5))/Pages/SO/SO302020.aspx?unum=0&HideScript=On
```

The `(W(n))` value may differ between sessions.
4. In **Incoming HTTP Requests: Method**, select `POST`.
5. Confirm the filtered duration represents one scan callback and that the Timeline shows one relevant CLR worker interval.
6. Select the highlighted request interval or its CLR worker thread. The selected request should become the root scope in **Call Tree**.

The **Thread State** filter is in the left Filters panel directly below **Interval Filters**. It contains choices such as **Not Selected**, **Running**, and **Waiting**. Record the totals first without applying a thread-state subfilter; then select Running or Waiting only when investigating that state.

### Analyze the full request

1. In Call Tree, expand the request from:

```text
PX.Web.UI.PXPage.ProcessRequest
```

2. Follow the callback-rendering path and record material branches. A typical Acumatica callback may include:

```text
GetCallbackResult
-> RenderClientData
-> CollectDataControls
-> DataBind
-> ExecuteSelect
-> scan
```

3. Expand `ProcessSingleBarcode`, but do not make it the root yet. This preserves the work occurring before and after barcode processing.
4. Search for and record these paths:

```text
OnPreLoad
PXGrid.LoadPostData
PXGrid.SyncCurrentPosition
SynchronizeGrid
pickedForPack
GetSplits
ProcessSingleBarcode
CompleteFlow
PackSplit
ShipmentState.SetNextState
CanPack
```

5. Record the entire incoming-request time, `PXPage.ProcessRequest`, Running, Waiting, `ProcessSingleBarcode`, SQL event time, and material work outside the barcode subtree.
6. Treat subsystem categories cautiously: System code, User code, GC Wait, Collections, and SQL can overlap and must not be added together.
7. Timeline instrumentation has different overhead from Sampling. Compare paths and percentages within the same Timeline capture; do not compare its absolute method times directly with Sampling values.
8. Export or transcribe both the full request tree and the `ProcessSingleBarcode` subtree. Retain a screenshot showing the active Incoming HTTP Requests, URL, Method, thread, and interval filters.

## Profiler instability and crash recovery

If `w3wp.exe` terminates or Windows displays a Just-In-Time debugger dialog while dotTrace is attached:

1. In the Visual Studio Just-In-Time dialog, choose **No, cancel debugging** unless a deliberate dump investigation has been approved. Do not attach Visual Studio casually to the production-style worker.
2. Retain Windows Application log Events 1000 and 1001 and the referenced WER dump path.
3. Browse to Acumatica to let IIS start a replacement worker, then rerun `appcmd.exe list wp` and record the new PID.
4. Warm the application with at least two scans before capturing again.
5. Retry with **Use safer sampling** enabled, or use Timeline with optional TPL/debug/native-allocation collection disabled.
6. If the crash repeats, stop profiling that worker and investigate dotTrace, Windows, and runtime compatibility before continuing.

Do not attribute a profiler-attached native crash to a customization or feature toggle without reproducing it when the profiler is not attached. A fault reported in `ntdll.dll` identifies where the exception surfaced; it does not by itself identify the root cause.

## Symbols and source code

Managed method names should normally be visible without changing the Acumatica site. If source-line navigation for a custom assembly is required:

1. Use a PDB produced from the exact assembly build being profiled.
2. In dotTrace Viewer, configure the PDB or source path through the symbol/source settings or the **Browse for PDB** action.
3. Do not copy mismatched PDB files into the Acumatica website.
4. Do not enable debug compilation merely to improve profiler display. If a later line-level investigation is unavoidable, reproduce it separately in an isolated development instance and do not compare its timing directly with the optimized baseline.

## Evidence to retain

For each capture, retain:

- The `.dtp` snapshot
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

General interpretation rules:

- Confirm a hot path in multiple warmed captures before changing code.
- Use back traces to distinguish an expensive method from a lightweight wrapper whose descendant performs the work.
- Preserve Acumatica cache, event, transaction, and current-record semantics when evaluating an optimization.
- If garbage collection dominates, investigate allocation sources before changing GC settings.
- If waiting dominates, identify the lock, I/O, SQL, or synchronization source before changing application logic.
- If SQL intervals dominate, use Acumatica Request Profiler, Query Store, and execution plans rather than changing application caching blindly.
- Retest the identical workflow after every change and compare captures made with the same profiler type and settings.

## Verified references

- [JetBrains: Download dotTrace](https://www.jetbrains.com/profiler/download/)
- [JetBrains: License and 30-Day Evaluation](https://www.jetbrains.com/help/profiler/Specifying_License_Information.html)
- [JetBrains: Run dotTrace](https://www.jetbrains.com/help/profiler/Profiling_Guidelines__Starting_a_Profiling_Session.html)
- [JetBrains: Start Profiling Session](https://www.jetbrains.com/help/profiler/Starting_Local_Profiling_Session.html)
- [JetBrains: Profiling Types](https://www.jetbrains.com/help/profiler/Basic_Concepts.html)
- [JetBrains: Profile a Web Application on IIS](https://www.jetbrains.com/help/profiler/Profile_ASP_Web_Site.html)
- [JetBrains: Control a Profiling Session](https://www.jetbrains.com/help/profiler/Profiling_Guidelines__Launching_and_Controlling_the_Profiling_Process.html)
- [JetBrains: Source View and PDB Configuration](https://www.jetbrains.com/help/profiler/Source_View.html)
- [JetBrains: Incoming HTTP Request Interval Filters](https://www.jetbrains.com/help/profiler/Interval_Filters.html)
- [JetBrains: Call Tree Analysis](https://www.jetbrains.com/help/profiler/Call_Tree.html)
- [Microsoft: Identify the IIS Application Pool for w3wp.exe](https://learn.microsoft.com/en-us/troubleshoot/developer/webapps/iis/site-behavior-performance/troubleshoot-high-cpu-in-iis-app-pool)
- [Microsoft: AppCmd Worker-Process Commands](https://learn.microsoft.com/en-us/iis/get-started/getting-started-with-iis/getting-started-with-appcmdexe)
