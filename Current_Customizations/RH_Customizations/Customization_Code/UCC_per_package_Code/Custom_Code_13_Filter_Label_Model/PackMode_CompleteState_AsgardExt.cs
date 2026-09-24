using System;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace AA.Objects.AL.Integration.PerPackage
{
    /// <summary>
    /// ========================================================================
    /// WMS SCAN HOOK - Package Confirmation Integration
    /// ========================================================================
    /// 
    /// This extension hooks into the WMS Pack Mode state machine to trigger
    /// Asgard label printing when a package is confirmed via scanning.
    /// 
    /// Hook Point: PackMode.BoxConfirming.CompleteState.Logic.SettleAndConfirmPackage
    /// 
    /// Why this hook is correct:
    /// - Fires AFTER package is confirmed (package.Confirmed = true)
    /// - Runs AFTER Basis.Save.Press() completes
    /// - Runs BEFORE ClearStates() and Reset() that follow
    /// - Safest available point in WMS execution flow
    /// - Direct access to SOPackageDetailEx package parameter
    /// 
    /// Architecture:
    /// SettleAndConfirmPackage override
    ///   → Capture shipmentNbr + lineNbr BEFORE baseMethod
    ///   → Call baseMethod(package) - does the actual confirmation
    ///   → Start PXLongOperation AFTER base
    ///   → Call PrintForPackageCore - reuses button print path (SHARED LOGIC)
    /// 
    /// Why no duplication:
    /// - Both button and scan call the same PrintForPackageCore method
    /// - No code duplication = no drift between paths
    /// - Changes to print logic automatically update both routes
    /// 
    /// Why no nested operations:
    /// - baseMethod() completes before we start our PXLongOperation
    /// - Fresh graph ensures isolated print context
    /// - No "previous operation not completed" race conditions
    /// </summary>
    public class PackMode_CompleteState_Logic_AsgardExt : 
        PXGraphExtension<
            PickPackShip.PackMode.BoxConfirming.CompleteState.Logic,
            PickPackShip.Host>
    {
        public static bool IsActive() => true;

        /// <summary>
        /// ✅ WMS SCAN HOOK: Triggers when a package is confirmed
        /// 
        /// Override of SettleAndConfirmPackage from CompleteState.Logic
        /// 
        /// This method:
        /// 1. Captures shipmentNbr + lineNbr BEFORE confirmation
        /// 2. Calls baseMethod to actually confirm the package
        /// 3. After confirmation succeeds, starts print operation
        /// 4. Calls PrintForPackageCore (shared with button path)
        /// </summary>
        [PXOverride]
        public virtual void SettleAndConfirmPackage(
            SOPackageDetailEx package,
            Action<SOPackageDetailEx> base_SettleAndConfirmPackage)
        {
            // ✅ STEP 1: Capture critical data BEFORE baseMethod
            // baseMethod will call ClearStates() and Reset(), so we capture NOW
            string shipmentNbr = package?.ShipmentNbr;
            int? lineNbr = package?.LineNbr;
            bool wasNotConfirmed = package?.Confirmed != true;

            PXTrace.WriteInformation(
                "[SCAN-HOOK] SettleAndConfirmPackage override: Shipment={0}, Package={1}, WasConfirmed={2}",
                shipmentNbr ?? "null", lineNbr, !wasNotConfirmed);

            // ✅ STEP 2: Call baseMethod
            // This does the actual confirmation:
            // - ApplyChanges(package)
            // - package.Confirmed = true
            // - Graph.Packages.Update(package)
            // - Basis.Save.Press()
            // - ClearStates()
            // - Basis.Reset(fullReset: false)
            // - Basis.ReportInfo(Msg.Success)
            PXTrace.WriteInformation("[SCAN-HOOK] Calling baseMethod for package confirmation");

            try
            {
                base_SettleAndConfirmPackage(package);
                PXTrace.WriteInformation("[SCAN-HOOK] ✅ baseMethod completed successfully");
            }
            catch (Exception ex)
            {
                PXTrace.WriteInformation("[SCAN-HOOK] ❌ baseMethod threw exception: {0}", ex.Message);
                throw;
            }

            // ✅ STEP 3: After base completes, queue print if conditions are met
            // We are now in the safest available point within the WMS method
            // (after confirmation, before complete cleanup)
            // 
            // The captured shipmentNbr and lineNbr are safe to use because
            // they were captured BEFORE baseMethod reset the state
            if (wasNotConfirmed && !string.IsNullOrEmpty(shipmentNbr) && lineNbr != null)
            {
                PXTrace.WriteInformation(
                    "[SCAN-HOOK] Package was confirmed, queueing Asgard print: Shipment={0}, Package={1}",
                    shipmentNbr, lineNbr);

                try
                {
                    // ✅ STEP 4: Start isolated PXLongOperation for printing
                    // This delegates to the shared PrintForPackageCore path from SOShipmentEntry_AsgardExt
                    // 
                    // Why PXLongOperation here:
                    // - Isolates print logic from WMS state machine
                    // - Uses fresh SOShipmentEntry graph for clean context
                    // - No race conditions or nested operation conflicts
                    PXLongOperation.StartOperation(Base, delegate()
                    {
                        PXTrace.WriteInformation(
                            "[SCAN-HOOK-LONGOP] Long operation started for print: Shipment={0}, Package={1}",
                            shipmentNbr, lineNbr.Value);

                        // ✅ Create fresh graph for isolated print context
                        SOShipmentEntry graph = PXGraph.CreateInstance<SOShipmentEntry>();

                        // ✅ Get the Asgard extension to call shared print logic
                        var asgardExt = graph.FindImplementation<SOShipmentEntry_AsgardExt>();
                        if (asgardExt == null)
                        {
                            PXTrace.WriteError("[SCAN-HOOK-LONGOP] ❌ SOShipmentEntry_AsgardExt not found");
                            throw new PXException("Asgard label extension not found on SOShipmentEntry");
                        }

                        // ✅ Call the shared print core logic
                        // This is the SAME method called by the manual button print path
                        // Both paths now use identical print logic
                        PXTrace.WriteInformation(
                            "[SCAN-HOOK-LONGOP] Calling PrintForPackageCore: Shipment={0}, Package={1}",
                            shipmentNbr, lineNbr.Value);

                        asgardExt.PrintForPackageCore(shipmentNbr, lineNbr.Value);

                        PXTrace.WriteInformation(
                            "[SCAN-HOOK-LONGOP] ✅ PrintForPackageCore completed successfully");
                    });

                    PXTrace.WriteInformation(
                        "[SCAN-HOOK] ✅ Print operation queued successfully for package {0}",
                        lineNbr);
                }
                catch (Exception printEx)
                {
                    PXTrace.WriteError("[SCAN-HOOK] ❌ Error queuing print: {0}", printEx.Message);
                    PXTrace.WriteError("[SCAN-HOOK] Stack: {0}", printEx.StackTrace);
                    // Don't re-throw - package was already confirmed successfully
                    // Print failure shouldn't fail the confirmation
                    // Note: ReportError not available in WMS context, trace logs the error instead
                }
            }
            else
            {
                PXTrace.WriteInformation(
                    "[SCAN-HOOK] Skipping print: wasNotConfirmed={0}, shipmentNbr={1}, lineNbr={2}",
                    wasNotConfirmed, shipmentNbr ?? "null", lineNbr);
            }
        }
    }
}
