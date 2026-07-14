using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;
using PX.Objects.SO;
using PX.Web.UI;

using WmsShipmentExt = WMS.SOShipmentEntryExt;

namespace CustomWMS
{
    public class SOShipmentEntryExt_SelectedPackageUi
        : PXGraphExtension<WmsShipmentExt, SOShipmentEntry>
    {
        private const string TracePrefix = "[SelectedPackageUI]";
        private const string Version = "2026-06-18-DB-SKIPPED-STATUS-UI-01";

        private const string EstimatedContentGridID = "CstPXGrid3";

        public static bool IsActive() => true;

        public override void Initialize()
        {
            base.Initialize();

            Page page = HttpContext.Current?.Handler as Page;
            if (page == null)
                return;

            page.Init -= Page_Init;
            page.Init += Page_Init;
        }

        private void Page_Init(object sender, EventArgs e)
        {
            Page page = sender as Page;
            if (page == null)
                return;

            PXGrid grid = FindControlRecursive(page, EstimatedContentGridID) as PXGrid;
            if (grid == null)
            {
                PXTrace.WriteInformation($"{TracePrefix} Grid not found: {EstimatedContentGridID}");
                return;
            }

            grid.RepaintColumns = true;
            grid.GenerateColumnsBeforeRepaint = true;

            if (grid.Levels == null || grid.Levels.Count == 0)
            {
                PXTrace.WriteInformation($"{TracePrefix} Grid has no levels: {EstimatedContentGridID}");
                return;
            }

            PXGridLevel level = grid.Levels[0];

            AddColumnIfMissing(level, "UsrRemainingQty", "Remaining Qty", 100, TypeCode.Decimal);
            AddColumnIfMissing(level, "UsrSkippedStatus", "Skipped Status", 120, TypeCode.String);

            PXTrace.WriteInformation(
                $"{TracePrefix} VERSION {Version}. Custom DAC columns ensured on {EstimatedContentGridID}. RepaintColumns=True, GenerateColumnsBeforeRepaint=True");
        }

        private void AddColumnIfMissing(
            PXGridLevel level,
            string dataField,
            string text,
            int width,
            TypeCode dataType)
        {
            if (level == null || level.Columns == null)
                return;

            bool exists = level.Columns
                .Cast<PXGridColumn>()
                .Any(c => string.Equals(c.DataField, dataField, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                PXTrace.WriteInformation($"{TracePrefix} Column already exists: {dataField}");
                return;
            }

            PXGridColumn column = new PXGridColumn
            {
                DataField = dataField,
                Width = Unit.Pixel(width),
                DataType = dataType
            };

            column.Header.Text = text;

            level.Columns.Add(column);

            PXTrace.WriteInformation($"{TracePrefix} Added column: {dataField}");
        }

        private Control FindControlRecursive(Control root, string id)
        {
            if (root == null)
                return null;

            if (string.Equals(root.ID, id, StringComparison.OrdinalIgnoreCase))
                return root;

            foreach (Control child in root.Controls)
            {
                Control found = FindControlRecursive(child, id);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}