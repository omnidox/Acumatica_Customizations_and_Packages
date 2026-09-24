using System;
using PX.Data;
using PX.Objects.SO;
using Asgard.Labels.Impl;
using Asgard.Labels.Impl.Context;
using Asgard.Labels.Impl.Poco;

namespace AA.Objects.Labels.Integration.PerPackage
{
    public class AsgardLabelService
    {
        private readonly SOShipmentEntry _graph;

        public AsgardLabelService(SOShipmentEntry graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public virtual void ValidatePackageForAsgardPrint(SOShipment shipment, SOPackageDetailEx package)
        {
            if (shipment == null)
                throw new PXException("No shipment is currently selected.");

            if (package == null)
                throw new PXException("No package is currently selected on the Packages tab.");

            if (package.LineNbr == null)
                throw new PXException("The selected package does not have a valid line number.");

            if (string.IsNullOrWhiteSpace(shipment.ShipmentNbr))
                throw new PXException("Shipment does not have a valid shipment number.");

            if (shipment.CustomerID == null)
                throw new PXException("Shipment does not have a valid customer ID.");
        }

        public virtual Guid? GetModelIdByName(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                throw new PXException("Model name cannot be empty.");

            ALModel model =
                PXSelect<
                    ALModel,
                    Where<ALModel.name, Equal<Required<ALModel.name>>>>
                .Select(_graph, modelName);

            return model?.LabelID;
        }

        public virtual void ValidateModelForPackagePrinting(ALModel model, Guid? modelId)
        {
            if (modelId == null || modelId == Guid.Empty)
                throw new PXException("Please choose a valid Asgard label model.");

            if (model == null)
                throw new PXException(
                    $"The selected label model (ID: {modelId}) could not be found.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new PXException(
                    $"The selected label model (ID: {modelId}) does not have a valid name.");

            if (string.IsNullOrWhiteSpace(model.ScreenID))
                throw new PXException("The selected label model is not tied to a screen.");

            if (!string.Equals(model.ScreenID, "SO302000", StringComparison.OrdinalIgnoreCase))
                throw new PXException("The selected label model must belong to the Shipments screen.");

            if (string.IsNullOrWhiteSpace(model.BasedOnView) ||
                model.BasedOnView.IndexOf("Packages", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new PXException("The selected label model must be based on the Packages view.");
            }
        }

        public virtual PrintResults PrintAsgardLabelForPackage(
            SOShipment shipment,
            SOPackageDetailEx package,
            Guid? modelId)
        {
            // CHECKPOINT 0: confirms this method is being entered at all (passed)
            //throw new PXException("CHECKPOINT 0: Entered PrintAsgardLabelForPackage");

            ValidatePackageForAsgardPrint(shipment, package);

            // CHECKPOINT 1: confirms package/shipment validation passed (passed)
            // throw new PXException("CHECKPOINT 1: Package validation passed");

            ALModel model = ALModel.PK.Find(_graph, modelId);

            // CHECKPOINT 2: confirms model lookup happened (passed)
            // throw new PXException($"CHECKPOINT 2: Model lookup completed. ModelID={modelId}, Found={(model != null)}");

            ValidateModelForPackagePrinting(model, modelId);

            // CHECKPOINT 3: confirms model validation passed (passed)
            // throw new PXException("CHECKPOINT 3: Model validation passed");

            try
            {
                AcuLabelContext printContext = AcuLabelContext.CreateSingleRowPrintContext(
                    typeof(SOShipmentEntry),
                    shipment,
                    package,
                    modelId,
                    shipment.CustomerID);

                if (printContext == null)
                    throw new PXException("CreateSingleRowPrintContext returned null.");

                // CHECKPOINT 4: confirms print context was created (passed)
                // throw new PXException("CHECKPOINT 4: AcuLabelContext created successfully");

                printContext.IsSilent = true;

                PrintResults results = new AcuLabelGenerator().PrintLabels(printContext);

                if (results == null)
                    throw new PXException("PrintLabels returned null.");

                // CHECKPOINT 5: confirms PrintLabels returned (Not passed)
                // throw new PXException($"CHECKPOINT 5: PrintLabels returned. NbLabels={results.NbLabels}");

                return results;
            }
            catch (PXException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PXException(
                    $"An error occurred while generating the Asgard label for package line {package.LineNbr}: {ex.Message}",
                    ex);
            }
        }
    }
}