using System;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.IN.WMS;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace WMS
{
	// Token: 0x0200000D RID: 13
	public class HostExt : PXGraphExtension<PickPackShip, PickPackShip.Host>
	{
		// Token: 0x06000063 RID: 99 RVA: 0x00002287 File Offset: 0x00000487
		[PXSelector(typeof(SearchFor<SOPackageDetailEx.lineNbr>.Where<BqlOperand<SOPackageDetailEx.shipmentNbr, IBqlString>.IsEqual<BqlField<WMSScanHeader.refNbr, IBqlString>.FromCurrent>>), new Type[]
		{
			typeof(TypeArrayOf<IBqlField>.FilledWith<SOPackageDetail.confirmed, SOPackageDetailEx.lineNbr, SOPackageDetailEx.boxID, SOPackageDetailExt.usrCartonNbr, SOPackageDetailExt.usrIsParentBox, SOPackageDetailExt.usrSelectedParentBox, SOPackageDetailExt.usrStoreNbr, SOPackageDetailExt.usrOrderNbr, SOPackageDetailEx.boxDescription, SOPackageDetail.weight, SOPackageDetailEx.maxWeight, SOPackageDetail.weightUOM, SOPackageDetail.length, SOPackageDetail.width, SOPackageDetail.height>)
		}, DescriptionField = typeof(SOPackageDetailEx.boxID), DirtyRead = true, SuppressUnconditionalSelect = true)]
		[PXMergeAttributes(Method = 2)]
		protected void _(Events.CacheAttached<PackScanHeader.packageLineNbrUI> e)
		{
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004038 File Offset: 0x00002238
		protected virtual void _(Events.FieldSelecting<SOPackageDetailExExt.usrEstPackageQuantity> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				PickPackShip wms = base.Base.WMS;
				decimal? num;
				if (wms == null)
				{
					num = null;
				}
				else
				{
					PackModeLogicExt packModeLogicExt = wms.Get<PackModeLogicExt>();
					num = ((packModeLogicExt != null) ? new decimal?(packModeLogicExt.GetSelectedPackageEstimatedTotal((SOPackageDetailEx)e.Row)) : null);
				}
				decimal? num2 = num;
				e.ReturnValue = num2.GetValueOrDefault();
			}
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000040B0 File Offset: 0x000022B0
		protected virtual void _(Events.FieldSelecting<SOPackageDetailExExt.usrContentPackageQuantity> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				PickPackShip wms = base.Base.WMS;
				decimal? num;
				if (wms == null)
				{
					num = null;
				}
				else
				{
					PackModeLogicExt packModeLogicExt = wms.Get<PackModeLogicExt>();
					num = ((packModeLogicExt != null) ? new decimal?(packModeLogicExt.GetSelectedPackageActualTotal((SOPackageDetailEx)e.Row)) : null);
				}
				decimal? num2 = num;
				e.ReturnValue = num2.GetValueOrDefault();
			}
		}

		// Token: 0x0200003C RID: 60
		[PXUIField(DisplayName = "Total Estimated Qty")]
		public class TotalEstimatedQtyForPackage : PXFieldAttachedTo<SOShipLineSplit>.By<PickPackShip.Host>.AsDecimal.Named<HostExt.TotalEstimatedQtyForPackage>
		{
			// Token: 0x06000180 RID: 384 RVA: 0x00008AA0 File Offset: 0x00006CA0
			public override decimal? GetValue(SOShipLineSplit row)
			{
				PickPackShip wms = base.Base.GetExtension<PickPackShip>();
				PackModeLogicExt ext = (wms != null) ? wms.Get<PackModeLogicExt>() : null;
				return (ext != null) ? new decimal?(ext.GetSelectedPackageEstimatedQtyForSplit(row, null)) : null;
			}
		}

		// Token: 0x0200003D RID: 61
		[PXUIField(DisplayName = "Total Packed Qty")]
		public class TotalPackedQtyForPackage : PXFieldAttachedTo<SOShipLineSplit>.By<PickPackShip.Host>.AsDecimal.Named<HostExt.TotalPackedQtyForPackage>
		{
			// Token: 0x06000182 RID: 386 RVA: 0x00008AFC File Offset: 0x00006CFC
			public override decimal? GetValue(SOShipLineSplit row)
			{
				SOShipLine shipmentLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLine.lineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
				{
					(row != null) ? row.ShipmentNbr : null,
					(row != null) ? row.LineNbr : null
				}).TopFirst;
				PickPackShip wms = base.Base.GetExtension<PickPackShip>();
				PackModeLogicExt ext = (wms != null) ? wms.Get<PackModeLogicExt>() : null;
				return (ext != null) ? new decimal?(ext.GetSelectedPackageActualQty(row, (shipmentLine != null) ? shipmentLine.OrigOrderNbr : null, null)) : null;
			}
		}
	}
}
