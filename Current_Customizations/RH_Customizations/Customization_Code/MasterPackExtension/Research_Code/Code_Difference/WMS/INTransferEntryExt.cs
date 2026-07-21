using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.IN;
using PX.Objects.PO;

namespace WMS
{
	// Token: 0x0200000C RID: 12
	public class INTransferEntryExt : PXGraphExtension<INTransferEntry.SiteStatusLookup, INTransferEntry>
	{
		// Token: 0x06000041 RID: 65 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002794 File Offset: 0x00000994
		protected void _(Events.RowInserting<INTran> row, PXRowInserting baseMethod)
		{
			if (baseMethod != null)
			{
				baseMethod.Invoke(row.Cache, row.Args);
			}
			bool flag = !string.IsNullOrEmpty(INTransferEntryExt.CurrentRefNbr) && !row.ExternalCall;
			if (flag)
			{
				PXCacheEx.GetExtension<INTranExt>(row.Row).UsrRefNbr = INTransferEntryExt.CurrentRefNbr;
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000027F0 File Offset: 0x000009F0
		protected void _(Events.FieldUpdated<INSiteStatusFilter, INTransferEntry.SiteStatusLookup.INTransferStatusFilter.receiptNbr> args)
		{
			bool flag = args.Row == null;
			if (!flag)
			{
				List<POReceiptLine> purchaseReceiptLines = GraphHelper.RowCast<POReceiptLine>(PXSelectBase<POReceiptLine, PXViewOf<POReceiptLine>.BasedOn<SelectFromBase<POReceiptLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<POReceiptLine.receiptNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					args.NewValue
				})).ToList<POReceiptLine>();
				bool flag2 = purchaseReceiptLines.Count > 0;
				if (flag2)
				{
					int? firstLocationID = purchaseReceiptLines[0].LocationID;
					bool allSameLocationID = purchaseReceiptLines.All(delegate(POReceiptLine line)
					{
						int? locationID = line.LocationID;
						int? firstLocationID = firstLocationID;
						return locationID.GetValueOrDefault() == firstLocationID.GetValueOrDefault() & locationID != null == (firstLocationID != null);
					});
					bool flag3 = !allSameLocationID;
					if (flag3)
					{
						PXUIFieldAttribute.SetError<INSiteStatusFilter.locationID>(args.Cache, null, "PO Receipt have different locations");
					}
					else
					{
						base.Base1.ItemFilter.Cache.SetValueExt<INSiteStatusFilter.locationID>(args.Row, firstLocationID);
					}
				}
				INTransferEntryExt.CurrentRefNbr = (string)args.NewValue;
				base.Base1.ItemFilter.Cache.Update(args.Row);
			}
		}

		// Token: 0x04000019 RID: 25
		public static string CurrentRefNbr = string.Empty;

		// Token: 0x02000031 RID: 49
		[Serializable]
		public class INSiteStatusSelectedExt : PXCacheExtension<INTransferEntry.SiteStatusLookup.INSiteStatusSelected>
		{
			// Token: 0x0600015D RID: 349 RVA: 0x000022B4 File Offset: 0x000004B4
			public static bool IsActive()
			{
				return true;
			}

			// Token: 0x1700005D RID: 93
			// (get) Token: 0x0600015E RID: 350 RVA: 0x00009A5A File Offset: 0x00007C5A
			// (set) Token: 0x0600015F RID: 351 RVA: 0x00009A62 File Offset: 0x00007C62
			[PXDBDecimal(BqlField = typeof(POReceiptLine.receiptQty))]
			[PXUIField(DisplayName = "Original Receipt Qty")]
			public decimal? UsrActualQty { get; set; }

			// Token: 0x02000093 RID: 147
			[Nullable(new byte[]
			{
				0,
				1,
				0
			})]
			public abstract class usrActualQty : BqlType<IBqlDecimal, decimal>.Field<INTransferEntryExt.INSiteStatusSelectedExt.usrActualQty>
			{
			}
		}
	}
}
