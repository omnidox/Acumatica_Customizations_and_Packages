using System;
using System.Collections.Generic;
using System.Linq;

namespace IstarEDIShipViaResolver
{
    /// <summary>
    /// Temporary customer-specific mappings from the working routing list.
    /// Customer IDs match Acumatica's Customer.AcctCD. Kohl's department
    /// stores, Sterling, and Nexcom rules apply to all their corresponding
    /// records in Customers 20260923.xlsx, per the current business decision.
    /// Contradictory source rows are deliberately excluded.
    /// </summary>
    internal static class ShipViaMappings
    {
        internal sealed class Route
        {
            internal readonly string Method;
            internal readonly string Scac;
            internal readonly string ShipVia;

            internal Route(string method, string scac, string shipVia)
            {
                Method = Normalize(method);
                Scac = Normalize(scac);
                ShipVia = shipVia;
            }
        }

        private static Route R(string method, string scac, string shipVia)
        {
            return new Route(method, scac, shipVia);
        }

        private static readonly Route[] KohlsRoutes = {
            R("UPS 3 DAY SELECT", "UPSN-3D", "UP3S") };

        private static readonly Route[] SterlingRoutes = {
            R("UPS GROUND", "UPSN-CG", "UPCG") };

        private static readonly Route[] NexcomRoutes = {
            R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
            R("ESTES EXPRESS", "EXLA", "EXLA"),
            R("FEDEX 2ND DAY 3RD PARTY BILL", "FDE-SE", "FDN3"),
            R("FEDEX GROUND 3RD PARTY BILL", "FDEG", "FDX3"),
            R("FEDEX 2ND DAY COLLECT", "FDE-SE", "FED2"),
            R("FEDEX AIR INTL PRIORITY OVNIGHT 3PB", "FDEN", "FXI3"),
            R("YRC WORLDWIDE", "RDWY", "RDWY"),
            R("UPS GROUND 3RD PARTY BILL", "UPSN-CG", "UPS3") };

        private static readonly Dictionary<string, Route[]> ByCustomer =
            new Dictionary<string, Route[]>(StringComparer.Ordinal)
            {
                // R&S 51850: AMAZON.COM.KYDC, INC.
                ["51850"] = new[] {
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
                    R("UPS GROUND 3RD PARTY BILL", "UPSN-CG", "UPS3") },
                // R&S 10252: BEALL'S OUTLET
                ["10252"] = new[] {
                    R("DYNAMIC DELIVERY SERVICE", "DYDL", "DYDL"),
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
                    R("PERFORMANCE TEAM", "PERF", "PERF") },
                // R&S 10254: BEALLS, INC.
                ["10254"] = new[] {
                    R("DYNAMIC DELIVERY SERVICE", "DYDL", "DYDL"),
                    R("FEDEX GROUND PREPAID", "FDEG", "FDXP"),
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX") },
                // R&S 38588: BOSCOV'S
                ["38588"] = new[] {
                    R("ABF FREIGHT SYSTEMS", "ABFS", "ABFS"),
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
                    R("YRC WORLDWIDE", "RDWY", "RDWY"),
                    R("TRIANGLE SC", "TGIR", "TGIR") },
                // Kohl's department stores: R&S and DALOW records.
                // KOHL'S.COM is a separate customer and is not included.
                ["10282"] = KohlsRoutes,
                ["DKOHLS"] = KohlsRoutes,
                ["DKOHLX"] = KohlsRoutes,
                // Nexcom: R&S, CANDELA, and STANLEY records.
                ["27230"] = NexcomRoutes,
                ["CNA096"] = NexcomRoutes,
                ["CNA099"] = NexcomRoutes,
                ["JNX995"] = NexcomRoutes,
                // Sterling: both STANLEY records.
                ["JSTERM"] = SterlingRoutes,
                ["JSTERX"] = SterlingRoutes,
                // R&S 28701: TARGET
                ["28701"] = new[] {
                    R("ABF FREIGHT SYSTEMS", "ABFS", "ABFS"),
                    R("ESTES EXPRESS", "EXLA", "EXLA"),
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
                    R("J.B. HUNT 360", "JBHU", "JBHU"),
                    R("NFCV", "NFCV", "NFCV"),
                    R("YRC WORLDWIDE", "RDWY", "RDWY"),
                    R("UPS GROUND", "UPSN-CG", "UPCG") },
                // R&S 36854: TARGET.COM
                ["36854"] = new[] {
                    R("UPS GROUND", "UPSN-CG", "UPCG"),
                    R("UPS GROUND 3RD PARTY BILL", "UPSN-CG", "UPS3"),
                    R("UPS SUREPOST", "UPSN-CG", "UPSP") },
                // R&S 28688: WALMART
                ["28688"] = new[] {
                    R("FEDEX GROUND 3RD PARTY BILL", "FDEG", "FDX3"),
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
                    R("FEDEX FREIGHT EAST", "FXFE", "FXFE"),
                    R("J.B. HUNT 360", "JBHU", "JBHU"),
                    R("NFCV", "NFCV", "NFCV"),
                    R("YRC WORLDWIDE", "RDWY", "RDWY") },
                // R&S 36856: WAL-MART.COM
                ["36856"] = new[] {
                    R("FEDEX 2ND DAY 3RD PARTY BILL", "FDE-SE", "FDN3"),
                    R("FEDEX GROUND 3RD PARTY BILL", "FDEG", "FDX3"),
                    R("FEDEX GROUND COLLECT", "FDEG", "FEDX"),
                    R("FEDEX HOME DELIVERY 3RD PARTY", "FDEG-HD", "FXHD"),
                    R("UPS GROUND", "UPSN-CG", "UPCG"),
                    R("US POSTAL SERVICE PRIORITY MAIL DOMESTIC", "USPS-PB", "USP2"),
                    R("US POSTAL SERV PRIORITY MAIL", "USPS-PB", "USPM") }
            };

        internal static IReadOnlyList<Route> Find(
            string customerCode, string method, string scac)
        {
            method = Normalize(method);
            scac = Normalize(scac);
            if ((method == null && scac == null) ||
                !ByCustomer.TryGetValue(Normalize(customerCode) ?? "",
                    out Route[] routes))
                return Array.Empty<Route>();

            return routes.Where(r =>
                (method == null || r.Method == method) &&
                (scac == null || r.Scac == scac)).ToArray();
        }

        internal static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = string.Join(" ", value.Trim()
                .Split((char[])null,
                    StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
            return normalized == "N/A" || normalized == "NA"
                ? null
                : normalized;
        }
    }
}
