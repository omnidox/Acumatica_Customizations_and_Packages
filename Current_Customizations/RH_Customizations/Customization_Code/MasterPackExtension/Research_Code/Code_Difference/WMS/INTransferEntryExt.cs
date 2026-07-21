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
	// Token: 0x0200000A RID: 10
	public class INTransferEntryExt : PXGraphExtension<INTransferEntry.SiteStatusLookup, INTransferEntry>
	{
		// Token: 0x06000035 RID: 53 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002560 File Offset: 0x00000760
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

		// Token: 0x06000037 RID: 55 RVA: 0x000025BC File Offset: 0x000007BC
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

		// Token: 0x04000015 RID: 21
		public static string CurrentRefNbr = string.Empty;

		// Token: 0x0200002F RID: 47
		[Serializable]
		public class INSiteStatusSelectedExt : PXCacheExtension<INTransferEntry.SiteStatusLookup.INSiteStatusSelected>
		{
			// Token: 0x06000146 RID: 326 RVA: 0x00002082 File Offset: 0x00000282
			public static bool IsActive()
			{
				return true;
			}

			// Token: 0x17000059 RID: 89
			// (get) Token: 0x06000147 RID: 327 RVA: 0x0000854A File Offset: 0x0000674A
			// (set) Token: 0x06000148 RID: 328 RVA: 0x00008552 File Offset: 0x00006752
			[PXDBDecimal(BqlField = typeof(POReceiptLine.receiptQty))]
			[PXUIField(DisplayName = "Original Receipt Qty")]
			public decimal? UsrActualQty { get; set; }

			// Token: 0x02000089 RID: 137
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
