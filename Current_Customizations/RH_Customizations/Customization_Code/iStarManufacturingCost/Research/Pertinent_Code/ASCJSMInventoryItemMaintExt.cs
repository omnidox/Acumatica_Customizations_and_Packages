using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ASCJSMCustom.AP.CacheExt;
using ASCJSMCustom.AP.DAC;
using ASCJSMCustom.Common.Builder;
using ASCJSMCustom.Common.Descriptor;
using ASCJSMCustom.Common.DTO.Interfaces;
using ASCJSMCustom.Common.Helper;
using ASCJSMCustom.IN.CacheExt;
using ASCJSMCustom.IN.DAC;
using ASCJSMCustom.PO.CacheExt;
using PX.Common;
using PX.CS;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.Objects.PO;

namespace ASCJSMCustom.IN.GraphExt
{
	// Token: 0x02000015 RID: 21
	public class ASCJSMInventoryItemMaintExt : PXGraphExtension<InventoryItemMaint>
	{
		// Token: 0x060001D5 RID: 469 RVA: 0x000088F5 File Offset: 0x00006AF5
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x000088F8 File Offset: 0x00006AF8
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBString(30, IsUnicode = true, InputMask = "####.##.####")]
		[PXUIField(DisplayName = "Tariff / HTS Code")]
		[PXSelector(typeof(SearchFor<ASCJSMAPTariffHTSCode.hSTariffCode>))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.hSTariffCode> e)
		{
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x000088FB File Offset: 0x00006AFB
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = "Last Modified Date", IsReadOnly = true)]
		protected virtual void _(Events.CacheAttached<INItemXRef.lastModifiedDateTime> e)
		{
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x00008900 File Offset: 0x00006B00
		[PXUIField(DisplayName = "Update Metal Cost", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable updateMetalCost(PXAdapter adapter)
		{
			bool flag = base.Base.Item.Current == null;
			IEnumerable result;
			if (flag)
			{
				result = adapter.Get();
			}
			else
			{
				this.UpdateCommodityCostMetal(base.Base.Item.Cache, base.Base.Item.Current, base.Base.Item.Current.GetExtension<ASCJSMINInventoryItemExt>());
				result = adapter.Get();
			}
			return result;
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x00008974 File Offset: 0x00006B74
		protected virtual void _(Events.RowSelecting<InventoryItem> e)
		{
			bool flag = e.Row == null || e.Row.KitItem.GetValueOrDefault();
			if (!flag)
			{
				using (new PXConnectionScope())
				{
					ASCJSMINInventoryItemExt extension = e.Row.GetExtension<ASCJSMINInventoryItemExt>();
					this.UpdateCommodityCostMetal(e.Cache, e.Row, extension);
				}
			}
		}

		// Token: 0x060001DA RID: 474 RVA: 0x000089EC File Offset: 0x00006BEC
		protected virtual void _(Events.FieldSelecting<InventoryItem, ASCJSMINInventoryItemExt.usrBasisValue> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || row.KitItem.GetValueOrDefault();
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				e.ReturnValue = extension.UsrBasisValue;
			}
		}

		// Token: 0x060001DB RID: 475 RVA: 0x00008A44 File Offset: 0x00006C44
		protected virtual void _(Events.FieldSelecting<InventoryItem, ASCJSMINInventoryItemExt.usrUnitCost> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(e.Row);
				e.ReturnValue = extension.UsrPackagingLaborCost.GetValueOrDefault() + extension.UsrOtherMaterialsCost.GetValueOrDefault() + extension.UsrFabricationCost.GetValueOrDefault() + extension.UsrPackagingCost.GetValueOrDefault() + extension.UsrPreciousMetalCost.GetValueOrDefault() + extension.UsrLaborCost.GetValueOrDefault() + extension.UsrHandlingCost.GetValueOrDefault();
			}
		}

		// Token: 0x060001DC RID: 476 RVA: 0x00008AFC File Offset: 0x00006CFC
		protected virtual void _(Events.RowSelected<InventoryItem> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				bool flag2 = this.IsVisibleFields(extension, row.ItemClassID);
				this.SetVisibleJewelFields(e.Cache, row, flag2);
				bool flag3 = this.JewelryItemView.Current == null;
				if (flag3)
				{
					this.JewelryItemView.Current = this.JewelryItemView.Select(Array.Empty<object>());
				}
				PXCache cache = e.Cache;
				InventoryItem row2 = row;
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				this.SetReadOnlyJewelAttrFields(cache, row2, (ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				bool flag4 = flag2 || extension.UsrCommodityType == "G" || extension.UsrCommodityType == "S";
				PXUIFieldAttribute.SetRequired<ASCJSMINJewelryItem.metalType>(this.JewelryItemView.Cache, flag4);
				PXDefaultAttribute.SetPersistingCheck<ASCJSMINJewelryItem.metalType>(this.JewelryItemView.Cache, this.JewelryItemView.Current, flag4 ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				bool flag5 = !row.KitItem.GetValueOrDefault();
				if (flag5)
				{
					this.UpdateCommodityCostMetal(e.Cache, row, extension);
					this.UpdateUsrIncrement(e.Cache, row);
				}
			}
		}

		// Token: 0x060001DD RID: 477 RVA: 0x00008C34 File Offset: 0x00006E34
		protected virtual void _(Events.FieldDefaulting<InventoryItem, ASCJSMINInventoryItemExt.usrCostingType> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = e.Row.GetExtension<ASCJSMINInventoryItemExt>();
				INItemClass initemClass = INItemClass.PK.Find(base.Base, e.Row.ItemClassID, PKFindOptions.None);
				ASCJSMINItemClassExt ascjsminitemClassExt = (initemClass != null) ? initemClass.GetExtension<ASCJSMINItemClassExt>() : null;
				e.NewValue = (((ascjsminitemClassExt != null) ? ascjsminitemClassExt.UsrCostingType : null) ?? "C");
			}
		}

		// Token: 0x060001DE RID: 478 RVA: 0x00008CA0 File Offset: 0x00006EA0
		protected virtual void _(Events.FieldVerifying<InventoryItem, ASCJSMINInventoryItemExt.usrMatrixStep> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0.0m;
				bool flag2 = num.GetValueOrDefault() <= d & num != null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrMatrixStep>(e.Row, 0.5m, new PXSetPropertyException("ERP is taking Market price, Matrix Step cannot be zero!", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00008D1C File Offset: 0x00006F1C
		protected virtual void _(Events.FieldVerifying<InventoryItem, ASCJSMINInventoryItemExt.usrContractSurcharge> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0.0m;
				bool flag2 = num.GetValueOrDefault() < d & num != null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractSurcharge>(e.Row, e.NewValue, new PXSetPropertyException("Surcharge can not be negative.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x00008D90 File Offset: 0x00006F90
		protected virtual void _(Events.FieldVerifying<InventoryItem, ASCJSMINInventoryItemExt.usrContractSurchargeAmount> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0.0m;
				bool flag2 = num.GetValueOrDefault() < d & num != null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractSurchargeAmount>(e.Row, e.NewValue, new PXSetPropertyException("Surcharge can not be negative.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x00008E04 File Offset: 0x00007004
		protected virtual void _(Events.FieldVerifying<InventoryItem, ASCJSMINInventoryItemExt.usrCostingType> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				POVendorInventory defaultVendor = this.GetDefaultVendor();
				bool flag2 = defaultVendor == null;
				if (!flag2)
				{
					ASCJSMPOVendorInventoryExt extension2 = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(defaultVendor);
					bool flag3;
					if (extension != null)
					{
						object newValue = e.NewValue;
						if (((newValue != null) ? newValue.ToString() : null) != "C")
						{
							bool valueOrDefault = defaultVendor.IsDefault.GetValueOrDefault();
							bool? usrIsOverrideVendor = extension2.UsrIsOverrideVendor;
							flag3 = (valueOrDefault == usrIsOverrideVendor.GetValueOrDefault() & usrIsOverrideVendor != null);
							goto IL_87;
						}
					}
					flag3 = false;
					IL_87:
					bool flag4 = flag3;
					if (flag4)
					{
						base.Base.Item.Cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrCostingType>(row, e.NewValue, new PXSetPropertyException("To see how Unit Cost depends from Vendor Price select \"Contract\" Costing Type!", PXErrorLevel.RowWarning));
					}
				}
			}
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00008EC8 File Offset: 0x000070C8
		protected virtual void _(Events.FieldUpdating<InventoryItem, InventoryItem.descr> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				bool flag2 = this.JewelryItemView.Current == null;
				if (flag2)
				{
					this.JewelryItemView.Current = this.JewelryItemView.Select(Array.Empty<object>());
				}
				bool flag3 = this.JewelryItemView.Current == null;
				if (flag3)
				{
					this.JewelryItemView.Current = this.JewelryItemView.Insert();
				}
				this.JewelryItemView.SetValueExt<ASCJSMINJewelryItem.shortDesc>(this.JewelryItemView.Current, e.NewValue);
			}
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x00008F60 File Offset: 0x00007160
		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.itemClassID> e)
		{
			InventoryItem row = e.Row;
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				bool isVisible = this.IsVisibleFields(extension, row.ItemClassID);
				this.SetVisibleJewelFields(e.Cache, row, isVisible);
			}
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x00008FA8 File Offset: 0x000071A8
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrCostingType> e)
		{
			InventoryItem row = e.Row;
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x00008FF0 File Offset: 0x000071F0
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrActualGRAMGold> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				decimal goldTypeValue = ASCJSMMetalType.GetGoldTypeValue((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				ASCJSMINInventoryItemExt extension = row.GetExtension<ASCJSMINInventoryItemExt>();
				decimal? num = e.Row.GetExtension<ASCJSMINInventoryItemExt>().UsrContractSurcharge;
				decimal? num2 = e.Row.GetExtension<ASCJSMINInventoryItemExt>().UsrContractSurcharge;
				decimal d = 100;
				num = num2 / d;
				num2 = num;
				d = 0.0m;
				bool flag2 = (num2.GetValueOrDefault() == d & num2 != null) || num == null;
				if (flag2)
				{
					num = new decimal?(0.0001m);
				}
				ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(extension, null);
				num2 = (decimal?)e.NewValue * goldTypeValue;
				d = 24;
				decimal? num3 = num2 / d;
				bool flag3 = ascjsmcostBuilder != null && ascjsmcostBuilder.INJewelryItem.MetalType.EndsWith("F");
				if (flag3)
				{
					num2 = num3;
					d = 0.05m;
					num3 = num2 * d;
				}
				e.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrPricingGRAMGold>(row, num3);
				this.RecalculateInventoryFabricationValue(row);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x000091E8 File Offset: 0x000073E8
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrActualGRAMSilver> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				decimal silverTypeValue = ASCJSMMetalType.GetSilverTypeValue((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				e.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrPricingGRAMSilver>(row, (decimal?)e.NewValue * silverTypeValue);
				this.RecalculateInventoryFabricationValue(row);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x00009290 File Offset: 0x00007490
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrPricingGRAMGold> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				decimal goldTypeValue = ASCJSMMetalType.GetGoldTypeValue((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				this.RecalculateInventoryFabricationValue(row);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00009308 File Offset: 0x00007508
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrPricingGRAMSilver> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				decimal silverTypeValue = ASCJSMMetalType.GetSilverTypeValue((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				this.RecalculateInventoryFabricationValue(row);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00009380 File Offset: 0x00007580
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrPreciousMetalCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001EA RID: 490 RVA: 0x000093B0 File Offset: 0x000075B0
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrUnitCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				decimal? num = (decimal?)e.NewValue;
				ASCJSMINInventoryItemExt ascjsmininventoryItemExt = extension;
				decimal? usrDutyCostPct = extension.UsrDutyCostPct;
				decimal? num2 = num;
				ascjsmininventoryItemExt.UsrDutyCost = ((usrDutyCostPct != null & num2 != null) ? new decimal?(usrDutyCostPct.GetValueOrDefault() * num2.GetValueOrDefault() / 100.0m) : null);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrDutyCost>(extension.UsrDutyCost, null);
			}
		}

		// Token: 0x060001EB RID: 491 RVA: 0x00009454 File Offset: 0x00007654
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrContractIncrement> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrContractIncrement>(e.NewValue, null);
				this.UpdateUsrIncrement(e.Cache, row);
				bool valueOrDefault = extension.UsrEnableVendorIncrement.GetValueOrDefault();
				if (valueOrDefault)
				{
					this.UpdateCommodityCostMetal(e.Cache, row, extension);
				}
			}
		}

		// Token: 0x060001EC RID: 492 RVA: 0x000094BC File Offset: 0x000076BC
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrEnableVendorIncrement> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				bool valueOrDefault = (e.OldValue as bool?).GetValueOrDefault();
				bool valueOrDefault2 = extension.UsrEnableVendorIncrement.GetValueOrDefault();
				bool flag2 = valueOrDefault && !valueOrDefault2;
				if (flag2)
				{
					e.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrContractIncrement>(row, 0m);
				}
				PXCache cache = e.Cache;
				InventoryItem row2 = row;
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				this.SetReadOnlyJewelAttrFields(cache, row2, (ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0000957C File Offset: 0x0000777C
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrMatrixStep> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || row.KitItem.GetValueOrDefault();
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrMatrixStep>(e.NewValue, null);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrBasisValue>(extension.UsrBasisValue, null);
			}
		}

		// Token: 0x060001EE RID: 494 RVA: 0x000095E4 File Offset: 0x000077E4
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrContractSurcharge> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrContractSurcharge>((decimal?)e.NewValue, null);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001EF RID: 495 RVA: 0x00009640 File Offset: 0x00007840
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrContractSurchargeAmount> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>((decimal?)e.NewValue, null);
			}
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x00009690 File Offset: 0x00007890
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrContractLossPct> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.UpdateCommodityCostMetal(e.Cache, row, extension);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrContractLossPct>((decimal?)e.NewValue, null);
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x000096EC File Offset: 0x000078EC
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrOtherMaterialsCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrOtherMaterialsCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x0000971C File Offset: 0x0000791C
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrFabricationCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrFabricationCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x0000974C File Offset: 0x0000794C
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrPackagingCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrPackagingCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0000977C File Offset: 0x0000797C
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrPackagingLaborCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrPackagingLaborCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x000097AC File Offset: 0x000079AC
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrLaborCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrLaborCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x000097DC File Offset: 0x000079DC
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrFreightCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrFreightCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0000980C File Offset: 0x00007A0C
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrDutyCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrDutyCost>(e.NewValue, null);
				bool flag2;
				if (extension.UsrUnitCost != null)
				{
					decimal? num = extension.UsrUnitCost;
					decimal d = 0.0m;
					flag2 = (num.GetValueOrDefault() == d & num != null);
				}
				else
				{
					flag2 = true;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					extension.UsrDutyCostPct = new decimal?(0m);
				}
				else
				{
					decimal? num = (decimal?)e.NewValue;
					decimal? num2 = extension.UsrUnitCost;
					decimal? num3 = (num != null & num2 != null) ? new decimal?(num.GetValueOrDefault() / num2.GetValueOrDefault() * 100.0m) : null;
					num2 = num3;
					num = extension.UsrDutyCostPct;
					bool flag4 = num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null);
					if (!flag4)
					{
						extension.UsrDutyCostPct = num3;
					}
				}
			}
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0000993C File Offset: 0x00007B3C
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrDutyCostPct> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				decimal? num = extension.UsrUnitCost;
				decimal? num2 = (decimal?)e.NewValue;
				decimal? num3 = (num != null & num2 != null) ? new decimal?(num.GetValueOrDefault() * num2.GetValueOrDefault() / 100.00m) : null;
				num2 = num3;
				num = extension.UsrDutyCost;
				bool flag2 = num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null);
				if (!flag2)
				{
					e.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrDutyCost>(row, num3);
				}
			}
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x00009A10 File Offset: 0x00007C10
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrHandlingCost> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrHandlingCost>(e.NewValue, null);
			}
		}

		// Token: 0x060001FA RID: 506 RVA: 0x00009A40 File Offset: 0x00007C40
		protected virtual void _(Events.FieldUpdated<InventoryItem, ASCJSMINInventoryItemExt.usrCommodityType> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null || e.NewValue == null;
			if (!flag)
			{
				e.Cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrCommodityType>(row, e.NewValue, new PXSetPropertyException("Metal Type is empty on Jewelry Attributes tab", PXErrorLevel.Warning));
				this.JewelryItemView.Cache.RaiseExceptionHandling<ASCJSMINJewelryItem.metalType>(this.JewelryItemView.Current, null, new PXSetPropertyException("As Commodity Type was changes, select new Metal Type", PXErrorLevel.Warning));
				this.UpdateUsrIncrement(e.Cache, row);
			}
		}

		// Token: 0x060001FB RID: 507 RVA: 0x00009AC0 File Offset: 0x00007CC0
		protected virtual void _(Events.FieldSelecting<InventoryItem, ASCJSMINInventoryItemExt.usrIncrement> e)
		{
			InventoryItem row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				bool flag2;
				if (extension.UsrIncrement != null)
				{
					decimal? usrIncrement = extension.UsrIncrement;
					decimal d = 0m;
					flag2 = !(usrIncrement.GetValueOrDefault() == d & usrIncrement != null);
				}
				else
				{
					flag2 = false;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					e.ReturnValue = extension.UsrIncrement;
				}
				else
				{
					bool flag4 = extension.UsrCommodityType == "G" && extension.UsrEnableVendorIncrement.GetValueOrDefault();
					if (flag4)
					{
						e.ReturnValue = extension.UsrContractIncrement;
					}
					else
					{
						bool flag5 = extension.UsrCommodityType == "G" && extension.UsrPricingGRAMGold != null;
						if (flag5)
						{
							decimal? num = 1m / ASCJSMConstants.TOZ2GRAM_31_10348.value * extension.UsrPricingGRAMGold * (1m + extension.UsrContractSurcharge.GetValueOrDefault() / 100m) * (1m + extension.UsrContractLossPct.GetValueOrDefault() / 100m);
							e.ReturnValue = num;
						}
						else
						{
							bool flag6 = extension.UsrCommodityType == "S";
							if (flag6)
							{
								e.ReturnValue = extension.UsrContractIncrement.GetValueOrDefault() * extension.UsrActualGRAMSilver.GetValueOrDefault();
							}
						}
					}
				}
			}
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00009CF0 File Offset: 0x00007EF0
		protected virtual void _(Events.FieldUpdated<ASCJSMINJewelryItem, ASCJSMINJewelryItem.metalType> e)
		{
			ASCJSMINJewelryItem row = e.Row;
			bool flag = row == null || base.Base.Item.Current == null;
			if (!flag)
			{
				object newValue = e.NewValue;
				this.UpdateMetalGrams((newValue != null) ? newValue.ToString() : null);
				this.UpdateCommodityCostMetal(base.Base.Item.Cache, base.Base.Item.Current, PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current));
			}
		}

		// Token: 0x060001FD RID: 509 RVA: 0x00009D7C File Offset: 0x00007F7C
		protected virtual void _(Events.RowPersisting<ASCJSMINJewelryItem> e)
		{
			ASCJSMINJewelryItem row = e.Row;
			bool flag = row == null || row.MetalType != null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
				bool flag2 = extension.UsrCommodityType == "G" || extension.UsrCommodityType == "S";
				if (flag2)
				{
					PXSetPropertyException<ASCJSMINJewelryItem.metalType> exception = new PXSetPropertyException<ASCJSMINJewelryItem.metalType>("Metal Type is empty on Jewelry Attributes tab", PXErrorLevel.Error);
					e.Cache.RaiseExceptionHandling<ASCJSMINJewelryItem.metalType>(row, row.MetalType, exception);
					throw new PXException();
				}
			}
		}

		// Token: 0x060001FE RID: 510 RVA: 0x00009E10 File Offset: 0x00008010
		protected virtual void _(Events.RowSelected<POVendorInventory> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetReadOnlyPOVendorInventoryFields(e.Cache, row);
				this.SetVisiblePOVendorInventoryFields(e.Cache);
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				bool flag2 = extension != null && extension.UsrIsOverrideVendor.GetValueOrDefault();
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrUnitCost>(row, extension.UsrUnitCost, new PXSetPropertyException("Unit Cost is custom, the Override Vendor checkBox is enabled", PXErrorLevel.Warning));
				}
				else
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrUnitCost>(row, extension.UsrUnitCost, null);
				}
			}
		}

		// Token: 0x060001FF RID: 511 RVA: 0x00009EAC File Offset: 0x000080AC
		protected virtual void _(Events.FieldSelecting<POVendorInventory, ASCJSMPOVendorInventoryExt.usrFabricationCost> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || row.VendorID == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = row.GetExtension<ASCJSMPOVendorInventoryExt>();
				decimal? num = this.CalculateFabricationValue(row);
				decimal? usrFabricationCost = extension.UsrFabricationCost;
				decimal? num2 = num;
				bool flag2 = !(usrFabricationCost.GetValueOrDefault() == num2.GetValueOrDefault() & usrFabricationCost != null == (num2 != null));
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrFabricationCost>(row, extension.UsrFabricationCost, new PXSetPropertyException("Fabrication Cost Inconsistency.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00009F4C File Offset: 0x0000814C
		protected virtual void _(Events.FieldVerifying<POVendorInventory, POVendorInventory.isDefault> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || !(bool)e.NewValue;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				bool flag2 = extension.UsrMarketID == null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrMarketID>(row, false, new PXSetPropertyException("Market can not be empty!", PXErrorLevel.RowError));
				}
				PXGraph @base = base.Base;
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				int? commodityInventoryByMetalType = ASCJSMMetalType.GetCommodityInventoryByMetalType(@base, (ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				bool flag3 = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, row.VendorID, commodityInventoryByMetalType, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Today) == null && !PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row).UsrIsOverrideVendor.GetValueOrDefault();
				if (flag3)
				{
					e.Cache.RaiseExceptionHandling<POVendorInventory.isDefault>(row, false, new PXSetPropertyException("Vendor price record not found, check Vendor Prices screen.", PXErrorLevel.RowWarning));
				}
				PXResultset<POVendorInventory> pxresultset = base.Base.VendorItems.Select(Array.Empty<object>());
				List<POVendorInventory> list = (pxresultset != null) ? pxresultset.FirstTableItems.ToList<POVendorInventory>() : null;
				foreach (POVendorInventory povendorInventory in list)
				{
					bool flag4 = povendorInventory.IsDefault.GetValueOrDefault() && povendorInventory != row;
					if (flag4)
					{
						base.Base.VendorItems.Cache.SetValue<POVendorInventory.isDefault>(povendorInventory, false);
						base.Base.VendorItems.View.RequestRefresh();
						break;
					}
				}
			}
		}

		// Token: 0x06000201 RID: 513 RVA: 0x0000A108 File Offset: 0x00008308
		protected virtual void _(Events.FieldVerifying<POVendorInventory, ASCJSMPOVendorInventoryExt.usrIsOverrideVendor> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || !row.IsDefault.GetValueOrDefault();
			if (!flag)
			{
				bool flag2 = (bool)e.NewValue;
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				bool flag3 = !flag2;
				if (!flag3)
				{
					decimal? usrCommodityVendorPrice = extension.UsrCommodityVendorPrice;
					decimal d = 0m;
					bool flag4 = usrCommodityVendorPrice.GetValueOrDefault() == d & usrCommodityVendorPrice != null;
					if (flag4)
					{
						e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice>(row, extension.UsrCommodityVendorPrice, new PXSetPropertyException("Vendor Price can not be empty.", PXErrorLevel.Error));
					}
					ASCJSMINInventoryItemExt extension2 = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
					bool flag5 = extension2 != null && extension2.UsrCostingType != "C";
					if (flag5)
					{
						base.Base.Item.Cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrCostingType>(base.Base.Item.Current, extension2.UsrCostingType, new PXSetPropertyException("To see how Unit Cost depends from Vendor Price select \"Contract\" Costing Type!", PXErrorLevel.RowWarning));
					}
				}
			}
		}

		// Token: 0x06000202 RID: 514 RVA: 0x0000A220 File Offset: 0x00008420
		protected virtual void _(Events.FieldVerifying<POVendorInventory, ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				bool flag2 = (decimal)e.NewValue == 0m && extension.UsrIsOverrideVendor.GetValueOrDefault();
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice>(row, extension.UsrBasisPrice, new PXSetPropertyException("Vendor Price can not be empty.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000203 RID: 515 RVA: 0x0000A298 File Offset: 0x00008498
		protected virtual void _(Events.FieldVerifying<POVendorInventory, ASCJSMPOVendorInventoryExt.usrBasisPrice> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0m;
				bool flag2 = num.GetValueOrDefault() == d & num != null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrBasisPrice>(row, e.NewValue, new PXSetPropertyException("Basis or Market price is empty, enter value or check Vendor Prices screen.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000204 RID: 516 RVA: 0x0000A308 File Offset: 0x00008508
		protected virtual void _(Events.FieldVerifying<POVendorInventory, ASCJSMPOVendorInventoryExt.usrMatrixStep> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0.0m;
				bool flag2 = (num.GetValueOrDefault() <= d & num != null) && extension.UsrCommodityType == "S";
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrMatrixStep>(e.Row, 0.5m, new PXSetPropertyException("ERP is taking Market price, Matrix Step cannot be zero!", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000205 RID: 517 RVA: 0x0000A3B4 File Offset: 0x000085B4
		protected virtual void _(Events.FieldVerifying<POVendorInventory, ASCJSMPOVendorInventoryExt.usrContractSurcharge> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = e.Row.GetExtension<ASCJSMPOVendorInventoryExt>();
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0.0m;
				bool flag2 = num.GetValueOrDefault() < d & num != null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrContractSurcharge>(e.Row, e.NewValue, new PXSetPropertyException("Surcharge can not be negative.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000206 RID: 518 RVA: 0x0000A434 File Offset: 0x00008634
		protected virtual void _(Events.FieldVerifying<POVendorInventory, ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = e.Row.GetExtension<ASCJSMPOVendorInventoryExt>();
				decimal? num = (decimal?)e.NewValue;
				decimal d = 0.0m;
				bool flag2 = num.GetValueOrDefault() < d & num != null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>(e.Row, e.NewValue, new PXSetPropertyException("Surcharge can not be negative.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000207 RID: 519 RVA: 0x0000A4B4 File Offset: 0x000086B4
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrIsOverrideVendor> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				this.UpdateItemAndPOVendorInventory(e.Cache, row, extension);
			}
		}

		// Token: 0x06000208 RID: 520 RVA: 0x0000A4EC File Offset: 0x000086EC
		protected virtual void _(Events.RowUpdated<POVendorInventory> e)
		{
			POVendorInventory row = e.Row;
			POVendorInventory oldRow = e.OldRow;
			bool flag = row == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				bool valueOrDefault = row.IsDefault.GetValueOrDefault();
				bool flag2 = oldRow != null && oldRow.IsDefault.GetValueOrDefault();
				bool flag3 = valueOrDefault != flag2;
				int? vendorID = row.VendorID;
				int? num = (oldRow != null) ? oldRow.VendorID : null;
				bool flag4 = !(vendorID.GetValueOrDefault() == num.GetValueOrDefault() & vendorID != null == (num != null));
				bool flag5 = (flag3 && valueOrDefault) || (valueOrDefault && flag4);
				if (flag5)
				{
					this.HandleDefaultVendorUpdate(row);
				}
				else
				{
					bool flag6 = flag3 && !valueOrDefault;
					if (flag6)
					{
						this.ClearDefaultVendorCountryOfOrigin();
					}
				}
			}
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0000A5D4 File Offset: 0x000087D4
		protected virtual void _(Events.RowDeleted<POVendorInventory> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				bool valueOrDefault = row.IsDefault.GetValueOrDefault();
				if (valueOrDefault)
				{
					this.ClearDefaultVendorCountryOfOrigin();
				}
			}
		}

		// Token: 0x0600020A RID: 522 RVA: 0x0000A61C File Offset: 0x0000881C
		protected virtual void _(Events.FieldUpdated<POVendorInventory, POVendorInventory.vendorID> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || e.NewValue == null || base.Base.IsCopyPasteContext;
			if (!flag)
			{
				Vendor vendor = Vendor.PK.Find(base.Base, (int?)e.NewValue, PKFindOptions.None);
				ASCJSMVendorExt ascjsmvendorExt = (vendor != null) ? vendor.GetExtension<ASCJSMVendorExt>() : null;
				e.Cache.SetValue<ASCJSMPOVendorInventoryExt.usrMarketID>(row, ascjsmvendorExt.UsrMarketID);
				PXGraph @base = base.Base;
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				int? commodityInventoryByMetalType = ASCJSMMetalType.GetCommodityInventoryByMetalType(@base, (ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, vendor.BAccountID, commodityInventoryByMetalType, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Today);
				bool flag2 = apvendorPrice == null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<POVendorInventory.vendorID>(row, e.NewValue, new PXSetPropertyException("Basis or Market price is empty, enter value or check Vendor Prices screen.", PXErrorLevel.Warning));
				}
				else
				{
					ASCJSMAPVendorPriceExt extension = apvendorPrice.GetExtension<ASCJSMAPVendorPriceExt>();
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrContractLossPct>(row, extension.UsrCommodityLossPct.GetValueOrDefault(0.0m));
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrMatrixStep>(row, extension.UsrMatrixStep.GetValueOrDefault(0.0m));
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrBasisValue>(row, extension.UsrBasisValue.GetValueOrDefault(0.0m));
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice>(row, apvendorPrice.SalesPrice.GetValueOrDefault(0.0m));
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrBasisPrice>(row, apvendorPrice.SalesPrice.GetValueOrDefault(0.0m));
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrContractSurcharge>(row, extension.UsrCommoditySurchargePct.GetValueOrDefault(0.001m));
					e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>(row, extension.UsrCommoditySurchargeAmount.GetValueOrDefault(0.0m));
					bool flag3 = row.IsDefault.GetValueOrDefault() && base.Base.Item.Current != null;
					if (flag3)
					{
						ASCJSMINInventoryItemExt extension2 = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
						extension2.UsrContractSurcharge = extension.UsrCommoditySurchargePct;
						extension2.UsrContractLossPct = extension.UsrCommodityLossPct;
						extension2.UsrMatrixStep = extension.UsrMatrixStep;
					}
				}
			}
		}

		// Token: 0x0600020B RID: 523 RVA: 0x0000A8B4 File Offset: 0x00008AB4
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrFabricationWeight> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.RecalculatePOVendorFabricationValue(row);
			}
		}

		// Token: 0x0600020C RID: 524 RVA: 0x0000A8DC File Offset: 0x00008ADC
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrFabricationPiece> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.RecalculatePOVendorFabricationValue(row);
			}
		}

		// Token: 0x0600020D RID: 525 RVA: 0x0000A904 File Offset: 0x00008B04
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				this.UpdateItemAndPOVendorInventory(e.Cache, row, extension);
			}
		}

		// Token: 0x0600020E RID: 526 RVA: 0x0000A93C File Offset: 0x00008B3C
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMINInventoryItemExt.usrContractIncrement> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || row.IsDefault.GetValueOrDefault();
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
				bool flag2 = ASCJSMMetalType.IsGold((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null);
				bool flag3 = flag2;
				if (flag3)
				{
					PXCache cache = e.Cache;
					object row2 = row;
					IASCJSMItemCostSpecDTO rowExt = extension;
					ASCJSMINJewelryItem ascjsminjewelryItem2 = this.JewelryItemView.Current;
					this.UpdateSurcharge<ASCJSMPOVendorInventoryExt.usrContractSurcharge>(cache, row2, rowExt, (ascjsminjewelryItem2 != null) ? ascjsminjewelryItem2.MetalType : null);
				}
				else
				{
					ASCJSMINJewelryItem ascjsminjewelryItem3 = this.JewelryItemView.Current;
					bool flag4 = ASCJSMMetalType.IsSilver((ascjsminjewelryItem3 != null) ? ascjsminjewelryItem3.MetalType : null);
					bool flag5 = flag4;
					if (flag5)
					{
						this.UpdateMetalCalcPOVendorItem(e.Cache, row, extension);
					}
				}
			}
		}

		// Token: 0x0600020F RID: 527 RVA: 0x0000AA00 File Offset: 0x00008C00
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrMatrixStep> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || row.IsDefault.GetValueOrDefault();
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
				this.UpdateMetalCalcPOVendorItem(e.Cache, row, extension);
			}
		}

		// Token: 0x06000210 RID: 528 RVA: 0x0000AA48 File Offset: 0x00008C48
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrContractSurcharge> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				bool flag2 = !row.IsDefault.GetValueOrDefault();
				if (flag2)
				{
					ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
					this.UpdateMetalCalcPOVendorItem(e.Cache, row, extension);
				}
			}
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0000AA98 File Offset: 0x00008C98
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				bool flag2 = !row.IsDefault.GetValueOrDefault();
				if (flag2)
				{
					ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
					this.UpdateMetalCalcPOVendorItem(e.Cache, row, extension);
				}
			}
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0000AAE8 File Offset: 0x00008CE8
		protected virtual void _(Events.FieldUpdated<ASCJSMINVendorDuty, ASCJSMINVendorDuty.vendorID> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				object value;
				e.Cache.RaiseFieldDefaulting<ASCJSMINVendorDuty.countryID>(e.Row, out value);
				e.Cache.SetValueExt<ASCJSMINVendorDuty.countryID>(e.Row, value);
			}
		}

		// Token: 0x06000213 RID: 531 RVA: 0x0000AB2C File Offset: 0x00008D2C
		protected void _(Events.RowInserting<INItemXRef> row)
		{
			bool flag = row.Row == null;
			if (!flag)
			{
				INItemXRef row2 = row.Row;
				ASCJSMINItemXRefExt extension = row2.GetExtension<ASCJSMINItemXRefExt>();
				extension.UsrCreationDate = new DateTime?(DateTime.Now);
			}
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0000AB68 File Offset: 0x00008D68
		protected virtual void SetVisibleJewelFields(PXCache cache, InventoryItem row, bool isVisible)
		{
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrUnitCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrEstLandedCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrFabricationCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrPreciousMetalCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrFreightCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrLaborCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrDutyCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrDutyCostPct>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrHandlingCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrPackagingCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrOtherMaterialsCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrPackagingLaborCost>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrContractLossPct>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrContractIncrement>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrBasisValue>(cache, row, isVisible);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrContractSurcharge>(cache, row, isVisible);
			ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
			bool isVisible2 = isVisible && extension.UsrCommodityType == "G";
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrActualGRAMGold>(cache, row, isVisible2);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrPricingGRAMGold>(cache, row, isVisible2);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrContractIncrement>(cache, row, isVisible2);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrIncrement>(cache, row, isVisible2);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrContractSurchargeAmount>(cache, row, isVisible2);
			bool isVisible3 = isVisible && extension.UsrCommodityType == "S";
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrActualGRAMSilver>(cache, row, isVisible3);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrPricingGRAMSilver>(cache, row, isVisible3);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrMatrixStep>(cache, row, isVisible3);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrMatrixPriceGram>(cache, row, isVisible3);
			PXUIFieldAttribute.SetVisible<ASCJSMINInventoryItemExt.usrMatrixPriceTOZ>(cache, row, isVisible3);
		}

		// Token: 0x06000215 RID: 533 RVA: 0x0000AC98 File Offset: 0x00008E98
		protected virtual bool IsVisibleFields(ASCJSMINInventoryItemExt rowExt, int? itemClassID)
		{
			INItemClass initemClass = INItemClass.PK.Find(base.Base, itemClassID, PKFindOptions.None);
			return ((initemClass != null) ? initemClass.ItemClassCD.Trim() : null) != "COMMODITY";
		}

		// Token: 0x06000216 RID: 534 RVA: 0x0000ACD4 File Offset: 0x00008ED4
		protected virtual void SetReadOnlyJewelAttrFields(PXCache cache, InventoryItem row, string metalType)
		{
			bool flag = !ASCJSMMetalType.IsGold(metalType);
			bool flag2 = !ASCJSMMetalType.IsSilver(metalType);
			PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrActualGRAMGold>(cache, row, flag);
			PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrPricingGRAMGold>(cache, row, flag);
			ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
			bool flag3 = !flag && extension != null && extension.UsrEnableVendorIncrement.GetValueOrDefault();
			PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrContractIncrement>(cache, row, !flag3);
			PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrActualGRAMSilver>(cache, row, flag2);
			PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrPricingGRAMSilver>(cache, row, flag2);
			PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrMatrixStep>(cache, row, flag2);
			bool flag4 = flag && flag2;
			if (flag4)
			{
				PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrPricingGRAMGold>(cache, row, true);
				PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrPricingGRAMSilver>(cache, row, true);
				PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrActualGRAMGold>(cache, row, true);
				PXUIFieldAttribute.SetReadOnly<ASCJSMINInventoryItemExt.usrActualGRAMSilver>(cache, row, true);
			}
		}

		// Token: 0x06000217 RID: 535 RVA: 0x0000AD84 File Offset: 0x00008F84
		protected virtual void SetReadOnlyPOVendorInventoryFields(PXCache cache, POVendorInventory row)
		{
			bool valueOrDefault = row.IsDefault.GetValueOrDefault();
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractIncrement>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractLossPct>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractSurcharge>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrOtherMaterialsCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrFabricationCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrPackagingCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrLaborCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrPackagingLaborCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrHandlingCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrFreightCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrDutyCost>(cache, row, valueOrDefault);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrMatrixStep>(cache, row, valueOrDefault);
			ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice>(cache, row, !extension.UsrIsOverrideVendor.GetValueOrDefault());
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000AE40 File Offset: 0x00009040
		protected virtual void SetVisiblePOVendorInventoryFields(PXCache cache)
		{
			bool flag = base.Base.Item.Current == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
				bool isVisible = extension.UsrCommodityType == "S";
				PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrFloor>(cache, null, isVisible);
				PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrCeiling>(cache, null, isVisible);
				PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrMatrixStep>(cache, null, isVisible);
				PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrCommodityID>(cache, null, false);
			}
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0000AEB4 File Offset: 0x000090B4
		protected virtual void UpdateCommodityCostMetal(PXCache cache, InventoryItem row, ASCJSMINInventoryItemExt rowExt)
		{
			bool flag = base.Base.IsContractBasedAPI || (row != null && row.KitItem.GetValueOrDefault());
			if (!flag)
			{
				bool flag2 = rowExt == null;
				if (flag2)
				{
					throw new PXException("Save Item first!");
				}
				ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(rowExt, null);
				bool flag3 = ascjsmcostBuilder == null;
				if (flag3)
				{
					rowExt.UsrPreciousMetalCost = new decimal?(0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrPreciousMetalCost>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMarketPriceTOZ>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMarketPriceGram>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMarketPriceAddOn>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrBasisValue>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrBasisValueAddOn>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrFloor>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrCeiling>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMatrixPriceTOZ>(row, 0m);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMatrixPriceGram>(row, 0m);
				}
				else
				{
					rowExt.UsrPreciousMetalCost = ascjsmcostBuilder.CalculatePreciousMetalCost(rowExt.UsrCostingType);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrPreciousMetalCost>(row, rowExt.UsrPreciousMetalCost);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMarketPriceTOZ>(row, ascjsmcostBuilder.PreciousMetalMarketCostPerTOZ);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMarketPriceGram>(row, ascjsmcostBuilder.PreciousMetalMarketCostPerGram);
					decimal? num = (ascjsmcostBuilder.PreciousMetalMarketCostPerTOZ + rowExt.UsrContractSurchargeAmount.GetValueOrDefault()) * (1 + rowExt.UsrContractSurcharge / 100);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrMarketPriceAddOn>(row, num);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrBasisValue>(row, ascjsmcostBuilder.BasisValue);
					decimal? basisValue = ascjsmcostBuilder.BasisValue;
					decimal? num2 = (basisValue + ((rowExt != null) ? rowExt.UsrContractSurchargeAmount : null).GetValueOrDefault()) * (1 + rowExt.UsrContractSurcharge / 100);
					cache.SetValueExt<ASCJSMINInventoryItemExt.usrBasisValueAddOn>(row, num2);
					this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrContractIncrement>(rowExt.UsrContractIncrement, null);
					this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrBasisValue>(rowExt.UsrBasisValue, null);
					this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrBasisPrice>(ascjsmcostBuilder.PreciousMetalContractCostPerTOZ, null);
					ASCJSMINJewelryItem injewelryItem = ascjsmcostBuilder.INJewelryItem;
					bool flag4 = ASCJSMMetalType.IsSilver((injewelryItem != null) ? injewelryItem.MetalType : null);
					if (flag4)
					{
						cache.SetValueExt<ASCJSMINInventoryItemExt.usrFloor>(row, ascjsmcostBuilder.Floor);
						cache.SetValueExt<ASCJSMINInventoryItemExt.usrCeiling>(row, ascjsmcostBuilder.Ceiling);
						cache.SetValueExt<ASCJSMINInventoryItemExt.usrMatrixPriceTOZ>(row, ascjsmcostBuilder.PreciousMetalAvrSilverMarketCostPerTOZ);
						cache.SetValueExt<ASCJSMINInventoryItemExt.usrMatrixPriceGram>(row, ascjsmcostBuilder.PreciousMetalAvrSilverMarketCostPerTOZ / ASCJSMConstants.TOZ2GRAM_31_10348.value);
					}
					this.UpdateCostsCurrentOverridenPOVendorItem(rowExt);
					this.VerifyLossAndSurcharge(cache, row, rowExt, ascjsmcostBuilder);
				}
			}
		}

		// Token: 0x0600021A RID: 538 RVA: 0x0000B324 File Offset: 0x00009524
		private decimal? CalculateFabricationValue(POVendorInventory poVendorInventory)
		{
			ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(poVendorInventory);
			decimal value = this.GetMetalWeight().GetValueOrDefault() * extension.UsrFabricationWeight.GetValueOrDefault(0.0m) + extension.UsrFabricationPiece.GetValueOrDefault(0.0m);
			return new decimal?(value);
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000B390 File Offset: 0x00009590
		private void RecalculateInventoryFabricationValue(InventoryItem inventoryItem)
		{
			POVendorInventory defaultVendor = this.GetDefaultVendor();
			bool flag = defaultVendor == null;
			if (!flag)
			{
				decimal? num = this.CalculateFabricationValue(defaultVendor);
				base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrFabricationCost>(inventoryItem, num);
			}
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000B3D4 File Offset: 0x000095D4
		private void RecalculatePOVendorFabricationValue(POVendorInventory poVendorInventory)
		{
			decimal? num = this.CalculateFabricationValue(poVendorInventory);
			bool valueOrDefault = poVendorInventory.IsDefault.GetValueOrDefault();
			if (valueOrDefault)
			{
				InventoryItem data = base.Base.Item.Current;
				base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrFabricationCost>(data, num);
			}
			else
			{
				this.SetValueExtPOVendorInventory<ASCJSMPOVendorInventoryExt.usrFabricationCost>(num, poVendorInventory);
			}
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0000B440 File Offset: 0x00009640
		private decimal? GetMetalWeight()
		{
			ASCJSMINInventoryItemExt extension = base.Base.Item.Current.GetExtension<ASCJSMINInventoryItemExt>();
			ASCJSMINJewelryItem ascjsminjewelryItem = this.JewelryItemView.Current;
			string text = (ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null;
			string text2 = text;
			string text3 = text2;
			decimal? result;
			if (!ASCJSMMetalType.IsGold(text3))
			{
				string metalType = text3;
				if (!ASCJSMMetalType.IsSilver(metalType))
				{
					result = new decimal?(0m);
				}
				else
				{
					result = extension.UsrActualGRAMSilver;
				}
			}
			else
			{
				result = extension.UsrActualGRAMGold;
			}
			return result;
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000B4C8 File Offset: 0x000096C8
		private void UpdateCostsCurrentOverridenPOVendorItem(ASCJSMINInventoryItemExt inventoryItemExt)
		{
			bool flag = base.Base.VendorItems.Current == null;
			if (flag)
			{
				base.Base.VendorItems.Current = this.GetDefaultVendor();
				bool flag2 = base.Base.VendorItems.Current == null;
				if (flag2)
				{
					return;
				}
			}
			bool flag3 = inventoryItemExt.UsrCostingType == "M";
			if (flag3)
			{
				this.UpdateMetalCalcPOVendorItem(base.Base.VendorItems.Cache, base.Base.VendorItems.Current, base.Base.VendorItems.Current.GetExtension<ASCJSMPOVendorInventoryExt>());
			}
			else
			{
				bool valueOrDefault = base.Base.VendorItems.Current.IsDefault.GetValueOrDefault();
				if (valueOrDefault)
				{
					base.Base.VendorItems.SetValueExt<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>(base.Base.VendorItems.Current, inventoryItemExt.UsrPreciousMetalCost);
					base.Base.VendorItems.SetValueExt<ASCJSMPOVendorInventoryExt.usrUnitCost>(base.Base.VendorItems.Current, inventoryItemExt.UsrUnitCost);
					base.Base.VendorItems.SetValueExt<ASCJSMPOVendorInventoryExt.usrFloor>(base.Base.VendorItems.Current, inventoryItemExt.UsrFloor);
					base.Base.VendorItems.SetValueExt<ASCJSMPOVendorInventoryExt.usrCeiling>(base.Base.VendorItems.Current, inventoryItemExt.UsrCeiling);
				}
			}
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000B654 File Offset: 0x00009854
		private void UpdateSurcharge<TField>(PXCache cache, object row, IASCJSMItemCostSpecDTO rowExt, string metalType) where TField : IBqlField
		{
			bool flag;
			if (rowExt.UsrActualGRAMSilver != null)
			{
				decimal? num = rowExt.UsrActualGRAMSilver;
				decimal d = 0.0m;
				if (!(num.GetValueOrDefault() == d & num != null))
				{
					flag = false;
					goto IL_79;
				}
			}
			if (rowExt.UsrActualGRAMGold != null)
			{
				decimal? num = rowExt.UsrActualGRAMGold;
				decimal d = 0.0m;
				flag = (num.GetValueOrDefault() == d & num != null);
			}
			else
			{
				flag = true;
			}
			IL_79:
			bool flag2 = flag;
			if (!flag2)
			{
				ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(rowExt, null);
				bool flag3 = ascjsmcostBuilder == null;
				if (flag3)
				{
				}
			}
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000B6F8 File Offset: 0x000098F8
		protected virtual void UpdateItemAndPOVendorInventory(PXCache cache, POVendorInventory row, ASCJSMPOVendorInventoryExt rowExt)
		{
			bool valueOrDefault = row.IsDefault.GetValueOrDefault();
			if (valueOrDefault)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
				this.UpdateCommodityCostMetal(base.Base.Item.Cache, base.Base.Item.Current, extension);
			}
			else
			{
				this.UpdateMetalCalcPOVendorItem(cache, row, rowExt);
			}
		}

		// Token: 0x06000221 RID: 545 RVA: 0x0000B768 File Offset: 0x00009968
		private void UpdateMetalCalcPOVendorItem(PXCache cache, POVendorInventory row, ASCJSMPOVendorInventoryExt rowExt)
		{
			bool flag = base.Base.Item.Current == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
				rowExt.UsrActualGRAMGold = extension.UsrActualGRAMGold;
				rowExt.UsrActualGRAMSilver = extension.UsrActualGRAMSilver;
				ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(rowExt, row);
				bool flag2 = ascjsmcostBuilder == null;
				if (!flag2)
				{
					rowExt.UsrPreciousMetalCost = ascjsmcostBuilder.CalculatePreciousMetalCost("C");
					cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>(row, rowExt.UsrPreciousMetalCost);
					rowExt.UsrContractIncrement = ascjsmcostBuilder.CalculateIncrementValue(rowExt);
					cache.SetValue<ASCJSMPOVendorInventoryExt.usrContractIncrement>(row, rowExt.UsrContractIncrement);
					ASCJSMINJewelryItem injewelryItem = ascjsmcostBuilder.INJewelryItem;
					bool flag3 = ASCJSMMetalType.IsSilver((injewelryItem != null) ? injewelryItem.MetalType : null);
					if (flag3)
					{
						cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrFloor>(row, ascjsmcostBuilder.Floor);
						cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrCeiling>(row, ascjsmcostBuilder.Ceiling);
						cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrBasisValue>(row, ascjsmcostBuilder.BasisValue);
					}
				}
			}
		}

		// Token: 0x06000222 RID: 546 RVA: 0x0000B878 File Offset: 0x00009A78
		private void SetValueExtPOVendorInventory<TField>(object newValue, POVendorInventory poVendorInventory = null) where TField : IBqlField
		{
			bool flag = poVendorInventory == null;
			if (flag)
			{
				poVendorInventory = this.GetDefaultVendor();
			}
			bool flag2 = poVendorInventory == null;
			if (!flag2)
			{
				base.Base.VendorItems.Cache.SetValueExt<TField>(poVendorInventory, newValue);
				base.Base.VendorItems.Cache.MarkUpdated(poVendorInventory);
			}
		}

		// Token: 0x06000223 RID: 547 RVA: 0x0000B8D4 File Offset: 0x00009AD4
		private void UpdateMetalGrams(string metalType)
		{
			bool flag = metalType == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
				bool flag2 = ASCJSMMetalType.IsGold(metalType) && ((extension != null) ? extension.UsrCommodityType : null) == "G";
				bool flag3 = ASCJSMMetalType.IsSilver(metalType) && ((extension != null) ? extension.UsrCommodityType : null) == "S";
				bool flag4 = flag2;
				if (flag4)
				{
					base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrActualGRAMSilver>(base.Base.Item.Current, 0m);
					base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrActualGRAMGold>(base.Base.Item.Current, PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current).UsrActualGRAMGold);
				}
				bool flag5 = flag3;
				if (flag5)
				{
					base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrActualGRAMGold>(base.Base.Item.Current, 0m);
					base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrActualGRAMSilver>(base.Base.Item.Current, PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current).UsrActualGRAMSilver);
				}
				base.Base.Item.Cache.SetValueExt<ASCJSMINInventoryItemExt.usrPreciousMetalCost>(base.Base.Item.Current, 0m);
			}
		}

		// Token: 0x06000224 RID: 548 RVA: 0x0000BA78 File Offset: 0x00009C78
		private ASCJSMCostBuilder CreateCostBuilder(IASCJSMItemCostSpecDTO currentRow, POVendorInventory defaultVendor = null)
		{
			bool flag;
			if (currentRow.UsrActualGRAMSilver != null)
			{
				decimal? num = currentRow.UsrActualGRAMSilver;
				decimal d = 0.0m;
				if (!(num.GetValueOrDefault() == d & num != null))
				{
					flag = false;
					goto IL_79;
				}
			}
			if (currentRow.UsrActualGRAMGold != null)
			{
				decimal? num = currentRow.UsrActualGRAMGold;
				decimal d = 0.0m;
				flag = (num.GetValueOrDefault() == d & num != null);
			}
			else
			{
				flag = true;
			}
			IL_79:
			bool flag2 = flag;
			ASCJSMCostBuilder result;
			if (flag2)
			{
				result = null;
			}
			else
			{
				bool flag3 = defaultVendor == null;
				if (flag3)
				{
					defaultVendor = this.GetDefaultVendor();
				}
				bool flag4 = defaultVendor == null;
				if (flag4)
				{
					result = null;
				}
				else
				{
					bool flag5 = this.JewelryItemView.Current == null;
					if (flag5)
					{
						this.JewelryItemView.Current = this.JewelryItemView.Select(Array.Empty<object>()).TopFirst;
					}
					bool flag6 = this.JewelryItemView.Current == null && base.Base.IsCopyPasteContext;
					if (flag6)
					{
						this.JewelryItemView.Current = this.JewelryItemView.Cache.Cached.RowCast<ASCJSMINJewelryItem>().FirstOrDefault<ASCJSMINJewelryItem>();
					}
					bool flag7 = this.JewelryItemView.Current == null;
					if (flag7)
					{
						result = null;
					}
					else
					{
						result = new ASCJSMCostBuilder(base.Base).WithInventoryItem(currentRow).WithPOVendorInventory(defaultVendor).WithJewelryAttrData(this.JewelryItemView.Current).WithPricingData(PXTimeZoneInfo.Today).Build();
					}
				}
			}
			return result;
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0000BBFC File Offset: 0x00009DFC
		private void CopyPOVendorInventoryToItem(POVendorInventory row)
		{
			ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
			ASCJSMINInventoryItemExt extension2 = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(base.Base.Item.Current);
			extension2.UsrBasisValue = extension.UsrBasisValue;
			InventoryItem inventoryItem = base.Base.Item.Current;
			bool flag = inventoryItem == null || !inventoryItem.KitItem.GetValueOrDefault();
			if (flag)
			{
				extension2.UsrFabricationCost = extension.UsrFabricationCost;
				extension2.UsrOtherMaterialsCost = extension.UsrOtherMaterialsCost;
				extension2.UsrPackagingCost = extension.UsrPackagingCost;
				extension2.UsrPackagingLaborCost = extension.UsrPackagingLaborCost;
				extension2.UsrLaborCost = extension.UsrLaborCost;
				extension2.UsrHandlingCost = extension.UsrHandlingCost;
				extension2.UsrFreightCost = extension.UsrFreightCost;
				extension2.UsrDutyCost = extension.UsrDutyCost;
				extension2.UsrContractSurcharge = extension.UsrContractSurcharge;
				extension2.UsrContractSurchargeAmount = extension.UsrContractSurchargeAmount;
				extension2.UsrContractLossPct = extension.UsrContractLossPct;
				extension2.UsrMatrixStep = extension.UsrMatrixStep;
			}
			base.Base.Item.UpdateCurrent();
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0000BD14 File Offset: 0x00009F14
		private void VerifyLossAndSurcharge(PXCache cache, InventoryItem row, ASCJSMINInventoryItemExt rowExt, ASCJSMCostBuilder costBuilder)
		{
			bool flag = costBuilder.APVendorPriceContract == null;
			if (!flag)
			{
				ASCJSMAPVendorPriceExt extension = PXCache<APVendorPrice>.GetExtension<ASCJSMAPVendorPriceExt>(costBuilder.APVendorPriceContract);
				decimal? num = rowExt.UsrContractLossPct;
				decimal? num2 = extension.UsrCommodityLossPct;
				bool flag2 = !(num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null));
				if (flag2)
				{
					cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractLossPct>(row, rowExt.UsrContractLossPct, new PXSetPropertyException("Vendor has another contracted value", PXErrorLevel.Warning));
				}
				else
				{
					cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractLossPct>(row, rowExt.UsrContractLossPct, null);
				}
				num2 = rowExt.UsrContractSurchargeAmount;
				num = extension.UsrCommoditySurchargeAmount;
				bool flag3 = !(num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null));
				if (flag3)
				{
					cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractSurchargeAmount>(row, rowExt.UsrContractSurchargeAmount, new PXSetPropertyException("Vendor has another contracted value", PXErrorLevel.Warning));
				}
				else
				{
					cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractSurchargeAmount>(row, rowExt.UsrContractSurchargeAmount, null);
				}
				num = rowExt.UsrContractSurcharge;
				num2 = extension.UsrCommoditySurchargePct;
				bool flag4 = !(num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null));
				if (flag4)
				{
					cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractSurcharge>(row, rowExt.UsrContractSurcharge, new PXSetPropertyException("Vendor has another contracted value", PXErrorLevel.Warning));
				}
				else
				{
					cache.RaiseExceptionHandling<ASCJSMINInventoryItemExt.usrContractSurcharge>(row, rowExt.UsrContractSurcharge, null);
				}
			}
		}

		// Token: 0x06000227 RID: 551 RVA: 0x0000BE9C File Offset: 0x0000A09C
		protected virtual void UpdateHeaderForERPValuation(PXCache cache, InventoryItem row)
		{
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(row);
				bool flag2 = extension.UsrCommodityType != "U" || extension.UsrCommodityType != "B" || extension.UsrCommodityType != "C" || extension.UsrCostingType != "S";
				if (!flag2)
				{
					INItemCost topFirst = PXSelectBase<INItemCost, PXViewOf<INItemCost>.BasedOn<SelectFromBase<INItemCost, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemCost.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
					{
						row.InventoryID
					}).TopFirst;
					bool flag3 = extension.UsrCommodityType == "U";
					if (flag3)
					{
						cache.SetValueExt<ASCJSMINInventoryItemExt.usrPackagingCost>(row, topFirst.AvgCost);
					}
				}
			}
		}

		// Token: 0x06000228 RID: 552 RVA: 0x0000BF64 File Offset: 0x0000A164
		private void SetStringList<TField>(PXCache cache, string attributeID) where TField : IBqlField
		{
			List<string> values = new List<string>();
			List<string> labels = new List<string>();
			this.SelectAttributeDetails(attributeID).ForEach(delegate(CSAttributeDetail x)
			{
				values.Add(x.ValueID);
				labels.Add(x.Description);
			});
			PXStringListAttribute.SetList<TField>(cache, null, values.ToArray(), labels.ToArray());
		}

		// Token: 0x06000229 RID: 553 RVA: 0x0000BFC5 File Offset: 0x0000A1C5
		private POVendorInventory GetDefaultVendor()
		{
			return base.Base.VendorItems.Select(Array.Empty<object>()).FirstTableItems.FirstOrDefault((POVendorInventory x) => x.IsDefault.GetValueOrDefault());
		}

		// Token: 0x0600022A RID: 554 RVA: 0x0000C005 File Offset: 0x0000A205
		private List<CSAttributeDetail> SelectAttributeDetails(string attributeID)
		{
			PXResultset<CSAttributeDetail> pxresultset = PXSelectBase<CSAttributeDetail, PXViewOf<CSAttributeDetail>.BasedOn<SelectFromBase<CSAttributeDetail, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSAttributeDetail.attributeID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
			{
				attributeID
			});
			return (pxresultset != null) ? pxresultset.FirstTableItems.ToList<CSAttributeDetail>() : null;
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0000C030 File Offset: 0x0000A230
		private void UpdateUsrIncrement(PXCache cache, InventoryItem row)
		{
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINInventoryItemExt extension = row.GetExtension<ASCJSMINInventoryItemExt>();
				bool flag2 = extension == null;
				if (!flag2)
				{
					decimal? num = null;
					bool flag3 = extension.UsrCommodityType == "G" && extension.UsrEnableVendorIncrement.GetValueOrDefault();
					decimal? num2;
					if (flag3)
					{
						num = extension.UsrContractIncrement;
					}
					else
					{
						bool flag4;
						if (extension.UsrCommodityType == "G" && extension.UsrPricingGRAMGold != null)
						{
							num2 = extension.UsrPricingGRAMGold;
							decimal d = 0m;
							flag4 = !(num2.GetValueOrDefault() == d & num2 != null);
						}
						else
						{
							flag4 = false;
						}
						bool flag5 = flag4;
						if (flag5)
						{
							num = 1m / ASCJSMConstants.TOZ2GRAM_31_10348.value * extension.UsrPricingGRAMGold * (1m + extension.UsrContractSurcharge.GetValueOrDefault() / 100m) * (1m + extension.UsrContractLossPct.GetValueOrDefault() / 100m);
						}
						else
						{
							bool flag6 = extension.UsrCommodityType == "S";
							if (flag6)
							{
								num = new decimal?(extension.UsrContractIncrement.GetValueOrDefault() * extension.UsrActualGRAMSilver.GetValueOrDefault());
							}
						}
					}
					num2 = extension.UsrIncrement;
					decimal? num3 = num;
					bool flag7 = !(num2.GetValueOrDefault() == num3.GetValueOrDefault() & num2 != null == (num3 != null));
					if (flag7)
					{
						cache.SetValueExt<ASCJSMINInventoryItemExt.usrIncrement>(row, num);
					}
				}
			}
		}

		// Token: 0x0600022C RID: 556 RVA: 0x0000C26C File Offset: 0x0000A46C
		protected virtual void HandleDefaultVendorUpdate(POVendorInventory row)
		{
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = row.GetExtension<ASCJSMPOVendorInventoryExt>();
				this.CopyPOVendorInventoryToItem(row);
				PXResult<Vendor> pxresult = PXSelectBase<Vendor, PXViewOf<Vendor>.BasedOn<SelectFromBase<Vendor, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<Address>.On<BqlOperand<Address.addressID, IBqlInt>.IsEqual<Vendor.defAddressID>>>>.Where<BqlOperand<Vendor.bAccountID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					row.VendorID
				}).FirstOrDefault<PXResult<Vendor>>();
				bool flag2 = pxresult != null;
				if (flag2)
				{
					Address item = pxresult.GetItem<Address>();
					bool flag3 = ((item != null) ? item.CountryID : null) != null;
					if (flag3)
					{
						InventoryItem inventoryItem = base.Base.Item.Current;
						bool flag4 = inventoryItem != null;
						if (flag4)
						{
							ASCJSMINInventoryItemExt extension2 = inventoryItem.GetExtension<ASCJSMINInventoryItemExt>();
							bool flag5 = extension2 != null;
							if (flag5)
							{
								extension2.UsrDefaultVendorCountryOfOrigin = item.CountryID;
								base.Base.Item.Update(inventoryItem);
							}
						}
					}
				}
				this.UpdateItemAndPOVendorInventory(base.Base.VendorItems.Cache, row, extension);
			}
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0000C358 File Offset: 0x0000A558
		protected virtual void ClearDefaultVendorCountryOfOrigin()
		{
			InventoryItem inventoryItem = base.Base.Item.Current;
			bool flag = inventoryItem != null;
			if (flag)
			{
				ASCJSMINInventoryItemExt extension = inventoryItem.GetExtension<ASCJSMINInventoryItemExt>();
				bool flag2 = extension != null;
				if (flag2)
				{
					extension.UsrDefaultVendorCountryOfOrigin = null;
					base.Base.Item.Update(inventoryItem);
				}
			}
		}

		// Token: 0x040000BF RID: 191
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			1,
			1,
			0,
			1,
			1,
			0
		})]
		public FbqlSelect<SelectFromBase<ASCJSMINJewelryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<ASCJSMINJewelryItem.inventoryID, IBqlInt>.IsEqual<BqlField<InventoryItem.inventoryID, IBqlInt>.FromCurrent>>, ASCJSMINJewelryItem>.View JewelryItemView;

		// Token: 0x040000C0 RID: 192
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			1,
			1,
			0,
			1,
			1,
			0
		})]
		public FbqlSelect<SelectFromBase<ASCJSMINVendorDuty, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<ASCJSMINVendorDuty.inventoryID, IBqlInt>.IsEqual<BqlField<InventoryItem.inventoryID, IBqlInt>.FromCurrent>>, ASCJSMINVendorDuty>.View VendorDutyView;

		// Token: 0x040000C1 RID: 193
		public PXAction<InventoryItem> UpdateMetalCost;
	}
}
