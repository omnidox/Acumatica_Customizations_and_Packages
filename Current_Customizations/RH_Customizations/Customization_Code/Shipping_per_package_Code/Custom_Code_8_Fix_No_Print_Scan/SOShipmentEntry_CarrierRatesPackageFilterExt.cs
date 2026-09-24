using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.CS;
using PX.CarrierService;
using PX.SM;

namespace PX.Objects.SO
{
    /// <summary>
    /// ========================================================================
    /// SOShipmentEntry.CarrierRates Extension - Per-Package Carrier Label Filter
    /// ========================================================================
    /// 
    /// Purpose:
    /// Override CarrierRates.GetPackages() to filter packages BEFORE confirmation
    /// validation. This is the critical path that throws:
    ///   "Confirmation for each and every Package is required"
    /// 
    /// When CarrierPackageFilterScope is active, we:
    /// 1. Query all packages for the shipment
    /// 2. Filter to only the selected package (BEFORE validation)
    /// 3. Validate and build carrier boxes for only that package
    /// 4. Return the filtered list so BuildRequest sees only one package
    /// 
    /// Result: Only the selected package is validated/printed, not all packages.
    /// 
    /// Architecture:
    /// This extension sits between PackageCarrierLabelService.GenerateCarrierLabelForPackage()
    /// and the confirmation validation logic inside GetPackages().
    /// 
    /// Hook Point:
    /// CarrierRates.GetPackages(SOShipment, Carrier, CarrierPlugin)
    /// 
    /// Design Pattern:
    /// PXGraphExtension on nested CarrierRates class within SOShipmentEntry
    /// </summary>
    public class SOShipmentEntry_CarrierRatesPackageFilterExt : PXGraphExtension<SOShipmentEntry.CarrierRates, SOShipmentEntry>
    {
        public static bool IsActive() => true;

        public delegate IList<CarrierBox> GetPackagesDelegate(
            SOShipment shiporder,
            Carrier carrier,
            CarrierPlugin plugin);

