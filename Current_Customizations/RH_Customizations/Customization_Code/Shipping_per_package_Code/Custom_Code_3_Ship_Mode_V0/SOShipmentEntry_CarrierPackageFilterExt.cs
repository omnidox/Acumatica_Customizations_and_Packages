using System;
using System.Collections;
using PX.Data;
using PX.Objects.SO;

namespace PX.Objects.SO
{
    /// <summary>
    /// ========================================================================
    /// SOShipmentEntry Extension - Carrier Package Filter View Replacement
    /// ========================================================================
    /// 
    /// Purpose:
    /// When CarrierPackageFilterScope is active, replaces SOShipmentEntry.Packages
    /// view with a filtered version that returns ONLY the selected package.
    /// 
    /// This ensures CarrierRates.GetPackages() (called by BuildRequest) only
    /// sees the selected package, not all packages on the shipment.
    /// 
    /// Result: Only the selected package is validated for confirmation status,
    /// allowing per-package carrier label generation even if other packages
    /// are not yet confirmed.
    /// 
    /// Hook Point:
    /// Intercepts Packages view delegate when CarrierPackageFilterScope is active.
    /// 
    /// Design Pattern:
    /// Similar to SOShipmentEntry_ALPackagesFilterExt, but for native Packages view.
    /// </summary>
    public class SOShipmentEntry_CarrierPackageFilterExt : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive() => true;

        /// <summary>
        /// Capture the original Packages view on initialization.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // Replace Packages view if filter scope becomes active
            if (CarrierPackageFilterScope.IsActive)
            {
                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-EXT] Initialize called with active filter scope");

                ReplacePackagesViewIfNeeded();
            }
        }

        /// <summary>
        /// Replace the Packages view delegate with a filtered version if scope is active.
        /// </summary>
        private void ReplacePackagesViewIfNeeded()
        {
            if (!CarrierPackageFilterScope.IsActive)
            {
                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER-EXT] ReplacePackagesViewIfNeeded: No active filter scope, skipping replacement");
                return;
            }

            if (!Base.Views.ContainsKey("Packages"))
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-EXT] Packages view not found in Base.Views");
                return;
            }

            PXView originalView = Base.Views["Packages"];

            if (originalView == null)
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-EXT] Original Packages view is null");
                return;
            }

            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-EXT] ✅ Replacing Packages view with filtered delegate");
            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-EXT] Original BqlSelect type: {0}",
                originalView.BqlSelect?.GetType().FullName ?? "null");

            // Replace view with filtered delegate, preserving original BQL
            Base.Views["Packages"] = new PXView(
                Base,
                true,
                originalView.BqlSelect,
                new PXSelectDelegate(() => FilteredPackagesDelegate(originalView)));

            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-EXT] ✅ Packages view successfully replaced");
        }

        /// <summary>
        /// Filtered view delegate that yields only the selected package.
        /// </summary>
        protected virtual IEnumerable FilteredPackagesDelegate(PXView originalView)
        {
            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-EXT.Delegate] FilteredPackagesDelegate called");

            if (!CarrierPackageFilterScope.IsActive)
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-EXT.Delegate] Filter scope NOT active. Returning no packages (fail-safe)");
                yield break;
            }

            string filterShipmentNbr = CarrierPackageFilterScope.ShipmentNbr;
            int? filterPackageLineNbr = CarrierPackageFilterScope.SelectedPackageLineNbr;

            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-EXT.Delegate] Filter active: shipment={0}, selectedLineNbr={1}",
                filterShipmentNbr, filterPackageLineNbr);

            SOShipment currentShipment = Base.Document.Current;
            if (currentShipment == null)
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-EXT.Delegate] Current shipment is null. Returning no packages (fail-safe)");
                yield break;
            }

            string currentShipmentNbr = currentShipment.ShipmentNbr;

            if (!string.Equals(currentShipmentNbr, filterShipmentNbr, StringComparison.OrdinalIgnoreCase))
            {
                PXTrace.WriteWarning(
                    "[CARRIER-PKG-FILTER-EXT.Delegate] Shipment mismatch: current={0}, filter={1}. Returning no packages (fail-safe)",
                    currentShipmentNbr, filterShipmentNbr);
                yield break;
            }

            // Yield only rows matching the selected package LineNbr
            int filteredCount = 0;
            int totalCount = 0;

            foreach (object rowObj in originalView.SelectMultiBound(new object[] { currentShipment }))
            {
                // Unwrap SOPackageDetailEx from the row (handles both simple and PXResult rows)
                SOPackageDetailEx package = PXResult.Unwrap<SOPackageDetailEx>(rowObj);
                if (package == null)
                {
                    PXTrace.WriteInformation(
                        "[CARRIER-PKG-FILTER-EXT.Delegate] Row does not contain SOPackageDetailEx, skipping");
                    continue;
                }

                totalCount++;

                if (CarrierPackageFilterScope.Matches(currentShipmentNbr, package.LineNbr))
                {
                    PXTrace.WriteInformation(
                        "[CARRIER-PKG-FILTER-EXT.Delegate] ✅ Package LineNbr={0} MATCHES filter, yielding row type {1}",
                        package.LineNbr, rowObj.GetType().Name);

                    yield return rowObj;  // ✅ Yield the entire row (PXResult), not just the package
                    filteredCount++;
                }
                else
                {
                    PXTrace.WriteInformation(
                        "[CARRIER-PKG-FILTER-EXT.Delegate] Package LineNbr={0} does NOT match filter, skipping",
                        package.LineNbr);
                }
            }

            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER-EXT.Delegate] ✅ Filtered delegate complete. Yielded {0} out of {1} package(s)",
                filteredCount, totalCount);
        }
    }
}
