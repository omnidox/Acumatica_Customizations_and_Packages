using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;

using WmsPlan = WMS.SelectedPackageContents;

namespace PackageContentDefaultIssueFrom
{
    /// <summary>
    /// Adds the display value used by the Package Content grid to the
    /// primary DAC of the standard Pick/Pack/Ship Packed view.
    ///
    /// The field is intentionally a string. SelectedPackageContents stores
    /// DefaultIssueFrom as the numeric INLocation.LocationID, but warehouse
    /// users see and organize the grid by INLocation.LocationCD.
    /// </summary>
    public sealed class SOShipLineSplitPackageContentExt
        : PXCacheExtension<SOShipLineSplit>
    {
        public static bool IsActive()
        {
            return true;
        }

        public abstract class usrDefaultIssueFrom
            : PX.Data.BQL.BqlString.Field<usrDefaultIssueFrom>
        {
        }

        [PXString(30, IsUnicode = true)]
        [PXUIField(
            DisplayName = "Default Issue From",
            Enabled = false)]
        public string UsrDefaultIssueFrom { get; set; }
    }

    /// <summary>
    /// Resolves the current package's expected Default Issue From value for
    /// each SOShipLineSplit returned by the standard Packed view.
    ///
    /// The lookup is loaded once per shipment/package pair and cached for
    /// the lifetime of the SOShipmentEntry graph. This prevents the grid
    /// from issuing one database query per displayed row.
    /// </summary>
    public class SOShipmentEntryPackageContentDefaultIssueFromExt
        : PXGraphExtension<SOShipmentEntry>
    {
        private string _cachedShipmentNbr;
        private int? _cachedPackageLineNbr;

        private readonly Dictionary<int, string>
            _locationCodeByShipmentSplitLineNbr =
                new Dictionary<int, string>();

        public static bool IsActive()
        {
            return true;
        }

        protected virtual void _(
            Events.FieldSelecting<
                SOShipLineSplit,
                SOShipLineSplitPackageContentExt
                    .usrDefaultIssueFrom> e)
        {
            if (e.Row == null)
            {
                return;
            }

            e.ReturnValue =
                GetDefaultIssueFromLocationCD(
                    e.Row);
        }

        protected virtual void _(
            Events.RowInserted<WmsPlan> e)
        {
            InvalidatePackageLookup();
        }

        protected virtual void _(
            Events.RowUpdated<WmsPlan> e)
        {
            InvalidatePackageLookup();
        }

        protected virtual void _(
            Events.RowDeleted<WmsPlan> e)
        {
            InvalidatePackageLookup();
        }

        private string GetDefaultIssueFromLocationCD(
            SOShipLineSplit split)
        {
            SOPackageDetailEx package =
                Base.Packages.Current;

            if (package == null ||
                string.IsNullOrEmpty(package.ShipmentNbr) ||
                package.LineNbr == null ||
                split.SplitLineNbr == null ||
                !string.Equals(
                    split.ShipmentNbr,
                    package.ShipmentNbr,
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            EnsurePackageLookup(
                package.ShipmentNbr,
                package.LineNbr);

            string locationCD;

            return _locationCodeByShipmentSplitLineNbr.TryGetValue(
                split.SplitLineNbr.Value,
                out locationCD)
                    ? locationCD ?? string.Empty
                    : string.Empty;
        }

        private void EnsurePackageLookup(
            string shipmentNbr,
            int? packageLineNbr)
        {
            if (string.Equals(
                    _cachedShipmentNbr,
                    shipmentNbr,
                    StringComparison.OrdinalIgnoreCase) &&
                _cachedPackageLineNbr == packageLineNbr)
            {
                return;
            }

            _locationCodeByShipmentSplitLineNbr.Clear();

            Dictionary<int, string> locationCodeByLocationID =
                new Dictionary<int, string>();

            foreach (WmsPlan plan in
                PXSelect<
                    WmsPlan,
                    Where<
                        WmsPlan.shipmentNbr,
                        Equal<
                            Required<
                                WmsPlan.shipmentNbr>>,
                        And<
                            WmsPlan.packageLineNbr,
                            Equal<
                                Required<
                                    WmsPlan.packageLineNbr>>>>>
                .Select(
                    Base,
                    shipmentNbr,
                    packageLineNbr)
                .RowCast<WmsPlan>())
            {
                if (plan == null ||
                    plan.ShipmentSplitLineNbr == null)
                {
                    continue;
                }

                string locationCD =
                    GetLocationCD(
                        plan.DefaultIssueFrom,
                        locationCodeByLocationID);

                _locationCodeByShipmentSplitLineNbr[
                    plan.ShipmentSplitLineNbr.Value] =
                        locationCD;
            }

            _cachedShipmentNbr =
                shipmentNbr;

            _cachedPackageLineNbr =
                packageLineNbr;
        }

        private string GetLocationCD(
            int? locationID,
            Dictionary<int, string> locationCodeByLocationID)
        {
            if (locationID == null)
            {
                return string.Empty;
            }

            string locationCD;

            if (locationCodeByLocationID.TryGetValue(
                locationID.Value,
                out locationCD))
            {
                return locationCD ?? string.Empty;
            }

            INLocation location =
                INLocation.PK.Find(
                    Base,
                    locationID);

            locationCD =
                location?.LocationCD?.Trim()
                ?? string.Empty;

            locationCodeByLocationID[locationID.Value] =
                locationCD;

            return locationCD;
        }

        private void InvalidatePackageLookup()
        {
            _cachedShipmentNbr =
                null;

            _cachedPackageLineNbr =
                null;

            _locationCodeByShipmentSplitLineNbr.Clear();
        }
    }
}