        /// <summary>
        /// Override GetPackages to filter packages BEFORE confirmation validation.
        /// 
        /// If CarrierPackageFilterScope is active for this shipment:
        ///   - Query packages
        ///   - Filter to selected package
        ///   - Validate only the selected package
        ///   - Build carrier boxes only for selected package
        ///   - Return filtered list
        /// 
        /// If inactive or shipment mismatch:
        ///   - Call base method unchanged
        /// </summary>
        [PXOverride]
        public virtual IList<CarrierBox> GetPackages(
            SOShipment shiporder,
            Carrier carrier,
            CarrierPlugin plugin,
            GetPackagesDelegate baseMethod)
        {
            // ========================================================================
            // DIAGNOSTIC TRACE: Log every call to GetPackages and scope state
            // ========================================================================
            PXTrace.WriteInformation(
                "[GET-PACKAGES-OVERRIDE] CALLED. ScopeActive={0}, Shipment={1}, SelectedLine={2}, Carrier={3}",
                CarrierPackageFilterScope.IsActive,
                shiporder?.ShipmentNbr ?? "(null)",
                CarrierPackageFilterScope.CurrentSelectedPackageLineNbr,
                carrier?.CarrierID ?? "(null)");

            // ========================================================================
            // PATH 1: Scope is ACTIVE - Manual print or WMS scan path
            // ========================================================================
            if (CarrierPackageFilterScope.IsActive)
            {
                PXTrace.WriteInformation(
                    "[GET-PACKAGES-OVERRIDE] Scope active, preserving selected-package behavior");

                // Validate shipment parameter
                if (shiporder?.ShipmentNbr == null)
                {
                    PXTrace.WriteWarning(
                        "[GET-PACKAGES-OVERRIDE] Shipment parameter is null. Calling base GetPackages.");
                    return baseMethod(shiporder, carrier, plugin);
                }

                // Check if scope applies to this shipment
                if (!CarrierPackageFilterScope.AppliesToShipment(shiporder.ShipmentNbr))
                {
                    PXTrace.WriteWarning(
                        "[GET-PACKAGES-OVERRIDE] Scope shipment mismatch. ScopeShipment={0}, MethodShipment={1}. Calling base.",
                        CarrierPackageFilterScope.CurrentShipmentNbr,
                        shiporder?.ShipmentNbr);
                    return baseMethod(shiporder, carrier, plugin);
                }

                // Scope is active and applies to this shipment - filter before validation
                PXTrace.WriteInformation(
                    "[GET-PACKAGES-OVERRIDE] Scope is ACTIVE and applies to shipment {0}, selected line {1}",
                    CarrierPackageFilterScope.CurrentShipmentNbr,
                    CarrierPackageFilterScope.CurrentSelectedPackageLineNbr);

                try
                {
                    // Query all packages for shipment
                    List<SOPackageDetailEx> rawPackages = PXSelect<
                        SOPackageDetailEx,
                        Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                        .Select(Base, shiporder.ShipmentNbr)
                        .RowCast<SOPackageDetailEx>()
                        .ToList();

                    PXTrace.WriteInformation(
                        "[GET-PACKAGES-OVERRIDE] Raw package count for shipment {0}: {1}",
                        shiporder.ShipmentNbr,
                        rawPackages.Count);

                    // Filter to only the selected package BEFORE validation
                    List<SOPackageDetailEx> filteredPackages = rawPackages
                        .Where(p => CarrierPackageFilterScope.Matches(shiporder.ShipmentNbr, p.LineNbr))
                        .ToList();

                    PXTrace.WriteInformation(
                        "[GET-PACKAGES-OVERRIDE] Filtered package count: {0}. Lines: {1}",
                        filteredPackages.Count,
                        filteredPackages.Count > 0 ? string.Join(",", filteredPackages.Select(p => p.LineNbr)) : "(none)");

                    // Defensive: If scope is active but no package matches, fail safely
                    if (filteredPackages.Count == 0)
                    {
                        throw new PXException(
                            "Carrier package filter was active for shipment {0}, line {1}, but no matching package was found.",
                            CarrierPackageFilterScope.CurrentShipmentNbr,
                            CarrierPackageFilterScope.CurrentSelectedPackageLineNbr);
                    }

                    // Validate only the filtered packages (not all packages)
                    var selectedPackageResult = ValidateAndBuildCarrierPackages(filteredPackages, carrier, plugin);
                    // ========================================================================
                    // CRITICAL: Log package count returned from filtered list
                    // ========================================================================
                    PXTrace.WriteInformation(
                        "[GET-PACKAGES-OVERRIDE] ✅ Returning {0} CarrierBox item(s) from filtered packages (scope was active)",
                        selectedPackageResult?.Count ?? 0);
                    return selectedPackageResult;
                }
                catch (Exception ex)
                {
                    PXTrace.WriteError(
                        "[GET-PACKAGES-OVERRIDE] Exception during filtered GetPackages: {0}",
                        ex.Message);
                    PXTrace.WriteError(
                        "[GET-PACKAGES-OVERRIDE] Stack: {0}",
                        ex.StackTrace);
                    throw;
                }
            }

            // ========================================================================
            // PATH 2: Scope is INACTIVE - Check for Confirm Shipment filter
            // ========================================================================
            PXTrace.WriteInformation("[GET-PACKAGES-OVERRIDE] Scope is INACTIVE");

            // SAFE CHECK: Only filter if ConfirmShipmentCarrierFilterScope is active
            // This prevents filtering during other carrier operations
            if (ConfirmShipmentCarrierFilterScope.IsActive)
            {
                PXTrace.WriteInformation("[CONFIRM-CARRIER-FILTER] Processing Confirm Shipment carrier flow");

                // Validate shipment parameter
                if (shiporder?.ShipmentNbr == null)
                {
                    PXTrace.WriteWarning(
                        "[CONFIRM-CARRIER-FILTER] Shipment parameter is null. Calling base GetPackages.");
                    return baseMethod(shiporder, carrier, plugin);
                }

                // Validate scope applies to this shipment
                if (!ConfirmShipmentCarrierFilterScope.AppliesToShipment(shiporder.ShipmentNbr))
                {
                    PXTrace.WriteWarning(
                        "[CONFIRM-CARRIER-FILTER] Scope shipment mismatch. ScopeShipment={0}, MethodShipment={1}. Calling base.",
                        ConfirmShipmentCarrierFilterScope.CurrentShipmentNbr,
                        shiporder?.ShipmentNbr);
                    return baseMethod(shiporder, carrier, plugin);
                }

                try
                {
                    // Query ALL packages for the shipment
                    List<SOPackageDetailEx> allPackages = PXSelect<
                        SOPackageDetailEx,
                        Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                        .Select(Base, shiporder.ShipmentNbr)
                        .RowCast<SOPackageDetailEx>()
                        .ToList();

                    PXTrace.WriteInformation(
                        "[CONFIRM-CARRIER-FILTER] Raw package count: {0}",
                        allPackages.Count);

                    // ========================================================================
                    // PREVENTIVE FIX: Filter to packages that DON'T have tracking yet
                    // CRITICAL: Evaluate IsAlreadyCarrierLabeled ONCE per package to avoid
                    // side effects (TrackNumber recovery) being executed twice
                    // ========================================================================
                    var packageStatuses = allPackages
                        .Select(p => new
                        {
                            Package = p,
                            AlreadyLabeled = IsAlreadyCarrierLabeled(p)
                        })
                        .ToList();

                    List<SOPackageDetailEx> alreadyLabeledPackages = packageStatuses
                        .Where(x => x.AlreadyLabeled)
                        .Select(x => x.Package)
                        .ToList();

                    List<SOPackageDetailEx> packagesNeedingTracking = packageStatuses
                        .Where(x => !x.AlreadyLabeled)
                        .Select(x => x.Package)
                        .ToList();

                    PXTrace.WriteInformation(
                        "[CONFIRM-CARRIER-FILTER] Already labeled package count: {0}",
                        alreadyLabeledPackages.Count);

                    PXTrace.WriteInformation(
                        "[CONFIRM-CARRIER-FILTER] Missing tracking package count: {0}",
                        packagesNeedingTracking.Count);

                    // Log each package's action
                    foreach (var pkg in alreadyLabeledPackages)
                    {
                        PXTrace.WriteInformation(
                            "[CONFIRM-CARRIER-FILTER] LineNbr={0}, TrackNumber={1}, Action=SKIP_ALREADY_LABELED",
                            pkg.LineNbr,
                            pkg.TrackNumber ?? "(empty)");
                    }

                    foreach (var pkg in packagesNeedingTracking)
                    {
                        PXTrace.WriteInformation(
                            "[CONFIRM-CARRIER-FILTER] LineNbr={0}, TrackNumber=<empty>, Action=INCLUDE_MISSING_TRACKING",
                            pkg.LineNbr);
                    }

                    // ========================================================================
                    // KEY DECISION: If all packages already have tracking, return zero items
                    // ========================================================================
                    if (packagesNeedingTracking.Count == 0)
                    {
                        PXTrace.WriteInformation(
                            "[CONFIRM-CARRIER-FILTER] All packages are already carrier-labeled. Returning 0 CarrierBox items to prevent FedEx regeneration.");
                        return new List<CarrierBox>(); // Return empty list - FedEx will not be called
                    }

                    // ========================================================================
                    // Otherwise, process only packages that need tracking
                    // ========================================================================
                    var filteredResult = ValidateAndBuildCarrierPackages(packagesNeedingTracking, carrier, plugin);

                    PXTrace.WriteInformation(
                        "[CONFIRM-CARRIER-FILTER] Returning {0} CarrierBox item(s)",
                        filteredResult?.Count ?? 0);

                    return filteredResult;
                }
                catch (Exception ex)
                {
                    PXTrace.WriteError(
                        "[CONFIRM-CARRIER-FILTER] Exception during Confirm Shipment carrier filtering: {0}",
                        ex.Message);
                    PXTrace.WriteError(
                        "[CONFIRM-CARRIER-FILTER] Stack: {0}",
                        ex.StackTrace);
                    throw;
                }
            }

            // ========================================================================
            // Confirm Shipment filter NOT active - use base behavior unchanged
            // ========================================================================
            PXTrace.WriteInformation(
                "[GET-PACKAGES-OVERRIDE] ConfirmShipmentCarrierFilterScope is INACTIVE - calling base GetPackages");
            var baseResult = baseMethod(shiporder, carrier, plugin);
            PXTrace.WriteWarning(
                "[GET-PACKAGES-OVERRIDE] Base method returned {0} CarrierBox items",
                baseResult?.Count ?? 0);
            return baseResult;
        }

