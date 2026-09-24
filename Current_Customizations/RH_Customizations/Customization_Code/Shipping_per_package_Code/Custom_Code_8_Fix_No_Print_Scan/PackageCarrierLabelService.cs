using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.CR;
using PX.SM;
using PX.CarrierService;
using PX.Objects.CS;

namespace PX.Objects.SO
{
    public class PackageCarrierLabelService
    {
        private readonly SOShipmentEntry _graph;

        public PackageCarrierLabelService(SOShipmentEntry graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public virtual void ValidatePackageForGeneration(SOShipment shipment, SOPackageDetailEx package)
        {
            if (shipment == null)
                throw new PXException("No shipment is currently selected.");

            if (package == null)
                throw new PXException("No package is currently selected on the Packages tab.");

            if (package.LineNbr == null)
                throw new PXException("The selected package does not have a valid line number.");

            if (string.IsNullOrWhiteSpace(shipment.ShipVia))
                throw new PXException("Ship Via is required before generating a package label.");

            if (package.Weight == null || package.Weight <= 0m)
                throw new PXException("Package weight must be greater than zero before generating a label.");
        }

        public class PreservedPackageTracking
        {
            public int? LineNbr { get; set; }
            public string TrackNumber { get; set; }
            public string TrackUrl { get; set; }
            public string TrackData { get; set; }
        }

          
        public virtual Dictionary<int, PreservedPackageTracking> CaptureTrackingForPackagesWithExistingLabels(string shipmentNbr)
        {
            var preserved = new Dictionary<int, PreservedPackageTracking>();
        
            if (string.IsNullOrWhiteSpace(shipmentNbr))
                return preserved;

            // ========================================================================
            // DIAGNOSTIC TRACE: Log capture attempt and results
            // ========================================================================
            PXTrace.WriteInformation("[CAPTURE-TRACKING] Starting capture for shipment {0}", shipmentNbr);
        
            foreach (SOPackageDetailEx package in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(_graph, shipmentNbr))
            {
                if (package?.LineNbr == null)
                    continue;
        
                FileInfo existingLabel = TryGetExistingCarrierLabel(package);
                
                // DIAGNOSTIC: Log skip/capture decision with full details
                PXTrace.WriteInformation(
                    "[CAPTURE-TRACKING] Evaluating LineNbr={0}: TrackNumber={1}, HasLabelFile={2}",
                    package.LineNbr,
                    package.TrackNumber ?? "(empty)",
                    existingLabel != null ? "YES" : "NO");

                if (existingLabel == null)
                {
                    PXTrace.WriteWarning(
                        "[CAPTURE-TRACKING] SKIP - LineNbr={0} has no label file (TrackNumber={1})",
                        package.LineNbr,
                        package.TrackNumber ?? "(empty)");
                    continue;
                }
        
                if (string.IsNullOrWhiteSpace(package.TrackNumber))
                {
                    PXTrace.WriteWarning(
                        "[CAPTURE-TRACKING] SKIP - LineNbr={0} has label file but no TrackNumber",
                        package.LineNbr);
                    continue;
                }
        
                preserved[package.LineNbr.Value] = new PreservedPackageTracking
                {
                    LineNbr = package.LineNbr,
                    TrackNumber = package.TrackNumber,
                    TrackUrl = package.TrackUrl,
                    TrackData = package.TrackData
                };

                PXTrace.WriteInformation(
                    "[CAPTURE-TRACKING] ✅ PRESERVED - LineNbr={0}, TrackNumber={1}, TrackUrl={2}, TrackData={3}",
                    package.LineNbr,
                    package.TrackNumber ?? "(empty)",
                    package.TrackUrl ?? "(empty)",
                    package.TrackData ?? "(empty)");
            }

            PXTrace.WriteInformation("[CAPTURE-TRACKING] Capture complete: {0} packages preserved out of all packages in shipment", preserved.Count);
            return preserved;
        }
          
        public virtual void RestoreTrackingForPackages(string shipmentNbr, Dictionary<int, PreservedPackageTracking> preserved)
        {
            if (string.IsNullOrWhiteSpace(shipmentNbr) || preserved == null || preserved.Count == 0)
                return;
        
            foreach (SOPackageDetailEx package in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(_graph, shipmentNbr))
            {
                if (package?.LineNbr == null)
                    continue;
        
                if (!preserved.TryGetValue(package.LineNbr.Value, out PreservedPackageTracking snapshot))
                    continue;
        
                package.TrackNumber = snapshot.TrackNumber;
                package.TrackUrl = snapshot.TrackUrl;
                package.TrackData = snapshot.TrackData;
        
                _graph.Packages.Update(package);
            }
        }
          
        public virtual FileInfo TryGetExistingCarrierLabel(SOPackageDetailEx package)
        {
            if (package == null)
                return null;

            Guid[] fileNotes = PXNoteAttribute.GetFileNotes(_graph.Packages.Cache, package);
            if (fileNotes == null || fileNotes.Length == 0)
                return null;

            UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
            string[] allowed = { ".zpl", ".zplii", ".epl", ".pdf" };

            return fileNotes
                .Select(id => upload.GetFile(id))
                .Where(f => f != null && !string.IsNullOrEmpty(f.Name))
                .FirstOrDefault(f => allowed.Any(ext => f.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
        }

        public virtual FileInfo GenerateCarrierLabelForPackage(SOShipment shipment, SOPackageDetailEx package)
        {
            ValidatePackageForGeneration(shipment, package);

            ICarrierService cs = CarrierMaint.CreateCarrierService(_graph, shipment.ShipVia);
            if (cs == null)
                throw new PXException($"Carrier service could not be created for Ship Via '{shipment.ShipVia}'.");

            // This assumes your graph extension/method exists in your environment.
            CarrierRequest cr = _graph.CarrierRatesExt.BuildRequest(shipment);

            if (cr == null)
                throw new PXException("Carrier request could not be created.");

            if (cr.Packages == null || cr.Packages.Count == 0)
                throw new PXException("The carrier request does not contain any packages.");

            var selectedCarrierPackage = cr.Packages
                .FirstOrDefault(p => object.Equals(p.RefNbr, package.LineNbr));

            if (selectedCarrierPackage == null)
                throw new PXException($"Could not match selected package line {package.LineNbr} to the carrier request packages.");

            cr.Packages = new List<CarrierBox> { selectedCarrierPackage };

            var result = cs.Ship(cr);

            if (result == null)
                throw new PXException("Carrier returned no result.");

            if (!result.IsSuccess)
            {
                string errors =
                    result.Messages == null || result.Messages.Count == 0
                        ? "Unknown carrier error."
                        : string.Join(" ", result.Messages.Select(m => $"{m.Code}:{m.Description}"));

                throw new PXException(
                    $"Carrier generation failed for package line {package.LineNbr}. {errors}"
                );
            }

            if (result.Result?.Data == null || result.Result.Data.Count == 0)
            {
                throw new PXException(
                    $"Carrier returned success but no package label data for package line {package.LineNbr}."
                );
            }

            var packageData =
                result.Result.Data.FirstOrDefault(d => object.Equals(d.RefNbr, package.LineNbr));

            if (packageData == null)
            {
                throw new PXException(
                    $"Carrier returned no matching package result for package line {package.LineNbr}.");
            }

            if (packageData.Image == null || packageData.Image.Length == 0)
            {
                throw new PXException(
                    $"Carrier returned no label image for package line {package.LineNbr}."
                );
            }

            string trackingNumber = string.IsNullOrWhiteSpace(packageData.TrackingNumber)
                ? $"PackageLine{package.LineNbr}"
                : packageData.TrackingNumber;

            string extension = string.IsNullOrWhiteSpace(packageData.Format)
                ? "pdf"
                : packageData.Format.Trim().TrimStart('.');

            string fileName = $"Label #{trackingNumber}.{extension}";

            UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
            FileInfo file = new FileInfo(fileName, null, packageData.Image);

            if (!upload.SaveFile(file))
                throw new PXException("Carrier label file could not be saved.");

            // TRACE #2: File UID verification
            PXTrace.WriteInformation(
                "[PRINT-FILE] Saved label file. UID={0}, Name={1}",
                file.UID,
                file.Name);

            AttachLabelFileToPackage(package, file);

            package.TrackNumber = packageData.TrackingNumber;
            package.TrackUrl = packageData.TrackingUrl;
            package.TrackData = packageData.TrackingData;
            
            _graph.Packages.Update(package);
            _graph.Actions.PressSave();

            return file;
        }

        public virtual void AttachLabelFileToPackage(SOPackageDetailEx package, FileInfo fileInfo)
        {
            if (package == null)
                throw new PXException("Package is required.");

            if (fileInfo?.UID == null)
                throw new PXException("A saved file is required.");

            PXNoteAttribute.SetFileNotes(_graph.Packages.Cache, package, fileInfo.UID.Value);
            _graph.Packages.Update(package);
        }

        public virtual void PrintSingleFile(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new PXException("No label file was provided for printing.");

            throw new PXRedirectToFileException(fileInfo, true);
        }

        /// <summary>
        /// Resolve the DeviceHub printer for carrier label printing.
        ///
        /// Uses Acumatica's NotificationUtility to look up the printer configured
        /// for the PrintLabels report on the current branch.
        ///
        /// Returns null if no printer is configured or resolution fails.
        /// </summary>
        public virtual Guid? ResolveDeviceHubPrinter()
        {
            try
            {
                var notificationUtility = new NotificationUtility(_graph);
                Guid? printerID = notificationUtility.SearchPrinter(
                    SONotificationSource.Customer,
                    SOReports.PrintLabels,
                    _graph.Accessinfo.BranchID);

                if (printerID.HasValue && printerID.Value != Guid.Empty)
                {
                    PXTrace.WriteInformation(
                        "[PRINT-RESOLVE] Printer resolved. PrinterID={0}",
                        printerID.Value);
                    return printerID;
                }

                PXTrace.WriteWarning("[PRINT-RESOLVE] No printer resolved for PrintLabels");
                return null;
            }
            catch (Exception ex)
            {
                PXTrace.WriteError("[PRINT-RESOLVE] Exception resolving printer: {0}", ex);
                return null;
            }
        }

        /// <summary>
        /// Queue a carrier label file to the DeviceHub printer asynchronously.
        /// Intended for the BUTTON PATH (UI context, not already inside a long operation).
        ///
        /// Uses LongOperationManager.StartAsyncOperation so the print job runs
        /// in a proper Acumatica async context with UI progress feedback.
        ///
        /// If no printer is configured:
        ///   - fallbackToDownload: true  → falls back to PXRedirectToFileException (browser download)
        ///   - fallbackToDownload: false → throws PXException with a clear error message
        /// </summary>
        public virtual void QueueLabelFileToDeviceHub(FileInfo fileInfo, bool fallbackToDownload)
        {
            if (fileInfo == null)
                throw new PXException("No label file was provided for DeviceHub printing.");

            if (!fileInfo.UID.HasValue || fileInfo.UID.Value == Guid.Empty)
                throw new PXException("Label file does not have a valid UID for DeviceHub printing.");

            Guid? printerID = ResolveDeviceHubPrinter();

            if (!printerID.HasValue)
            {
                PXTrace.WriteWarning("[QUEUE-DEVICEHUB] No DeviceHub printer configured.");

                if (fallbackToDownload)
                {
                    PXTrace.WriteWarning("[QUEUE-DEVICEHUB] Falling back to file download.");
                    PrintSingleFile(fileInfo);
                    return;
                }

                throw new PXException(
                    "Carrier label was generated, but no DeviceHub printer is configured. " +
                    "Please configure a printer for the PrintLabels report on this branch.");
            }

            Guid fileID = fileInfo.UID.Value;
            Guid resolvedPrinterID = printerID.Value;

            PXTrace.WriteInformation(
                "[QUEUE-DEVICEHUB] Queuing async DeviceHub print job. File={0}, Printer={1}",
                fileID, resolvedPrinterID);

            _graph.LongOperationManager.StartAsyncOperation(ct =>
            {
                PXTrace.WriteInformation(
                    "[QUEUE-DEVICEHUB] Async operation started for file {0}", fileID);

                SOShipmentEntry freshGraph = PXGraph.CreateInstance<SOShipmentEntry>();
                PackageCarrierLabelService svc = new PackageCarrierLabelService(freshGraph);

                return svc.SendRawFileToPrinterAsync(fileID, resolvedPrinterID, ct);
            });

            PXTrace.WriteInformation("[QUEUE-DEVICEHUB] ✅ Print job queued successfully.");
        }

        /// <summary>
        /// Send a carrier label file to the DeviceHub printer synchronously.
        /// Intended for the WMS SCAN PATH, which is already executing inside a PXLongOperation.
        ///
        /// Calls SendRawFileToPrinterAsync via GetAwaiter().GetResult() because we are already
        /// on a background thread (inside PXLongOperation.StartOperation) with no ASP.NET
        /// synchronization context, making sync-over-async safe here.
        ///
        /// IMPORTANT: Do NOT call this from a UI thread or synchronization-context-bound context.
        /// If the scan path is ever migrated to LongOperationManager.StartAsyncOperation,
        /// pass the provided CancellationToken instead of CancellationToken.None.
        ///
        /// If no printer is configured, throws PXException with a clear message.
        /// Does NOT fall back to file download — a redirect is unreliable inside WMS long operations.
        /// </summary>
        public virtual void PrintLabelFileToDeviceHubNow(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new PXException("No label file was provided for DeviceHub printing.");

            if (!fileInfo.UID.HasValue || fileInfo.UID.Value == Guid.Empty)
                throw new PXException("Label file does not have a valid UID for DeviceHub printing.");

            Guid? printerID = ResolveDeviceHubPrinter();

            if (!printerID.HasValue)
            {
                throw new PXException(
                    "Carrier label was generated, but no DeviceHub printer is configured. " +
                    "Please configure a printer for the PrintLabels report on this branch.");
            }

            PXTrace.WriteInformation(
                "[PRINT-NOW] Sending label to DeviceHub printer synchronously. File={0}, Printer={1}",
                fileInfo.UID.Value, printerID.Value);

            // TODO: If scan path migrates to StartAsyncOperation, replace CancellationToken.None
            // with the token provided by that context.
            SendRawFileToPrinterAsync(fileInfo.UID.Value, printerID.Value, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            PXTrace.WriteInformation("[PRINT-NOW] ✅ Label sent to DeviceHub printer successfully.");
        }

        /// <summary>
        /// Queue raw ZPL/EPL label file to DeviceHub printer asynchronously.
        /// 
        /// Purpose:
        /// Creates a print job in Acumatica DeviceHub instead of downloading file.
        /// This is called from button action via LongOperationManager.StartAsyncOperation.
        /// 
        /// Parameters:
        /// - fileID: The persisted file UID (must exist in UploadFileMaintenance)
        /// - printerID: The target DeviceHub printer GUID
        /// - cancellationToken: For async operation cancellation
        /// 
        /// Why persisted file retrieval is critical:
        /// - Original in-memory FileInfo may have lifetime issues in async context
        /// - Fresh graph ensures no stale cache state
        /// - Re-fetching guarantees file data is available
        /// 
        /// Safety considerations:
        /// - Uses fresh graph instance (not UI graph)
        /// - Re-fetches file from UploadFileMaintenance before printing
        /// - Validates printer ID before creating job
        /// - Comprehensive error logging for troubleshooting
        /// </summary>
        public virtual async System.Threading.Tasks.Task SendRawFileToPrinterAsync(
            Guid fileID,
            Guid? printerID,
            CancellationToken cancellationToken)
        {
            if (fileID == Guid.Empty)
                throw new PXException("File ID is required for printing.");

            if (!printerID.HasValue || printerID.Value == Guid.Empty)
                throw new PXException("Printer ID is required for DeviceHub printing.");

            PXTrace.WriteInformation(
                "[PRINT-JOB] Queuing DeviceHub print job for file {0} to printer {1}",
                fileID, printerID.Value);

            try
            {
                // ====================================================================
                // CRITICAL: Re-fetch persisted file from storage
                // Do NOT rely on original in-memory FileInfo in async context
                // ====================================================================
                UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
                FileInfo persistedFile = upload.GetFile(fileID);

                if (persistedFile == null)
                    throw new PXException($"Label file {fileID} could not be found in file storage.");

                // TRACE #4: Persisted file retrieval
                PXTrace.WriteInformation(
                    "[PRINT-ASYNC] Persisted file loaded successfully. UID={0}, Size={1}",
                    fileID,
                    persistedFile.BinData?.Length ?? 0);

                // ====================================================================
                // Create PXAdapter for DeviceHub print job
                // Minimal arguments: PrinterID and PrintWithDeviceHub
                // ====================================================================
                var adapter = new PXAdapter(PXView.Dummy.For<SOShipment>(_graph))
                {
                    MassProcess = true,
                    Arguments =
                    {
                        [nameof(IPrintable.PrinterID)] = printerID,
                        [nameof(IPrintable.PrintWithDeviceHub)] = true
                    }
                };

                // TRACE #5: Before CreatePrintJobForRawFile (MOST IMPORTANT)
                PXTrace.WriteInformation(
                    "[PRINT-JOB] Calling CreatePrintJobForRawFile. File={0}, Printer={1}",
                    fileID,
                    printerID);

                // ====================================================================
                // Queue print job using Acumatica native DeviceHub API
                // This creates the actual printer job, not just downloads file
                // ====================================================================
                await SMPrintJobMaint.CreatePrintJobForRawFile(
                    adapter,
                    delegate { return printerID; },
                    SONotificationSource.Customer,
                    SOReports.PrintLabels,
                    _graph.Accessinfo.BranchID,
                    new Dictionary<string, string>
                    {
                        { "FILEID", fileID.ToString() }
                    },
                    "Selected package carrier label",
                    cancellationToken);

                // TRACE #6: After CreatePrintJobForRawFile
                PXTrace.WriteInformation(
                    "[PRINT-JOB] CreatePrintJobForRawFile completed successfully.");

                PXTrace.WriteInformation(
                    "[PRINT-JOB] ✅ DeviceHub print job queued successfully for file {0}",
                    fileID);
            }
            catch (Exception ex)
            {
                // TRACE #7: Full exception logging with stack trace
                PXTrace.WriteError(ex);
                throw;
            }
        }
    }
}