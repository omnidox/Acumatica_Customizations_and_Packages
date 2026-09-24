using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    /// <summary>
    /// Generic Asgard view filter extension that intercepts ALPackages and
    /// ALiStarPackages without replacing Acumatica's editable Packages grid view.
    /// 
    /// CRITICAL: This extension must filter the ACTUAL view that the Asgard model uses (BasedOnView).
    /// For models based on ALiStarPackages (a joined view with multiple tables), we must:
    /// 1. Intercept the ALiStarPackages view (not just ALPackages)
    /// 2. Filter the PXResult rows to match the selected package
    /// 3. Let Asgard receive the proper joined row structure (PXResult<SOPackageDetail, SOShipment, SOOrder, CSBox, InventoryItem>)
    /// 
    /// The filter works by:
    /// - Capturing the original view in Initialize()
    /// - Replacing it with a filtered delegate that checks ALPackagesFilterScope
    /// - Yielding only the row matching the selected package
    /// </summary>
    public class SOShipmentEntry_AsgardViewFilterExt : PXGraphExtension<SOShipmentEntry>
    {
        private static readonly bool DetailedDiagnostics = false;

        private static void WriteDiagnostic(string message, params object[] args)
        {
            if (DetailedDiagnostics)
                PXTrace.WriteInformation(message, args);
        }

        private PXView _originalALPackagesView;
        private PXView _originalALiStarPackagesView;

        public static bool IsActive()
        {
            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Do not replace the native Packages view: constructing it as a read-only
            // PXView prevents users from changing package-grid checkboxes.
            FilterViewIfExists("ALPackages");
            FilterViewIfExists("ALiStarPackages");
        }

        /// <summary>
        /// Generic method to filter any Asgard view by name.
        /// Replaces the original view with a filtered delegate that checks ALPackagesFilterScope.
        /// </summary>
        private void FilterViewIfExists(string viewName)
        {
            if (!Base.Views.ContainsKey(viewName))
            {
                WriteDiagnostic("[VIEW-FILTER] View '{0}' does not exist in SOShipmentEntry", viewName);
                return;
            }

            PXView originalView = Base.Views[viewName];
            if (originalView == null)
            {
                WriteDiagnostic("[VIEW-FILTER] View '{0}' is NULL", viewName);
                return;
            }

            // Store reference based on view name
            if (viewName == "ALPackages")
                _originalALPackagesView = originalView;
            else if (viewName == "ALiStarPackages")
                _originalALiStarPackagesView = originalView;

            // Replace with filtered delegate
            // Use a closure to capture the viewName and originalView
            Base.Views[viewName] = new PXView(
                Base,
                true,
                originalView.BqlSelect,
                new PXSelectDelegate(() => FilteredAsgardView(viewName, originalView)));

            WriteDiagnostic("[VIEW-FILTER] ✅ Base.Views['{0}'] replaced with filtered view", viewName);
            WriteDiagnostic("[VIEW-FILTER] Original BqlSelect type: {0}", 
                originalView.BqlSelect?.GetType().FullName ?? "null");
        }

        /// <summary>
        /// Generic filtered view delegate for any Asgard view.
        /// Yields only the package row that matches ALPackagesFilterScope.
        /// </summary>
        protected virtual IEnumerable FilteredAsgardView(string viewName, PXView originalView)
        {
            if (originalView == null)
                yield break;

            object[] currents = new object[] { Base.Document.Current };
            IEnumerable rawRows = originalView.SelectMultiBound(currents);

            if (!ALPackagesFilterScope.IsActive)
            {
                WriteDiagnostic("[FILTER-{0}] ALPackagesFilterScope is NOT active - returning all rows", viewName);
                foreach (object row in rawRows)
                    yield return row;
                yield break;
            }

            WriteDiagnostic("[FILTER-{0}] ALPackagesFilterScope IS active for shipment {1}", 
                viewName, ALPackagesFilterScope.ShipmentNbr);

            string currentShipmentNbr = Base.Document.Current?.ShipmentNbr;
            int filteredCount = 0;
            int totalCount = 0;

            foreach (object row in rawRows)
            {
                // Unwrap SOPackageDetail from the row (works for both ALPackages and ALiStarPackages)
                SOPackageDetail package = PXResult.Unwrap<SOPackageDetail>(row);
                if (package == null)
                    continue;

                totalCount++;

                if (!ALPackagesFilterScope.Matches(currentShipmentNbr, package.LineNbr))
                {
                    WriteDiagnostic("[FILTER-{0}] Package LineNbr={1} does NOT match filter", 
                        viewName, package.LineNbr);
                    continue;
                }

                filteredCount++;
                WriteDiagnostic("[FILTER-{0}] Package LineNbr={1} MATCHES filter - yielding row type {2}", 
                    viewName, package.LineNbr, row.GetType().Name);
                yield return row;  // ✅ Yield the entire row (PXResult), not just the package
            }

            PXTrace.WriteInformation("[ASGARD-FILTER] View={0}, selected {1} of {2} package rows", viewName, filteredCount, totalCount);
        }
    }
}