        /// <summary>
        /// Helper: Determine if a package already has carrier tracking.
        /// 
        /// Returns true if EITHER:
        /// 1. SOPackageDetailEx.TrackNumber is populated (primary indicator)
        /// 2. An existing carrier label file is found attached to the package
        /// 
        /// If a label file exists but TrackNumber is blank, attempts to recover
        /// the tracking number from the label filename and populate package.TrackNumber.
        /// 
        /// Filename format: "Label #TRACKINGNUMBER.ext"
        /// Example: "Label #123456789012.zpl" → recovered TrackNumber = "123456789012"
        /// </summary>
        private bool IsAlreadyCarrierLabeled(SOPackageDetailEx package)
        {
            if (package == null)
                return false;

            // ====================================================================
            // CASE 1: TrackNumber already exists - PRIMARY INDICATOR
            // ====================================================================
            if (!string.IsNullOrWhiteSpace(package.TrackNumber))
            {
                PXTrace.WriteInformation(
                    "[ALREADY-LABELED] LineNbr={0}: Package skipped because TrackNumber already exists: {1}",
                    package.LineNbr,
                    package.TrackNumber);
                return true;
            }

            // ====================================================================
            // CASE 2: Check for existing carrier label file attached to package
            // ====================================================================
            PX.SM.FileInfo existingLabel = TryGetExistingCarrierLabel(package);

            if (existingLabel != null)
            {
                PXTrace.WriteInformation(
                    "[ALREADY-LABELED] LineNbr={0}: Found existing label file: {1}",
                    package.LineNbr,
                    existingLabel.Name);

                // Attempt to recover tracking number from filename
                string recoveredTrackNumber = TryRecoverTrackingNumberFromLabelFilename(existingLabel.Name);

                if (!string.IsNullOrWhiteSpace(recoveredTrackNumber))
                {
                    // Set the recovered tracking number on the package
                    package.TrackNumber = recoveredTrackNumber;
                    
                    // Update the cache so the value is available throughout this graph operation.
                    // NOTE: Final persistence is handled by the parent Confirm Shipment flow
                    // via native Acumatica save operations. This Update() marks it dirty in the
                    // cache so it will be included when the shipment is saved. Do NOT call
                    // PressSave() here - that happens in the outer flow.
                    Base.Packages.Update(package);

                    PXTrace.WriteInformation(
                        "[ALREADY-LABELED] LineNbr={0}: TrackNumber recovered from filename and populated: {1}",
                        package.LineNbr,
                        recoveredTrackNumber);

                    PXTrace.WriteInformation(
                        "[ALREADY-LABELED] LineNbr={0}: Package cache updated with recovered TrackNumber",
                        package.LineNbr);
                }
                else
                {
                    // File exists but tracking number could not be recovered
                    PXTrace.WriteWarning(
                        "[ALREADY-LABELED] LineNbr={0}: Label file exists but TrackNumber could not be recovered from filename: {1}",
                        package.LineNbr,
                        existingLabel.Name);
                }

                // Either way, the package is already labeled - do not send to FedEx again
                PXTrace.WriteInformation(
                    "[ALREADY-LABELED] LineNbr={0}: Package skipped because label file already exists. Avoiding duplicate FedEx generation.",
                    package.LineNbr);
                return true;
            }

            // ====================================================================
            // CASE 3: No TrackNumber and no existing label file
            // ====================================================================
            PXTrace.WriteInformation(
                "[ALREADY-LABELED] LineNbr={0}: Package needs carrier generation (no TrackNumber, no label file)",
                package.LineNbr);
            return false;
        }

