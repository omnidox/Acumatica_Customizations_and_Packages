Below is the safest procedure for enabling temporary dual tracing on your local Acumatica IIS instance. It preserves the normal Acumatica Trace screen while also writing `PXTrace` messages to a persistent file.

## 1. Identify the active Acumatica website folder

1. Open **IIS Manager** as Administrator.
2. Expand:

```text
Sites
→ Default Web Site
→ AcumaticaERP
```

3. Select `AcumaticaERP`.
4. In the right Actions panel, click **Basic Settings**.
5. Copy the **Physical Path**.

The required file is:

```text
<Physical Path>\web.config
```

Use the active website’s physical path—not `AcumaticaERPValidation` or another customization-validation directory.

## 2. Confirm the application pool

In the same Basic Settings dialog, verify:

```text
Application Pool: DefaultAppPool
```

This identifies the Windows account that will need permission to write the trace file:

```text
IIS APPPOOL\DefaultAppPool
```

## 3. Create the trace directory

Open **Command Prompt as Administrator** and run:

```cmd
mkdir C:\AcumaticaLogs
```

Grant Modify permission to the application-pool identity:

```cmd
icacls "C:\AcumaticaLogs" /grant "IIS APPPOOL\DefaultAppPool:(OI)(CI)M"
```

A successful result should say that one file was processed successfully.

You can verify the permissions with:

```cmd
icacls "C:\AcumaticaLogs"
```

Look for an entry similar to:

```text
IIS APPPOOL\DefaultAppPool:(OI)(CI)(M)
```

## 4. Back up `web.config`

Open the website’s physical directory in File Explorer.

Copy:

```text
web.config
```

to a backup such as:

```text
web.config.before-file-tracing
```

Do not overwrite or delete the original file.

Editing `web.config` restarts the Acumatica application. Finish or stop any active testing first.

## 5. Open `web.config` as Administrator

1. Open Notepad or your preferred text editor using **Run as administrator**.
2. Open the active website’s `web.config`.
3. Search for:

```xml
<pxtrace
```

It should be inside the existing:

```xml
<px.core>
```

section.

Do not create a second `<px.core>` or `<pxtrace>` block if one already exists.

## 6. Preserve the existing configuration

Before changing anything, copy the complete existing `<pxtrace>...</pxtrace>` section into a separate text file.

Your current configuration may be as simple as:

```xml
<pxtrace defaultProvider="PXSessionTraceProvider">
  <providers>
    ...
  </providers>
</pxtrace>
```

Preserve any existing providers or attributes that are unrelated to this test.

## 7. Add the file trace provider

For dual session-and-file tracing, the resulting section should follow this structure:

```xml
<pxtrace defaultProvider="PXSessionTraceProvider">
  <providers>
    <remove name="PXSessionTraceProvider" />
    <add
      name="PXSessionTraceProvider"
      type="PX.Data.PXSessionTraceProvider, PX.Data"
      url="../Frames/Trace.aspx" />

    <remove name="PXFileTraceProvider" />
    <add
      name="PXFileTraceProvider"
      type="PX.Data.PXFileTraceProvider, PX.Data"
      file="C:\AcumaticaLogs\PickedForPackDiagnostics_20260828.txt" />
  </providers>
</pxtrace>
```

Important points:

- Keep `defaultProvider="PXSessionTraceProvider"`.
- Retain the session provider so Acumatica’s Trace screen still works.
- Add the file provider only once.
- Use an absolute local path.
- Do not add `minimumLevel` unless it already exists and is supported by your configuration.
- Keep the diagnostic filename unique so old tests do not mix with new tests.

If the existing section already defines `PXSessionTraceProvider`, do not add a duplicate registration. Add only the missing `PXFileTraceProvider` registration while preserving the existing session configuration.

## 8. Save and allow Acumatica to restart

Save `web.config`.

IIS should automatically restart the application. This can:

- Interrupt active requests.
- Clear application-level caches.
- Require you to sign in again.
- Make the first request slower while Acumatica warms its caches.

Open Acumatica again and wait until the site loads normally.

