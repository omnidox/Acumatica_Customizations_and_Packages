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
        /// ========================================================================
        /// LAYER 1: PrintForPackage - Button Entry Point
        /// ========================================================================
        /// 
        /// Called ONLY by the "Print Asgard Label" button.
        /// Validates current state and delegates to QueuePrintForPackage.
        /// 
        /// Accepts optional selectedPackageLineNbr for testing/scan integration.
        /// If null, uses currently selected package from grid.
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

            // ✅ Log all available information from the selected package
            // This provides complete visibility into what fields are available during testing
            // Includes both base DAC fields AND extension fields (UsrTCUCC128, UsrCartonNbr, etc.)
            if (currentSelected != null)
            {
                PXTrace.WriteInformation("[PKG-ALL] === Base SOPackageDetail Fields ===");
                
                foreach (var prop in currentSelected.GetType().GetProperties())
                {
                    try
                    {
                        object value = prop.GetValue(currentSelected);
                        PXTrace.WriteInformation("[PKG-ALL] {0}: {1}", prop.Name, value?.ToString() ?? "null");
                    }
                    catch (Exception ex)
                    {
                        PXTrace.WriteInformation("[PKG-ALL] {0}: ERROR - {1}", prop.Name, ex.Message);
                    }
                }

                PXTrace.WriteInformation("[PKG-EXT] === DAC Extension Fields ===");

                PXCache cache = Base.Caches[currentSelected.GetType()];

                foreach (Type extType in cache.GetExtensionTypes())
                {
                    try
                    {
                        // Use reflection to invoke generic GetExtension<T>(row) method
                        var method = typeof(PXCache)
                            .GetMethods()
                            .FirstOrDefault(m =>
                                m.Name == "GetExtension" &&
                                m.IsGenericMethodDefinition &&
                                m.GetParameters().Length == 1);

                        if (method == null)
                        {
                            PXTrace.WriteInformation("[PKG-EXT] Could not find generic GetExtension<T>(row) method.");
                            continue;
                        }

                        object ext = method
                            .MakeGenericMethod(extType)
                            .Invoke(cache, new object[] { currentSelected });

                        if (ext == null)
                        {
                            PXTrace.WriteInformation("[PKG-EXT] Extension {0}: null", extType.FullName);
                            continue;
                        }

                        PXTrace.WriteInformation("[PKG-EXT] Extension: {0}", extType.FullName);

                        foreach (var prop in ext.GetType().GetProperties())
                        {
                            try
                            {
                                object value = prop.GetValue(ext, null);
                                PXTrace.WriteInformation("[PKG-EXT] {0}: {1}", prop.Name, value?.ToString() ?? "null");
                            }
                            catch (Exception ex)
                            {
                                PXTrace.WriteInformation("[PKG-EXT] {0}: ERROR - {1}", prop.Name, ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        PXTrace.WriteInformation("[PKG-EXT] Could not inspect extension {0}: {1}", extType.FullName, ex.Message);
                    }
                }

                // ✅ TARGETED CHECK: Log critical fields for Asgard label generation
                // This cuts through the noise and directly answers:
                // "Are UsrTCUCC128 and UsrCartonNbr populated at print time?"
                try
                {
                    PXCache cacheForCheck = Base.Caches[currentSelected.GetType()];
                    
                    object ucc128Value = null;
                    object cartonNbrValue = null;

                    // Search for the fields in each extension
                    foreach (Type extType in cacheForCheck.GetExtensionTypes())
                    {
                        try
                        {
                            var method = typeof(PXCache)
                                .GetMethods()
                                .FirstOrDefault(m =>
                                    m.Name == "GetExtension" &&
                                    m.IsGenericMethodDefinition &&
                                    m.GetParameters().Length == 1);

                            if (method == null)
                                continue;

                            object ext = method
                                .MakeGenericMethod(extType)
                                .Invoke(cacheForCheck, new object[] { currentSelected });

                            if (ext == null)
                                continue;

                            // Look for UsrTCUCC128
                            if (ucc128Value == null)
                            {
                                var ucc128Prop = ext.GetType().GetProperty("UsrTCUCC128");
                                if (ucc128Prop != null)
                                {
                                    try
                                    {
                                        ucc128Value = ucc128Prop.GetValue(ext);
                                    }
                                    catch { }
                                }
                            }

                            // Look for UsrCartonNbr
                            if (cartonNbrValue == null)
                            {
                                var cartonNbrProp = ext.GetType().GetProperty("UsrCartonNbr");
                                if (cartonNbrProp != null)
                                {
                                    try
                                    {
                                        cartonNbrValue = cartonNbrProp.GetValue(ext);
                                    }
                                    catch { }
                                }
                            }

                            // If we found both, stop searching
                            if (ucc128Value != null && cartonNbrValue != null)
                                break;
                        }
                        catch { }
                    }

                    PXTrace.WriteInformation(
                        "[PKG-CHECK] LineNbr={0}, UsrTCUCC128={1}, UsrCartonNbr={2}",
                        currentSelected.LineNbr,
                        ucc128Value?.ToString() ?? "null",
                        cartonNbrValue?.ToString() ?? "null");
                }
                catch (Exception checkEx)
                {
                    PXTrace.WriteInformation("[PKG-CHECK] ERROR reading critical fields: {0}", checkEx.Message);
                }
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

            // ✅ Delegate to LAYER 2
            QueuePrintForPackage(shipmentNbr, packageLineNbr);
        }

        /// <summary>
        /// ========================================================================
        /// LAYER 2: QueuePrintForPackage - UI-Safe Wrapper
        /// ========================================================================
        /// 
        /// Called by:
        /// - PrintForPackage (button UI layer)
        /// - SettleAndConfirmPackage override (WMS scan hook)
        /// 
        /// Responsibility: Start one clean PXLongOperation that uses a fresh graph.
        /// This is the boundary between UI/WMS context and the isolated print operation.
        /// 
        /// Does NOT start nested operations - each caller is responsible for its context.
        /// </summary>
        public virtual void QueuePrintForPackage(string shipmentNbr, int packageLineNbr)
        {
            PXTrace.WriteInformation("[QUEUE] QueuePrintForPackage called: Shipment={0}, Package={1}", 
                shipmentNbr, packageLineNbr);

            PXLongOperation.StartOperation(Base, delegate()
            {
                PXTrace.WriteInformation("[QUEUE] PXLongOperation started for shipment {0}", shipmentNbr);

                // ✅ LAYER 2→3 delegation
                PrintForPackageCore(shipmentNbr, packageLineNbr);

                PXTrace.WriteInformation("[QUEUE] PXLongOperation completed for shipment {0}", shipmentNbr);
            });
        }

        /// <summary>
        /// ========================================================================
        /// LAYER 3: PrintForPackageCore - Core Shared Logic
        /// ========================================================================
        /// 
        /// Called by:
        /// - QueuePrintForPackage (from button via LAYER 2)
        /// - Scan hook direct call (from WMS SettleAndConfirmPackage override)
        /// 
        /// Responsibility: 
        /// - Create fresh SOShipmentEntry graph
        /// - Resolve Asgard label model
        /// - Delegate to AsgardLabelService
        /// - Handle checkbox + filter scope + context + print
        /// 
        /// Assumes: Already inside PXLongOperation with isolated context
        /// 
        /// PUBLIC so that scan hook can call it directly from WMS context
        /// </summary>
        public virtual void PrintForPackageCore(string shipmentNbr, int packageLineNbr)
        {
            PXTrace.WriteInformation("[INTERNAL] PrintForPackageCore: Creating fresh graph for shipment {0}", 
                shipmentNbr);

            SOShipmentEntry graph = PXGraph.CreateInstance<SOShipmentEntry>();

            SOShipment shipmentInLongOp = SOShipment.PK.Find(graph, shipmentNbr);
            if (shipmentInLongOp == null)
            {
                throw new PXException(
                    $"Shipment '{shipmentNbr}' could not be reloaded inside the print operation.");
            }

            graph.Document.Current = shipmentInLongOp;

            // ✅ CRITICAL: Delegate to AsgardLabelService
            // The service handles:
            // 1. Checkbox management (UsrALPrintLabel state)
            // 2. Filter scope activation (ALPackagesFilterScope)
            // 3. Native CreatePrintContext() call
            // 4. PrintLabels() invocation
            
            var service = new AsgardLabelService(graph, _labelGenerator);

            // ✅ CRITICAL: Dynamically resolve the correct model based on Asgard rules
            // This replaces hardcoding and uses Asgard's confirmed rule evaluation system
            Guid? modelId = service.ResolveModelIdByAsgardRules(shipmentInLongOp);

            if (modelId == null || modelId == Guid.Empty)
            {
                throw new PXException(
                    "Could not resolve an Asgard label model for selected-package native printing. " +
                    "Please verify the Asgard label model rules for package models on SO302000.");
            }

            PXTrace.WriteInformation("[INTERNAL] Model resolved: ModelID={0}", modelId);

            // ✅ Service owns filter scope + checkbox + context + print
            // No outer wrapping needed here
            PrintResults results = service.PrintSelectedPackageUsingNativeContext(
                shipmentInLongOp,
                modelId,
                packageLineNbr,
                new PXAdapter(graph.Document));

            if (results == null)
                throw new PXException("Label printing returned no results.");

            if (results.NbLabels <= 0)
            {
                throw new PXException(
                    "No labels were generated. Please verify the selected package is valid and the selected label model is configured correctly.");
            }

            PXTrace.WriteInformation("[INTERNAL] ✅ Successfully printed {0} label(s) for package line {1}", 
                results.NbLabels, packageLineNbr);
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
