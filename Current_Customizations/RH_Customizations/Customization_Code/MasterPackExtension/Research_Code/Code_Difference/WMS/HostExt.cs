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
	// Token: 0x0200000F RID: 15
	public class HostExt : PXGraphExtension<PickPackShip, PickPackShip.Host>
	{
		// Token: 0x0600006F RID: 111 RVA: 0x000024BB File Offset: 0x000006BB
		[PXSelector(typeof(SearchFor<SOPackageDetailEx.lineNbr>.Where<BqlOperand<SOPackageDetailEx.shipmentNbr, IBqlString>.IsEqual<BqlField<WMSScanHeader.refNbr, IBqlString>.FromCurrent>>), new Type[]
		{
			typeof(TypeArrayOf<IBqlField>.FilledWith<SOPackageDetail.confirmed, SOPackageDetailEx.lineNbr, SOPackageDetailEx.boxID, SOPackageDetailExt.usrCartonNbr, SOPackageDetailExt.usrIsParentBox, SOPackageDetailExt.usrSelectedParentBox, SOPackageDetailExt.usrStoreNbr, SOPackageDetailExt.usrOrderNbr, SOPackageDetailEx.boxDescription, SOPackageDetail.weight, SOPackageDetailEx.maxWeight, SOPackageDetail.weightUOM, SOPackageDetail.length, SOPackageDetail.width, SOPackageDetail.height>)
		}, DescriptionField = typeof(SOPackageDetailEx.boxID), DirtyRead = true, SuppressUnconditionalSelect = true)]
		[PXMergeAttributes(Method = 2)]
		protected void _(Events.CacheAttached<PackScanHeader.packageLineNbrUI> e)
		{
		}

		// Token: 0x06000070 RID: 112 RVA: 0x0000426C File Offset: 0x0000246C
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

		// Token: 0x06000071 RID: 113 RVA: 0x000042E4 File Offset: 0x000024E4
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

		// Token: 0x0200003E RID: 62
		[PXUIField(DisplayName = "Total Estimated Qty")]
		public class TotalEstimatedQtyForPackage : PXFieldAttachedTo<SOShipLineSplit>.By<PickPackShip.Host>.AsDecimal.Named<HostExt.TotalEstimatedQtyForPackage>
		{
			// Token: 0x06000197 RID: 407 RVA: 0x00009FB0 File Offset: 0x000081B0
			public override decimal? GetValue(SOShipLineSplit row)
			{
				PickPackShip wms = base.Base.GetExtension<PickPackShip>();
				PackModeLogicExt ext = (wms != null) ? wms.Get<PackModeLogicExt>() : null;
				return (ext != null) ? new decimal?(ext.GetSelectedPackageEstimatedQtyForSplit(row, null)) : null;
			}
		}

		// Token: 0x0200003F RID: 63
		[PXUIField(DisplayName = "Total Packed Qty")]
		public class TotalPackedQtyForPackage : PXFieldAttachedTo<SOShipLineSplit>.By<PickPackShip.Host>.AsDecimal.Named<HostExt.TotalPackedQtyForPackage>
		{
			// Token: 0x06000199 RID: 409 RVA: 0x0000A00C File Offset: 0x0000820C
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
