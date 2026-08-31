using System;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
    /// <summary>
    /// Stores threshold maps only for the current HTTP request. A separate
    /// entry is maintained for each host graph, shipment, and inventory item.
    /// </summary>
    internal static class QtyThresholdRequestCache
    {
        private const string SlotKey =
            "IStar.ScanPerformance.QtyThresholdRequestCache";

        internal sealed class Entry
        {
            public PickPackShip.Host Graph { get; set; }
            public string ShipmentNbr { get; set; }
            public int? InventoryID { get; set; }
            public Dictionary<int, decimal> ThresholdByLine { get; set; }
        }

        internal sealed class State
        {
            public List<Entry> Entries { get; } = new List<Entry>();
        }

        public static bool TryGet(
            PickPackShip.Host graph,
            string shipmentNbr,
            int? inventoryID,
            out Dictionary<int, decimal> thresholdByLine)
        {
            State state = PXContext.GetSlot<State>(SlotKey);

            if (state != null)
            {
                foreach (Entry entry in state.Entries)
                {
                    if (ReferenceEquals(entry.Graph, graph) &&
                        string.Equals(
                            entry.ShipmentNbr,
                            shipmentNbr,
                            StringComparison.OrdinalIgnoreCase) &&
                        entry.InventoryID == inventoryID)
                    {
                        thresholdByLine = entry.ThresholdByLine;
                        return true;
                    }
                }
            }

            thresholdByLine = null;
            return false;
        }

        public static void Store(
            PickPackShip.Host graph,
            string shipmentNbr,
            int? inventoryID,
            Dictionary<int, decimal> thresholdByLine)
        {
            State state = PXContext.GetSlot<State>(SlotKey);

            if (state == null)
            {
                state = new State();
                PXContext.SetSlot<State>(SlotKey, state);
            }

            state.Entries.Add(
                new Entry
                {
                    Graph = graph,
                    ShipmentNbr = shipmentNbr,
                    InventoryID = inventoryID,
                    ThresholdByLine = thresholdByLine
                });
        }
    }

    /// <summary>
    /// Replaces per-split GetQtyThreshold SQL calls in PickPackShip Pack mode
    /// with one batched SOLine/SOShipLine query per shipment and inventory.
    /// Other screens and scan modes retain the standard Acumatica behavior.
    /// </summary>
    public class PackModeQtyThresholdOptimization
        : PXGraphExtension<PickPackShip.Host>
    {
        public static bool IsActive()
        {
            return true;
        }

        public delegate decimal GetQtyThresholdDelegate(
            SOShipLineSplit split);

        [PXOverride]
        public virtual decimal GetQtyThreshold(
            SOShipLineSplit split,
            GetQtyThresholdDelegate baseMethod)
        {
            PickPackShip wms = Base.WMS;

            if (split == null ||
                string.IsNullOrEmpty(split.ShipmentNbr) ||
                split.LineNbr == null ||
                split.InventoryID == null ||
                wms == null ||
                !(wms.CurrentMode is PickPackShip.PackMode))
            {
                return baseMethod(split);
            }

            Dictionary<int, decimal> thresholdByLine;

            if (!QtyThresholdRequestCache.TryGet(
                Base,
                split.ShipmentNbr,
                split.InventoryID,
                out thresholdByLine))
            {
                thresholdByLine = LoadThresholds(
                    split.ShipmentNbr,
                    split.InventoryID);

                QtyThresholdRequestCache.Store(
                    Base,
                    split.ShipmentNbr,
                    split.InventoryID,
                    thresholdByLine);
            }

            decimal threshold;

            if (thresholdByLine.TryGetValue(
                split.LineNbr.Value,
                out threshold))
            {
                return threshold;
            }

            // Preserve standard behavior if an unexpected line was not
            // returned by the batch query.
            return baseMethod(split);
        }

        private Dictionary<int, decimal> LoadThresholds(
            string shipmentNbr,
            int? inventoryID)
        {
            var result = new Dictionary<int, decimal>();

            PXResultset<SOLine> rows =
                PXSelectJoin<
                    SOLine,
                    InnerJoin<
                        SOShipLine,
                        On<
                            SOShipLine.origOrderType,
                            Equal<SOLine.orderType>,
                            And<
                                SOShipLine.origOrderNbr,
                                Equal<SOLine.orderNbr>,
                            And<
                                SOShipLine.origLineNbr,
                                Equal<SOLine.lineNbr>>>>>,
                    Where<
                        SOShipLine.shipmentNbr,
                        Equal<Required<SOShipLine.shipmentNbr>>,
                        And<
                            SOShipLine.inventoryID,
                            Equal<Required<SOShipLine.inventoryID>>>>>
                .Select(
                    Base,
                    shipmentNbr,
                    inventoryID);

            foreach (PXResult<SOLine> rawRow in rows)
            {
                SOLine orderLine = rawRow.GetItem<SOLine>();
                SOShipLine shipmentLine = rawRow.GetItem<SOShipLine>();

                if (orderLine == null ||
                    shipmentLine?.LineNbr == null)
                {
                    continue;
                }

                decimal threshold =
                    orderLine.CompleteQtyMax.GetValueOrDefault(100m) /
                    100m;

                result[shipmentLine.LineNbr.Value] = threshold;
            }

            return result;
        }
    }
}
