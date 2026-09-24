using System;
using System.Collections;
using AA.Objects.License;
using Asgard.Labels.Abstractions.Interface;
using Asgard.Labels.Abstractions.Service;
using Asgard.Labels.Impl.Context;
using Asgard.Labels.Impl.Poco;
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.Labels.Integration.PerPackage
{
    /// <summary>
    /// Graph extension for SOShipmentEntry that adds per-package Asgard label printing capability.
    /// Provides a button that allows users to select a package and print its Asgard label on demand.
    /// 
    /// Uses dependency injection to provide ILabelGenerator to AsgardLabelService,
    /// matching the pattern from ALBoxPrintSOShipmentEntryExt.
    /// </summary>
    public class SOShipmentEntry_AsgardExt : PXGraphExtension<SOShipmentEntry>
    {
        /// <summary>
        /// Determines if this extension should be active.
        /// Checks that:
        /// 1. Asgard is active for SOShipmentEntry
        /// 2. Box Print feature is actually ENABLED
        /// 3. Box Print model is configured
        /// </summary>
        public static bool IsActive()
        {
            // Check if Asgard is active for this graph
            if (!ALSetupSlot.IsActive(typeof(SOShipmentEntry)))
                return false;

            // Check if Box Print feature is actually enabled
            if (!ALSetupSlot.BoxPrint)
                return false;

            // Check if Box Print model is configured
            Guid? boxPrintModelId = ALSetupSlot.BoxPrintModelID;
            return boxPrintModelId != null && boxPrintModelId != Guid.Empty;
        }

        // Dependency injection for Asgard services
        [InjectDependency]
        private IALLicenseManager _licenseManager { get; set; }

        /// <summary>
        /// CRITICAL: The injected ILabelGenerator is passed to AsgardLabelService.
        /// This is NOT instantiated with 'new' - it comes from Acumatica's dependency injection.
        /// </summary>
        [InjectDependency]
        private ILabelGenerator<IAcuLabelContext> _labelGenerator { get; set; }

        [InjectDependency]
        private IModelProvider _modelProvider { get; set; }

        /// <summary>
        /// Action for printing Asgard label for the selected package.
        /// Button appears in the Packages tab of the Shipment Entry screen.
        /// </summary>
        public PXAction<SOShipment> PrintAsgardPackageLabel;

        [PXButton(CommitChanges = true)]
        [PXUIField(
            DisplayName = "Print Asgard Label",
            Visible = true,
            Enabled = true)]
        protected virtual IEnumerable printAsgardPackageLabel(PXAdapter adapter)
        {
            SOShipment shipment = Base.Document.Current;
            SOPackageDetailEx package = Base.Packages.Current;

            // Validate that we have both a shipment and a package selected
            if (shipment == null)
            {
                throw new PXException("No shipment is currently selected.");
            }

            if (package == null)
            {
                throw new PXException("Please select a package from the Packages tab before printing.");
            }

            try
            {
                // Create the Asgard label service
                // CRITICAL: Pass the injected _labelGenerator to AsgardLabelService
                // This ensures PrintLabels() has all the dependencies it needs
                var asgardService = new AsgardLabelService(
                    Base,
                    _labelGenerator);  // ← Pass the injected ILabelGenerator dependency

                // Validate the package is ready for printing
                asgardService.ValidatePackageForAsgardPrint(shipment, package);

                // Get the model ID for per-package printing
                // Uses BoxPrintModelID (package-level printing)
                Guid? modelId = ALSetupSlot.BoxPrintModelID;

                if (modelId == null || modelId == Guid.Empty)
                {
                    throw new PXException(
                        "Asgard Box Print model is not configured. " +
                        "Please configure a Box Print model in Asgard Setup > Box Print Model.");
                }

                // Print the label for the SELECTED PACKAGE using the corrected single-row context
                // Now uses the injected _labelGenerator through AsgardLabelService
                PrintResults results = asgardService.PrintAsgardLabelForPackage(shipment, package, modelId);

                if (results == null)
                {
                    throw new PXException("Label printing returned no results.");
                }

                // Validate that labels were actually printed
                if (results.NbLabels <= 0)
                {
                    throw new PXException(
                        "No labels were generated. Please verify the Box Print model is configured correctly.");
                }

                // Silent success pattern (idiomatic Acumatica)
                // The action completes without throwing, button press succeeds
                PXTrace.WriteInformation($"Successfully printed {results.NbLabels} label(s) for package line {package.LineNbr}");
            }
            catch (PXException)
            {
                throw;  // Re-throw all Acumatica exceptions
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(ex);
                throw new PXException(
                    $"An error occurred while printing the Asgard label: {ex.Message}",
                    ex);
            }

            return adapter.Get();
        }

        /// <summary>
        /// RowSelected event handler for SOShipment.
        /// Sets the visibility and enabled state of the Print Asgard Label button.
        /// Button is only visible/enabled when a valid shipment is selected.
        /// </summary>
        protected virtual void _(Events.RowSelected<SOShipment> e)
        {
            if (e.Row == null)
            {
                PrintAsgardPackageLabel.SetVisible(false);
                PrintAsgardPackageLabel.SetEnabled(false);
                return;
            }

            SOShipment shipment = e.Row;

            // Show the button when a shipment is selected
            PrintAsgardPackageLabel.SetVisible(true);

            // Enable the button only when a valid shipment is selected
            bool isEnabled = shipment.ShipmentNbr != null;
            PrintAsgardPackageLabel.SetEnabled(isEnabled);
        }

        /// <summary>
        /// RowSelected event handler for SOPackageDetailEx.
        /// Updates button state when package selection changes.
        /// Button is only enabled when a valid package is selected.
        /// </summary>
        protected virtual void _(Events.RowSelected<SOPackageDetailEx> e)
        {
            if (e.Row == null)
            {
                // No package selected - disable the button
                PrintAsgardPackageLabel.SetEnabled(false);
                return;
            }

            SOPackageDetailEx package = e.Row;

            // Enable button only if a valid package with a line number is selected
            bool isEnabled = package.LineNbr != null;
            PrintAsgardPackageLabel.SetEnabled(isEnabled);
        }
    }
}
