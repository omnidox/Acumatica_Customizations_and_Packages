using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.CS;
using PX.CarrierService;

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
            PXTrace.WriteInformation("[CARRIER-PKG-FILTER-RATES] PUBLIC override entered");
            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-RATES] Override invoked. ScopeActive={0}, Shipment={1}, SelectedLine={2}",
                CarrierPackageFilterScope.IsActive,
                shiporder?.ShipmentNbr,
                CarrierPackageFilterScope.CurrentSelectedPackageLineNbr);

            // If scope not active, use base unchanged
            if (!CarrierPackageFilterScope.IsActive)
            {
                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-RATES] Scope inactive. Calling base GetPackages.");
                return baseMethod(shiporder, carrier, plugin);
            }

            // Validate shipment parameter
            if (shiporder?.ShipmentNbr == null)
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-RATES] Shipment parameter is null. Calling base GetPackages.");
                return baseMethod(shiporder, carrier, plugin);
            }

            // Check if scope applies to this shipment
            if (!CarrierPackageFilterScope.AppliesToShipment(shiporder.ShipmentNbr))
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-RATES] Scope shipment mismatch. ScopeShipment={0}, MethodShipment={1}. Calling base.",
                    CarrierPackageFilterScope.CurrentShipmentNbr,
                    shiporder?.ShipmentNbr);
                return baseMethod(shiporder, carrier, plugin);
            }

            // Scope is active and applies to this shipment - filter before validation
            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-RATES] Active for shipment {0}, selected line {1}",
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
                    "[CARRIER-PKG-FILTER-RATES] Raw package count for shipment {0}: {1}",
                    shiporder.ShipmentNbr,
                    rawPackages.Count);

                // Filter to only the selected package BEFORE validation
                List<SOPackageDetailEx> filteredPackages = rawPackages
                    .Where(p => CarrierPackageFilterScope.Matches(shiporder.ShipmentNbr, p.LineNbr))
                    .ToList();

                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-RATES] Filtered package count: {0}. Lines: {1}",
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
                return ValidateAndBuildCarrierPackages(filteredPackages, carrier, plugin);
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(
                    "[CARRIER-PKG-FILTER-RATES] Exception during filtered GetPackages: {0}",
                    ex.Message);
                PXTrace.WriteError(
                    "[CARRIER-PKG-FILTER-RATES] Stack: {0}",
                    ex.StackTrace);
                throw;
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
