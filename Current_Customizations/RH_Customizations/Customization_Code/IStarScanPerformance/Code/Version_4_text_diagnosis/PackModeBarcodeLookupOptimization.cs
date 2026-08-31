using System;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
    /// <summary>
    /// Replaces the Master Pack barcode resolver after WMS.PackModeLogicExt
    /// installs its handler. The original handler loads every shipment split
    /// and then executes an INItemXRef query for each split until it finds a
    /// match. This implementation resolves the barcode first and then checks
    /// shipment membership with a single existence query.
    /// </summary>
    public class PackModeBarcodeLookupOptimization
        : BarcodeDrivenStateMachine<
            PickPackShip,
            PickPackShip.Host>
            .ScanExtension<
                WMS.PackModeLogicExt>
    {
        public static bool IsActive()
        {
            return true;
        }

        [PXOverride]
        public virtual ScanState<PickPackShip> DecorateScanState(
            ScanState<PickPackShip> original,
            Func<
                ScanState<PickPackShip>,
                ScanState<PickPackShip>> base_DecorateScanState)
        {
            ScanState<PickPackShip> state =
                base_DecorateScanState(original);

            var itemState =
                state as WarehouseManagementSystem<
                    PickPackShip,
                    PickPackShip.Host>
                    .InventoryItemState;

            if (itemState != null)
            {
                /*
                 * This runs after WMS.PackModeLogicExt.DecorateScanState,
                 * replacing only its expensive GetByBarcode handler.
                 */
                itemState.Intercept.GetByBarcode.ByReplace(
                    FindShipmentItemByBarcode,
                    null);
            }

            return state;
        }

        private PXResult<INItemXRef, InventoryItem>
            FindShipmentItemByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode) ||
                string.IsNullOrWhiteSpace(Basis.RefNbr))
            {
                return null;
            }

            /*
             * A barcode can have more than one cross-reference. Query all
             * matching references once, while retaining the original
             * preference for BAR and GIN references.
             */
            PXResultset<INItemXRef> matches =
                PXSelectReadonly2<
                    INItemXRef,
                    InnerJoin<
                        InventoryItem,
                        On<
                            InventoryItem.inventoryID,
                            Equal<INItemXRef.inventoryID>>>,
                    Where<
                        INItemXRef.alternateID,
                        Equal<Required<INItemXRef.alternateID>>>>
                .Select(
                    Basis,
                    barcode);

            PXResult<INItemXRef, InventoryItem> result =
                FindMatchingReference(
                    matches,
                    true);

            if (result == null)
            {
                result =
                    FindMatchingReference(
                        matches,
                        false);
            }

            if (result != null)
            {
                return result;
            }

            /*
             * Preserve the original fallback that accepts InventoryCD as
             * the scanned value when no INItemXRef record matches.
             */
            InventoryItem inventory =
                InventoryItem.UK.Find(
                    Basis,
                    barcode,
                    PKFindOptions.None);

            if (inventory?.InventoryID == null ||
                !InventoryExistsOnShipment(
                    inventory.InventoryID))
            {
                return null;
            }

            var syntheticReference =
                new INItemXRef
                {
                    InventoryID = inventory.InventoryID,
                    AlternateType = "BAR",
                    AlternateID = barcode
                };

            object defaultSubItem;

            Basis.Graph
                .Caches<INItemXRef>()
                .RaiseFieldDefaulting<INItemXRef.subItemID>(
                    syntheticReference,
                    out defaultSubItem);

            syntheticReference.SubItemID =
                (int?)defaultSubItem;

            return new PXResult<INItemXRef, InventoryItem>(
                syntheticReference,
                inventory);
        }

        private PXResult<INItemXRef, InventoryItem>
            FindMatchingReference(
                PXResultset<INItemXRef> matches,
                bool preferredTypesOnly)
        {
            foreach (PXResult<INItemXRef, InventoryItem> match
                in matches)
            {
                INItemXRef crossReference = match;
                InventoryItem inventory = match;

                bool preferredType =
                    string.Equals(
                        crossReference?.AlternateType,
                        "BAR",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        crossReference?.AlternateType,
                        "GIN",
                        StringComparison.OrdinalIgnoreCase);

                if (preferredType != preferredTypesOnly)
                {
                    continue;
                }

                if (inventory?.InventoryID != null &&
                    InventoryExistsOnShipment(
                        inventory.InventoryID))
                {
                    return new PXResult<
                        INItemXRef,
                        InventoryItem>(
                            crossReference,
                            inventory);
                }
            }

            return null;
        }

        private bool InventoryExistsOnShipment(
            int? inventoryID)
        {
            if (inventoryID == null)
            {
                return false;
            }

            /*
             * Do not filter IsUnassigned here. Standard PickedForPack calls
             * GetSplits(..., includeUnassigned: true), so checking every
             * split preserves the original membership behavior.
             */
            SOShipLineSplit split =
                PXSelectReadonly<
                    SOShipLineSplit,
                    Where<
                        SOShipLineSplit.shipmentNbr,
                        Equal<Required<
                            SOShipLineSplit.shipmentNbr>>,
                        And<
                            SOShipLineSplit.inventoryID,
                            Equal<Required<
                                SOShipLineSplit.inventoryID>>>>>
                .SelectWindowed(
                    Basis,
                    0,
                    1,
                    Basis.RefNbr,
                    inventoryID);

            return split != null;
        }
    }
}
