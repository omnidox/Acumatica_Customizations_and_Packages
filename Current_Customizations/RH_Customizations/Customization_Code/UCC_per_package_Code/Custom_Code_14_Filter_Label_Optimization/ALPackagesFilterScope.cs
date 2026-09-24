using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PX.Data;

namespace AA.Objects.AL.Integration.PerPackage
{
    internal sealed class ALPackagesFilterState
    {
        public string ShipmentNbr { get; set; }
        public HashSet<int?> PackageLineNbrs { get; set; }
    }

    internal static class ALPackagesFilterScope
    {
        private static readonly bool DetailedDiagnostics = false;

        private static void WriteDiagnostic(string message, params object[] args)
        {
            if (DetailedDiagnostics)
                PXTrace.WriteInformation(message, args);
        }

        private static readonly AsyncLocal<ALPackagesFilterState> _state = new AsyncLocal<ALPackagesFilterState>();

        public static bool IsActive => _state.Value != null;

        public static string ShipmentNbr => _state.Value?.ShipmentNbr;

        public static bool Matches(string shipmentNbr, int? packageLineNbr)
        {
            ALPackagesFilterState state = _state.Value;
            if (state == null)
            {
                WriteDiagnostic("[SCOPE.Matches] State is null - MISMATCH");
                return false;
            }

            if (!string.Equals(state.ShipmentNbr, shipmentNbr, StringComparison.OrdinalIgnoreCase))
            {
                WriteDiagnostic("[SCOPE.Matches] Shipment mismatch: expected={0}, actual={1} - MISMATCH", 
                    state.ShipmentNbr, shipmentNbr);
                return false;
            }

            if (state.PackageLineNbrs == null || state.PackageLineNbrs.Count == 0)
            {
                WriteDiagnostic("[SCOPE.Matches] No package line numbers in filter state - MISMATCH");
                return false;
            }

            bool matches = state.PackageLineNbrs.Contains(packageLineNbr);
            
            if (matches)
            {
                WriteDiagnostic("[SCOPE.Matches] Package {0} found in filter - MATCH", packageLineNbr);
            }
            else
            {
                WriteDiagnostic("[SCOPE.Matches] Package {0} NOT in filter set [{1}] - MISMATCH", 
                    packageLineNbr, string.Join(",", state.PackageLineNbrs.Where(x => x.HasValue).Select(x => x.Value)));
            }

            return matches;
        }

        public static IDisposable Activate(string shipmentNbr, IEnumerable<int?> packageLineNbrs)
        {
            HashSet<int?> lines = new HashSet<int?>(packageLineNbrs ?? Enumerable.Empty<int?>());

            WriteDiagnostic("[SCOPE] Activating filter scope for shipment {0} with {1} package line(s): {2}", 
                shipmentNbr, lines.Count, string.Join(",", lines.Where(x => x.HasValue).Select(x => x.Value)));

            ALPackagesFilterState previous = _state.Value;

            _state.Value = new ALPackagesFilterState
            {
                ShipmentNbr = shipmentNbr,
                PackageLineNbrs = lines
            };

            WriteDiagnostic("[SCOPE] Filter scope activated. Previous state: {0}", 
                previous != null ? $"Shipment={previous.ShipmentNbr}, Packages={string.Join(",", previous.PackageLineNbrs)}" : "null");

            return new RestoreDisposable(previous);
        }

        private sealed class RestoreDisposable : IDisposable
        {
            private readonly ALPackagesFilterState _previous;
            private bool _disposed;

            public RestoreDisposable(ALPackagesFilterState previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _state.Value = _previous;
                _disposed = true;
            }
        }
    }
}