Do not profile the first warm-up request as a representative scan.

## 9. Verify that file tracing works

The file might not appear until Acumatica writes a trace message.

After publishing the diagnostic customization, execute an action that contains:

```csharp
PXTrace.WriteInformation(
    "[PFP-DIAG] File trace verification");
```

Then check:

```text
C:\AcumaticaLogs\PickedForPackDiagnostics_20260828.txt
```

Also open Acumatica’s Trace screen and confirm that the same message remains visible there. This verifies that dual tracing is working.

From an administrative PowerShell window, you can monitor the file as it grows:

```powershell
Get-Content `
  -LiteralPath "C:\AcumaticaLogs\PickedForPackDiagnostics_20260828.txt" `
  -Wait
```

Press `Ctrl+C` when finished monitoring.

## 10. Clear the test file before the controlled capture

After verifying the setup:

1. Close anything actively reading the file.
2. Stop scanning.
3. Rename the verification file:

```text
PickedForPackDiagnostics_20260828_verification.txt
```

4. Allow Acumatica to create a fresh diagnostic file when the next trace is written.

Renaming is preferable to editing the active file’s contents.

## 11. Run the diagnostic scans

Use one consolidated `[PFP-DIAG]` message for each `pickedForPack()` invocation.

Perform controlled tests such as:

1. Valid item scan.
2. Repeated scan of the same item.
3. Change package, then scan.
4. Remove or unpack an item.
5. Scan a normal-sized shipment.
6. Scan shipment `0000787`.

Record the shipment, package, action, and approximate scan time separately so each file entry can be interpreted later.

Avoid unrelated activity in the local instance during the capture.

## 12. Extract only the diagnostic entries

After testing, open PowerShell and run:

```powershell
Select-String `
  -LiteralPath "C:\AcumaticaLogs\PickedForPackDiagnostics_20260828.txt" `
  -Pattern "\[PFP-DIAG\]" |
  Select-Object -ExpandProperty Line
```

To save the filtered results:

```powershell
Select-String `
  -LiteralPath "C:\AcumaticaLogs\PickedForPackDiagnostics_20260828.txt" `
  -Pattern "\[PFP-DIAG\]" |
  Select-Object -ExpandProperty Line |
  Set-Content `
    -LiteralPath "C:\AcumaticaLogs\PickedForPackDiagnostics_20260828_filtered.txt"
```

The filtered output should show the two different contexts:

```text
Grid synchronization:
MaximumRows=1
Searches contain selected-row values

CanPack:
Different paging/search context
Requires the complete post-mutation result
```

## 13. Preserve the evidence

Copy both files into the project research area:

```text
PickedForPackDiagnostics_20260828.txt
PickedForPackDiagnostics_20260828_filtered.txt
```

Do not commit logs containing confidential information until they have been reviewed for:

- Barcodes
- Customer information
- User names
- SQL parameters
- Shipment and order information

## 14. Restore the normal trace configuration

After collecting the diagnostic evidence:

1. Stop active testing.
2. Open `web.config` as Administrator.
3. Remove the temporary `PXFileTraceProvider` registration, or restore the previously backed-up `<pxtrace>` section.
4. Save the file.
5. Allow IIS to restart Acumatica.
6. Sign back in and verify that the site functions normally.
7. Confirm that Acumatica’s normal Trace screen still works.

Do not leave file tracing enabled indefinitely. Acumatica does not automatically rotate this trace file, so it could continue growing.

## 15. Remove the temporary diagnostic code

Once the required evidence has been captured:

- Remove or deactivate the diagnostic customization.
- Restore the production Version 1 cache implementation.
- Retain the diagnostic source separately as a research artifact.
- Do not publish `[PFP-DIAG]` logging into production.

This procedure follows Acumatica’s documented `PXFileTraceProvider` approach while keeping the existing session-based Trace screen available: [Acumatica — Using Logs](https://help-2024r2.acumatica.com/Wiki/ShowWiki.aspx?PageID=36e00e6e-4cdb-4cc9-a16f-9fc2c295bd4e&wikiname=HelpRoot_Administration).