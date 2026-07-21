using System;
using System.Collections.Generic;
using System.Linq;
using ASCiStarKohls.CacheExtension.AR;
using ASCiStarKohls.CacheExtension.SO;
using ASCiStarKohls.DataProvider.Interface;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;

namespace ASCiStarKohls.GraphExtension.SO
{
	// Token: 0x02000005 RID: 5
	public class SOOrderEntryExt : PXGraphExtension<SOOrderEntry>
	{
		// Token: 0x06000006 RID: 6 RVA: 0x000020A7 File Offset: 0x000002A7
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000007 RID: 7 RVA: 0x000020AA File Offset: 0x000002AA
		// (set) Token: 0x06000008 RID: 8 RVA: 0x000020B2 File Offset: 0x000002B2
		[InjectDependency]
		public ISODataProvider _soDataProvider { get; set; }

		// Token: 0x06000009 RID: 9 RVA: 0x000020BC File Offset: 0x000002BC
		[PXOverride]
		public void InvoiceOrder(Dictionary<string, object> parameters, IEnumerable<SOOrder> list, InvoiceList created, bool isMassProcess, PXQuickProcess.ActionFlow quickProcessFlow, bool groupByCustomerOrderNumber, SOOrderEntryExt.InvoiceOrderDelegate baseMethod)
		{
			baseMethod(parameters, list, created, isMassProcess, quickProcessFlow, groupByCustomerOrderNumber);
			SOOrderType soorderType = base.Base.soordertype.Current;
			bool flag = soorderType == null || !this.IsHandlingOrderType(soorderType);
			if (!flag)
			{
				using (PXTransactionScope pxtransactionScope = new PXTransactionScope())
				{
					foreach (PXResult<ARInvoice, SOInvoice> pxresult in created)
					{
						ARInvoice arinvoice = pxresult;
						SOInvoiceEntry soinvoiceEntry = PXGraph.CreateInstance<SOInvoiceEntry>();
						PXResultset<ARInvoice> pxresultset = soinvoiceEntry.Document.Search<ARInvoice.docType, ARInvoice.refNbr>(arinvoice.DocType, arinvoice.RefNbr, Array.Empty<object>());
						bool flag2 = pxresultset != null;
						if (flag2)
						{
							this.ProcessInvoice(soinvoiceEntry, pxresultset, soorderType);
						}
					}
					pxtransactionScope.Complete();
				}
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000021B8 File Offset: 0x000003B8
		private bool IsHandlingOrderType(SOOrderType orderType)
		{
			SOOrderTypeExt extension = PXCacheEx.GetExtension<SOOrderTypeExt>(orderType);
			return extension != null && extension.UsrHandlingOrderType.GetValueOrDefault();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000021E8 File Offset: 0x000003E8
		private void ProcessInvoice(SOInvoiceEntry invoiceEntry, ARInvoice invoice, SOOrderType orderType)
		{
			invoiceEntry.Document.Current = invoice;
			List<ARTran> list = GraphHelper.RowCast<ARTran>(invoiceEntry.Transactions.Select(Array.Empty<object>())).ToList<ARTran>();
			bool flag = !list.Any<ARTran>();
			if (!flag)
			{
				decimal orderQty = this.UpdateTransactionLines(invoiceEntry, list);
				this.InsertHandlingTransaction(invoiceEntry, orderType, orderQty);
				invoiceEntry.Save.Press();
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002250 File Offset: 0x00000450
		private decimal UpdateTransactionLines(SOInvoiceEntry invoiceEntry, List<ARTran> tranLines)
		{
			decimal num = 0m;
			foreach (ARTran artran in tranLines)
			{
				num += artran.Qty.GetValueOrDefault();
				SOLine soline = this._soDataProvider.GetSOLine(artran.SOOrderType, artran.SOOrderNbr, artran.SOOrderLineNbr);
				SOLineExt extension = PXCacheEx.GetExtension<SOLineExt>(soline);
				PXCacheEx.GetExtension<ARTranExt>(artran).UsrLineNbr = extension.UsrLineNbr;
				invoiceEntry.Transactions.Update(artran);
				invoiceEntry.Save.Press();
			}
			return num;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002314 File Offset: 0x00000514
		private void InsertHandlingTransaction(SOInvoiceEntry invoiceEntry, SOOrderType orderType, decimal orderQty)
		{
			SOOrderTypeExt extension = PXCacheEx.GetExtension<SOOrderTypeExt>(orderType);
			ARTran artran = new ARTran
			{
				InventoryID = extension.UsrHandlingItem,
				Qty = new decimal?(orderQty),
				CuryUnitPrice = extension.UsrUnitPriceForHandling
			};
			invoiceEntry.Transactions.Insert(artran);
			invoiceEntry.Save.Press();
		}

		// Token: 0x02000010 RID: 16
		// (Invoke) Token: 0x0600003D RID: 61
		public delegate void InvoiceOrderDelegate(Dictionary<string, object> parameters, IEnumerable<SOOrder> list, InvoiceList created, bool isMassProcess, PXQuickProcess.ActionFlow quickProcessFlow, bool groupByCustomerOrderNumber);
	}
}
