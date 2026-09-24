using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AA.Objects.Labels;
using Asgard.Labels.Abstractions.Interface;
using Asgard.Labels.Impl.Context;  
using Asgard.Labels.Impl.Poco;      
using Asgard.Labels.Impl.Language.MyScriban;  // ← REQUIRED: For NewScribanUtils
using PX.Data;
using PX.Objects.SO;
using Scriban;  // ← REQUIRED: For TemplateContext

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

                    // ✅ CHECKPOINT: Verify the selected package has UsrALPrintLabel=true before CreatePrintContext
                    // This confirms the checkbox was actually saved and is visible to Asgard
                    SOPackageDetailEx verifyPackage = PXSelect<
                        SOPackageDetailEx,
                        Where<
                            SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>,
                            And<SOPackageDetailEx.lineNbr, Equal<Required<SOPackageDetailEx.lineNbr>>>>>
                        .Select(_graph, shipment.ShipmentNbr, selectedPackageLineNbr);

                    if (verifyPackage != null)
                    {
                        object flag = _graph.Packages.Cache.GetValue(verifyPackage, "UsrALPrintLabel");
                        object copies = _graph.Packages.Cache.GetValue(verifyPackage, "UsrALNbrOfCopies");
                        object qty = _graph.Packages.Cache.GetValue(verifyPackage, "UsrALLabelQty");
                        object ucc128 = _graph.Packages.Cache.GetValue(verifyPackage, "UsrTCUCC128");
                        object carton = _graph.Packages.Cache.GetValue(verifyPackage, "UsrCartonNbr");

                        PXTrace.WriteInformation(
                            "[CHECKBOX-VERIFY] After save: LineNbr={0}, UsrALPrintLabel={1}, UsrALNbrOfCopies={2}, UsrALLabelQty={3}, UsrTCUCC128={4}, UsrCartonNbr={5}",
                            verifyPackage.LineNbr,
                            flag,
                            copies,
                            qty,
                            ucc128,
                            carton);
                    }
                    else
                    {
                        PXTrace.WriteInformation("[CHECKBOX-VERIFY] ⚠️ WARNING: verifyPackage is null after selecting LineNbr={0}", selectedPackageLineNbr);
                    }

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

        /// <summary>
        /// ========================================================================
        /// DYNAMIC MODEL RESOLUTION BY ASGARD RULES
        /// ========================================================================
        /// 
        /// Dynamically resolves the correct Asgard label model for a shipment
        /// by evaluating Asgard's own rule system, rather than hardcoding a model name.
        /// 
        /// This method:
        /// 1. Queries all active ALModel records for SO302000 screen
        /// 2. Filters for package-based models (ALPackages, ALiStarPackages)
        /// 3. For each model, evaluates its PrintRuleID or FilterRuleID using NewScribanUtils
        /// 4. Returns exactly one matching model, or throws clear errors for:
        ///    - 0 matches: No model applies to this customer
        ///    - 2+ matches: Multiple models match; need to adjust rules
        /// 
        /// Why use Asgard rules instead of hardcoding:
        /// - Different customers need different labels (Target, Boscov, etc.)
        /// - Asgard rules already encode this logic (Document.CustomerID.AcctName | string.Contains 'TARGET')
        /// - Hardcoding bypasses Asgard's rule system, creating maintenance burden
        /// - Using Asgard rules keeps label selection centralized and consistent
        /// </summary>
        public virtual Guid? ResolveModelIdByAsgardRules(SOShipment shipment)
        {
            PXTrace.WriteInformation("[MODEL-RESOLVE] ResolveModelIdByAsgardRules called for shipment: {0}", 
                shipment?.ShipmentNbr ?? "null");

            ValidateShipmentForAsgardPrint(shipment);

            if (shipment == null)
                throw new PXException("Shipment is null");

            string shipmentNbr = shipment.ShipmentNbr;

            PXTrace.WriteInformation("[MODEL-RESOLVE] Shipment={0}", shipmentNbr);

            // ✅ Query all active ALModel records for SO302000 (Shipments screen)
            // Filter for package-based models only (ALPackages, ALiStarPackages) in LINQ
            List<ALModel> packageModels = PXSelect<
                ALModel,
                Where<
                    ALModel.active, Equal<True>,
                    And<ALModel.screenID, Equal<Required<ALModel.screenID>>>>>
                .Select(_graph, "SO302000")
                .RowCast<ALModel>()
                .Where(m => m.BasedOnView == "ALPackages" || m.BasedOnView == "ALiStarPackages")
                .ToList();

            PXTrace.WriteInformation("[MODEL-RESOLVE] Found {0} package-based models for SO302000", packageModels.Count);

            if (packageModels.Count == 0)
            {
                throw new PXException(
                    "No Asgard package label models found for screen SO302000. " +
                    "Please create at least one label model based on ALPackages or ALiStarPackages.");
            }

            // ✅ Evaluate rules and collect matching models
            List<ALModel> matchingModels = new List<ALModel>();

            foreach (ALModel model in packageModels)
            {
                try
                {
                    PXTrace.WriteInformation("[RULE-EVAL] Evaluating model: {0} (ID: {1})", 
                        model.Name, model.LabelID);

                    // ✅ Prefer PrintRuleID; fall back to FilterRuleID
                    Guid? ruleIdToEvaluate = model.PrintRuleID ?? model.FilterRuleID;

                    if (ruleIdToEvaluate == null || ruleIdToEvaluate == Guid.Empty)
                    {
                        PXTrace.WriteInformation("[RULE-EVAL] Model {0} has no PrintRuleID or FilterRuleID - SKIP", 
                            model.Name);
                        continue;
                    }

                    string ruleSource = (model.PrintRuleID != null && model.PrintRuleID != Guid.Empty) 
                        ? "PrintRuleID" 
                        : "FilterRuleID";

                    PXTrace.WriteInformation("[RULE-EVAL] Using {0}: {1}", ruleSource, ruleIdToEvaluate);

                    // ✅ Load the rule record
                    ALRule rule = LoadRuleById(ruleIdToEvaluate);

                    if (rule == null)
                    {
                        throw new PXException(
                            "Rule {0} referenced by model '{1}' could not be loaded from the database.",
                            ruleIdToEvaluate, model.Name);
                    }

                    PXTrace.WriteInformation("[RULE-EVAL] Rule name: {0}", rule.Name);
                    PXTrace.WriteInformation("[RULE-EVAL] Rule expression: {0}", rule.Expression ?? "null");

                    if (string.IsNullOrWhiteSpace(rule.Expression))
                    {
                        throw new PXException(
                            "Rule '{0}' (used by model '{1}') has an empty expression.",
                            rule.Name, model.Name);
                    }

                    // ✅ Check if rule is active before evaluating
                    if (rule.Active != true)
                    {
                        PXTrace.WriteInformation("[RULE-EVAL] Rule {0} is inactive - SKIP", rule.Name);
                        continue;
                    }

                    // ✅ Build Scriban context with shipment (Document will resolve from current shipment)
                    TemplateContext scribanContext = BuildScribanContextForRuleEvaluation(shipment);

                    if (scribanContext == null)
                    {
                        throw new PXException(
                            "Failed to build Scriban context for evaluating rule '{0}' on model '{1}'.",
                            rule.Name, model.Name);
                    }

                    // ✅ Evaluate the rule expression using CONFIRMED NewScribanUtils method
                    bool matched = EvaluateRuleExpression(scribanContext, rule.Expression, rule.Name, model.Name);

                    PXTrace.WriteInformation("[RULE-MATCH] Model {0}: rule evaluated to {1}", 
                        model.Name, matched);

                    if (matched)
                    {
                        matchingModels.Add(model);
                        PXTrace.WriteInformation("[RULE-MATCH] ✅ Model {0} MATCHED", model.Name);
                    }
                    else
                    {
                        PXTrace.WriteInformation("[RULE-MATCH] ⊘ Model {0} did not match", model.Name);
                    }
                }
                catch (PXException)
                {
                    // ✅ Re-throw PXException immediately (user-facing errors)
                    throw;
                }
                catch (Exception ex)
                {
                    // ✅ Convert other exceptions to PXException with clear context
                    throw new PXException(
                        "Error evaluating rule for model '{0}': {1}",
                        model.Name, ex.Message);
                }
            }

            // ✅ Check matching models count
            PXTrace.WriteInformation("[MODEL-SELECT] Total matching models: {0}", matchingModels.Count);

            if (matchingModels.Count == 0)
            {
                string modelList = string.Join(", ", packageModels.Select(m => m.Name));
                throw new PXException(
                    "No Asgard package label model matched this shipment ({0}). " +
                    "Available models: {1}. " +
                    "Please verify the Asgard label model rules on screen SO302000.",
                    shipmentNbr,
                    modelList);
            }

            if (matchingModels.Count > 1)
            {
                string matchedNames = string.Join(", ", matchingModels.Select(m => m.Name));
                throw new PXException(
                    "Multiple Asgard package label models matched shipment {0}: {1}. " +
                    "Please adjust the Asgard model rules so only one package label model matches.",
                    shipmentNbr,
                    matchedNames);
            }

            // ✅ Exactly one match
            Guid? selectedModelId = matchingModels[0].LabelID;
            PXTrace.WriteInformation("[MODEL-SELECT] ✅ Selected model: {0} (ID: {1})", 
                matchingModels[0].Name, selectedModelId);

            return selectedModelId;
        }

        /// <summary>
        /// Helper: Load an ALRule by ID from the database.
        /// Throws PXException if rule not found.
        /// </summary>
        private ALRule LoadRuleById(Guid? ruleId)
        {
            if (ruleId == null || ruleId == Guid.Empty)
                return null;

            try
            {
                ALRule rule = PXSelect<
                    ALRule,
                    Where<ALRule.ruleID, Equal<Required<ALRule.ruleID>>>>
                    .Select(_graph, ruleId);

                if (rule == null)
                {
                    PXTrace.WriteInformation("[RULE-LOAD] Rule {0} not found in database", ruleId);
                }

                return rule;
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[RULE-LOAD] Error loading rule {0}: {1}", ruleId, ex.Message);
                throw new PXException("Error loading rule {0} from database: {1}", ruleId, ex.Message);
            }
        }

        /// <summary>
        /// Helper: Build a Scriban TemplateContext with the shipment as the current document.
        /// This ensures that rule expressions like "Document.CustomerID.AcctName" resolve correctly.
        /// 
        /// The context is built using Asgard's ScribanUtils pattern:
        /// - graph.Document.Current = shipment (so "Document" resolves in Scriban)
        /// - ScribanUtils.CreateContext() populates the context with graph data
        /// - Returns a TemplateContext ready for rule expression evaluation
        /// 
        /// Includes a validation probe to confirm Document.CustomerID.AcctName resolves correctly.
        /// </summary>
        private TemplateContext BuildScribanContextForRuleEvaluation(SOShipment shipment)
        {
            try
            {
                PXTrace.WriteInformation("[CONTEXT] Building Scriban context for shipment {0}", 
                    shipment?.ShipmentNbr ?? "null");

                // ✅ Ensure Document.Current is set to the shipment
                // This makes "Document" resolve correctly in Scriban expressions
                _graph.Document.Current = shipment;

                // ✅ Create the Scriban context using Asgard's ScribanUtils
                // Pass the graph INSTANCE (not type), the shipment row, no oldRow, devMode=false
                TemplateContext scribanContext = ScribanUtils.CreateContext(
                    _graph,         // ← Graph INSTANCE, not _graph.GetType()
                    shipment,       // ← Row to use for context
                    null,           // ← oldRow: not needed for rule evaluation
                    false);         // ← devMode: false

                if (scribanContext == null)
                {
                    throw new PXException("ScribanUtils.CreateContext returned null");
                }

                PXTrace.WriteInformation("[CONTEXT] ✅ Scriban context built successfully");

                // ✅ DIAGNOSTIC PROBE (non-blocking)
                // Attempt to resolve Document.CustomerID.AcctName for diagnostic purposes
                // If the probe fails, we log it but continue - the actual rule evaluation will determine success/failure
                try
                {
                    object probe = NewScribanUtils.EvalExpr<object>(
                        scribanContext,
                        "Document.CustomerID.AcctName",
                        null);

                    if (probe == null)
                    {
                        PXTrace.WriteInformation("[CONTEXT-PROBE] Document.CustomerID.AcctName evaluated to null - rule evaluation may depend on different context");
                    }
                    else
                    {
                        PXTrace.WriteInformation("[CONTEXT-PROBE] Document.CustomerID.AcctName resolved successfully: {0}", probe);
                    }
                }
                catch (Exception probeEx)
                {
                    PXTrace.WriteInformation("[CONTEXT-PROBE] Diagnostic probe encountered error (non-blocking): {0}", probeEx.Message);
                    PXTrace.WriteInformation("[CONTEXT-PROBE] Continuing - actual rule evaluation will determine if Document context is sufficient");
                }

                return scribanContext;
            }
            catch (PXException)
            {
                throw;
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[CONTEXT] ⚠️ Error building Scriban context: {0}", ex.Message);
                throw new PXException("Error building Scriban context: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Helper: Evaluate a Scriban rule expression using CONFIRMED NewScribanUtils.EvalExpr<bool>.
        /// Returns true if the expression evaluates to true, false otherwise.
        /// 
        /// CONFIRMED signature from NewScribanUtils.cs:
        /// public static T EvalExpr<T>(TemplateContext scribanContext, string scribanExpr, T defaultValue = default(T))
        /// 
        /// Important behavior from NewScribanUtils:
        /// - Returns defaultValue if expression is null/empty
        /// - Calls scribanExpr.ToScriban() internally
        /// - Calls Template.Parse() and template.Evaluate()
        /// - Calls scribanContext.CheckTemplateErrors() for parse errors (may throw through Asgard exception system)
        /// - Throws AAException on type conversion failure
        /// - Throws generic Exception on other evaluation errors
        /// 
        /// We catch both AAException and generic Exception and wrap them in PXException.
        /// </summary>
        private bool EvaluateRuleExpression(TemplateContext scribanContext, string expression, string ruleName, string modelName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    throw new PXException("Rule expression is empty");
                }

                PXTrace.WriteInformation("[EVAL-EXPR] Evaluating rule '{0}' for model '{1}'", ruleName, modelName);
                PXTrace.WriteInformation("[EVAL-EXPR] Expression: {0}", expression);

                // ✅ Use CONFIRMED NewScribanUtils.EvalExpr<bool> method signature
                // NewScribanUtils.EvalExpr<T>(TemplateContext scribanContext, string scribanExpr, T defaultValue)
                // Default value (false) ensures safe handling if evaluation returns null/empty
                bool result = NewScribanUtils.EvalExpr<bool>(scribanContext, expression, false);

                PXTrace.WriteInformation("[EVAL-EXPR] Expression evaluated to: {0}", result);
                return result;
            }
            catch (PXException)
            {
                // ✅ Re-throw PXException immediately
                throw;
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[EVAL-EXPR] ⚠️ Error evaluating rule '{0}': {1}", ruleName, ex.Message);
                PXTrace.WriteInformation("[EVAL-EXPR] Exception type: {0}", ex.GetType().FullName);
                PXTrace.WriteInformation("[EVAL-EXPR] Stack trace: {0}", ex.StackTrace);

                string message = string.Format(
                    "Error evaluating rule '{0}' used by model '{1}'. Expression: '{2}'. Error: {3}",
                    ruleName,
                    modelName,
                    expression,
                    ex.Message);

                throw new PXException(message);
            }
        }
    }
}