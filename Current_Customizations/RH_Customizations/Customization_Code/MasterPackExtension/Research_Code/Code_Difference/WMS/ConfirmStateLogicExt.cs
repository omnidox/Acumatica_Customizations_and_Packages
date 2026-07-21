using System;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace WMS
{
	// Token: 0x0200000C RID: 12
	public class ConfirmStateLogicExt : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.ConfirmState.Logic>
	{
		// Token: 0x0600005F RID: 95 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003BF0 File Offset: 0x00001DF0
		[PXOverride]
		public virtual IEnumerable<SOShipLineSplit> GetSplitsToPack(Func<IEnumerable<SOShipLineSplit>> base_GetSplitsToPack)
		{
			ConfirmStateLogicExt.<>c__DisplayClass1_0 CS$<>8__locals1 = new ConfirmStateLogicExt.<>c__DisplayClass1_0();
			IEnumerable<SOShipLineSplit> enumerable = base_GetSplitsToPack();
			List<SOShipLineSplit> baseSplits = ((enumerable != null) ? enumerable.ToList<SOShipLineSplit>() : null) ?? new List<SOShipLineSplit>();
			bool flag = baseSplits.Count <= 1 || base.Basis.Remove.GetValueOrDefault();
			IEnumerable<SOShipLineSplit> result;
			if (flag)
			{
				result = baseSplits;
			}
			else
			{
				PickPackShip.PackMode.Logic mode = base.Basis.Get<PickPackShip.PackMode.Logic>();
				CS$<>8__locals1.ext = base.Basis.Get<PackModeLogicExt>();
				bool flag2 = mode == null || mode.PackageLineNbr == null || CS$<>8__locals1.ext == null;
				if (flag2)
				{
					result = baseSplits;
				}
				else
				{
					ConfirmStateLogicExt.<>c__DisplayClass1_0 CS$<>8__locals2 = CS$<>8__locals1;
					int? packageLineNbrUI = mode.PackageLineNbrUI;
					int? packageLineNbr2;
					if (packageLineNbrUI == null)
					{
						int? packageLineNbr = mode.PackageLineNbr;
						if (packageLineNbr == null)
						{
							SOPackageDetailEx selectedPackage = mode.SelectedPackage;
							packageLineNbr2 = ((selectedPackage != null) ? selectedPackage.LineNbr : null);
						}
						else
						{
							packageLineNbr2 = packageLineNbr;
						}
					}
					else
					{
						packageLineNbr2 = packageLineNbrUI;
					}
					CS$<>8__locals2.packageLineNbr = packageLineNbr2;
					bool flag3 = CS$<>8__locals1.packageLineNbr == null;
					if (flag3)
					{
						result = baseSplits;
					}
					else
					{
						List<SelectedPackageContents> planRows = GraphHelper.RowCast<SelectedPackageContents>(PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
						{
							base.Basis.RefNbr,
							CS$<>8__locals1.packageLineNbr
						})).ToList<SelectedPackageContents>();
						bool flag4 = planRows.Count == 0;
						if (flag4)
						{
							result = baseSplits;
						}
						else
						{
							CS$<>8__locals1.plannedSplitKeys = new HashSet<ValueTuple<int, int>>(from p in planRows
							where p.ShipmentLineNbr != null && p.ShipmentSplitLineNbr != null
							select new ValueTuple<int, int>(p.ShipmentLineNbr.Value, p.ShipmentSplitLineNbr.Value));
							CS$<>8__locals1.plannedLineInventoryKeys = new HashSet<ValueTuple<int, int>>(from p in planRows
							where p.ShipmentLineNbr != null && p.InventoryID != null
							select new ValueTuple<int, int>(p.ShipmentLineNbr.Value, p.InventoryID.Value));
							result = (from split in baseSplits
							orderby split != null && split.LineNbr != null && split != null && split.SplitLineNbr != null && CS$<>8__locals1.plannedSplitKeys.Contains(new ValueTuple<int, int>(split.LineNbr.Value, split.SplitLineNbr.Value)) descending, split != null && split.LineNbr != null && split != null && split.InventoryID != null && CS$<>8__locals1.plannedLineInventoryKeys.Contains(new ValueTuple<int, int>(split.LineNbr.Value, split.InventoryID.Value)) descending, CS$<>8__locals1.ext.GetSelectedPackageEstimatedQtyForSplit(split, CS$<>8__locals1.packageLineNbr) descending, ((split != null) ? split.LineNbr : null).GetValueOrDefault(), ((split != null) ? split.SplitLineNbr : null).GetValueOrDefault()
							select split).ToList<SOShipLineSplit>();
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00003E88 File Offset: 0x00002088
		[PXOverride]
		public virtual FlowStatus Confirm(ConfirmStateLogicExt.ConfirmDelegate baseMethod)
		{
			bool valueOrDefault = base.Basis.Remove.GetValueOrDefault();
			FlowStatus result;
			if (valueOrDefault)
			{
				result = baseMethod();
			}
			else
			{
				PickPackShip.PackMode.Logic mode = base.Basis.Get<PickPackShip.PackMode.Logic>();
				bool flag = mode == null || mode.PackageLineNbr == null || base.Basis.InventoryID == null;
				if (flag)
				{
					result = baseMethod();
				}
				else
				{
					SOPackageDetailEx selectedPackage = mode.SelectedPackage;
					bool flag2 = selectedPackage == null;
					if (flag2)
					{
						result = baseMethod();
					}
					else
					{
						PackModeLogicExt ext = base.Basis.Get<PackModeLogicExt>();
						bool flag3 = ext == null;
						if (flag3)
						{
							result = baseMethod();
						}
						else
						{
							decimal estimatedTotal = ext.GetSelectedPackageEstimatedTotal(selectedPackage);
							bool flag4 = estimatedTotal <= 0m;
							if (flag4)
							{
								result = baseMethod();
							}
							else
							{
								decimal actualTotal = ext.GetSelectedPackageActualTotal(selectedPackage);
								decimal remaining = estimatedTotal - actualTotal;
								SOPackageDetailExt extension = PXCacheEx.GetExtension<SOPackageDetailExt>(selectedPackage);
								string cartonLabel = ((extension != null) ? extension.UsrCartonNbr : null) ?? selectedPackage.BoxID;
								bool flag5 = remaining <= 0m;
								if (flag5)
								{
									result = FlowStatus.Fail("Carton #{0} is already at the estimated quantity ({1}). Scan a different carton.", new object[]
									{
										cartonLabel,
										estimatedTotal
									});
								}
								else
								{
									decimal qty = base.Basis.BaseQty;
									bool flag6 = qty > remaining;
									if (flag6)
									{
										result = FlowStatus.Fail("Packing {0} unit(s) would exceed the remaining estimated quantity ({1}) for carton #{2}. Scan a different carton.", new object[]
										{
											qty,
											remaining,
											cartonLabel
										});
									}
									else
									{
										result = baseMethod();
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x02000039 RID: 57
		// (Invoke) Token: 0x06000171 RID: 369
		public delegate FlowStatus ConfirmDelegate();
	}
}
