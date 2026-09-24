using System;
using System.Collections;
using Asgard.Labels.Abstractions.Interface;
using Asgard.Labels.Abstractions.Service;
using Asgard.Labels.Impl.Context;
using Asgard.Labels.Impl.Poco;
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.Labels.Integration.PerPackage
{
    /// <summary>
    /// Adds a custom button to Shipment Entry that attempts to print
    /// an Asgard label for the currently selected package.
    ///
    /// Current proof-of-concept behavior:
    /// - no popup model selector
    /// - model is hardcoded by name
    /// - delegates printing to AsgardLabelService
    /// </summary>
    public class SOShipmentEntry_AsgardExt : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive()
        {
            return ALSetupSlot.IsActive(typeof(SOShipmentEntry));
        }

        [InjectDependency]
        private ILabelGenerator<IAcuLabelContext> _labelGenerator { get; set; }

        public PXAction<SOShipment> PrintAsgardPackageLabel;

        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Print Asgard Label", Visible = true, Enabled = true)]
        protected virtual IEnumerable printAsgardPackageLabel(PXAdapter adapter)
        {
            SOShipment shipment = Base.Document.Current;
            SOPackageDetailEx package = Base.Packages.Current;

            if (shipment == null)
                throw new PXException("No shipment is currently selected.");

            if (package == null)
                throw new PXException("Please select a package from the Packages tab before printing.");

            try
            {
                // CRITICAL: Pass the injected _labelGenerator to AsgardLabelService
                var asgardService = new AsgardLabelService(Base, _labelGenerator);

                // Optional UI-level checkpoint to prove the button action itself is firing.
                // Uncomment only when needed.
                // throw new PXException("CHECKPOINT UI: Entered printAsgardPackageLabel action");

                asgardService.ValidatePackageForAsgardPrint(shipment, package);

                // IMPORTANT:
                // Based on your screenshot, the actual model Name is most likely "istar_test_label"
                // and the Description is "label_test".
                const string modelName = "istar_test_label";

                Guid? modelId = asgardService.GetModelIdByName(modelName);

                if (modelId == null || modelId == Guid.Empty)
                {
                    throw new PXException(
                        $"Could not find Asgard label model '{modelName}'. " +
                        "Please verify the model exists and is active.");
                }

                PrintResults results = asgardService.PrintAsgardLabelForPackage(
                    shipment,
                    package,
                    modelId);

                if (results == null)
                    throw new PXException("Label printing returned no results.");

                if (results.NbLabels <= 0)
                {
                    throw new PXException(
                        "No labels were generated. Please verify the selected label model is configured correctly.");
                }

                PXTrace.WriteInformation($"Successfully printed {results.NbLabels} label(s) for package line {package.LineNbr}");
            }
            catch (PXException)
            {
                throw;
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

        protected virtual void _(Events.RowSelected<SOShipment> e)
        {
            if (e.Row == null)
            {
                PrintAsgardPackageLabel.SetVisible(false);
                PrintAsgardPackageLabel.SetEnabled(false);
                return;
            }

            PrintAsgardPackageLabel.SetVisible(true);
            PrintAsgardPackageLabel.SetEnabled(!string.IsNullOrWhiteSpace(e.Row.ShipmentNbr));
        }

        protected virtual void _(Events.RowSelected<SOPackageDetailEx> e)
        {
            if (e.Row == null)
            {
                PrintAsgardPackageLabel.SetEnabled(false);
                return;
            }

            PrintAsgardPackageLabel.SetEnabled(e.Row.LineNbr != null);
        }
    }
}