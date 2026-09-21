using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;
using PX.Web.UI;

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
        : PXGraphExtension<PickPackShip.Host>
    {
        private const string TracePrefix =
            "[PackageContentDefaultIssueFrom]";

        private const string PackageContentGridID =
            "gridPackedItems";

        private string _cachedShipmentNbr;
        private int? _cachedPackageLineNbr;

        private readonly Dictionary<int, string>
            _locationCodeByShipmentSplitLineNbr =
                new Dictionary<int, string>();

        public static bool IsActive()
        {
            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            Page page =
                HttpContext.Current?.Handler as Page;

            if (page == null)
            {
                return;
            }

            page.LoadComplete -= Page_LoadComplete;
            page.LoadComplete += Page_LoadComplete;

            page.PreRender -= Page_PreRender;
            page.PreRender += Page_PreRender;
        }

        private void Page_LoadComplete(
            object sender,
            EventArgs e)
        {
            EnsurePackageContentColumn(sender as Page);
        }

        private void Page_PreRender(
            object sender,
            EventArgs e)
        {
            EnsurePackageContentColumn(sender as Page);
        }

        private void EnsurePackageContentColumn(Page page)
        {

            if (page == null)
            {
                return;
            }

            PXGrid grid =
                FindControlRecursive(
                    page,
                    PackageContentGridID) as PXGrid;

            if (grid == null)
            {
                // The scan callback does not always construct the Package
                // Content controls. This is expected and is not an error.
                return;
            }

            if (grid.Levels == null ||
                grid.Levels.Count == 0)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Grid has no levels: " +
                    PackageContentGridID);

                return;
            }

            PXGridLevel level =
                grid.Levels
                    .Cast<PXGridLevel>()
                    .FirstOrDefault(
                        candidate =>
                            string.Equals(
                                candidate.DataMember,
                                "Packed",
                                StringComparison.OrdinalIgnoreCase));

            if (level == null ||
                level.Columns == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Packed grid level was not found.");

                return;
            }

            if (level.Columns
                .Cast<PXGridColumn>()
                .Any(
                    column =>
                        string.Equals(
                            column.DataField,
                            "UsrDefaultIssueFrom",
                            StringComparison.OrdinalIgnoreCase)))
            {
                PXTrace.WriteInformation(
                    $"{TracePrefix} Column already exists: " +
                    "UsrDefaultIssueFrom");

                return;
            }

            PXGridColumn locationColumn =
                new PXGridColumn
                {
                    DataField = "UsrDefaultIssueFrom",
                    Width = Unit.Pixel(140),
                    DataType = TypeCode.String,
                    Visible = true,
                    AllowShowHide = AllowShowHide.Server,
                    AllowSort = false,
                    SyncVisible = false
                };

            locationColumn.Header.Text =
                "Default Issue From";

            int inventoryIndex =
                level.Columns
                    .Cast<PXGridColumn>()
                    .Select(
                        (column, index) =>
                            new
                            {
                                Column = column,
                                Index = index
                            })
                    .Where(
                        item =>
                            string.Equals(
                                item.Column.DataField,
                                "InventoryID",
                                StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Index)
                    .DefaultIfEmpty(-1)
                    .First();

            if (inventoryIndex >= 0)
            {
                level.Columns.Insert(
                    inventoryIndex + 1,
                    locationColumn);
            }
            else
            {
                level.Columns.Add(
                    locationColumn);
            }

            grid.RepaintColumns = true;
            grid.GenerateColumnsBeforeRepaint = true;

            PXTrace.WriteInformation(
                $"{TracePrefix} Added UsrDefaultIssueFrom to " +
                $"{PackageContentGridID} after InventoryID.");
        }

        private Control FindControlRecursive(
            Control root,
            string id)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(
                root.ID,
                id,
                StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            foreach (Control child in root.Controls)
            {
                Control found =
                    FindControlRecursive(
                        child,
                        id);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Exposes the unbound extension field through the cache metadata so
        /// it can be selected from the Package Content grid's Column
        /// Configuration dialog without modifying SO302020.aspx.
        /// </summary>
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(
            DisplayName = "Default Issue From",
            Enabled = false,
            Visible = true,
            Visibility = PXUIVisibility.SelectorVisible)]
        protected virtual void _(
            Events.CacheAttached<
                SOShipLineSplitPackageContentExt
                    .usrDefaultIssueFrom> e)
        {
        }

        protected virtual void _(
            Events.RowSelected<SOShipLineSplit> e)
        {
            if (e.Row == null)
            {
                return;
            }

            SOShipLineSplitPackageContentExt extension =
                e.Cache.GetExtension<
                    SOShipLineSplitPackageContentExt>(e.Row);

            if (extension == null)
            {
                return;
            }

            string locationCD =
                GetDefaultIssueFromLocationCD(e.Row);

            if (!string.Equals(
                extension.UsrDefaultIssueFrom,
                locationCD,
                StringComparison.Ordinal))
            {
                extension.UsrDefaultIssueFrom = locationCD;
            }

            PXTrace.WriteInformation(
                $"{TracePrefix} RowSelected. " +
                $"Shipment={e.Row.ShipmentNbr ?? "<null>"}; " +
                $"LineNbr={e.Row.LineNbr?.ToString() ?? "<null>"}; " +
                $"SplitLineNbr={e.Row.SplitLineNbr?.ToString() ?? "<null>"}; " +
                $"ResolvedLocation={locationCD ?? "<null>"}.");
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

            SOShipLineSplitPackageContentExt extension =
                e.Cache.GetExtension<
                    SOShipLineSplitPackageContentExt>(e.Row);

            string locationCD =
                extension?.UsrDefaultIssueFrom;

            if (locationCD == null)
            {
                locationCD =
                    GetDefaultIssueFromLocationCD(
                        e.Row);

                if (extension != null)
                {
                    extension.UsrDefaultIssueFrom = locationCD;
                }
            }

            e.ReturnValue = locationCD;
            e.IsAltered = true;

            PXTrace.WriteInformation(
                $"{TracePrefix} FieldSelecting. " +
                $"Shipment={e.Row.ShipmentNbr ?? "<null>"}; " +
                $"LineNbr={e.Row.LineNbr?.ToString() ?? "<null>"}; " +
                $"SplitLineNbr={e.Row.SplitLineNbr?.ToString() ?? "<null>"}; " +
                $"ResolvedLocation={locationCD ?? "<null>"}.");
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
            PickPackShip wms =
                Base.WMS;

            PickPackShip.PackMode.Logic packMode =
                wms?.Get<PickPackShip.PackMode.Logic>();

            string shipmentNbr =
                wms?.RefNbr;

            if (string.IsNullOrEmpty(shipmentNbr))
            {
                shipmentNbr = split.ShipmentNbr;
            }

            int? packageLineNbr =
                packMode?.PackageLineNbrUI;

            PXTrace.WriteInformation(
                $"{TracePrefix} Resolving package content. " +
                $"Shipment={shipmentNbr ?? "<null>"}; " +
                $"PackageLineNbrUI={packageLineNbr?.ToString() ?? "<null>"}; " +
                $"SplitLineNbr={split.SplitLineNbr?.ToString() ?? "<null>"}.");

            if (string.IsNullOrEmpty(shipmentNbr) ||
                packageLineNbr == null ||
                split.SplitLineNbr == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Cannot resolve location because one " +
                    "or more lookup keys are missing.");

                return string.Empty;
            }

            EnsurePackageLookup(
                shipmentNbr,
                packageLineNbr);

            string locationCD;

            bool matched =
                _locationCodeByShipmentSplitLineNbr.TryGetValue(
                split.SplitLineNbr.Value,
                out locationCD);

            PXTrace.WriteInformation(
                $"{TracePrefix} Package-content match. " +
                $"SplitLineNbr={split.SplitLineNbr.Value}; " +
                $"Matched={matched}; " +
                $"LocationCD={locationCD ?? "<null>"}.");

            return matched
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

            List<WmsPlan> plans =
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
                .RowCast<WmsPlan>()
                .ToList();

            PXTrace.WriteInformation(
                $"{TracePrefix} Loaded SelectedPackageContents. " +
                $"Shipment={shipmentNbr}; " +
                $"PackageLineNbr={packageLineNbr}; " +
                $"PlanCount={plans.Count}.");

            foreach (WmsPlan plan in plans)
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

                PXTrace.WriteInformation(
                    $"{TracePrefix} Loaded plan location. " +
                    $"ShipmentSplitLineNbr={plan.ShipmentSplitLineNbr}; " +
                    $"DefaultIssueFrom={plan.DefaultIssueFrom?.ToString() ?? "<null>"}; " +
                    $"LocationCD={locationCD ?? "<null>"}.");
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
