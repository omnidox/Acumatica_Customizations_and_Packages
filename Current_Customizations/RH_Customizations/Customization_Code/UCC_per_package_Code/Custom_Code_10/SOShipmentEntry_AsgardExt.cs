using System;
using System.Collections;
using System.Linq;
using AA.Objects.Labels;
using Asgard.Labels.Abstractions.Interface;
using Asgard.Labels.Impl.Context;
using Asgard.Labels.Impl.Poco;
using PX.Data;
using PX.Data.DependencyInjection;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    public class SOShipmentEntry_AsgardExt : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive()
        {
            return true;
        }

        [InjectDependency]
        private ILabelGenerator<IAcuLabelContext> _labelGenerator { get; set; }

        public PXAction<SOShipment> PrintAsgardPackageLabel;

        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Print Asgard Label", Visible = true, Enabled = true)]
        protected virtual IEnumerable printAsgardPackageLabel(PXAdapter adapter)
        {
            // Delegate to the action method
            PrintForPackage(adapter);
            return adapter.Get();
        }

        /// <summary>
        /// Core action method - called by BOTH button AND scan trigger
        /// This is the single source of truth for print logic
        ///
        /// NEW: Accepts optional selectedPackageLineNbr parameter for row-selection printing
        /// If null, prints the currently selected package in the grid
        /// </summary>
        public virtual void PrintForPackage(PXAdapter adapter, int? selectedPackageLineNbr = null)
        {
            SOShipment shipment = Base.Document.Current;

            if (shipment == null)
                throw new PXException("No shipment is currently selected.");

            // ✅ CRITICAL: Capture selected package BEFORE PressSave()
            // PressSave can disturb Base.Packages.Current, so we must capture it first
            SOPackageDetail currentSelected = Base.Packages.Current;
            int? selectedBeforeSave = selectedPackageLineNbr ?? currentSelected?.LineNbr;

            if (selectedBeforeSave == null)
                throw new PXException(
                    "No package is selected. Please select a package row and try again.");

            // ✅ NEW: Log all available information from the selected package
            // This provides complete visibility into what fields are available during Phase 1 testing
            if (currentSelected != null)
            {
                PXTrace.WriteInformation("[PKG-ALL] === All Available Fields from SOPackageDetail ===");
                
                var properties = currentSelected.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    try
                    {
                        object value = prop.GetValue(currentSelected);
                        string displayValue = value?.ToString() ?? "null";
                        PXTrace.WriteInformation("[PKG-ALL] {0}: {1}", prop.Name, displayValue);
                    }
                    catch (Exception ex)
                    {
                        PXTrace.WriteInformation("[PKG-ALL] {0}: ERROR - {1}", prop.Name, ex.Message);
                    }
                }
                
                PXTrace.WriteInformation("[PKG-ALL] === End of Field List ===");
            }
            else
            {
                PXTrace.WriteInformation("[PKG-ALL] WARNING: currentSelected is NULL - cannot log fields");
            }

            if (Base.IsDirty)
            {
                Base.Actions.PressSave();
                shipment = Base.Document.Current;
            }

            // ✅ Use the package captured BEFORE the save
            selectedPackageLineNbr = selectedBeforeSave;

            if (selectedPackageLineNbr != null)
            {
                PXTrace.WriteInformation("[PRINT] Using selected package: {0}", selectedPackageLineNbr);
            }

            string shipmentNbr = shipment.ShipmentNbr;
            int packageLineNbr = (int)selectedPackageLineNbr;

            PXLongOperation.StartOperation(Base, delegate()
            {
                PXTrace.WriteInformation("[LONGOP] Creating fresh graph and reloading shipment {0}", shipmentNbr);

                SOShipmentEntry graph = PXGraph.CreateInstance<SOShipmentEntry>();

                SOShipment shipmentInLongOp = SOShipment.PK.Find(graph, shipmentNbr);
                if (shipmentInLongOp == null)
                {
                    throw new PXException(
                        $"Shipment '{shipmentNbr}' could not be reloaded inside the long operation.");
                }

                graph.Document.Current = shipmentInLongOp;

                // ✅ CRITICAL REFACTOR: Delegate to AsgardLabelService
                // The service handles:
                // 1. Filter scope activation (ALPackagesFilterScope)
                // 2. Native CreatePrintContext() call (not CreateSingleRowPrintContext)
                // 3. Proper PXResult row structure from ALiStarPackages view
                
                var service = new AsgardLabelService(graph, _labelGenerator);

                // ✅ CRITICAL: Resolve model explicitly BEFORE delegating
                // The model's BasedOnView determines the row structure
                // Passing null would make service guess, causing row structure mismatch
                const bool preferBoxPrintModel = false;
                const string fallbackModelName = "iStar-8A-Packing for Boscov";

                Guid? modelId = service.ResolveModelId(fallbackModelName, preferBoxPrintModel);

                if (modelId == null || modelId == Guid.Empty)
                {
                    throw new PXException(
                        "Could not resolve an Asgard label model for selected-package native printing. " +
                        "Please verify ALSetupSlot.BoxPrintModelID or the fallback model name.");
                }

                PXTrace.WriteInformation("[LONGOP] Delegating to AsgardLabelService with resolved modelId={0}", modelId);

                // ✅ Service owns filter scope activation
                // No outer wrapping needed here - service will activate ALPackagesFilterScope internally
                PrintResults results = service.PrintSelectedPackageUsingNativeContext(
                    shipmentInLongOp,
                    modelId,  // ✅ Pass the explicitly resolved modelId
                    packageLineNbr,
                    adapter);

                if (results == null)
                    throw new PXException("Label printing returned no results.");

                if (results.NbLabels <= 0)
                {
                    throw new PXException(
                        "No labels were generated. Please verify the selected package is valid and the selected label model is configured correctly.");
                }

                PXTrace.WriteInformation("[LONGOP] Successfully printed {0} label(s) for package line {1}", results.NbLabels, packageLineNbr);
            });
        }

        protected virtual void _(Events.RowSelected<SOShipment> e)
        {
            if (PrintAsgardPackageLabel == null)
                return;

            if (e.Row == null)
            {
                PrintAsgardPackageLabel.SetVisible(false);
                PrintAsgardPackageLabel.SetEnabled(false);
                return;
            }

            PrintAsgardPackageLabel.SetVisible(true);
            PrintAsgardPackageLabel.SetEnabled(!string.IsNullOrWhiteSpace(e.Row.ShipmentNbr));
        }

        protected virtual void _(Events.RowSelected<SOPackageDetail> e)
        {
            if (PrintAsgardPackageLabel == null)
                return;

            if (e.Row == null)
            {
                PrintAsgardPackageLabel.SetEnabled(Base.Document.Current != null &&
                                                  !string.IsNullOrWhiteSpace(Base.Document.Current.ShipmentNbr));
                return;
            }

            PrintAsgardPackageLabel.SetEnabled(true);
        }
    }
}