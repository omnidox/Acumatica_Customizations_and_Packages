using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AA.Objects.Labels;
using AA.Objects.Core;
using Asgard.Labels.Abstractions.Interface;
using Asgard.Labels.Impl.Context;
using Asgard.Labels.Impl.Poco;
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    public class AsgardLabelService
    {
        private readonly SOShipmentEntry _graph;
        private readonly ILabelGenerator<IAcuLabelContext> _labelGenerator;

        public AsgardLabelService(
            SOShipmentEntry graph,
            ILabelGenerator<IAcuLabelContext> labelGenerator)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _labelGenerator = labelGenerator ?? throw new ArgumentNullException(nameof(labelGenerator));
        }

        public virtual void ValidateShipmentForAsgardPrint(SOShipment shipment)
        {
            if (shipment == null)
                throw new PXException("No shipment is currently selected.");

            if (string.IsNullOrWhiteSpace(shipment.ShipmentNbr))
                throw new PXException("Shipment does not have a valid shipment number.");

            if (shipment.CustomerID == null)
                throw new PXException("Shipment does not have a valid customer ID.");
        }

        public virtual Guid? GetModelIdByName(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                throw new PXException("Model name cannot be empty.");

            ALModel model = PXSelect<
                ALModel,
                Where<ALModel.name, Equal<Required<ALModel.name>>>>
                .Select(_graph, modelName);

            return model?.LabelID;
        }

        public virtual ALModel GetModelById(Guid? modelId)
        {
            if (modelId == null || modelId == Guid.Empty)
                return null;

            return PXSelect<
                ALModel,
                Where<ALModel.labelID, Equal<Required<ALModel.labelID>>>>
                .Select(_graph, modelId);
        }

        public virtual Guid? ResolveModelId(string modelName, bool preferBoxPrintModel)
        {
            if (preferBoxPrintModel)
            {
                Guid? boxPrintModelId = ALSetupSlot.BoxPrintModelID;

                if (boxPrintModelId != null && boxPrintModelId != Guid.Empty)
                {
                    PXTrace.WriteInformation(
                        $"Selected-package native print: using ALSetupSlot.BoxPrintModelID = {boxPrintModelId}");

                    return boxPrintModelId;
                }

                PXTrace.WriteInformation(
                    "Selected-package native print: ALSetupSlot.BoxPrintModelID is empty, falling back to model name lookup.");
            }

            Guid? modelIdByName = GetModelIdByName(modelName);

            PXTrace.WriteInformation(
                $"Selected-package native print: resolved model '{modelName}' to ModelID = {modelIdByName}");

            return modelIdByName;
        }

        public virtual void ValidateModelForNativeContextPrinting(ALModel model, Guid? modelId)
        {
            if (modelId == null || modelId == Guid.Empty)
                throw new PXException("Please choose a valid Asgard label model.");

            if (model == null)
                throw new PXException($"The selected label model (ID: {modelId}) could not be found.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new PXException($"The selected label model (ID: {modelId}) does not have a valid name.");

            if (string.IsNullOrWhiteSpace(model.ScreenID))
                throw new PXException("The selected label model is not tied to a screen.");

            if (!string.Equals(model.ScreenID, "SO302000", StringComparison.OrdinalIgnoreCase))
            {
                throw new PXException(
                    $"The selected label model must belong to the Shipments screen (SO302000). Current ScreenID: '{model.ScreenID}'.");
            }
        }

        public virtual void TraceModelDiagnostics(ALModel model, Guid? modelId, SOShipment shipment)
        {
            string modelName = model != null ? model.Name : "<null>";
            string screenId = model != null ? model.ScreenID : "<null>";
            string basedOnView = model != null ? model.BasedOnView : "<null>";
            string shipmentNbr = shipment != null ? shipment.ShipmentNbr : "<null>";

            PXTrace.WriteInformation(
                $"Selected-package native print diagnostics: Shipment={shipmentNbr}, ModelID={modelId}, ModelName={modelName}, ScreenID={screenId}, BasedOnView={basedOnView}, GraphType={_graph.GetType().FullName}");
        }

        /// <summary>
        /// Helper method: Clear UsrALPrintLabel on ALL package rows for the shipment.
        /// Used to ensure a clean state before selecting the specific package to print.
        /// NOTE: Does NOT save - caller must save when both clear and set operations are complete.
        /// </summary>
        private void ClearAllPackagePrintFlags(string shipmentNbr)
        {
            try
            {
                PXTrace.WriteInformation("[CHECKBOX] Clearing UsrALPrintLabel on all packages for shipment {0}", shipmentNbr);
                
                var allPackages = PXSelect<
                    SOPackageDetailEx,
                    Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                    .Select(_graph, shipmentNbr);

                int clearedCount = 0;
                foreach (SOPackageDetailEx pkg in allPackages)
                {
                    try
                    {
                        // Use Acumatica-native cache method if possible
                        _graph.Packages.Cache.SetValueExt(pkg, "UsrALPrintLabel", false);
                        _graph.Packages.Cache.Update(pkg);
                        clearedCount++;
                    }
                    catch (Exception setEx)
                    {
                        PXTrace.WriteInformation("[CHECKBOX] ⚠️ Error clearing UsrALPrintLabel on package line {0}: {1}", 
                            pkg.LineNbr, setEx.Message);
                    }
                }

                PXTrace.WriteInformation("[CHECKBOX] ✅ Cleared UsrALPrintLabel on {0} package rows", clearedCount);
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[CHECKBOX] ⚠️ Error in ClearAllPackagePrintFlags: {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Helper method: Set UsrALPrintLabel = true ONLY on the selected package row.
        /// All other rows are assumed to already be cleared by ClearAllPackagePrintFlags.
        /// NOTE: Does NOT save - caller must save when both clear and set operations are complete.
        /// </summary>
        private void SetOnlySelectedPackagePrintFlag(string shipmentNbr, int? selectedPackageLineNbr)
        {
            if (selectedPackageLineNbr == null)
            {
                throw new PXException("Cannot set print flag: no package line number specified.");
            }

            try
            {
                PXTrace.WriteInformation("[CHECKBOX] Setting UsrALPrintLabel on selected package line {0} for shipment {1}", 
                    selectedPackageLineNbr, shipmentNbr);

                SOPackageDetailEx selectedPackage = PXSelect<
                    SOPackageDetailEx,
                    Where<
                        SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>,
                        And<SOPackageDetailEx.lineNbr, Equal<Required<SOPackageDetailEx.lineNbr>>>>>
                    .Select(_graph, shipmentNbr, selectedPackageLineNbr);

                if (selectedPackage == null)
                {
                    throw new PXException(
                        $"Package line {selectedPackageLineNbr} not found in shipment {shipmentNbr}.");
                }

                // Use Acumatica-native cache method if possible
                _graph.Packages.Cache.SetValueExt(selectedPackage, "UsrALPrintLabel", true);
                _graph.Packages.Cache.Update(selectedPackage);

                PXTrace.WriteInformation("[CHECKBOX] ✅ Set UsrALPrintLabel=true on package line {0}", selectedPackageLineNbr);
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[CHECKBOX] ⚠️ Error in SetOnlySelectedPackagePrintFlag: {0}", ex.Message);
                throw;
            }
        }

        public virtual PrintResults PrintSelectedPackageUsingNativeContext(
            SOShipment shipment,
            Guid? modelId,
            int? selectedPackageLineNbr,
            PXAdapter adapter)
        {
            PXTrace.WriteInformation("[SERVICE] PrintSelectedPackageUsingNativeContext called - Shipment={0}, Package={1}, ModelID={2}", 
                shipment?.ShipmentNbr, selectedPackageLineNbr, modelId);

            ValidateShipmentForAsgardPrint(shipment);

            if (selectedPackageLineNbr == null)
                throw new PXException("No package line number was specified for printing.");

            ALModel model = GetModelById(modelId);
            ValidateModelForNativeContextPrinting(model, modelId);
            TraceModelDiagnostics(model, modelId, shipment);

            PXTrace.WriteInformation("[SERVICE] Verifying package {0} exists in shipment {1}", selectedPackageLineNbr, shipment.ShipmentNbr);

            SOPackageDetailEx packageToVerify = PXSelect<
                SOPackageDetailEx,
                Where<
                    SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>,
                    And<SOPackageDetailEx.lineNbr, Equal<Required<SOPackageDetailEx.lineNbr>>>>>
                .Select(_graph, shipment.ShipmentNbr, selectedPackageLineNbr);

            if (packageToVerify == null)
            {
                throw new PXException(
                    $"Package line {selectedPackageLineNbr} not found in shipment {shipment.ShipmentNbr}.");
            }

            PXTrace.WriteInformation(
                $"[SERVICE] Package {selectedPackageLineNbr} verified. Graph type: {_graph.GetType().FullName}");

            // ✅ [PKG PRINT] Diagnostics: Log selected package state BEFORE CreatePrintContext
            PXTrace.WriteInformation("[PKG PRINT] Shipment={0}", shipment.ShipmentNbr);
            PXTrace.WriteInformation("[PKG PRINT] Selected LineNbr={0}", selectedPackageLineNbr);
            
            // Get UCC128 via reflection
            object selectedUcc128 = null;
            try
            {
                selectedUcc128 = packageToVerify.GetType().GetProperty("UsrTCUCC128")?.GetValue(packageToVerify);
            }
            catch { }
            PXTrace.WriteInformation("[PKG PRINT] Selected UsrTCUCC128={0}", selectedUcc128 ?? "null");

            PXTrace.WriteInformation(
                $"[SERVICE] Row-selection native print: shipment {shipment.ShipmentNbr} will print package line {selectedPackageLineNbr}");

            // ✅ Determine the model's BasedOnView to understand the data structure
            string basedOnViewName = null;
            
            try
            {
                ALModel resolvedModel = GetModelById(modelId);
                if (resolvedModel != null)
                {
                    basedOnViewName = resolvedModel.BasedOnView;
                    PXTrace.WriteInformation("[SERVICE] Model {0} is based on view: {1}", resolvedModel.Name, basedOnViewName ?? "null");
                }
                
                if (string.IsNullOrWhiteSpace(basedOnViewName))
                {
                    basedOnViewName = "ALPackages";
                    PXTrace.WriteInformation("[SERVICE] Model has no BasedOnView specified, using default: {0}", basedOnViewName);
                }
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[SERVICE] Error determining BasedOnView: {0}", ex.Message);
                basedOnViewName = "ALPackages";
            }

            // ✅ CHECKBOX LOGIC: Manage UsrALPrintLabel + ALPackagesFilterScope + CreatePrintContext
            // This try/finally ensures checkbox cleanup even if printing fails
            try
            {
                // Step 1: Clear all package print flags
                ClearAllPackagePrintFlags(shipment.ShipmentNbr);

                // Step 2: Set print flag ONLY on selected package
                SetOnlySelectedPackagePrintFlag(shipment.ShipmentNbr, selectedPackageLineNbr);

                // Step 3: Save once after both operations complete
                _graph.Actions.PressSave();
                PXTrace.WriteInformation("[CHECKBOX] ✅ Graph saved after setting print flags");

                // Step 4: Activate filter scope for the selected package
                // ✅ CRITICAL: Use int?[] to match Activate's signature: IEnumerable<int?>
                using (ALPackagesFilterScope.Activate(shipment.ShipmentNbr, new int?[] { selectedPackageLineNbr }))
                {
                    PXTrace.WriteInformation("[CHECKBOX] ✅ ALPackagesFilterScope activated for package line {0}", selectedPackageLineNbr);

                    // ✅ CRITICAL: Use native CreatePrintContext (not CreateSingleRowPrintContext)
                    // While filter scope is active, Asgard will query the filtered view and get the correct row structure
                    // The UsrALPrintLabel flag is set above, so Asgard's NbCopies logic will see it checked
                    PXTrace.WriteInformation("[CHECKBOX] ✅ Calling CreatePrintContext with BasedOnView={0}, ModelID={1}", 
                        basedOnViewName, modelId);

                    AcuLabelContext printContext = AcuLabelContext.CreatePrintContext(
                        _graph.GetType(),
                        shipment,
                        modelId,
                        false,
                        adapter);

                    if (printContext == null)
                        throw new PXException("CreatePrintContext returned null.");

                    string modelName = printContext.Model != null ? printContext.Model.Name : "<null>";
                    string printerName = printContext.Printer != null ? printContext.Printer.Name : "<null>";
                    PXTrace.WriteInformation("[CHECKBOX] ✅ CreatePrintContext succeeded. Model={0}, Printer={1}", 
                        modelName, printerName);

                    if (printContext.Model == null)
                        throw new PXException("printContext.Model is null.");

                    if (printContext.Row == null)
                        throw new PXException("printContext.Row is null.");

                    if (printContext.Printer == null)
                    {
                        throw new PXException(
                            "No printer is configured for this model. Please configure a printer for the model or printer override as needed.");
                    }

                    PXTrace.WriteInformation(
                        $"[CHECKBOX] ✅ Print context ready: Model={printContext.Model.Name}, Printer={printContext.Printer.Name}, Shipment={shipment.ShipmentNbr}, Package={selectedPackageLineNbr}");

                    // ✅ Call PrintLabels while filter scope is active AND checkbox is set
                    // Filter ensures Asgard gets only the selected package row
                    // Checkbox ensures Asgard's NbCopies logic sees the row as eligible
                    try
                    {
                        PXTrace.WriteInformation("[CHECKBOX] ✅ Calling PrintLabels...");
                        PrintResults results = _labelGenerator.PrintLabels(printContext);
                        
                        if (results == null)
                            throw new PXException("PrintLabels returned null.");

                        PXTrace.WriteInformation("[CHECKBOX] ✅ PrintLabels returned NbLabels={0} for package {1}", results.NbLabels, selectedPackageLineNbr);
                        
                        if (results.NbLabels == 1)
                        {
                            PXTrace.WriteInformation("[RESULT] ✅ SUCCESS: Single label printed for package line {0}", selectedPackageLineNbr);
                        }
                        else if (results.NbLabels == 0)
                        {
                            PXTrace.WriteInformation("[RESULT] ⚠️ WARNING: No labels generated for package line {0}", selectedPackageLineNbr);
                        }
                        else
                        {
                            PXTrace.WriteInformation("[RESULT] ⚠️ UNEXPECTED: {0} labels printed (expected 1) for package line {1}", 
                                results.NbLabels, selectedPackageLineNbr);
                        }

                        PXTrace.WriteInformation(
                            $"[SERVICE] Print completed: Shipment={shipment.ShipmentNbr}, Package={selectedPackageLineNbr}, NbLabels={results.NbLabels}");

                        return results;
                    }
                    catch (Exception printEx)
                    {
                        PXTrace.WriteInformation("[SERVICE] PrintLabels exception: {0}", printEx.GetType().FullName);
                        PXTrace.WriteInformation("[SERVICE] Exception message: {0}", printEx.Message);
                        throw;
                    }
                }  // End of ALPackagesFilterScope using block
            }
            finally
            {
                // ✅ CRITICAL: Clear all package print flags in finally block
                // This runs even if printing fails, ensuring clean state
                PXTrace.WriteInformation("[CHECKBOX] Finally block: Clearing all package print flags...");
                try
                {
                    ClearAllPackagePrintFlags(shipment.ShipmentNbr);
                    _graph.Actions.PressSave();
                    PXTrace.WriteInformation("[CHECKBOX] ✅ Finally: All package flags cleared and saved");
                }
                catch (Exception cleanupEx)
                {
                    PXTrace.WriteInformation("[CHECKBOX] ⚠️ Error during finally cleanup: {0}", cleanupEx.Message);
                    // Don't re-throw from finally - let the original exception propagate if there was one
                }
            }
        }

        [Obsolete("Use PrintSelectedPackageUsingNativeContext instead")]
        public virtual PrintResults PrintCheckedPackagesUsingNativeContext(
            SOShipment shipment,
            Guid? modelId,
            PXAdapter adapter)
        {
            throw new PXException(
                "PrintCheckedPackagesUsingNativeContext is deprecated. Use PrintSelectedPackageUsingNativeContext with a specific package line number instead.");
        }

        /// <summary>
        /// Helper method to invoke AsgardUtils.FindExtension<T> via reflection for any type.
        /// This avoids compile-time type resolution issues.
        /// </summary>
        private object TryFindExtension(object row, Type extensionType)
        {
            if (row == null || extensionType == null)
                return null;

            try
            {
                // Get all public static methods from AsgardUtils
                var methods = typeof(AsgardUtils)
                    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                // Find the generic FindExtension<T> method that takes one parameter
                var targetMethod = methods.FirstOrDefault(m =>
                    m.Name == "FindExtension" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1);

                if (targetMethod == null)
                {
                    PXTrace.WriteInformation("[PROOF] Could not find generic AsgardUtils.FindExtension<T>(row)");
                    return null;
                }

                // Create the closed generic method: FindExtension<extensionType>
                var closedMethod = targetMethod.MakeGenericMethod(extensionType);
                
                // Invoke the method with the row as argument
                return closedMethod.Invoke(null, new[] { row });
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[PROOF] TryFindExtension error: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Helper method to safely access field values on Asgard extension objects using reflection.
        /// </summary>
        private string GetFieldValueAsString(object obj, string fieldName)
        {
            if (obj == null)
                return "null";

            try
            {
                var property = obj.GetType().GetProperty(fieldName);
                if (property == null)
                    return "field_not_found";

                var value = property.GetValue(obj);
                return value?.ToString() ?? "null";
            }
            catch (Exception ex)
            {
                return "error_" + ex.GetType().Name;
            }
        }

        /// <summary>
        /// Helper method to invoke NbCopiesHelper.CheckLineDoPrint via reflection.
        /// Returns TRUE on failure (allow printing) - don't block on reflection errors.
        /// </summary>
        private bool InvokeCheckLineDoPrint(object labelContext)
        {
            try
            {
                // Get NbCopiesHelper type from Asgard assembly
                Type nbCopiesHelperType = Type.GetType("AA.Objects.AL.NbCopiesHelper, AA.Objects.AL.Basic");
                if (nbCopiesHelperType == null)
                {
                    PXTrace.WriteInformation("[DIAG-GATE-PRINT] ⚠️ NbCopiesHelper type not found via reflection");
                    PXTrace.WriteInformation("[DIAG-GATE-PRINT] Defaulting to TRUE (allow printing) - native path will decide");
                    return true;  // ← ALLOW PRINTING ON REFLECTION FAILURE
                }

                // Get CheckLineDoPrint static method
                var method = nbCopiesHelperType.GetMethod("CheckLineDoPrint", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (method == null)
                {
                    PXTrace.WriteInformation("[DIAG-GATE-PRINT] ⚠️ CheckLineDoPrint method not found on NbCopiesHelper");
                    PXTrace.WriteInformation("[DIAG-GATE-PRINT] Defaulting to TRUE (allow printing) - native path will decide");
                    return true;  // ← ALLOW PRINTING ON REFLECTION FAILURE
                }

                // Invoke the method
                object result = method.Invoke(null, new[] { labelContext });
                bool methodResult = (bool)(result ?? false);
                
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] ✓ Native CheckLineDoPrint invoked successfully: {0}", methodResult);
                return methodResult;
            }
            catch (TargetInvocationException tiEx)
            {
                // Unwrap the inner exception from reflection invocation
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] ⚠️ CheckLineDoPrint threw exception during invocation");
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] Inner exception type: {0}", 
                    tiEx.InnerException?.GetType().FullName ?? "null");
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] Inner exception message: {0}", 
                    tiEx.InnerException?.Message ?? "null");
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] Defaulting to TRUE (allow printing)");
                return true;  // ← ALLOW PRINTING ON INVOCATION ERROR
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] ⚠️ Error invoking CheckLineDoPrint: {0}", ex.GetType().FullName);
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] Exception message: {0}", ex.Message);
                PXTrace.WriteInformation("[DIAG-GATE-PRINT] Defaulting to TRUE (allow printing) - native path will decide");
                return true;  // ← ALLOW PRINTING ON REFLECTION ERROR
            }
        }
    }
}