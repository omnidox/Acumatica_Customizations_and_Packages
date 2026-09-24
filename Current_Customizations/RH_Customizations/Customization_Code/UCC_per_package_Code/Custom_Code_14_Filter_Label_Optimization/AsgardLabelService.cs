using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AA.Objects.Labels;
using Asgard.Labels.Abstractions.Interface;
using Asgard.Labels.Impl;
using Asgard.Labels.Impl.Context;  
using Asgard.Labels.Impl.Poco;      
using Asgard.Labels.Impl.Language.MyScriban;  // ← REQUIRED: For NewScribanUtils
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    public class AsgardLabelService
    {
        // Enable temporarily when investigating model/rule internals. Keep false in normal use
        // so one print operation does not push useful errors out of Acumatica's trace window.
        private static readonly bool DetailedDiagnostics = false;

        private static void WriteDiagnostic(string message, params object[] args)
        {
            if (DetailedDiagnostics)
                PXTrace.WriteInformation(message, args);
        }

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

        public virtual ALModel GetModelById(Guid? modelId)
        {
            if (modelId == null || modelId == Guid.Empty)
                return null;

            return PXSelect<
                ALModel,
                Where<ALModel.labelID, Equal<Required<ALModel.labelID>>>>
                .Select(_graph, modelId);
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

            WriteDiagnostic(
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
                WriteDiagnostic("[CHECKBOX] Clearing UsrALPrintLabel on all packages for shipment {0}", shipmentNbr);
                
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
                        WriteDiagnostic("[CHECKBOX] ⚠️ Error clearing UsrALPrintLabel on package line {0}: {1}", 
                            pkg.LineNbr, setEx.Message);
                    }
                }

                WriteDiagnostic("[CHECKBOX] ✅ Cleared UsrALPrintLabel on {0} package rows", clearedCount);
            }
            catch (Exception ex)
            {
                WriteDiagnostic("[CHECKBOX] ⚠️ Error in ClearAllPackagePrintFlags: {0}", ex.Message);
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
                WriteDiagnostic("[CHECKBOX] Setting UsrALPrintLabel on selected package line {0} for shipment {1}", 
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

                WriteDiagnostic("[CHECKBOX] ✅ Set UsrALPrintLabel=true on package line {0}", selectedPackageLineNbr);
            }
            catch (Exception ex)
            {
                WriteDiagnostic("[CHECKBOX] ⚠️ Error in SetOnlySelectedPackagePrintFlag: {0}", ex.Message);
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

            WriteDiagnostic("[SERVICE] Verifying package {0} exists in shipment {1}", selectedPackageLineNbr, shipment.ShipmentNbr);

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

            WriteDiagnostic(
                $"[SERVICE] Package {selectedPackageLineNbr} verified. Graph type: {_graph.GetType().FullName}");

            // ✅ [PKG PRINT] Diagnostics: Log selected package state BEFORE CreatePrintContext
            WriteDiagnostic("[PKG PRINT] Shipment={0}", shipment.ShipmentNbr);
            WriteDiagnostic("[PKG PRINT] Selected LineNbr={0}", selectedPackageLineNbr);
            
            // Get UCC128 via reflection
            object selectedUcc128 = null;
            try
            {
                selectedUcc128 = packageToVerify.GetType().GetProperty("UsrTCUCC128")?.GetValue(packageToVerify);
            }
            catch { }
            WriteDiagnostic("[PKG PRINT] Selected UsrTCUCC128={0}", selectedUcc128 ?? "null");

            WriteDiagnostic(
                $"[SERVICE] Row-selection native print: shipment {shipment.ShipmentNbr} will print package line {selectedPackageLineNbr}");

            // ✅ Determine the model's BasedOnView to understand the data structure
            string basedOnViewName = null;
            
            try
            {
                ALModel resolvedModel = GetModelById(modelId);
                if (resolvedModel != null)
                {
                    basedOnViewName = resolvedModel.BasedOnView;
                    WriteDiagnostic("[SERVICE] Model {0} is based on view: {1}", resolvedModel.Name, basedOnViewName ?? "null");
                }
                
                if (string.IsNullOrWhiteSpace(basedOnViewName))
                {
                    basedOnViewName = "ALPackages";
                    WriteDiagnostic("[SERVICE] Model has no BasedOnView specified, using default: {0}", basedOnViewName);
                }
            }
            catch (Exception ex)
            {
                WriteDiagnostic("[SERVICE] Error determining BasedOnView: {0}", ex.Message);
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
                WriteDiagnostic("[CHECKBOX] ✅ Graph saved after setting print flags");

                // Step 4: Activate filter scope for the selected package
                // ✅ CRITICAL: Use int?[] to match Activate's signature: IEnumerable<int?>
                using (ALPackagesFilterScope.Activate(shipment.ShipmentNbr, new int?[] { selectedPackageLineNbr }))
                {
                    WriteDiagnostic("[CHECKBOX] ✅ ALPackagesFilterScope activated for package line {0}", selectedPackageLineNbr);

                    // Use Asgard's native view-enumerating context. The temporary
                    // UsrALPrintLabel flag determines which package actually prints.
                    WriteDiagnostic("[CHECKBOX] ✅ Calling CreatePrintContext with BasedOnView={0}, ModelID={1}", 
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

                        WriteDiagnostic(
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
                        WriteDiagnostic("[CHECKBOX-VERIFY] ⚠️ WARNING: verifyPackage is null after selecting LineNbr={0}", selectedPackageLineNbr);
                    }

                    SOPackageDetailEx selectedLabelRow = verifyPackage ?? packageToVerify;
                    object selectedUccValue = _graph.Packages.Cache.GetValue(
                        selectedLabelRow,
                        "UsrTCUCC128");
                    string selectedUccForTrace = selectedUccValue?.ToString() ?? "<null>";

                    // Follow Asgard's working native action path: enumerate the model view
                    // and let CheckLineDoPrint select the one package whose flag was set above.
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
                    WriteDiagnostic("[CHECKBOX] ✅ CreatePrintContext succeeded. Model={0}, Printer={1}", 
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

                    WriteDiagnostic(
                        $"[CHECKBOX] ✅ Print context ready: Model={printContext.Model.Name}, Printer={printContext.Printer.Name}, Shipment={shipment.ShipmentNbr}, Package={selectedPackageLineNbr}");

                    // ✅ Call PrintLabels while filter scope is active AND checkbox is set
                    // Diagnostic only: compare the requested/context package with the first
                    // row exposed by the model's actual BasedOnView. This determines whether
                    // an unfiltered native Packages view is redirecting the label to its top row.
                    string firstPackageFromModelView = "<not available>";
                    string firstUccFromModelView = "<not available>";
                    try
                    {
                        string modelViewName = printContext.Model.BasedOnView;
                        if (!string.IsNullOrWhiteSpace(modelViewName)
                            && printContext.Graph.Views.ContainsKey(modelViewName))
                        {
                            PXView modelView = printContext.Graph.Views[modelViewName];
                            IEnumerable modelRows = modelView.SelectMultiBound(
                                new object[] { printContext.Row });

                            foreach (object modelRow in modelRows)
                            {
                                SOPackageDetail packageFromView =
                                    PXResult.Unwrap<SOPackageDetail>(modelRow);
                                firstPackageFromModelView = packageFromView?.LineNbr?.ToString()
                                    ?? "<row has no package>";

                                if (packageFromView != null)
                                {
                                    PXCache packageFromViewCache =
                                        printContext.Graph.Caches[packageFromView.GetType()];
                                    object viewUccValue = packageFromViewCache.GetValue(
                                        packageFromView,
                                        "UsrTCUCC128");
                                    firstUccFromModelView = viewUccValue?.ToString() ?? "<null>";
                                }
                                break;
                            }
                        }
                        else
                        {
                            firstPackageFromModelView = "<view missing>";
                        }
                    }
                    catch (Exception viewProbeEx)
                    {
                        firstPackageFromModelView = "<probe error: " + viewProbeEx.Message + ">";
                        firstUccFromModelView = "<probe error>";
                    }

                    // Filter ensures Asgard gets only the selected package row
                    // Checkbox ensures Asgard's NbCopies logic sees the row as eligible
                    try
                    {
                        WriteDiagnostic("[CHECKBOX] ✅ Calling PrintLabels...");
                        PrintResults results = _labelGenerator.PrintLabels(printContext);
                        
                        if (results == null)
                            throw new PXException("PrintLabels returned null.");

                        PXTrace.WriteInformation(
                            "[ASGARD-PRINT] Complete: Labels={0}, RequestedPackage={1}, " +
                            "ContextPackage={2}, SelectedUCC={3}, Model={4}, View={5}, " +
                            "ViewFirstPackage={6}, ViewFirstUCC={7}",
                            results.NbLabels,
                            selectedPackageLineNbr,
                            selectedLabelRow.LineNbr,
                            selectedUccForTrace,
                            printContext.Model.Name,
                            printContext.Model.BasedOnView ?? "<null>",
                            firstPackageFromModelView,
                            firstUccFromModelView);
                        
                        if (results.NbLabels == 1)
                        {
                            WriteDiagnostic("[RESULT] ✅ SUCCESS: Single label printed for package line {0}", selectedPackageLineNbr);
                        }
                        else if (results.NbLabels == 0)
                        {
                            WriteDiagnostic("[RESULT] ⚠️ WARNING: No labels generated for package line {0}", selectedPackageLineNbr);
                        }
                        else
                        {
                            WriteDiagnostic("[RESULT] ⚠️ UNEXPECTED: {0} labels printed (expected 1) for package line {1}", 
                                results.NbLabels, selectedPackageLineNbr);
                        }

                        WriteDiagnostic(
                            $"[SERVICE] Print completed: Shipment={shipment.ShipmentNbr}, Package={selectedPackageLineNbr}, NbLabels={results.NbLabels}");

                        return results;
                    }
                    catch (Exception printEx)
                    {
                        WriteDiagnostic("[SERVICE] PrintLabels exception: {0}", printEx.GetType().FullName);
                        WriteDiagnostic("[SERVICE] Exception message: {0}", printEx.Message);
                        throw;
                    }
                }  // End of ALPackagesFilterScope using block
            }
            finally
            {
                // ✅ CRITICAL: Clear all package print flags in finally block
                // This runs even if printing fails, ensuring clean state
                WriteDiagnostic("[CHECKBOX] Finally block: Clearing all package print flags...");
                try
                {
                    ClearAllPackagePrintFlags(shipment.ShipmentNbr);
                    _graph.Actions.PressSave();
                    WriteDiagnostic("[CHECKBOX] ✅ Finally: All package flags cleared and saved");
                }
                catch (Exception cleanupEx)
                {
                    PXTrace.WriteWarning("[ASGARD-PRINT] Package flag cleanup failed: {0}", cleanupEx.Message);
                    // Don't re-throw from finally - let the original exception propagate if there was one
                }
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
        /// 2. Filters for package-based models (Packages, ALPackages, ALiStarPackages)
        /// 3. Evaluates FilterRuleID and then PrintRuleID using Asgard's public Scriban evaluator
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
        public virtual Guid? ResolveModelIdByAsgardRules(
            SOShipment shipment,
            SOPackageDetailEx selectedPackage)
        {
            PXTrace.WriteInformation(
                "[MODEL-RESOLVE-NATIVE] Shipment={0}, Package={1}",
                shipment?.ShipmentNbr ?? "<null>",
                selectedPackage?.LineNbr);

            ValidateShipmentForAsgardPrint(shipment);

            if (selectedPackage == null)
                throw new PXException("A selected package is required to resolve the Asgard label model.");

            if (!string.Equals(
                shipment.ShipmentNbr,
                selectedPackage.ShipmentNbr,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new PXException(
                    "Selected package {0} does not belong to shipment {1}.",
                    selectedPackage.LineNbr,
                    shipment.ShipmentNbr);
            }

            _graph.Document.Current = shipment;
            _graph.Packages.Current = selectedPackage;

            List<ALModel> activeShipmentModels = PXSelect<
                ALModel,
                Where<
                    ALModel.active, Equal<True>,
                    And<ALModel.screenID, Equal<Required<ALModel.screenID>>>>>
                .Select(_graph, "SO302000")
                .RowCast<ALModel>()
                .ToList();

            foreach (ALModel model in activeShipmentModels)
            {
                bool packageBased = IsPackageBasedModel(model);

                WriteDiagnostic(
                    "[MODEL-DIAG-NATIVE] ModelID={0}, Name={1}, Active={2}, ScreenID={3}, " +
                    "BasedOnView='{4}', FilterRuleID={5}, ReverseFilter={6}, " +
                    "PrintRuleID={7}, ReversePrint={8}, PackageBased={9}",
                    model.LabelID,
                    model.Name ?? "<null>",
                    model.Active,
                    model.ScreenID ?? "<null>",
                    model.BasedOnView ?? "<null>",
                    model.FilterRuleID,
                    model.ReverseFilter,
                    model.PrintRuleID,
                    model.ReversePrint,
                    packageBased);

                if (!packageBased)
                {
                    WriteDiagnostic(
                        "[MODEL-DIAG-NATIVE] Model '{0}' EXCLUDED: BasedOnView '{1}' is not " +
                        "Packages, ALPackages, or ALiStarPackages.",
                        model.Name ?? "<null>",
                        model.BasedOnView ?? "<null>");
                }
            }

            List<ALModel> packageModels = activeShipmentModels
                .Where(IsPackageBasedModel)
                .ToList();

            PXTrace.WriteInformation(
                "[MODEL-RESOLVE-NATIVE] Found {0} active package-based models for SO302000",
                packageModels.Count);

            if (packageModels.Count == 0)
            {
                throw new PXException(
                    "No active Asgard package label models were found for SO302000 using " +
                    "Packages, ALPackages, or ALiStarPackages.");
            }

            List<ALModel> matchingModels = new List<ALModel>();

            using (ALPackagesFilterScope.Activate(
                shipment.ShipmentNbr,
                new int?[] { selectedPackage.LineNbr }))
            {
                foreach (ALModel model in packageModels)
                {
                    try
                    {
                        WriteDiagnostic(
                            "[RULE-EVAL-NATIVE] Model={0}, ModelID={1}, Package={2}",
                            model.Name,
                            model.LabelID,
                            selectedPackage.LineNbr);

                        // Construct the same native context used for a one-row print. This
                        // resolves current-user printer eligibility before the model matches.
                        AcuLabelContext ruleContext = AcuLabelContext.CreateSingleRowPrintContext(
                            _graph.GetType(),
                            shipment,
                            selectedPackage,
                            model.LabelID,
                            shipment.CustomerID);

                        PXCache packageCache = ruleContext.Graph.Caches[typeof(SOPackageDetail)];
                        packageCache.Current = selectedPackage;

                        bool filterMatched = EvaluateNativeModelRule(
                            ruleContext,
                            model,
                            model.FilterRuleID,
                            model.ReverseFilter == true,
                            "FilterRuleID");

                        if (!filterMatched)
                        {
                            PXTrace.WriteInformation(
                                "[RULE-MATCH-NATIVE] Model {0} EXCLUDED by FilterRuleID",
                                model.Name);
                            continue;
                        }

                        bool printMatched = EvaluateNativeModelRule(
                            ruleContext,
                            model,
                            model.PrintRuleID,
                            model.ReversePrint == true,
                            "PrintRuleID");

                        if (!printMatched)
                        {
                            PXTrace.WriteInformation(
                                "[RULE-MATCH-NATIVE] Model {0} EXCLUDED by PrintRuleID",
                                model.Name);
                            continue;
                        }

                        matchingModels.Add(model);
                        PXTrace.WriteInformation(
                            "[RULE-MATCH-NATIVE] Model {0} INCLUDED; Printer={1}",
                            model.Name,
                            ruleContext.Printer?.Name ?? "<null>");
                    }
                    catch (Exception ex)
                    {
                        PXTrace.WriteWarning(
                            "[MODEL-DIAG-NATIVE] Model '{0}' EXCLUDED while creating/evaluating " +
                            "the native context: {1}",
                            model.Name,
                            ex.Message);
                    }
                }
            }

            PXTrace.WriteInformation(
                "[MODEL-SELECT-NATIVE] Total matching models: {0}",
                matchingModels.Count);

            if (matchingModels.Count == 0)
            {
                throw new PXException(
                    "No Asgard package label model matched shipment {0}, package {1}. " +
                    "Candidates: {2}.",
                    shipment.ShipmentNbr,
                    selectedPackage.LineNbr,
                    string.Join(", ", packageModels.Select(m => m.Name)));
            }

            if (matchingModels.Count > 1)
            {
                List<ALModel> uccModels = matchingModels
                    .Where(m => !string.IsNullOrWhiteSpace(m.Description)
                        && m.Description.IndexOf(
                            "UCC",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (uccModels.Count == 1)
                {
                    PXTrace.WriteInformation(
                        "[MODEL-SELECT-NATIVE] Multiple rule matches; selected the only " +
                        "model whose Description contains UCC: {0}",
                        uccModels[0].Name);
                    matchingModels = uccModels;
                }
                else if (uccModels.Count == 0)
                {
                    throw new PXException(
                        "Multiple Asgard package label models matched shipment {0}, package {1}, " +
                        "but none has a Description containing 'UCC': {2}. " +
                        "Mark exactly one intended UCC model in its Description.",
                        shipment.ShipmentNbr,
                        selectedPackage.LineNbr,
                        string.Join(", ", matchingModels.Select(m => m.Name)));
                }
                else
                {
                    throw new PXException(
                        "Multiple Asgard package label models matched shipment {0}, package {1}, " +
                        "and more than one has a Description containing 'UCC': {2}. " +
                        "Only one matching model may be marked as UCC.",
                        shipment.ShipmentNbr,
                        selectedPackage.LineNbr,
                        string.Join(", ", uccModels.Select(m => m.Name)));
                }
            }

            ALModel selectedModel = matchingModels[0];
            PXTrace.WriteInformation(
                "[MODEL-SELECT-NATIVE] Selected model {0}, View={1}, ID={2}",
                selectedModel.Name,
                selectedModel.BasedOnView ?? "<null>",
                selectedModel.LabelID);

            return selectedModel.LabelID;
        }

        private static bool IsPackageBasedModel(ALModel model)
        {
            if (model == null)
                return false;

            return string.Equals(model.BasedOnView, "Packages", StringComparison.Ordinal)
                || string.Equals(model.BasedOnView, "ALPackages", StringComparison.Ordinal)
                || string.Equals(model.BasedOnView, "ALiStarPackages", StringComparison.Ordinal);
        }

        private bool EvaluateNativeModelRule(
            AcuLabelContext context,
            ALModel model,
            Guid? ruleId,
            bool reverse,
            string ruleField)
        {
            ALRule rule = LoadRuleById(ruleId);

            if (ruleId != null && ruleId != Guid.Empty && rule == null)
            {
                throw new PXException(
                    "Rule {0} referenced by {1} on model '{2}' could not be loaded.",
                    ruleId,
                    ruleField,
                    model.Name);
            }

            WriteDiagnostic(
                "[RULE-EVAL-NATIVE] Model={0}, Stage={1}, RuleID={2}, RuleName={3}, " +
                "Active={4}, Reverse={5}, Expression={6}",
                model.Name,
                ruleField,
                ruleId,
                rule?.Name ?? "<none>",
                rule?.Active,
                reverse,
                rule?.Expression ?? "<none>");

            // RuleUtils is internal in this Asgard build. This mirrors its decompiled
            // EvalRule behavior using the public evaluator: a missing/empty rule passes,
            // otherwise evaluate with a true default and apply the reverse flag.
            string expression = rule?.Expression;
            bool matched = rule == null || string.IsNullOrEmpty(expression)
                ? true
                : NewScribanUtils.EvalExpr<bool>(context, expression, true);

            if (rule != null && !string.IsNullOrEmpty(expression) && reverse)
                matched = !matched;

            WriteDiagnostic(
                "[RULE-EVAL-NATIVE] Model={0}, Stage={1}, Result={2}",
                model.Name,
                ruleField,
                matched);

            return matched;
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
                    WriteDiagnostic("[RULE-LOAD] Rule {0} not found in database", ruleId);
                }

                return rule;
            }
            catch (Exception ex)
            {
                WriteDiagnostic("[RULE-LOAD] Error loading rule {0}: {1}", ruleId, ex.Message);
                throw new PXException("Error loading rule {0} from database: {1}", ruleId, ex.Message);
            }
        }

    }
}
