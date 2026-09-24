using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PX.Data;

namespace PX.Objects.SO
{
    /// <summary>
    /// Thread-safe AsyncLocal filter state for carrier package generation.
    /// Allows temporary filtering of SOShipmentEntry.Packages view to expose
    /// only a selected package during carrier rate calculation.
    /// 
    /// Usage:
    ///   using (CarrierPackageFilterScope.Activate(shipmentNbr, packageLineNbr))
    ///   {
    ///       // Only the selected package is visible to GetPackages()
    ///       graph.CarrierRatesExt.BuildRequest(shipment);
    ///   }
    /// </summary>
    internal sealed class CarrierPackageFilterState
    {
        public string ShipmentNbr { get; set; }
        public int? SelectedPackageLineNbr { get; set; }
    }

    internal static class CarrierPackageFilterScope
    {
        private static readonly AsyncLocal<CarrierPackageFilterState> _state = 
            new AsyncLocal<CarrierPackageFilterState>();

        public static bool IsActive => _state.Value != null;

        public static string ShipmentNbr => _state.Value?.ShipmentNbr;

        public static int? SelectedPackageLineNbr => _state.Value?.SelectedPackageLineNbr;

        /// <summary>
        /// Read-only property: Current active shipment number (safe for extension access)
        /// </summary>
        public static string CurrentShipmentNbr => _state.Value?.ShipmentNbr;

        /// <summary>
        /// Read-only property: Current selected package line number (safe for extension access)
        /// </summary>
        public static int? CurrentSelectedPackageLineNbr => _state.Value?.SelectedPackageLineNbr;

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
        /// Check if a package matches the active filter.
        /// Returns true if filter is active and package LineNbr matches the selected package.
        /// </summary>
        public static bool Matches(string shipmentNbr, int? packageLineNbr)
        {
            CarrierPackageFilterState state = _state.Value;
            if (state == null)
            {
                PXTrace.WriteInformation("[CARRIER-PKG-FILTER.Matches] State is null - NO FILTER ACTIVE");
                return false;
            }

            if (!string.Equals(state.ShipmentNbr, shipmentNbr, StringComparison.OrdinalIgnoreCase))
            {
                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER.Matches] Shipment mismatch: expected={0}, actual={1} - MISMATCH",
                    state.ShipmentNbr, shipmentNbr);
                return false;
            }

            bool matches = state.SelectedPackageLineNbr == packageLineNbr;

            if (matches)
            {
                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER.Matches] Package LineNbr={0} MATCHES selected package",
                    packageLineNbr);
            }
            else
            {
                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER.Matches] Package LineNbr={0} DOES NOT match selected LineNbr={1}",
                    packageLineNbr, state.SelectedPackageLineNbr);
            }

            return matches;
        }

        /// <summary>
        /// Activate the filter scope for a single package.
        /// Returns IDisposable that restores previous state when disposed.
        /// </summary>
        public static IDisposable Activate(string shipmentNbr, int? packageLineNbr)
        {
            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER] Activating filter scope for shipment={0}, packageLineNbr={1}",
                shipmentNbr, packageLineNbr);

            CarrierPackageFilterState previous = _state.Value;

            _state.Value = new CarrierPackageFilterState
            {
                ShipmentNbr = shipmentNbr,
                SelectedPackageLineNbr = packageLineNbr
            };

            PXTrace.WriteInformation(
                "[CARRIER-PKG-FILTER] ✅ Filter scope activated. Previous state: {0}",
                previous != null ? $"Shipment={previous.ShipmentNbr}, PackageLineNbr={previous.SelectedPackageLineNbr}" : "null");

            return new RestoreDisposable(previous);
        }

        private sealed class RestoreDisposable : IDisposable
        {
            private readonly CarrierPackageFilterState _previous;
            private bool _disposed;

            public RestoreDisposable(CarrierPackageFilterState previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER] Restoring previous state");

                _state.Value = _previous;
                _disposed = true;

                PXTrace.WriteInformation(
                    "[CARRIER-PKG-FILTER] ✅ Previous state restored");
            }
        }
    }
}
