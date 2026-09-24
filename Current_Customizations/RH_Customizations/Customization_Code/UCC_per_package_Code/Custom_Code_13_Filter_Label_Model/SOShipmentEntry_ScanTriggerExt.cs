using System;
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    /// <summary>
    /// ========================================================================
    /// OLD SCAN TRIGGER - DISABLED (PHASE 1 ONLY)
    /// ========================================================================
    /// 
    /// This extension is INTENTIONALLY DISABLED.
    /// 
    /// WHY DISABLED:
    /// - RowPersisted fires during the Persist() cycle
    /// - It was causing "The previous operation has not been completed yet" errors
    /// - Created race conditions with button print operations
    /// 
    /// REPLACEMENT:
    /// The scan hook has been moved to WMS Pack Mode:
    /// → PackMode_CompleteState_AsgardExt.cs
    /// → Hooks into SettleAndConfirmPackage (the actual confirmation method)
    /// → Runs after confirmation is complete, not during persistence
    /// → No race conditions, no nested operations
    /// 
    /// FUTURE REFERENCE:
    /// If you need to re-examine RowPersisted logic, see the commented code below.
    /// But DO NOT use RowPersisted for scan integration - use the WMS hook instead.
    /// </summary>
    public class SOShipmentEntry_ScanTriggerExt : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive() => false;  // ✅ DISABLED - using WMS hook instead

        protected virtual void _(Events.RowPersisted<SOPackageDetail> e)
        {
            // DISABLED: This trigger is not used for Phase 2+
            // See PackMode_CompleteState_AsgardExt.cs for the WMS scan hook
            PXTrace.WriteInformation("[SCAN-TRIGGER] ℹ️ Old RowPersisted trigger is disabled. Using WMS hook instead.");
            return;

            /*
            // ============================================================================
            // OLD CODE (DISABLED - Reference only)
            // ============================================================================
            // This code is kept for historical reference only.
            // DO NOT enable this - use the WMS hook instead.
            // 
            // try
            // {
            //     PXTrace.WriteInformation("=== SCAN TRIGGER: RowPersisted<SOPackageDetail> event fired ===");
            //
            //     SOPackageDetail package = e.Row;
            //     SOShipment shipment = Base.Document.Current;
            //
            //     if (shipment == null || package == null)
            //     {
            //         PXTrace.WriteInformation("[SKIP] Shipment or package is null");
            //         return;
            //     }
            //
            //     if (package.Confirmed != true)
            //     {
            //         PXTrace.WriteInformation("[SKIP] Package not confirmed");
            //         return;
            //     }
            //
            //     PXTrace.WriteInformation("[PASS] Package {0} confirmed in shipment {1}",
            //         package.LineNbr,
            //         shipment.ShipmentNbr);
            //
            //     string shipmentNbr = shipment.ShipmentNbr;
            //     int packageLineNbr = (int)package.LineNbr;
            //
            //     PXLongOperation.StartOperation(Base, delegate()
            //     {
            //         PXTrace.WriteInformation("[LONGOP] Started for package {0}", packageLineNbr);
            //
            //         SOShipmentEntry graph = PXGraph.CreateInstance<SOShipmentEntry>();
            //         SOShipment reloadedShipment = SOShipment.PK.Find(graph, shipmentNbr);
            //
            //         if (reloadedShipment == null)
            //         {
            //             PXTrace.WriteError("[ERROR] Could not reload shipment {0}", shipmentNbr);
            //             return;
            //         }
            //
            //         graph.Document.Current = reloadedShipment;
            //
            //         var asgardExt = graph.FindImplementation<SOShipmentEntry_AsgardExt>();
            //
            //         if (asgardExt == null)
            //         {
            //             PXTrace.WriteError("[ERROR] SOShipmentEntry_AsgardExt not found");
            //             return;
            //         }
            //
            //         PXTrace.WriteInformation("[CALLING] PrintForPackage() in long operation");
            //
            //         PXAdapter adapter = new PXAdapter(graph.Document);
            //         adapter.Searches = new string[] { };
            //         adapter.Parameters = new object[] { };
            //
            //         asgardExt.PrintForPackage(adapter, packageLineNbr);
            //
            //         PXTrace.WriteInformation("[SUCCESS] PrintForPackage() completed");
            //     });
            // }
            // catch (Exception ex)
            // {
            //     PXTrace.WriteError("[FATAL] Exception in RowPersisted: {0}", ex.Message);
            //     PXTrace.WriteError("[STACK] {0}", ex.StackTrace);
            // }
            */
        }
    }
}
