using System;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.SO;
using WMS.SO.DACExt;

namespace WMS.SO.GraphExt
{
	// Token: 0x0200001C RID: 28
	public class SOOrderEntryExt : PXGraphExtension<SOOrderEntry>
	{
		// Token: 0x06000114 RID: 276 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000960C File Offset: 0x0000780C
		protected void _(Events.RowPersisting<SOOrder> e)
		{
			SOOrder row = e.Row;
			bool flag = row == null || PXDBOperationExt.Command(e.Operation) != 2;
			if (!flag)
			{
				SOOrderType orderType = PXSelectBase<SOOrderType, PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>>>.Config>.Select(base.Base, new object[]
				{
					row.OrderType
				});
				bool? flag2;
				if (orderType == null)
				{
					flag2 = null;
				}
				else
				{
					SOOrderTypeExt extension = PXCacheEx.GetExtension<SOOrderTypeExt>(orderType);
					flag2 = ((extension != null) ? extension.UsrIsTCOrderType : null);
				}
				bool? flag3 = flag2;
				bool isTCOrderType = flag3.GetValueOrDefault();
				bool flag4 = isTCOrderType && orderType.CustomerOrderValidation == "W";
				if (flag4)
				{
					SOOrderExt extension2 = PXCacheEx.GetExtension<SOOrderExt>(row);
					string currentEDIStore = (extension2 != null) ? extension2.UsrTCStoreNumber : null;
					SOOrder duplicate = PXSelectBase<SOOrder, PXViewOf<SOOrder>.BasedOn<SelectFromBase<SOOrder, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOOrder.orderType, Equal<P.AsString>>>>, And<BqlOperand<SOOrder.orderNbr, IBqlString>.IsNotEqual<P.AsString>>>, And<BqlOperand<SOOrder.status, IBqlString>.IsNotEqual<SOOrderStatus.cancelled>>>, And<Brackets<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOOrder.customerOrderNbr, Equal<P.AsString>>>>>.Or<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOOrder.customerOrderNbr, IsNull>>>>.And<BqlOperand<Required<Parameter.ofString>, IBqlString>.IsNull>>>>>>.And<Brackets<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOOrderExt.usrTCStoreNumber, Equal<P.AsString>>>>>.Or<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOOrderExt.usrTCStoreNumber, IsNull>>>>.And<BqlOperand<Required<Parameter.ofString>, IBqlString>.IsNull>>>>>>.Config>.Select(base.Base, new object[]
					{
						row.OrderType,
						row.OrderNbr,
						row.CustomerOrderNbr,
						row.CustomerOrderNbr,
						currentEDIStore,
						currentEDIStore
					}).TopFirst;
					bool flag5 = duplicate != null;
					if (flag5)
					{
						throw new PXRowPersistingException(typeof(SOOrder.customerOrderNbr).Name, row.CustomerOrderNbr, string.Concat(new string[]
						{
							"This SO with the same order type, Customer Order Nbr '",
							row.CustomerOrderNbr,
							"' and EDI store nbr '",
							currentEDIStore,
							"' already exists (",
							duplicate.OrderNbr,
							")."
						}));
					}
				}
			}
		}
	}
}
