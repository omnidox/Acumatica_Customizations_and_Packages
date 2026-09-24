using System;
using System.Threading;
using PX.Data;

namespace PX.Objects.SO
{
    /// <summary>
    /// Thread-safe AsyncLocal filter state for Confirm Shipment carrier processing.
    /// 
    /// Purpose:
    /// Prevents already-tracked packages from being reprocessed by FedEx during Confirm Shipment.
    /// Only activates during the native ShipPackages call within Confirm Shipment flow.
    /// 
    /// Usage:
    ///   using (ConfirmShipmentCarrierFilterScope.Activate(shipmentNbr))
    ///   {
    ///       baseMethod(shiporder);  // GetPackages will filter out already-tracked packages
    ///   }
    /// 
    /// Design:
    /// This is separate from CarrierPackageFilterScope because:
    /// - Manual print uses CarrierPackageFilterScope (single selected package)
    /// - WMS scan uses CarrierPackageFilterScope (scanned package)
    /// - Confirm Shipment uses BOTH scopes (depends on context):
    ///   - If a package was manually printed during confirm, CarrierPackageFilterScope might be active
    ///   - During native ShipPackages, ConfirmShipmentCarrierFilterScope is active
    ///   - GetPackages checks both and applies appropriate filtering
    /// </summary>
    internal sealed class ConfirmShipmentCarrierFilterState
    {
        public string ShipmentNbr { get; set; }
    }

    internal static class ConfirmShipmentCarrierFilterScope
    {
        private static readonly AsyncLocal<ConfirmShipmentCarrierFilterState> _state =
            new AsyncLocal<ConfirmShipmentCarrierFilterState>();

        public static bool IsActive => _state.Value != null;

        public static string CurrentShipmentNbr => _state.Value?.ShipmentNbr;

        /// <summary>
        /// Check if the active filter applies to a specific shipment.
        /// Returns true if filter is active and shipment matches the active scope.
        /// </summary>
        public static bool AppliesToShipment(string shipmentNbr)
        {
            if (!IsActive)
                return false;

            if (string.IsNullOrWhiteSpace(shipmentNbr) || string.IsNullOrWhiteSpace(CurrentShipmentNbr))
                return false;

            return string.Equals(shipmentNbr, CurrentShipmentNbr, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Activate the Confirm Shipment filter scope for a specific shipment.
        /// Returns IDisposable that restores previous state when disposed.
        /// </summary>
        public static IDisposable Activate(string shipmentNbr)
        {
            PXTrace.WriteInformation(
                "[CONFIRM-SHIPMENT-FILTER] Activating ConfirmShipmentCarrierFilterScope for shipment={0}",
                shipmentNbr);

            ConfirmShipmentCarrierFilterState previous = _state.Value;

            _state.Value = new ConfirmShipmentCarrierFilterState
            {
                ShipmentNbr = shipmentNbr
            };

            PXTrace.WriteInformation(
                "[CONFIRM-SHIPMENT-FILTER] ✅ Scope activated. Previous state: {0}",
                previous != null ? $"Shipment={previous.ShipmentNbr}" : "null");

            return new RestoreDisposable(previous);
        }

        private sealed class RestoreDisposable : IDisposable
        {
            private readonly ConfirmShipmentCarrierFilterState _previous;
            private bool _disposed;

            public RestoreDisposable(ConfirmShipmentCarrierFilterState previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                PXTrace.WriteInformation(
                    "[CONFIRM-SHIPMENT-FILTER] Restoring previous state");

                _state.Value = _previous;
                _disposed = true;

                PXTrace.WriteInformation(
                    "[CONFIRM-SHIPMENT-FILTER] ✅ Previous state restored");
            }
        }
    }
}
