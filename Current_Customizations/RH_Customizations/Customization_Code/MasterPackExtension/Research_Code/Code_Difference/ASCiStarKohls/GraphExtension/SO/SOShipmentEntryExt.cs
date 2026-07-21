using System;
using System.Collections.Generic;
using System.Linq;
using ASCiStarKohls.CacheExtension.AR;
using ASCiStarKohls.CacheExtension.SO;
using ASCiStarKohls.DataProvider.Interface;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;

namespace ASCiStarKohls.GraphExtension.SO
{
	// Token: 0x02000007 RID: 7
	public class SOShipmentEntryExt : PXGraphExtension<CreateShipmentExtension, InvoiceExtension, SOShipmentEntry>
	{
		// Token: 0x06000012 RID: 18 RVA: 0x000023DD File Offset: 0x000005DD
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000013 RID: 19 RVA: 0x000023E0 File Offset: 0x000005E0
		// (set) Token: 0x06000014 RID: 20 RVA: 0x000023E8 File Offset: 0x000005E8
		[InjectDependency]
		public ISODataProvider _soDataProvider { get; set; }

		// Token: 0x06000015 RID: 21 RVA: 0x000023F1 File Offset: 0x000005F1
		[PXOverride]
		public void CreateShipment(CreateShipmentArgs args, SOShipmentEntryExt.CreateShipmentDelegate baseMethod)
		{
			baseMethod(args);
			this.UpdateShipmentLines(args.Order);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x0000240C File Offset: 0x0000060C
		[PXOverride]
		public void InvoiceShipment(SOInvoiceEntry docgraph, SOShipment shiporder, DateTime invoiceDate, InvoiceList list, PXQuickProcess.ActionFlow quickProcessFlow, SOShipmentEntryExt.InvoiceShipmentDelegate baseMethod)
		{
			baseMethod(docgraph, shiporder, invoiceDate, list, quickProcessFlow);
			using (PXTransactionScope pxtransactionScope = new PXTransactionScope())
			{
				try
				{
					IEnumerable<IGrouping<string, ARTran>> enumerable = from _ in GraphHelper.RowCast<ARTran>(docgraph.Transactions.Select(Array.Empty<object>()))
					group _ by _.SOOrderType;
					foreach (IGrouping<string, ARTran> group in enumerable)
					{
						this.ProcessTransactionGroup(docgraph, group);
						this.UpdateInvoiceTransactionLines(docgraph, group);
					}
					docgraph.Save.Press();
					pxtransactionScope.Complete();
				}
				catch (Exception ex)
				{
					PXTrace.WriteError(ex);
					throw;
				}
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002500 File Offset: 0x00000700
		private void UpdateShipmentLines(SOOrder order)
		{
			IEnumerable<SOLine> solines = this._soDataProvider.GetSOLines(order.OrderType, order.OrderNbr);
			foreach (SOLine soline in solines)
			{
				SOLineExt extension = PXCache<SOLine>.GetExtension<SOLineExt>(soline);
				bool flag = extension.UsrLineNbr != null;
				if (flag)
				{
					SOShipLine soshipLine = this.FindShipmentLine(soline);
					bool flag2 = soshipLine != null;
					if (flag2)
					{
						this.UpdateShipmentLine(soshipLine, extension.UsrLineNbr);
					}
				}
			}
			GraphHelper.PressButton(base.Base.Save);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000025B4 File Offset: 0x000007B4
		private SOShipLine FindShipmentLine(SOLine soLine)
		{
			return GraphHelper.RowCast<SOShipLine>(base.Base.Transactions.Select(Array.Empty<object>())).FirstOrDefault(delegate(SOShipLine shipLine)
			{
				bool result;
				if (shipLine.OrigOrderType == soLine.OrderType && shipLine.OrigOrderNbr == soLine.OrderNbr)
				{
					int? origLineNbr = shipLine.OrigLineNbr;
					int? lineNbr = soLine.LineNbr;
					result = (origLineNbr.GetValueOrDefault() == lineNbr.GetValueOrDefault() & origLineNbr != null == (lineNbr != null));
				}
				else
				{
					result = false;
				}
				return result;
			});
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002600 File Offset: 0x00000800
		private void UpdateShipmentLine(SOShipLine shipLine, int? usrLineNbr)
		{
			SOShipLineExt extension = PXCache<SOShipLine>.GetExtension<SOShipLineExt>(shipLine);
			extension.UsrLineNbr = usrLineNbr;
			base.Base.Transactions.Update(shipLine);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002630 File Offset: 0x00000830
		private void ProcessTransactionGroup(SOInvoiceEntry docgraph, IGrouping<string, ARTran> group)
		{
			decimal? qty = group.Sum((ARTran _) => _.Qty);
			SOOrderType soorderType = docgraph.soordertype.Current;
			bool flag = soorderType != null;
			if (flag)
			{
				SOOrderTypeExt extension = PXCache<SOOrderType>.GetExtension<SOOrderTypeExt>(soorderType);
				bool valueOrDefault = extension.UsrHandlingOrderType.GetValueOrDefault();
				if (valueOrDefault)
				{
					this.InsertHandlingTransaction(docgraph, extension, qty);
				}
			}
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000026A4 File Offset: 0x000008A4
		private void InsertHandlingTransaction(SOInvoiceEntry docgraph, SOOrderTypeExt orderTypeExt, decimal? qty)
		{
			ARTran artran = new ARTran
			{
				InventoryID = orderTypeExt.UsrHandlingItem,
				Qty = qty,
				CuryUnitPrice = orderTypeExt.UsrUnitPriceForHandling
			};
			docgraph.Transactions.Insert(artran);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000026E8 File Offset: 0x000008E8
		private void UpdateInvoiceTransactionLines(SOInvoiceEntry docgraph, IGrouping<string, ARTran> group)
		{
			foreach (ARTran artran in group)
			{
				SOLine soline = this._soDataProvider.GetSOLine(artran.SOOrderType, artran.SOOrderNbr, artran.SOOrderLineNbr);
				bool flag = soline != null;
				if (flag)
				{
					SOLineExt extension = PXCache<SOLine>.GetExtension<SOLineExt>(soline);
					ARTranExt extension2 = PXCache<ARTran>.GetExtension<ARTranExt>(artran);
					extension2.UsrLineNbr = extension.UsrLineNbr;
					docgraph.Transactions.Update(artran);
				}
			}
		}

		// Token: 0x02000011 RID: 17
		// (Invoke) Token: 0x06000041 RID: 65
		public delegate void CreateShipmentDelegate(CreateShipmentArgs args);

		// Token: 0x02000012 RID: 18
		// (Invoke) Token: 0x06000045 RID: 69
		public delegate void InvoiceShipmentDelegate(SOInvoiceEntry docgraph, SOShipment shiporder, DateTime invoiceDate, InvoiceList list, PXQuickProcess.ActionFlow quickProcessFlow);
	}
}