        /// <summary>
        /// Extract tracking number from carrier label filename.
        /// 
        /// Expected format: "Label #TRACKINGNUMBER.ext"
        /// Examples:
        ///   "Label #123456789012.zpl" → "123456789012"
        ///   "Label #FDX98765.zplii" → "FDX98765"
        ///   "Label #ABC.pdf" → "ABC"
        /// 
        /// Returns null or empty string if format does not match.
        /// This is a best-effort recovery - if it fails, the package is still
        /// treated as already-labeled to prevent duplicate FedEx generation.
        /// </summary>
        private string TryRecoverTrackingNumberFromLabelFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return null;

            try
            {
                // Remove directory path if present (just in case)
                int lastSlash = filename.LastIndexOf('\\');
                if (lastSlash >= 0)
                    filename = filename.Substring(lastSlash + 1);

                lastSlash = filename.LastIndexOf('/');
                if (lastSlash >= 0)
                    filename = filename.Substring(lastSlash + 1);

                // Expected format: "Label #TRACKINGNUMBER.ext"
                // Find "Label #" prefix
                string prefix = "Label #";
                if (!filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    PXTrace.WriteWarning(
                        "[TRACK-RECOVERY] Filename does not match expected 'Label #' format: {0}",
                        filename);
                    return null;
                }

                // Extract part after "Label #"
                string afterPrefix = filename.Substring(prefix.Length);

                // Find last dot (file extension)
                int lastDot = afterPrefix.LastIndexOf('.');
                if (lastDot < 0)
                {
                    // No extension found - use entire string
                    PXTrace.WriteWarning(
                        "[TRACK-RECOVERY] No file extension found in filename: {0}",
                        filename);
                    return afterPrefix;
                }

                // Extract tracking number (everything between "Label #" and extension)
                string trackingNumber = afterPrefix.Substring(0, lastDot).Trim();

                if (string.IsNullOrWhiteSpace(trackingNumber))
                {
                    PXTrace.WriteWarning(
                        "[TRACK-RECOVERY] Extracted tracking number is empty from filename: {0}",
                        filename);
                    return null;
                }

                PXTrace.WriteInformation(
                    "[TRACK-RECOVERY] Successfully recovered tracking number from filename '{0}': {1}",
                    filename,
                    trackingNumber);

                return trackingNumber;
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(
                    "[TRACK-RECOVERY] Exception while parsing label filename '{0}': {1}",
                    filename,
                    ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get existing carrier label file for a package.
        /// Reimplemented here to avoid dependency on PackageCarrierLabelService
        /// and to keep logic scoped within the filter.
        /// 
        /// Only returns files that:
        /// 1. Start with "Label #" (case-insensitive) - our standard naming format
        /// 2. Have recognized carrier label extension: .zpl, .zplii, .epl, .pdf
        /// 
        /// This prevents random attached PDFs from causing packages to be skipped.
        /// Only files matching our generated label format are considered carrier labels.
        /// </summary>
        private PX.SM.FileInfo TryGetExistingCarrierLabel(SOPackageDetailEx package)
        {
            if (package == null)
                return null;

            try
            {
                Guid[] fileNotes = PXNoteAttribute.GetFileNotes(Base.Packages.Cache, package);
                if (fileNotes == null || fileNotes.Length == 0)
                    return null;

                UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
                string[] allowed = { ".zpl", ".zplii", ".epl", ".pdf" };

                return fileNotes
                    .Select(id => upload.GetFile(id))
                    .Where(f => f != null && !string.IsNullOrEmpty(f.Name))
                    .FirstOrDefault(f =>
                        f.Name.StartsWith("Label #", StringComparison.OrdinalIgnoreCase)
                        && allowed.Any(ext => f.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                PXTrace.WriteWarning(
                    "[GET-EXISTING-LABEL] Exception retrieving label file for LineNbr={0}: {1}",
                    package.LineNbr,
                    ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Validate filtered packages and build carrier boxes.
        /// This is the core logic from the original GetPackages but operating on filtered packages only.
        /// </summary>
        private IList<CarrierBox> ValidateAndBuildCarrierPackages(
            List<SOPackageDetailEx> packages,
            Carrier carrier,
            CarrierPlugin plugin)
        {
            List<CarrierBox> list = new List<CarrierBox>();

            // Get carrier package mappings
            var carrierPackageDetail = GetCarrierPackageDetail(packages, carrier.CarrierID);

            foreach (SOCarrierPackageDetailEx pkgDetail in carrierPackageDetail)
            {
                SOPackageDetailEx package = pkgDetail.Package;

                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-RATES] Validating selected package LineNbr={0}, Confirmed={1}",
                    package.LineNbr,
                    package.Confirmed);

                // Validate confirmation status (only for the filtered package)
                if (carrier.ConfirmationRequired == true)
                {
                    if (package.Confirmed != true)
                    {
                        PXTrace.WriteWarning(
                            "[CARRIER-PKG-FILTER-RATES] Selected package LineNbr={0} is not confirmed",
                            package.LineNbr);
                        Base.Packages.Cache.RaiseExceptionHandling<SOPackageDetail.confirmed>(
                            package,
                            package.Confirmed,
                            new PXSetPropertyException(Messages.ConfirmationIsRequired, PXErrorLevel.Error));
                        throw new PXException(Messages.ConfirmationIsRequired);
                    }
                }

                // Build carrier box for this package
                CarrierBox box = Base1.BuildCarrierPackage(pkgDetail, plugin);
                list.Add(box);

                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-RATES] Built CarrierBox for LineNbr={0}",
                    package.LineNbr);
            }

            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-RATES] Returning {0} CarrierBox item(s)",
                list.Count);

            return list;
        }

        /// <summary>
        /// Get carrier package mappings (carriers that support the box types).
        /// Reimplemented from private method in base CarrierRates class.
        /// </summary>
        private List<SOCarrierPackageDetailEx> GetCarrierPackageDetail(
            List<SOPackageDetailEx> packages,
            string carrierID)
        {
            List<SOCarrierPackageDetailEx> sOCarrierPackages = new List<SOCarrierPackageDetailEx>();

            // Query carrier package types
            var carrierPackages = PXSelect<
                CarrierPackage,
                Where<CarrierPackage.carrierID, Equal<Required<CarrierPackage.carrierID>>>>
                .Select(Base, carrierID)
                .RowCast<CarrierPackage>()
                .AsEnumerable();

            // Map each package to its carrier box
            foreach (SOPackageDetailEx package in packages)
            {
                SOCarrierPackageDetailEx box = new SOCarrierPackageDetailEx();
                box.CarrierID = carrierID;
                box.CarrierBoxName = carrierPackages
                    .Where(x => x.BoxID.Equals(package.BoxID))
                    .Select(y => y.CarrierBox)
                    .FirstOrDefault();
                box.Package = package;

                sOCarrierPackages.Add(box);

                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-RATES] Mapped package LineNbr={0} to CarrierBox={1}",
                    package.LineNbr,
                    box.CarrierBoxName ?? "(default)");
            }

            return sOCarrierPackages;
        }
    }
}
