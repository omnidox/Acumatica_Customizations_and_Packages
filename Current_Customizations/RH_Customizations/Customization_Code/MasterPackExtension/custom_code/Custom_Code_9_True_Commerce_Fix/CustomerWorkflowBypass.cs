using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;

namespace CustomWMS
{
    public static class CustomerWorkflowBypass
    {
        private static readonly HashSet<string> TopRowBypassCustomerCDs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DKOHL"
            };

        public static bool ShouldBypassTopRowWorkflow(PXGraph graph, SOShipment shipment)
        {
            if (graph == null || shipment?.CustomerID == null)
                return false;

            Customer customer = Customer.PK.Find(graph, shipment.CustomerID);

            string customerCD = customer?.AcctCD?.Trim();

            if (string.IsNullOrWhiteSpace(customerCD))
                return false;

            bool bypass = TopRowBypassCustomerCDs.Contains(customerCD);

            if (bypass)
            {
                WmsDebugTrace.Info(
                    $"[CustomerWorkflowBypass] Top row workflow bypassed. " +
                    $"Shipment={shipment.ShipmentNbr}, CustomerID={shipment.CustomerID}, CustomerCD={customerCD}");
            }

            return bypass;
        }
    }
}