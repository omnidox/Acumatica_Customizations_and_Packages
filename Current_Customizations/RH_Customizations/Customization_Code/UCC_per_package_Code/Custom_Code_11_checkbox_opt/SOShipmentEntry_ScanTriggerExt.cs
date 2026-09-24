using System;
using PX.Data;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    public class SOShipmentEntry_ScanTriggerExt : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive() => true;

        /// <summary>
        /// ========================================================================
        /// TEMPORARY DISABLE NOTICE - PHASE 1 TESTING
        /// ========================================================================
        /// 
        /// This scan trigger is DISABLED during testing phase.
        /// 
        /// REASON FOR DISABLE:
        /// - RowPersisted fires during the Persist() cycle
        /// - When the user clicks "Print Asgard Label" button, PrintForPackage() calls PressSave()
        /// - PressSave() triggers Persist() → fires RowPersisted events
        /// - This creates a RACE CONDITION: both button print and scan trigger try to start
        ///   PXLongOperation simultaneously
        /// - Result: Acumatica throws "Error: The previous operation has not been completed yet"
        /// 
        /// ISSUES IDENTIFIED:
        /// Issue #1: Missing parameter in original code (line ~72)
        ///   WRONG: asgardExt.PrintForPackage(adapter);
        ///   RIGHT: asgardExt.PrintForPackage(adapter, packageLineNbr);
        /// 
        /// Issue #2: PXLongOperation.StartOperation called from RowPersisted
        ///   This happens during the Persist cycle, causing race with button action
        /// 
        /// TESTING ROADMAP:
        /// Phase 1: Test button printing only (this trigger disabled)
        ///   - Verify "Print Asgard Label" button works in isolation
        ///   - Confirm labels print correctly for selected package
        ///   - NO concurrent operation errors
        /// 
        /// Phase 2: Re-enable scan trigger (AFTER button path is solid)
        ///   - FIX: Pass packageLineNbr parameter to PrintForPackage()
        ///   - CHANGE: Remove PXLongOperation from RowPersisted
        ///   - NEW APPROACH: Call PrintForPackage directly OR defer the operation
        ///   - Test: Confirm package triggers print without concurrent-operation errors
        /// 
        /// Phase 3: Production validation (if Phase 1+2 pass)
        ///   - Load test and quality verification
        /// ========================================================================
        protected virtual void _(Events.RowPersisted<SOPackageDetail> e)
        {
            // DISABLED: Return early to skip scan trigger during Phase 1 testing
            PXTrace.WriteInformation("[SCAN-TRIGGER] ⚠️ Scan trigger is DISABLED for Phase 1 testing. See code comments for roadmap.");
            return;

            /*
            // ============================================================================
            // ORIGINAL CODE (DISABLED) - Will be refactored after button path works
            // ============================================================================
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
            //         // ⚠️ ISSUE #1: Missing parameter below
            //         // WRONG: asgardExt.PrintForPackage(adapter);
            //         // RIGHT: asgardExt.PrintForPackage(adapter, packageLineNbr);
            //         // This loses the packageLineNbr, causing "No package is selected" error
            //         
            //         asgardExt.PrintForPackage(adapter, packageLineNbr);  // ← CORRECTED FOR PHASE 2
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