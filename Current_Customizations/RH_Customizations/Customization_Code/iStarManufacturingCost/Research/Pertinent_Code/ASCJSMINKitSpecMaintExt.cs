using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ASCJSMCustom.AP.CacheExt;
using ASCJSMCustom.Common.Builder;
using ASCJSMCustom.Common.Descriptor;
using ASCJSMCustom.Common.Helper;
using ASCJSMCustom.Common.Helper.Extensions;
using ASCJSMCustom.Common.Services.DataProvider.Interfaces;
using ASCJSMCustom.IN.CacheExt;
using ASCJSMCustom.IN.DAC;
using ASCJSMCustom.INKit.CacheExt;
using ASCJSMCustom.INKit.DAC;
using ASCJSMCustom.PO.CacheExt;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.SM;

namespace ASCJSMCustom.INKit
{
	// Token: 0x02000007 RID: 7
	public class ASCJSMINKitSpecMaintExt : PXGraphExtension<INKitSpecMaint>
	{
		// Token: 0x06000010 RID: 16 RVA: 0x00002188 File Offset: 0x00000388
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000011 RID: 17 RVA: 0x0000218B File Offset: 0x0000038B
		// (set) Token: 0x06000012 RID: 18 RVA: 0x00002193 File Offset: 0x00000393
		[InjectDependency]
		public IASCJSMInventoryItemDataProvider _itemDataProvider { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000013 RID: 19 RVA: 0x0000219C File Offset: 0x0000039C
		// (set) Token: 0x06000014 RID: 20 RVA: 0x000021A4 File Offset: 0x000003A4
		[InjectDependency]
		public IASCJSMVendorDataProvider _vendorDataProvider { get; set; }

		// Token: 0x06000015 RID: 21 RVA: 0x000021B0 File Offset: 0x000003B0
		public override void Initialize()
		{
			base.Initialize();
			INSetup insetup = this.ASCIStarINSetup.Current;
			bool flag = insetup != null;
			if (flag)
			{
				ASCJSMINSetupExt extension = PXCache<INSetup>.GetExtension<ASCJSMINSetupExt>(insetup);
				this.ASCIStarCreateProdItem.SetVisible((!extension.UsrIsActiveKitVersion).GetValueOrDefault());
				this.ASCIStarCreateProdItem.SetEnabled((!extension.UsrIsActiveKitVersion).GetValueOrDefault());
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002268 File Offset: 0x00000468
		[PXOverride]
		public void Persist(ASCJSMINKitSpecMaintExt.PersistDelegate baseMethod)
		{
			this.CopyFieldsValueToStockItem(base.Base.Hdr.Current);
			this.CopyFieldsValueToPOVendorInventory(base.Base.Hdr.Current);
			INSetup insetup = this.ASCIStarINSetup.Current;
			bool flag = insetup != null;
			if (flag)
			{
				ASCJSMINSetupExt extension = PXCache<INSetup>.GetExtension<ASCJSMINSetupExt>(insetup);
				bool? usrIsActiveKitVersion = extension.UsrIsActiveKitVersion;
				bool flag2 = false;
				bool flag3 = usrIsActiveKitVersion.GetValueOrDefault() == flag2 & usrIsActiveKitVersion != null;
				if (flag3)
				{
					this.CopyJewelryItemFieldsToStockItem(base.Base.Hdr.Current);
				}
			}
			baseMethod();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002303 File Offset: 0x00000503
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<INKitSpecHdr.allowCompAddition> e)
		{
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002306 File Offset: 0x00000506
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<INKitSpecStkDet.allowSubstitution> e)
		{
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002309 File Offset: 0x00000509
		[PXRemoveBaseAttribute(typeof(PXDBStringAttribute))]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBString(10, IsUnicode = true, IsKey = true, InputMask = ">##")]
		[PXDefault("01")]
		[PXUIField(DisplayName = "Variant")]
		protected void _(Events.CacheAttached<INKitSpecHdr.revisionID> cacheAttached)
		{
		}

		// Token: 0x0600001A RID: 26 RVA: 0x0000230C File Offset: 0x0000050C
		[PXRemoveBaseAttribute(typeof(PXUIFieldAttribute))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIField(DisplayName = "Default", Enabled = true)]
		protected virtual void _(Events.CacheAttached<POVendorInventory.isDefault> cacheAttached)
		{
		}

		// Token: 0x0600001B RID: 27 RVA: 0x0000230F File Offset: 0x0000050F
		[PXRemoveBaseAttribute(typeof(PXDBDefaultAttribute))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBDefault(typeof(INKitSpecHdr.kitInventoryID))]
		protected virtual void _(Events.CacheAttached<POVendorInventory.inventoryID> cacheAttached)
		{
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002314 File Offset: 0x00000514
		[PXUIField(DisplayName = "Send Email to Vendor", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void sendEmailToVendor()
		{
			bool flag = base.Base.Hdr.Current == null;
			if (!flag)
			{
				PXLongOperation.StartOperation(base.Base, delegate()
				{
					POVendorInventory defaultPOVendorInventory = this.GetDefaultPOVendorInventory();
					bool flag2 = defaultPOVendorInventory == null;
					if (flag2)
					{
						throw new PXException("To proceed, please add a default vendor or select one on the Vendors tab.");
					}
					this.SendEmailNotification(base.Base.Hdr.Current);
				});
			}
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002354 File Offset: 0x00000554
		[PXUIField(DisplayName = "Create Production Item", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable aSCIStarCreateProdItem(PXAdapter adapter)
		{
			return adapter.Get();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000236C File Offset: 0x0000056C
		[PXUIField(DisplayName = "Update Metal Cost", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true)]
		public virtual IEnumerable aSCIStarUpdateMetalCost(PXAdapter adapter)
		{
			INKitSpecHdr inkitSpecHdr = base.Base.Hdr.Current;
			bool flag = inkitSpecHdr != null;
			if (flag)
			{
				this.VendorItems.Select(Array.Empty<object>()).RowCast<POVendorInventory>().ForEach(delegate(POVendorInventory row)
				{
					bool flag2 = this.JewelryItemView.Current == null;
					if (flag2)
					{
						PXSelectBase<ASCJSMINKitSpecJewelryItem> jewelryItemView = this.JewelryItemView;
						PXResultset<ASCJSMINKitSpecJewelryItem> pxresultset = this.JewelryItemView.Select(Array.Empty<object>());
						jewelryItemView.Current = ((pxresultset != null) ? pxresultset.TopFirst : null);
					}
					ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem = this.JewelryItemView.Current;
					string metalType = (ascjsminkitSpecJewelryItem != null) ? ascjsminkitSpecJewelryItem.MetalType : null;
					bool flag3 = ASCJSMMetalType.IsGold(metalType);
					if (flag3)
					{
						InventoryItem inventoryItemByCD = this._itemDataProvider.GetInventoryItemByCD("24K");
						this.SetOrUpdatePreciousMetalCost(row, inventoryItemByCD, metalType);
					}
					else
					{
						bool flag4 = ASCJSMMetalType.IsSilver(metalType);
						if (flag4)
						{
							InventoryItem inventoryItemByCD2 = this._itemDataProvider.GetInventoryItemByCD("SSS");
							this.SetOrUpdatePreciousMetalCost(row, inventoryItemByCD2, metalType);
						}
					}
				});
				base.Base.Save.PressButton();
			}
			return adapter.Get();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000023D8 File Offset: 0x000005D8
		protected virtual void _(Events.RowInserted<INKitSpecHdr> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row == null || base.Base.Hdr.Current == null;
			if (!flag)
			{
				this.CopyJewelryItemFields(base.Base.Hdr.Current);
				this.CopyFieldsValueFromStockItem(base.Base.Hdr.Current);
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000243C File Offset: 0x0000063C
		protected virtual void _(Events.RowSelected<INKitSpecHdr> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				INSetup insetup = this.ASCIStarINSetup.Current;
				bool flag2 = insetup != null;
				if (flag2)
				{
					ASCJSMINSetupExt ascjsminsetupExt = (insetup != null) ? insetup.GetExtension<ASCJSMINSetupExt>() : null;
					PXUIFieldAttribute.SetVisible<INKitSpecHdr.revisionID>(base.Base.Hdr.Cache, base.Base.Hdr.Current, ascjsminsetupExt != null && ascjsminsetupExt.UsrIsActiveKitVersion.GetValueOrDefault());
				}
				this.SetVisibleItemWeightFields(e.Cache, row);
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x000024CC File Offset: 0x000006CC
		protected virtual void _(Events.FieldSelecting<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrBasisValue> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				POVendorInventory povendorInventory = this.VendorItems.Select(Array.Empty<object>()).RowCast<POVendorInventory>().FirstOrDefault((POVendorInventory _) => _.IsDefault.GetValueOrDefault());
				bool flag2 = povendorInventory != null;
				if (flag2)
				{
					decimal? num = new decimal?(0m);
					bool flag3 = this.JewelryItemView.Current == null;
					if (flag3)
					{
						PXSelectBase<ASCJSMINKitSpecJewelryItem> jewelryItemView = this.JewelryItemView;
						PXResultset<ASCJSMINKitSpecJewelryItem> pxresultset = this.JewelryItemView.Select(Array.Empty<object>());
						jewelryItemView.Current = ((pxresultset != null) ? pxresultset.TopFirst : null);
					}
					ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem = this.JewelryItemView.Current;
					string metalType = (ascjsminkitSpecJewelryItem != null) ? ascjsminkitSpecJewelryItem.MetalType : null;
					bool flag4 = ASCJSMMetalType.IsGold(metalType) || ASCJSMMetalType.IsSilver(metalType);
					if (flag4)
					{
						string inventoryCD = ASCJSMMetalType.IsGold(metalType) ? "24K" : "SSS";
						InventoryItem inventoryItemByCD = this._itemDataProvider.GetInventoryItemByCD(inventoryCD);
						bool flag5 = inventoryItemByCD != null;
						if (flag5)
						{
							APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, povendorInventory.VendorID, inventoryItemByCD.InventoryID, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Now);
							bool flag6 = apvendorPrice != null;
							if (flag6)
							{
								bool flag7 = ASCJSMMetalType.IsGold(metalType);
								if (flag7)
								{
									num = apvendorPrice.SalesPrice;
								}
								else
								{
									ASCJSMINKitSpecHdrExt extension = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(row);
									decimal? salesPrice = apvendorPrice.SalesPrice;
									decimal? num2 = apvendorPrice.SalesPrice + extension.UsrMatrixStep.GetValueOrDefault(0.5m);
									num = ((salesPrice != null & num2 != null) ? new decimal?((salesPrice.GetValueOrDefault() + num2.GetValueOrDefault()) / 2m) : null);
								}
							}
						}
					}
					e.ReturnValue = num;
				}
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000026F0 File Offset: 0x000008F0
		protected virtual void _(Events.FieldVerifying<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrBasisValue> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				bool flag2 = !this.IsBaseItemsExists();
				if (flag2)
				{
					ASCJSMINKitSpecHdrExt extension = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(row);
					e.Cache.RaiseExceptionHandling<ASCJSMINKitSpecHdrExt.usrBasisValue>(row, extension.UsrBasisValue, new PXSetPropertyException("System is missing the base items. Please ensure 'SSS' and '24K' items are created before proceeding.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002748 File Offset: 0x00000948
		protected virtual void _(Events.FieldUpdated<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrContractSurcharge> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.UpdateInKitStkComponents(row);
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002770 File Offset: 0x00000970
		protected virtual void _(Events.FieldUpdated<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrContractLossPct> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.UpdateInKitStkComponents(row);
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002798 File Offset: 0x00000998
		protected virtual void _(Events.FieldUpdated<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrUnitCost> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINKitSpecHdrExt extension = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(row);
				decimal? num = (decimal?)e.NewValue;
				ASCJSMINKitSpecHdrExt ascjsminkitSpecHdrExt = extension;
				decimal? usrDutyCostPct = extension.UsrDutyCostPct;
				decimal? num2 = num;
				ascjsminkitSpecHdrExt.UsrDutyCost = ((usrDutyCostPct != null & num2 != null) ? new decimal?(usrDutyCostPct.GetValueOrDefault() * num2.GetValueOrDefault() / 100.0m) : null);
			}
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002824 File Offset: 0x00000A24
		protected virtual void _(Events.FieldUpdated<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrDutyCost> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINKitSpecHdrExt extension = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(row);
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

		// Token: 0x06000027 RID: 39 RVA: 0x00002948 File Offset: 0x00000B48
		protected virtual void _(Events.FieldUpdated<INKitSpecHdr, ASCJSMINKitSpecHdrExt.usrDutyCostPct> e)
		{
			INKitSpecHdr row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINKitSpecHdrExt extension = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(row);
				decimal? num = extension.UsrUnitCost;
				decimal? num2 = (decimal?)e.NewValue;
				decimal? num3 = (num != null & num2 != null) ? new decimal?(num.GetValueOrDefault() * num2.GetValueOrDefault() / 100.00m) : null;
				num2 = num3;
				num = extension.UsrDutyCost;
				bool flag2 = num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null);
				if (!flag2)
				{
					e.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrDutyCost>(row, num3);
				}
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002A1C File Offset: 0x00000C1C
		protected void _(Events.RowInserted<INKitSpecStkDet> e)
		{
			INKitSpecStkDet row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.CalculateLineIncrement(e.Cache, row);
				this.SetLineBasisValue(e.Cache, row);
				this.UpdateHeaderFields();
				this.SortAndAddStkDetailLines();
				this.InsertComponentVendors(row);
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002A70 File Offset: 0x00000C70
		protected void _(Events.RowUpdated<INKitSpecStkDet> e)
		{
			INKitSpecStkDet row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				decimal? dfltCompQty = row.DfltCompQty;
				INKitSpecStkDet oldRow = e.OldRow;
				decimal? num = (oldRow != null) ? oldRow.DfltCompQty : null;
				bool flag2 = !(dfltCompQty.GetValueOrDefault() == num.GetValueOrDefault() & dfltCompQty != null == (num != null));
				if (flag2)
				{
					ASCJSMINKitSpecStkDetExt extension = row.GetExtension<ASCJSMINKitSpecStkDetExt>();
					this.UpdateUnitCostAndBasisPrice(e.Cache, row, extension);
				}
				this.UpdateHeaderFields();
				this.SortAndAddStkDetailLines();
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002B08 File Offset: 0x00000D08
		protected void _(Events.RowDeleted<INKitSpecStkDet> e)
		{
			INKitSpecStkDet row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.UpdateHeaderFields();
				this.SortAndAddStkDetailLines();
				this.RemoveOrphanedVendors(row);
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002B40 File Offset: 0x00000D40
		protected virtual void _(Events.FieldUpdated<INKitSpecStkDet, INKitSpecStkDet.compInventoryID> e)
		{
			INKitSpecStkDet row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.InsertComponentVendors(row);
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002B68 File Offset: 0x00000D68
		protected virtual void _(Events.FieldUpdated<INKitSpecStkDet, ASCJSMINKitSpecStkDetExt.usrCostingType> e)
		{
			INKitSpecStkDet row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				ASCJSMINKitSpecStkDetExt extension = PXCache<INKitSpecStkDet>.GetExtension<ASCJSMINKitSpecStkDetExt>(row);
				string text = (string)e.NewValue;
				bool flag2 = this.IsCommodityItem(row);
				if (flag2)
				{
					decimal? unitCostForCommodityItem = this.GetUnitCostForCommodityItem(row);
					e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrUnitCost>(row, unitCostForCommodityItem);
					e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrPreciousMetalCost>(row, unitCostForCommodityItem * row.DfltCompQty);
				}
				else
				{
					InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID(row.CompInventoryID);
					bool flag3 = inventoryItemByID == null;
					if (flag3)
					{
						return;
					}
					ASCJSMINInventoryItemExt extension2 = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(inventoryItemByID);
					ASCJSMINJewelryItem ascistarINJewelryItem = this.GetASCIStarINJewelryItem(row.CompInventoryID);
					bool flag4 = ascistarINJewelryItem == null || string.IsNullOrEmpty(ascistarINJewelryItem.MetalType);
					if (flag4)
					{
						return;
					}
					bool flag5 = !ASCJSMMetalType.IsGold(ascistarINJewelryItem.MetalType) && !ASCJSMMetalType.IsSilver(ascistarINJewelryItem.MetalType);
					if (flag5)
					{
						INItemCost initemCost = INItemCost.PK.Find(base.Base, row.CompInventoryID, base.Base.Accessinfo.BaseCuryID, PKFindOptions.None);
						e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrUnitCost>(row, ((initemCost != null) ? initemCost.AvgCost : null).GetValueOrDefault());
						e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrPreciousMetalCost>(row, ((initemCost != null) ? initemCost.AvgCost : null).GetValueOrDefault() * row.DfltCompQty);
						return;
					}
					POVendorInventory itemVendor = this.GetItemVendor(row);
					bool flag6 = itemVendor == null;
					if (flag6)
					{
						e.Cache.RaiseExceptionHandling<ASCJSMINKitSpecStkDetExt.usrCostingType>(row, text, new PXSetPropertyException("To proceed, please add a default vendor or select one on the Vendors tab.", PXErrorLevel.Warning));
						return;
					}
					decimal? num = new decimal?(0m);
					bool flag7 = text == "S";
					if (flag7)
					{
						INItemCost initemCost2 = INItemCost.PK.Find(base.Base, row.CompInventoryID, base.Base.Accessinfo.BaseCuryID, PKFindOptions.None);
						e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrUnitCost>(row, ((initemCost2 != null) ? initemCost2.AvgCost : null).GetValueOrDefault());
						num = ((initemCost2 != null) ? initemCost2.AvgCost : null).GetValueOrDefault() * row.DfltCompQty;
					}
					else
					{
						bool flag8 = text == "M" || text == "C";
						if (flag8)
						{
							ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(extension, row);
							bool flag9 = ascjsmcostBuilder != null;
							if (flag9)
							{
								decimal? num2 = ascjsmcostBuilder.CalculatePreciousMetalCost(text);
								decimal? dfltCompQty = row.DfltCompQty;
								decimal d = 0m;
								decimal? num3 = (dfltCompQty.GetValueOrDefault() == d & dfltCompQty != null) ? num2 : (num2 / row.DfltCompQty);
								e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrUnitCost>(row, num3);
								num = num2;
								decimal? num4 = (text == "M") ? ascjsmcostBuilder.PreciousMetalMarketCostPerTOZ : ascjsmcostBuilder.PreciousMetalContractCostPerTOZ;
								e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrSalesPrice>(row, num4);
								e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBasisPrice>(row, ascjsmcostBuilder.PreciousMetalContractCostPerTOZ);
								e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBasisValue>(row, ascjsmcostBuilder.BasisValue);
							}
						}
					}
					e.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrPreciousMetalCost>(row, num);
				}
				this.UpdateHeaderFields();
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002FBC File Offset: 0x000011BC
		protected virtual void _(Events.FieldDefaulting<INKitSpecNonStkDet, ASCJSMINKitSpecNonStkDetExt.usrUnitCost> e)
		{
			INKitSpecNonStkDet row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				InventoryItemCurySettings inventoryItemCurySettings = InventoryItemCurySettings.PK.Find(base.Base, row.CompInventoryID, base.Base.Accessinfo.BaseCuryID, PKFindOptions.None);
				e.NewValue = ((inventoryItemCurySettings != null) ? inventoryItemCurySettings.StdCost : null).GetValueOrDefault();
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00003028 File Offset: 0x00001228
		protected virtual void _(Events.RowPersisting<INKitSpecNonStkDet> e)
		{
			INKitSpecNonStkDet row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				ASCJSMINKitSpecNonStkDetExt extension = PXCache<INKitSpecNonStkDet>.GetExtension<ASCJSMINKitSpecNonStkDetExt>(row);
				bool flag2 = extension.UsrCostRollupType == null;
				if (flag2)
				{
					e.Cache.RaiseExceptionHandling<ASCJSMINKitSpecNonStkDetExt.usrCostRollupType>(row, extension.UsrCostRollupType, new PXSetPropertyException("Cost Rollup Type is not set. Please select Rollup Type before saving.", PXErrorLevel.Error));
					e.Cancel = true;
					throw new PXException("Cost Rollup Type is not set. Please select Rollup Type before saving.");
				}
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003090 File Offset: 0x00001290
		protected virtual void _(Events.RowSelected<POVendorInventory> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.SetReadOnlyPOVendorInventoryFields(e.Cache, row);
				this.SetVisiblePOVendorInventoryFields(e.Cache, row);
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000030CC File Offset: 0x000012CC
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
					e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrMarketID>(e.Row, false, new PXSetPropertyException("Market can not be empty!", PXErrorLevel.RowError));
				}
				PXGraph @base = base.Base;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem = this.JewelryItemView.Current;
				int? commodityInventoryByMetalType = ASCJSMMetalType.GetCommodityInventoryByMetalType(@base, (ascjsminkitSpecJewelryItem != null) ? ascjsminkitSpecJewelryItem.MetalType : null);
				bool flag3 = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, row.VendorID, commodityInventoryByMetalType, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Today) == null && !PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row).UsrIsOverrideVendor.GetValueOrDefault();
				if (flag3)
				{
					e.Cache.RaiseExceptionHandling<POVendorInventory.isDefault>(row, false, new PXSetPropertyException("Vendor price record not found, check Vendor Prices screen.", PXErrorLevel.RowWarning));
				}
				PXResultset<POVendorInventory> pxresultset = this.VendorItems.Select(Array.Empty<object>());
				List<POVendorInventory> list = (pxresultset != null) ? pxresultset.FirstTableItems.ToList<POVendorInventory>() : null;
				foreach (POVendorInventory povendorInventory in list)
				{
					bool flag4 = povendorInventory.IsDefault.GetValueOrDefault() && povendorInventory != row;
					if (flag4)
					{
						this.VendorItems.Cache.SetValue<POVendorInventory.isDefault>(povendorInventory, false);
						this.VendorItems.View.RequestRefresh();
						break;
					}
				}
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x0000327C File Offset: 0x0000147C
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

		// Token: 0x06000032 RID: 50 RVA: 0x000032EC File Offset: 0x000014EC
		protected virtual void _(Events.FieldUpdated<POVendorInventory, ASCJSMPOVendorInventoryExt.usrCommodityID> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row == null || !e.ExternalCall;
			if (!flag)
			{
				ASCJSMPOVendorInventoryExt extension = row.GetExtension<ASCJSMPOVendorInventoryExt>();
				int? inventoryID = (int?)e.NewValue;
				APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, (extension != null) ? extension.UsrMarketID : null, inventoryID, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Today);
				ASCJSMAPVendorPriceExt extension2 = PXCache<APVendorPrice>.GetExtension<ASCJSMAPVendorPriceExt>(apvendorPrice);
				e.Cache.SetValueExt<ASCJSMPOVendorInventoryExt.usrBasisValue>(row, extension2.UsrBasisValue);
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003378 File Offset: 0x00001578
		protected virtual void _(Events.RowPersisting<POVendorInventory> e)
		{
			POVendorInventory row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				bool valueOrDefault = e.Row.IsDefault.GetValueOrDefault();
				if (valueOrDefault)
				{
					ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(e.Row);
					bool flag2 = extension.UsrMarketID == null;
					if (flag2)
					{
						e.Cache.RaiseExceptionHandling<ASCJSMPOVendorInventoryExt.usrMarketID>(row, row.IsDefault, new PXSetPropertyException("Market field cannot be empty", PXErrorLevel.Error));
						e.Cancel = true;
						throw new PXException("Market field cannot be empty");
					}
				}
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003410 File Offset: 0x00001610
		protected virtual void _(Events.RowUpdated<POVendorInventory> e)
		{
			bool flag = e.OldRow == null || e.Row == null || e.Row.VendorID == null;
			if (!flag)
			{
				IEnumerable<InventoryItemCurySettings> enumerable = this.ASCIStarAllItemCurySettings.Select(new object[]
				{
					e.Row.InventoryID
				}).RowCast<InventoryItemCurySettings>();
				Vendor vendor = Vendor.PK.Find(base.Base, e.Row.VendorID, PKFindOptions.None);
				bool flag2 = false;
				foreach (InventoryItemCurySettings inventoryItemCurySettings in enumerable)
				{
					Vendor vendor2 = Vendor.PK.Find(base.Base, inventoryItemCurySettings.PreferredVendorID, PKFindOptions.None);
					bool flag3 = vendor.BaseCuryID == null || string.Equals(inventoryItemCurySettings.CuryID, vendor.BaseCuryID, StringComparison.OrdinalIgnoreCase);
					if (flag3)
					{
						if (!e.Row.IsDefault.GetValueOrDefault())
						{
							goto IL_15E;
						}
						int? num = inventoryItemCurySettings.PreferredVendorID;
						int? num2 = e.Row.VendorID;
						if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
						{
							num2 = inventoryItemCurySettings.PreferredVendorLocationID;
							num = e.Row.VendorLocationID;
							if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
							{
								goto IL_15E;
							}
						}
						bool flag4 = true;
						IL_1EA:
						bool flag5 = flag4;
						bool flag6 = flag5;
						if (flag6)
						{
							inventoryItemCurySettings.PreferredVendorID = (e.Row.IsDefault.GetValueOrDefault() ? e.Row.VendorID : null);
							inventoryItemCurySettings.PreferredVendorLocationID = (e.Row.IsDefault.GetValueOrDefault() ? e.Row.VendorLocationID : null);
							this.ASCIStarItemCurySettings.Update(inventoryItemCurySettings);
							flag2 = true;
						}
						continue;
						IL_15E:
						if (e.Row.IsDefault.GetValueOrDefault())
						{
							goto IL_1E6;
						}
						num = inventoryItemCurySettings.PreferredVendorID;
						num2 = e.Row.VendorID;
						if (!(num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null)))
						{
							goto IL_1E6;
						}
						num2 = inventoryItemCurySettings.PreferredVendorLocationID;
						num = e.Row.VendorLocationID;
						flag4 = (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null));
						IL_1E7:
						goto IL_1EA;
						IL_1E6:
						flag4 = false;
						goto IL_1E7;
					}
					bool flag7 = vendor2 != null && vendor2.BaseCuryID == null;
					if (flag7)
					{
						inventoryItemCurySettings.PreferredVendorID = null;
						inventoryItemCurySettings.PreferredVendorLocationID = null;
						this.ASCIStarItemCurySettings.Update(inventoryItemCurySettings);
					}
				}
				bool flag8 = !e.Row.IsDefault.GetValueOrDefault() || !flag2;
				if (!flag8)
				{
					foreach (POVendorInventory povendorInventory in this.VendorItems.Select(Array.Empty<object>()).RowCast<POVendorInventory>())
					{
						int? num = povendorInventory.RecordID;
						int? num2 = e.Row.RecordID;
						bool flag9 = !(num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null)) && povendorInventory.IsDefault.GetValueOrDefault();
						if (flag9)
						{
							this.VendorItems.Cache.SetValue<POVendorInventory.isDefault>(povendorInventory, false);
						}
					}
					this.VendorItems.Cache.ClearQueryCacheObsolete();
					this.VendorItems.View.RequestRefresh();
					ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(e.Row);
					this.SetBasisValueOnStockComp(extension);
				}
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003838 File Offset: 0x00001A38
		protected virtual void SetReadOnlyPOVendorInventoryFields(PXCache cache, POVendorInventory row)
		{
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractIncrement>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractLossPct>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractSurcharge>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrOtherMaterialsCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrFabricationCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrPackagingCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrLaborCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrPackagingLaborCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrHandlingCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrFreightCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrDutyCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrUnitCost>(cache, row, true);
			PXUIFieldAttribute.SetReadOnly<ASCJSMPOVendorInventoryExt.usrMatrixStep>(cache, row, true);
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000038D0 File Offset: 0x00001AD0
		protected virtual void SetVisiblePOVendorInventoryFields(PXCache cache, POVendorInventory row)
		{
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrContractIncrement>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrContractLossPct>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrContractSurcharge>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrOtherMaterialsCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrFabricationCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrPackagingCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrLaborCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrPackagingLaborCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrHandlingCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrFreightCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrDutyCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrUnitCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrMatrixStep>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrEstLandedCost>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrCeiling>(cache, null, false);
			PXUIFieldAttribute.SetVisible<ASCJSMPOVendorInventoryExt.usrFloor>(cache, null, false);
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003980 File Offset: 0x00001B80
		protected virtual void SetVisibleItemWeightFields(PXCache cache, INKitSpecHdr row)
		{
			bool flag = this.JewelryItemView.Current == null;
			if (flag)
			{
				PXSelectBase<ASCJSMINKitSpecJewelryItem> jewelryItemView = this.JewelryItemView;
				PXResultset<ASCJSMINKitSpecJewelryItem> pxresultset = this.JewelryItemView.Select(Array.Empty<object>());
				jewelryItemView.Current = ((pxresultset != null) ? pxresultset.TopFirst : null);
			}
			ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem = this.JewelryItemView.Current;
			string mixedTypeValue = ASCJSMMetalType.GetMixedTypeValue((ascjsminkitSpecJewelryItem != null) ? ascjsminkitSpecJewelryItem.MetalType : null);
			bool flag2;
			if (!(mixedTypeValue == "MD"))
			{
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem2 = this.JewelryItemView.Current;
				flag2 = (((ascjsminkitSpecJewelryItem2 != null) ? ascjsminkitSpecJewelryItem2.MetalType : null) == null);
			}
			else
			{
				flag2 = true;
			}
			bool flag3 = flag2;
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrActualGRAMSilverRight>(cache, row, flag3);
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrPricingGRAMSilverRight>(cache, row, flag3);
			bool flag4;
			if (!(mixedTypeValue == "MG"))
			{
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem3 = this.JewelryItemView.Current;
				flag4 = ASCJSMMetalType.IsGold((ascjsminkitSpecJewelryItem3 != null) ? ascjsminkitSpecJewelryItem3.MetalType : null);
			}
			else
			{
				flag4 = true;
			}
			bool flag5 = flag4;
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrActualGRAMGold>(cache, row, flag3 || flag5);
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrPricingGRAMGold>(cache, row, flag3 || flag5);
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrContractSurcharge>(cache, row, flag3 || flag5);
			ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem4 = this.JewelryItemView.Current;
			bool flag6 = ASCJSMMetalType.IsSilver((ascjsminkitSpecJewelryItem4 != null) ? ascjsminkitSpecJewelryItem4.MetalType : null);
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrActualGRAMSilver>(cache, row, flag6);
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrPricingGRAMSilver>(cache, row, flag6);
			PXUIFieldAttribute.SetVisible<ASCJSMINKitSpecHdrExt.usrMatrixStep>(cache, row, flag3 || flag6);
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003AAC File Offset: 0x00001CAC
		protected virtual void CopyJewelryItemFields(INKitSpecHdr kitSpecHdr)
		{
			PXResultset<ASCJSMINJewelryItem> pxresultset = PXSelectBase<ASCJSMINJewelryItem, PXViewOf<ASCJSMINJewelryItem>.BasedOn<SelectFromBase<ASCJSMINJewelryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<ASCJSMINJewelryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				(kitSpecHdr != null) ? kitSpecHdr.KitInventoryID : null
			});
			ASCJSMINJewelryItem ascjsminjewelryItem = (pxresultset != null) ? pxresultset.TopFirst : null;
			bool flag = ascjsminjewelryItem == null;
			if (!flag)
			{
				ASCJSMINKitSpecJewelryItem item = new ASCJSMINKitSpecJewelryItem
				{
					KitInventoryID = kitSpecHdr.KitInventoryID,
					RevisionID = kitSpecHdr.RevisionID,
					ShortDesc = ascjsminjewelryItem.ShortDesc,
					LongDesc = ascjsminjewelryItem.LongDesc,
					StyleStatus = ascjsminjewelryItem.StyleStatus,
					CustomerCode = ascjsminjewelryItem.CustomerCode,
					InvCategory = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.InvCategory : null),
					ItemType = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.ItemType : null),
					ItemSubType = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.ItemSubType : null),
					Collection = ascjsminjewelryItem.Collection,
					MetalType = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalType : null),
					MetalNote = ascjsminjewelryItem.MetalNote,
					MetalColor = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.MetalColor : null),
					Plating = ascjsminjewelryItem.Plating,
					Finishes = ascjsminjewelryItem.Finishes,
					VendorMaker = ascjsminjewelryItem.VendorMaker,
					OrgCountry = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.OrgCountry : null),
					StoneType = ascjsminjewelryItem.StoneType,
					WebNotesComment = ascjsminjewelryItem.WebNotesComment,
					StoneComment = ascjsminjewelryItem.StoneComment,
					StoneColor = ((ascjsminjewelryItem != null) ? ascjsminjewelryItem.StoneColor : null),
					StoneShape = ascjsminjewelryItem.StoneShape,
					StoneCreation = ascjsminjewelryItem.StoneCreation,
					GemstoneTreatment = ascjsminjewelryItem.GemstoneTreatment,
					SettingType = ascjsminjewelryItem.SettingType,
					Findings = ascjsminjewelryItem.Findings,
					FindingsSubType = ascjsminjewelryItem.FindingsSubType,
					ChainType = ascjsminjewelryItem.ChainType,
					RingLength = ascjsminjewelryItem.RingLength,
					RingSize = ascjsminjewelryItem.RingSize,
					OD = ascjsminjewelryItem.OD
				};
				this.JewelryItemView.Insert(item);
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003CD4 File Offset: 0x00001ED4
		protected virtual void CopyJewelryItemFieldsToStockItem(INKitSpecHdr kitSpecHdr)
		{
			PXResultset<ASCJSMINJewelryItem> pxresultset = PXSelectBase<ASCJSMINJewelryItem, PXViewOf<ASCJSMINJewelryItem>.BasedOn<SelectFromBase<ASCJSMINJewelryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<ASCJSMINJewelryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				(kitSpecHdr != null) ? kitSpecHdr.KitInventoryID : null
			});
			ASCJSMINJewelryItem ascjsminjewelryItem = (pxresultset != null) ? pxresultset.TopFirst : null;
			bool flag = ascjsminjewelryItem == null;
			if (!flag)
			{
				ASCJSMINJewelryItem ascjsminjewelryItem2 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem = this.JewelryItemView.Current;
				ascjsminjewelryItem2.ShortDesc = ((ascjsminkitSpecJewelryItem != null) ? ascjsminkitSpecJewelryItem.ShortDesc : null);
				ASCJSMINJewelryItem ascjsminjewelryItem3 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem2 = this.JewelryItemView.Current;
				ascjsminjewelryItem3.LongDesc = ((ascjsminkitSpecJewelryItem2 != null) ? ascjsminkitSpecJewelryItem2.LongDesc : null);
				ASCJSMINJewelryItem ascjsminjewelryItem4 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem3 = this.JewelryItemView.Current;
				ascjsminjewelryItem4.StyleStatus = ((ascjsminkitSpecJewelryItem3 != null) ? ascjsminkitSpecJewelryItem3.StyleStatus : null);
				ASCJSMINJewelryItem ascjsminjewelryItem5 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem4 = this.JewelryItemView.Current;
				ascjsminjewelryItem5.CustomerCode = ((ascjsminkitSpecJewelryItem4 != null) ? ascjsminkitSpecJewelryItem4.CustomerCode : null);
				ASCJSMINJewelryItem ascjsminjewelryItem6 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem5 = this.JewelryItemView.Current;
				ascjsminjewelryItem6.InvCategory = ((ascjsminkitSpecJewelryItem5 != null) ? ascjsminkitSpecJewelryItem5.InvCategory : null);
				ASCJSMINJewelryItem ascjsminjewelryItem7 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem6 = this.JewelryItemView.Current;
				ascjsminjewelryItem7.ItemType = ((ascjsminkitSpecJewelryItem6 != null) ? ascjsminkitSpecJewelryItem6.ItemType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem8 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem7 = this.JewelryItemView.Current;
				ascjsminjewelryItem8.ItemSubType = ((ascjsminkitSpecJewelryItem7 != null) ? ascjsminkitSpecJewelryItem7.ItemSubType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem9 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem8 = this.JewelryItemView.Current;
				ascjsminjewelryItem9.Collection = ((ascjsminkitSpecJewelryItem8 != null) ? ascjsminkitSpecJewelryItem8.Collection : null);
				ASCJSMINJewelryItem ascjsminjewelryItem10 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem9 = this.JewelryItemView.Current;
				ascjsminjewelryItem10.MetalType = ((ascjsminkitSpecJewelryItem9 != null) ? ascjsminkitSpecJewelryItem9.MetalType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem11 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem10 = this.JewelryItemView.Current;
				ascjsminjewelryItem11.MetalNote = ((ascjsminkitSpecJewelryItem10 != null) ? ascjsminkitSpecJewelryItem10.MetalNote : null);
				ASCJSMINJewelryItem ascjsminjewelryItem12 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem11 = this.JewelryItemView.Current;
				ascjsminjewelryItem12.MetalColor = ((ascjsminkitSpecJewelryItem11 != null) ? ascjsminkitSpecJewelryItem11.MetalColor : null);
				ASCJSMINJewelryItem ascjsminjewelryItem13 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem12 = this.JewelryItemView.Current;
				ascjsminjewelryItem13.Plating = ((ascjsminkitSpecJewelryItem12 != null) ? ascjsminkitSpecJewelryItem12.Plating : null);
				ASCJSMINJewelryItem ascjsminjewelryItem14 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem13 = this.JewelryItemView.Current;
				ascjsminjewelryItem14.Finishes = ((ascjsminkitSpecJewelryItem13 != null) ? ascjsminkitSpecJewelryItem13.Finishes : null);
				ASCJSMINJewelryItem ascjsminjewelryItem15 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem14 = this.JewelryItemView.Current;
				ascjsminjewelryItem15.VendorMaker = ((ascjsminkitSpecJewelryItem14 != null) ? ascjsminkitSpecJewelryItem14.VendorMaker : null);
				ASCJSMINJewelryItem ascjsminjewelryItem16 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem15 = this.JewelryItemView.Current;
				ascjsminjewelryItem16.OrgCountry = ((ascjsminkitSpecJewelryItem15 != null) ? ascjsminkitSpecJewelryItem15.OrgCountry : null);
				ASCJSMINJewelryItem ascjsminjewelryItem17 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem16 = this.JewelryItemView.Current;
				ascjsminjewelryItem17.StoneType = ((ascjsminkitSpecJewelryItem16 != null) ? ascjsminkitSpecJewelryItem16.StoneType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem18 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem17 = this.JewelryItemView.Current;
				ascjsminjewelryItem18.WebNotesComment = ((ascjsminkitSpecJewelryItem17 != null) ? ascjsminkitSpecJewelryItem17.WebNotesComment : null);
				ASCJSMINJewelryItem ascjsminjewelryItem19 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem18 = this.JewelryItemView.Current;
				ascjsminjewelryItem19.StoneComment = ((ascjsminkitSpecJewelryItem18 != null) ? ascjsminkitSpecJewelryItem18.StoneComment : null);
				ASCJSMINJewelryItem ascjsminjewelryItem20 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem19 = this.JewelryItemView.Current;
				ascjsminjewelryItem20.StoneColor = ((ascjsminkitSpecJewelryItem19 != null) ? ascjsminkitSpecJewelryItem19.StoneColor : null);
				ASCJSMINJewelryItem ascjsminjewelryItem21 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem20 = this.JewelryItemView.Current;
				ascjsminjewelryItem21.StoneShape = ((ascjsminkitSpecJewelryItem20 != null) ? ascjsminkitSpecJewelryItem20.StoneShape : null);
				ASCJSMINJewelryItem ascjsminjewelryItem22 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem21 = this.JewelryItemView.Current;
				ascjsminjewelryItem22.StoneCreation = ((ascjsminkitSpecJewelryItem21 != null) ? ascjsminkitSpecJewelryItem21.StoneCreation : null);
				ASCJSMINJewelryItem ascjsminjewelryItem23 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem22 = this.JewelryItemView.Current;
				ascjsminjewelryItem23.GemstoneTreatment = ((ascjsminkitSpecJewelryItem22 != null) ? ascjsminkitSpecJewelryItem22.GemstoneTreatment : null);
				ASCJSMINJewelryItem ascjsminjewelryItem24 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem23 = this.JewelryItemView.Current;
				ascjsminjewelryItem24.SettingType = ((ascjsminkitSpecJewelryItem23 != null) ? ascjsminkitSpecJewelryItem23.SettingType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem25 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem24 = this.JewelryItemView.Current;
				ascjsminjewelryItem25.Findings = ((ascjsminkitSpecJewelryItem24 != null) ? ascjsminkitSpecJewelryItem24.Findings : null);
				ASCJSMINJewelryItem ascjsminjewelryItem26 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem25 = this.JewelryItemView.Current;
				ascjsminjewelryItem26.FindingsSubType = ((ascjsminkitSpecJewelryItem25 != null) ? ascjsminkitSpecJewelryItem25.FindingsSubType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem27 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem26 = this.JewelryItemView.Current;
				ascjsminjewelryItem27.ChainType = ((ascjsminkitSpecJewelryItem26 != null) ? ascjsminkitSpecJewelryItem26.ChainType : null);
				ASCJSMINJewelryItem ascjsminjewelryItem28 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem27 = this.JewelryItemView.Current;
				ascjsminjewelryItem28.RingLength = ((ascjsminkitSpecJewelryItem27 != null) ? ascjsminkitSpecJewelryItem27.RingLength : null);
				ASCJSMINJewelryItem ascjsminjewelryItem29 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem28 = this.JewelryItemView.Current;
				ascjsminjewelryItem29.RingSize = ((ascjsminkitSpecJewelryItem28 != null) ? ascjsminkitSpecJewelryItem28.RingSize : null);
				ASCJSMINJewelryItem ascjsminjewelryItem30 = ascjsminjewelryItem;
				ASCJSMINKitSpecJewelryItem ascjsminkitSpecJewelryItem29 = this.JewelryItemView.Current;
				ascjsminjewelryItem30.OD = ((ascjsminkitSpecJewelryItem29 != null) ? ascjsminkitSpecJewelryItem29.OD : null);
				this.ASCIStarJewelryItem.Update(ascjsminjewelryItem);
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x0000409C File Offset: 0x0000229C
		protected virtual void CopyFieldsValueFromStockItem(INKitSpecHdr kitSpecHdr)
		{
			InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID((kitSpecHdr != null) ? kitSpecHdr.KitInventoryID : null);
			bool flag = inventoryItemByID != null && kitSpecHdr != null;
			if (flag)
			{
				kitSpecHdr.Descr = inventoryItemByID.Descr;
				base.Base.Hdr.Update(kitSpecHdr);
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000040FC File Offset: 0x000022FC
		protected virtual void CopyFieldsValueToStockItem(INKitSpecHdr kitSpecHdr)
		{
			InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID((kitSpecHdr != null) ? kitSpecHdr.KitInventoryID : null);
			bool flag = inventoryItemByID != null && kitSpecHdr != null;
			if (flag)
			{
				ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(inventoryItemByID);
				ASCJSMINKitSpecHdrExt extension2 = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(kitSpecHdr);
				extension.UsrActualGRAMGold = extension2.UsrActualGRAMGold;
				extension.UsrPricingGRAMGold = extension2.UsrPricingGRAMGold;
				extension.UsrActualGRAMSilver = extension2.UsrActualGRAMSilver;
				extension.UsrPricingGRAMSilver = extension2.UsrPricingGRAMSilver;
				extension.UsrPreciousMetalCost = extension2.UsrPreciousMetalCost;
				extension.UsrContractLossPct = extension2.UsrContractLossPct;
				extension.UsrContractSurcharge = extension2.UsrContractSurcharge;
				extension.UsrIncrement = extension2.UsrContractIncrement;
				extension.UsrFabricationCost = extension2.UsrFabricationCost;
				extension.UsrOtherCost = extension2.UsrOtherCost;
				extension.UsrOtherMaterialsCost = extension2.UsrOtherMaterialsCost;
				extension.UsrPackagingCost = extension2.UsrPackagingCost;
				extension.UsrPackagingLaborCost = extension2.UsrPackagingLaborCost;
				extension.UsrLaborCost = extension2.UsrLaborCost;
				extension.UsrHandlingCost = extension2.UsrHandlingCost;
				extension.UsrFreightCost = extension2.UsrFreightCost;
				extension.UsrDutyCost = extension2.UsrDutyCost;
				extension.UsrDutyCostPct = extension2.UsrDutyCostPct;
				extension.UsrLegacyID = extension2.UsrLegacyID;
				extension.UsrLegacyShortRef = extension2.UsrLegacyShortRef;
				this.ASCIStarInventoryItem.Update(inventoryItemByID);
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00004274 File Offset: 0x00002474
		protected virtual void CopyFieldsValueToPOVendorInventory(INKitSpecHdr kitSpecHdr)
		{
			POVendorInventory povendorInventory = this.VendorItems.Select(Array.Empty<object>()).RowCast<POVendorInventory>().FirstOrDefault((POVendorInventory _) => _.IsDefault.GetValueOrDefault());
			bool flag = povendorInventory != null && kitSpecHdr != null;
			if (flag)
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(povendorInventory);
				ASCJSMINKitSpecHdrExt extension2 = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(kitSpecHdr);
				extension.UsrPreciousMetalCost = extension2.UsrPreciousMetalCost;
				extension.UsrContractLossPct = extension2.UsrContractLossPct;
				extension.UsrContractSurcharge = extension2.UsrContractSurcharge;
				extension.UsrContractIncrement = extension2.UsrContractIncrement;
				extension.UsrFabricationCost = extension2.UsrFabricationCost;
				extension.UsrOtherCost = extension2.UsrOtherCost;
				extension.UsrOtherMaterialsCost = extension2.UsrOtherMaterialsCost;
				extension.UsrPackagingCost = extension2.UsrPackagingCost;
				extension.UsrPackagingLaborCost = extension2.UsrPackagingLaborCost;
				extension.UsrLaborCost = extension2.UsrLaborCost;
				extension.UsrHandlingCost = extension2.UsrHandlingCost;
				extension.UsrFreightCost = extension2.UsrFreightCost;
				extension.UsrDutyCost = extension2.UsrDutyCost;
				extension.UsrDutyCostPct = extension2.UsrDutyCostPct;
				this.VendorItems.Update(povendorInventory);
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000043A0 File Offset: 0x000025A0
		protected virtual ASCJSMCostBuilder CreateCostBuilder(ASCJSMINKitSpecStkDetExt currentRow, INKitSpecStkDet row)
		{
			POVendorInventory itemVendor = this.GetItemVendor(row);
			bool flag = itemVendor != null;
			if (flag)
			{
				return new ASCJSMCostBuilder(base.Base).WithInventoryItem(currentRow).WithPOVendorInventory(itemVendor).WithPricingData(PXTimeZoneInfo.Today).Build();
			}
			throw new PXSetPropertyException("To proceed, please add a default vendor or select one on the Vendors tab.");
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000043F4 File Offset: 0x000025F4
		protected virtual POVendorInventory GetItemVendor(INKitSpecStkDet row)
		{
			return PXSelectBase<POVendorInventory, PXViewOf<POVendorInventory>.BasedOn<SelectFromBase<POVendorInventory, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<POVendorInventory.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				row.CompInventoryID
			}).FirstTableItems.FirstOrDefault((POVendorInventory x) => x.IsDefault.GetValueOrDefault());
		}

		// Token: 0x0600003F RID: 63 RVA: 0x0000444C File Offset: 0x0000264C
		protected virtual void UpdateUnitCostAndBasisPrice(PXCache cache, INKitSpecStkDet row, ASCJSMINKitSpecStkDetExt rowExt)
		{
			decimal? dfltCompQty = row.DfltCompQty;
			decimal d = 0m;
			bool flag = dfltCompQty.GetValueOrDefault() == d & dfltCompQty != null;
			if (flag)
			{
				rowExt.UsrActualGRAMGold = rowExt.UsrBaseGoldGrams;
				rowExt.UsrActualGRAMSilver = rowExt.UsrBaseSilverGrams;
			}
			ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(rowExt, row);
			bool flag2 = ascjsmcostBuilder == null;
			if (!flag2)
			{
				decimal valueOrDefault = ascjsmcostBuilder.CalculatePreciousMetalCost(rowExt.UsrCostingType).GetValueOrDefault();
				dfltCompQty = row.DfltCompQty;
				d = 0m;
				decimal? num = (dfltCompQty.GetValueOrDefault() == d & dfltCompQty != null) ? new decimal?(valueOrDefault) : (valueOrDefault / row.DfltCompQty);
				cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrUnitCost>(row, num);
				decimal? num2 = (rowExt.UsrCostingType == "M") ? ascjsmcostBuilder.PreciousMetalMarketCostPerTOZ : ascjsmcostBuilder.PreciousMetalContractCostPerTOZ;
				cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrSalesPrice>(row, num2);
				cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBasisPrice>(row, ascjsmcostBuilder.PreciousMetalContractCostPerTOZ);
				cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBasisValue>(row, ascjsmcostBuilder.BasisValue);
			}
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00004598 File Offset: 0x00002798
		protected virtual void SetOneTypeForMetalRows(string usrCostingType)
		{
			IEnumerable<INKitSpecStkDet> firstTableItems = base.Base.StockDet.Select(Array.Empty<object>()).FirstTableItems;
			bool flag = false;
			foreach (INKitSpecStkDet inkitSpecStkDet in firstTableItems)
			{
				ASCJSMINKitSpecStkDetExt extension = inkitSpecStkDet.GetExtension<ASCJSMINKitSpecStkDetExt>();
				bool flag2 = extension.UsrCostingType == usrCostingType;
				if (!flag2)
				{
					base.Base.StockDet.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrCostingType>(inkitSpecStkDet, usrCostingType);
					base.Base.StockDet.Cache.Update(inkitSpecStkDet);
					flag = true;
				}
			}
			bool flag3 = flag;
			if (flag3)
			{
				base.Base.StockDet.View.RequestRefresh();
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00004668 File Offset: 0x00002868
		protected virtual decimal? GetUnitCostForCommodityItem(INKitSpecStkDet row)
		{
			decimal num = 0m;
			POVendorInventory itemVendor = this.GetItemVendor(row);
			bool flag = itemVendor == null;
			decimal? result;
			if (flag)
			{
				result = new decimal?(num);
			}
			else
			{
				ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(itemVendor);
				bool flag2 = this.JewelryItemView.Current == null;
				if (flag2)
				{
					this.JewelryItemView.Current = this.JewelryItemView.Select(Array.Empty<object>());
				}
				bool flag3 = this.JewelryItemView.Current == null;
				if (flag3)
				{
					result = new decimal?(num);
				}
				else
				{
					int? inventoryID = null;
					decimal d = 24m;
					bool flag4 = ASCJSMMetalType.IsGold(this.JewelryItemView.Current.MetalType);
					bool flag5 = flag4;
					if (flag5)
					{
						inventoryID = this._itemDataProvider.GetInventoryItemByCD("24K").InventoryID;
						d = ASCJSMMetalType.GetGoldTypeValue(this.JewelryItemView.Current.MetalType) / 24m;
					}
					bool flag6 = ASCJSMMetalType.IsSilver(this.JewelryItemView.Current.MetalType);
					bool flag7 = flag6;
					if (flag7)
					{
						inventoryID = this._itemDataProvider.GetInventoryItemByCD("SSS").InventoryID;
						d = ASCJSMMetalType.GetSilverTypeValue(this.JewelryItemView.Current.MetalType);
					}
					APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, extension.UsrMarketID, inventoryID, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Now);
					bool flag8 = apvendorPrice == null;
					if (flag8)
					{
						result = new decimal?(num);
					}
					else
					{
						ASCJSMINKitSpecStkDetExt extension2 = PXCache<INKitSpecStkDet>.GetExtension<ASCJSMINKitSpecStkDetExt>(row);
						ASCJSMAPVendorPriceExt extension3 = PXCache<APVendorPrice>.GetExtension<ASCJSMAPVendorPriceExt>(apvendorPrice);
						bool flag9 = row.UOM == ASCJSMConstants.GRAM.value;
						if (flag9)
						{
							bool flag10 = flag6;
							if (flag10)
							{
								ASCJSMCostBuilder ascjsmcostBuilder = this.CreateCostBuilder(extension2, row);
								bool flag11 = ascjsmcostBuilder == null;
								if (flag11)
								{
									return new decimal?(num);
								}
								decimal? num2 = ascjsmcostBuilder.CalculatePreciousMetalCost(ascjsmcostBuilder.ItemCostSpecification.UsrCostingType);
								num = ascjsmcostBuilder.PreciousMetalAvrSilverMarketCostPerTOZ.GetValueOrDefault(0.0m) / ASCJSMConstants.TOZ2GRAM_31_10348.value * d;
							}
							bool flag12 = flag4;
							if (flag12)
							{
								num = ((extension3 != null) ? extension3.UsrCommodityPerGram : null).GetValueOrDefault() * d;
							}
						}
						else
						{
							bool flag13 = row.UOM == ASCJSMConstants.TOZ.value;
							if (flag13)
							{
								return new decimal?(((apvendorPrice != null) ? apvendorPrice.SalesPrice : null).GetValueOrDefault());
							}
						}
						decimal? num3 = new decimal?((100.0m + extension2.UsrContractSurcharge.GetValueOrDefault(0.0m)) / 100.0m);
						decimal? num4 = new decimal?((100.0m + extension2.UsrContractLossPct.GetValueOrDefault(0.0m)) / 100.0m);
						result = num * num3 * num4;
					}
				}
			}
			return result;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000049F8 File Offset: 0x00002BF8
		protected virtual bool IsCommodityItem(INKitSpecStkDet row)
		{
			InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID(row.CompInventoryID);
			INItemClass itemClassByID = this._itemDataProvider.GetItemClassByID((inventoryItemByID != null) ? inventoryItemByID.ItemClassID : null);
			return ((itemClassByID != null) ? itemClassByID.ItemClassCD.NormalizeCD() : null) == ASCJSMConstants.CommodityClass.value;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00004A58 File Offset: 0x00002C58
		protected virtual void DfltGramsForCommodityItemType(PXCache cache, INKitSpecStkDet row)
		{
			ASCJSMINJewelryItem ascistarINJewelryItem = this.GetASCIStarINJewelryItem(row.CompInventoryID);
			bool flag = string.IsNullOrEmpty((ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null);
			if (flag)
			{
				cache.RaiseExceptionHandling<INKitSpecStkDet.compInventoryID>(row, row.CompInventoryID, new PXSetPropertyException("The Metal Type is missing!", PXErrorLevel.RowWarning));
			}
			else
			{
				bool flag2 = ASCJSMMetalType.IsGold((ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null);
				if (flag2)
				{
					decimal goldTypeValue = ASCJSMMetalType.GetGoldTypeValue((ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null);
					decimal num = 1m * goldTypeValue / 24m;
					cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBaseGoldGrams>(row, 1m);
					cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBaseFineGoldGrams>(row, num);
				}
				bool flag3 = ASCJSMMetalType.IsSilver((ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null);
				if (flag3)
				{
					decimal silverTypeValue = ASCJSMMetalType.GetSilverTypeValue((ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null);
					decimal num2 = 1m * silverTypeValue;
					cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBaseSilverGrams>(row, 1m);
					cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBaseFineSilverGrams>(row, num2);
				}
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00004B70 File Offset: 0x00002D70
		protected virtual void InsertComponentVendors(INKitSpecStkDet row)
		{
			bool flag = row.CompInventoryID == null;
			if (!flag)
			{
				List<POVendorInventory> list = PXSelectBase<POVendorInventory, PXSelectReadonly<POVendorInventory, Where<POVendorInventory.inventoryID, Equal<Required<POVendorInventory.inventoryID>>>>.Config>.Select(base.Base, new object[]
				{
					row.CompInventoryID
				}).FirstTableItems.ToList<POVendorInventory>();
				bool flag2 = !list.Any<POVendorInventory>();
				if (!flag2)
				{
					HashSet<int?> hashSet = (from x in this.VendorItems.Select(Array.Empty<object>()).FirstTableItems
					select x.VendorID).ToHashSet<int?>();
					InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID(row.KitInventoryID);
					ASCJSMINJewelryItem ascistarINJewelryItem = this.GetASCIStarINJewelryItem(row.CompInventoryID);
					string metalType = (ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null;
					foreach (POVendorInventory povendorInventory in list)
					{
						bool flag3 = hashSet.Contains(povendorInventory.VendorID);
						if (!flag3)
						{
							POVendorInventory povendorInventory2 = new POVendorInventory
							{
								InventoryID = row.KitInventoryID,
								VendorID = povendorInventory.VendorID,
								VendorLocationID = povendorInventory.VendorLocationID,
								SubItemID = povendorInventory.SubItemID,
								IsDefault = new bool?(false),
								VendorInventoryID = povendorInventory.VendorInventoryID,
								LastPrice = povendorInventory.LastPrice
							};
							bool flag4 = inventoryItemByID != null;
							if (flag4)
							{
								povendorInventory2.PurchaseUnit = inventoryItemByID.PurchaseUnit;
								bool flag5 = string.IsNullOrEmpty(povendorInventory2.PurchaseUnit);
								if (flag5)
								{
									povendorInventory2.PurchaseUnit = inventoryItemByID.BaseUnit;
								}
							}
							povendorInventory2 = this.VendorItems.Insert(povendorInventory2);
							bool flag6 = povendorInventory2 != null;
							if (flag6)
							{
								int? commodityInventoryByMetalType = ASCJSMMetalType.GetCommodityInventoryByMetalType(base.Base, metalType);
								bool flag7 = commodityInventoryByMetalType != null;
								if (flag7)
								{
									APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, povendorInventory2.VendorID, commodityInventoryByMetalType, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Today);
									bool flag8 = apvendorPrice != null;
									if (flag8)
									{
										ASCJSMAPVendorPriceExt extension = PXCache<APVendorPrice>.GetExtension<ASCJSMAPVendorPriceExt>(apvendorPrice);
										ASCJSMPOVendorInventoryExt extension2 = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(povendorInventory2);
										extension2.UsrBasisPrice = extension.UsrBasisValue;
										this.VendorItems.Update(povendorInventory2);
									}
								}
								hashSet.Add(povendorInventory2.VendorID);
							}
						}
					}
					this.VendorItems.View.RequestRefresh();
				}
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00004E0C File Offset: 0x0000300C
		protected virtual void RemoveOrphanedVendors(INKitSpecStkDet deletedRow)
		{
			bool flag = deletedRow.CompInventoryID == null;
			if (!flag)
			{
				HashSet<int?> hashSet = (from x in PXSelectBase<POVendorInventory, PXSelectReadonly<POVendorInventory, Where<POVendorInventory.inventoryID, Equal<Required<POVendorInventory.inventoryID>>>>.Config>.Select(base.Base, new object[]
				{
					deletedRow.CompInventoryID
				}).FirstTableItems
				select x.VendorID).ToHashSet<int?>();
				bool flag2 = !hashSet.Any<int?>();
				if (!flag2)
				{
					IEnumerable<INKitSpecStkDet> enumerable = from x in base.Base.StockDet.Select(Array.Empty<object>()).FirstTableItems
					where x.CompInventoryID != null
					select x;
					HashSet<int?> hashSet2 = new HashSet<int?>();
					foreach (INKitSpecStkDet inkitSpecStkDet in enumerable)
					{
						IEnumerable<POVendorInventory> firstTableItems = PXSelectBase<POVendorInventory, PXSelectReadonly<POVendorInventory, Where<POVendorInventory.inventoryID, Equal<Required<POVendorInventory.inventoryID>>>>.Config>.Select(base.Base, new object[]
						{
							inkitSpecStkDet.CompInventoryID
						}).FirstTableItems;
						foreach (POVendorInventory povendorInventory in firstTableItems)
						{
							hashSet2.Add(povendorInventory.VendorID);
						}
					}
					List<POVendorInventory> list = this.VendorItems.Select(Array.Empty<object>()).FirstTableItems.ToList<POVendorInventory>();
					foreach (POVendorInventory povendorInventory2 in list)
					{
						bool flag3 = hashSet.Contains(povendorInventory2.VendorID) && !hashSet2.Contains(povendorInventory2.VendorID);
						if (flag3)
						{
							this.VendorItems.Delete(povendorInventory2);
						}
					}
					this.VendorItems.View.RequestRefresh();
				}
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x0000502C File Offset: 0x0000322C
		protected virtual void UpdateInKitStkComponents(INKitSpecHdr inKitSpecHdr)
		{
			ASCJSMINKitSpecHdrExt extension = PXCache<INKitSpecHdr>.GetExtension<ASCJSMINKitSpecHdrExt>(inKitSpecHdr);
			PXResultset<INKitSpecStkDet> pxresultset = base.Base.StockDet.Select(Array.Empty<object>());
			List<INKitSpecStkDet> list = (pxresultset != null) ? pxresultset.FirstTableItems.ToList<INKitSpecStkDet>() : null;
			foreach (INKitSpecStkDet inkitSpecStkDet in list)
			{
				bool flag = this.IsCommodityItem(inkitSpecStkDet);
				if (flag)
				{
					ASCJSMINKitSpecStkDetExt extension2 = PXCache<INKitSpecStkDet>.GetExtension<ASCJSMINKitSpecStkDetExt>(inkitSpecStkDet);
					bool flag2 = extension2.UsrCostRollupType == "C";
					if (flag2)
					{
						extension2.UsrContractLossPct = extension.UsrContractLossPct;
						extension2.UsrContractSurcharge = extension.UsrContractSurcharge;
						decimal? unitCostForCommodityItem = this.GetUnitCostForCommodityItem(inkitSpecStkDet);
						extension2.UsrUnitCost = unitCostForCommodityItem;
						base.Base.StockDet.Cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrUnitCost>(inkitSpecStkDet, extension2.UsrUnitCost);
						base.Base.StockDet.Update(inkitSpecStkDet);
					}
				}
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00005144 File Offset: 0x00003344
		private void SetBasisValueOnStockComp(ASCJSMPOVendorInventoryExt rowExt)
		{
			PXResultset<INKitSpecStkDet> pxresultset = base.Base.StockDet.Select(Array.Empty<object>());
			List<INKitSpecStkDet> list = (pxresultset != null) ? pxresultset.FirstTableItems.ToList<INKitSpecStkDet>() : null;
			foreach (INKitSpecStkDet item in list)
			{
				ASCJSMINKitSpecStkDetExt extension = PXCache<INKitSpecStkDet>.GetExtension<ASCJSMINKitSpecStkDetExt>(item);
				bool flag = extension.UsrCostRollupType == "C";
				if (flag)
				{
					extension.UsrBasisValue = rowExt.UsrBasisValue;
					base.Base.StockDet.Update(item);
				}
			}
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000051F8 File Offset: 0x000033F8
		private void SetOrUpdatePreciousMetalCost(POVendorInventory row, InventoryItem item, string metalType)
		{
			ASCJSMPOVendorInventoryExt extension = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(row);
			int? vendorMarketID = this.GetVendorMarketID(row, extension);
			APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(base.Base, vendorMarketID, item.InventoryID, ASCJSMConstants.TOZ.value, PXTimeZoneInfo.Today);
			bool flag = apvendorPrice != null;
			if (flag)
			{
				decimal? usrPreciousMetalCost = apvendorPrice.SalesPrice * ASCJSMMetalType.GetMultFactorConvertTOZtoGram(metalType);
				extension.UsrPreciousMetalCost = usrPreciousMetalCost;
				this.VendorItems.Update(row);
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00005290 File Offset: 0x00003490
		private void UpdateTotalSurchargeAndLoss()
		{
			bool flag = base.Base.Hdr.Current == null;
			if (!flag)
			{
				decimal? fieldTotalPersentage = this.GetFieldTotalPersentage<ASCJSMINKitSpecStkDetExt.usrContractLossPct, ASCJSMINKitSpecStkDetExt.usrContractSurcharge>(base.Base.StockDet.Cache);
				decimal? fieldTotalPersentage2 = this.GetFieldTotalPersentage<ASCJSMINKitSpecStkDetExt.usrContractSurcharge, ASCJSMINKitSpecStkDetExt.usrContractLossPct>(base.Base.StockDet.Cache);
				decimal? incrementTotalValue = this.GetIncrementTotalValue();
				base.Base.Hdr.SetValueExt<ASCJSMINKitSpecHdrExt.usrContractLossPct>(base.Base.Hdr.Current, fieldTotalPersentage);
				base.Base.Hdr.SetValueExt<ASCJSMINKitSpecHdrExt.usrContractSurcharge>(base.Base.Hdr.Current, fieldTotalPersentage2);
				base.Base.Hdr.SetValueExt<ASCJSMINKitSpecHdrExt.usrContractIncrement>(base.Base.Hdr.Current, incrementTotalValue);
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00005364 File Offset: 0x00003564
		private decimal? GetFieldTotalPersentage<TField, THField>(PXCache cache) where TField : IBqlField where THField : IBqlField
		{
			PXResultset<INKitSpecStkDet> pxresultset = base.Base.StockDet.Select(Array.Empty<object>());
			List<INKitSpecStkDet> list;
			if (pxresultset == null)
			{
				list = null;
			}
			else
			{
				IEnumerable<INKitSpecStkDet> firstTableItems = pxresultset.FirstTableItems;
				if (firstTableItems == null)
				{
					list = null;
				}
				else
				{
					list = (from x in firstTableItems
					where x.GetExtension<ASCJSMINKitSpecStkDetExt>().UsrCostRollupType == "C"
					select x).ToList<INKitSpecStkDet>();
				}
			}
			List<INKitSpecStkDet> list2 = list;
			decimal? num = new decimal?(0m);
			decimal? num2 = new decimal?(0m);
			decimal num5;
			foreach (INKitSpecStkDet inkitSpecStkDet in list2)
			{
				ASCJSMINKitSpecStkDetExt extension = inkitSpecStkDet.GetExtension<ASCJSMINKitSpecStkDetExt>();
				decimal? num3 = (decimal?)cache.GetValue<TField>(inkitSpecStkDet);
				decimal? num4 = (decimal?)cache.GetValue<THField>(inkitSpecStkDet);
				decimal? usrExtCost = extension.UsrExtCost;
				num5 = 1;
				decimal? num6 = usrExtCost / (num5 + num3 / 100);
				num5 = 1;
				decimal? num7 = num6 / (num5 + num4 / 100);
				num += num7;
				num6 = num2;
				decimal? num8 = num7 * num3;
				num5 = 100;
				num2 = num6 + num8 / num5;
			}
			decimal? num9 = num;
			num5 = 0.0m;
			decimal? result;
			if (!(num9.GetValueOrDefault() == num5 & num9 != null) && num != null)
			{
				num9 = num2 / num;
				num5 = 100;
				result = num9 * num5;
			}
			else
			{
				result = new decimal?(0m);
			}
			return result;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00005738 File Offset: 0x00003938
		private decimal? GetIncrementTotalValue()
		{
			PXResultset<INKitSpecStkDet> pxresultset = base.Base.StockDet.Select(Array.Empty<object>());
			List<INKitSpecStkDet> list;
			if (pxresultset == null)
			{
				list = null;
			}
			else
			{
				IEnumerable<INKitSpecStkDet> firstTableItems = pxresultset.FirstTableItems;
				list = ((firstTableItems != null) ? firstTableItems.ToList<INKitSpecStkDet>() : null);
			}
			List<INKitSpecStkDet> list2 = list;
			decimal? num = new decimal?(0m);
			decimal? num2 = new decimal?(0m);
			ASCJSMINKitSpecJewelryItem topFirst = this.JewelryItemView.Select(Array.Empty<object>()).TopFirst;
			string metalType = (topFirst != null) ? topFirst.MetalType : null;
			decimal multFactorConvertTOZtoGram = ASCJSMMetalType.GetMultFactorConvertTOZtoGram(metalType);
			decimal? num3;
			decimal num4;
			foreach (INKitSpecStkDet inkitSpecStkDet in list2)
			{
				ASCJSMINKitSpecStkDetExt extension = inkitSpecStkDet.GetExtension<ASCJSMINKitSpecStkDetExt>();
				ASCJSMINJewelryItem ascistarINJewelryItem = this.GetASCIStarINJewelryItem(inkitSpecStkDet.CompInventoryID);
				string metalType2 = (ascistarINJewelryItem != null) ? ascistarINJewelryItem.MetalType : null;
				num3 = num;
				num4 = ASCJSMMetalType.GetMultFactorConvertTOZtoGram(metalType2);
				num = num3 + num4 * (extension.UsrActualGRAMGold + extension.UsrActualGRAMSilver);
				decimal? num5 = num2;
				num4 = multFactorConvertTOZtoGram;
				num2 = num5 + num4 * (extension.UsrActualGRAMGold + extension.UsrActualGRAMSilver);
			}
			num3 = num2;
			num4 = 0.0m;
			decimal? result;
			if (!(num3.GetValueOrDefault() == num4 & num3 != null) && num2 != null)
			{
				num4 = ASCJSMMetalType.GetMultFactorConvertTOZtoGram(metalType);
				result = num4 * num / num2;
			}
			else
			{
				result = new decimal?(0m);
			}
			return result;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00005A40 File Offset: 0x00003C40
		private int? GetVendorMarketID(POVendorInventory row, ASCJSMPOVendorInventoryExt rowExt)
		{
			int? result = null;
			bool flag = rowExt.UsrMarketID == null;
			if (flag)
			{
				Vendor vendor = this._vendorDataProvider.GetVendor(row.VendorID, false);
				bool flag2 = vendor != null;
				if (flag2)
				{
					ASCJSMVendorExt extension = PXCache<Vendor>.GetExtension<ASCJSMVendorExt>(vendor);
					result = ((extension != null) ? extension.UsrMarketID : null);
				}
			}
			else
			{
				result = rowExt.UsrMarketID;
			}
			return result;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00005ABC File Offset: 0x00003CBC
		private bool IsBaseItemsExists()
		{
			return this._itemDataProvider.GetInventoryItemByCD("24K") != null && this._itemDataProvider.GetInventoryItemByCD("SSS") != null;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00005AF6 File Offset: 0x00003CF6
		private ASCJSMINJewelryItem GetASCIStarINJewelryItem(int? inventoryID)
		{
			PXResultset<ASCJSMINJewelryItem> pxresultset = PXSelectBase<ASCJSMINJewelryItem, PXViewOf<ASCJSMINJewelryItem>.BasedOn<SelectFromBase<ASCJSMINJewelryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<ASCJSMINJewelryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				inventoryID
			});
			return (pxresultset != null) ? pxresultset.TopFirst : null;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00005B20 File Offset: 0x00003D20
		private POVendorInventory GetDefaultPOVendorInventory()
		{
			PXResultset<POVendorInventory> pxresultset = this.VendorItems.Select(Array.Empty<object>());
			POVendorInventory result;
			if (pxresultset == null)
			{
				result = null;
			}
			else
			{
				result = pxresultset.FirstTableItems.FirstOrDefault((POVendorInventory x) => x.IsDefault.GetValueOrDefault());
			}
			return result;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00005B70 File Offset: 0x00003D70
		private void UpdateHeaderBasisValue()
		{
			bool flag = base.Base.Hdr.Current == null;
			if (!flag)
			{
				decimal? num = new decimal?(0m);
				decimal? num2 = new decimal?(0m);
				IEnumerable<INKitSpecStkDet> firstTableItems = base.Base.StockDet.Select(Array.Empty<object>()).FirstTableItems;
				foreach (INKitSpecStkDet inkitSpecStkDet in firstTableItems)
				{
					InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
					{
						inkitSpecStkDet.CompInventoryID
					});
					ASCJSMINInventoryItemExt extension = item.GetExtension<ASCJSMINInventoryItemExt>();
					bool flag2 = extension.UsrCommodityType != "G" && extension.UsrCommodityType != "S";
					if (!flag2)
					{
						ASCJSMINKitSpecStkDetExt extension2 = inkitSpecStkDet.GetExtension<ASCJSMINKitSpecStkDetExt>();
						num += extension2.UsrBasisPrice;
						num2 += 1;
					}
				}
				decimal? num3 = num2;
				decimal d = 0m;
				bool flag3 = num3.GetValueOrDefault() == d & num3 != null;
				if (!flag3)
				{
					decimal? num4 = num / num2;
					base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrBasisValue>(base.Base.Hdr.Current, num4);
					base.Base.Hdr.Update(base.Base.Hdr.Current);
				}
			}
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00005DAC File Offset: 0x00003FAC
		private void CalculateLineIncrement(PXCache cache, INKitSpecStkDet row)
		{
			ASCJSMINKitSpecStkDetExt extension = row.GetExtension<ASCJSMINKitSpecStkDetExt>();
			bool flag;
			if (extension.UsrContractIncrement != null)
			{
				decimal? num = extension.UsrContractIncrement;
				decimal d = 0m;
				flag = !(num.GetValueOrDefault() == d & num != null);
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			if (!flag2)
			{
				InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID(row.CompInventoryID);
				bool flag3 = inventoryItemByID == null;
				if (!flag3)
				{
					ASCJSMINInventoryItemExt extension2 = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(inventoryItemByID);
					decimal? num2 = null;
					bool flag4;
					if (extension2.UsrCommodityType == "G" && extension2.UsrPricingGRAMGold != null)
					{
						decimal? num = extension2.UsrPricingGRAMGold;
						decimal d = 0m;
						flag4 = !(num.GetValueOrDefault() == d & num != null);
					}
					else
					{
						flag4 = false;
					}
					bool flag5 = flag4;
					if (flag5)
					{
						num2 = 1m / ASCJSMConstants.TOZ2GRAM_31_10348.value * extension2.UsrPricingGRAMGold * (1m + extension2.UsrContractSurcharge.GetValueOrDefault() / 100m) * (1m + extension2.UsrContractLossPct.GetValueOrDefault() / 100m);
					}
					else
					{
						bool flag6 = extension2.UsrCommodityType == "S";
						if (flag6)
						{
							num2 = new decimal?(extension2.UsrContractIncrement.GetValueOrDefault() * extension2.UsrActualGRAMSilver.GetValueOrDefault());
						}
					}
					bool flag7 = num2 != null;
					if (flag7)
					{
						cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrContractIncrement>(row, num2);
					}
				}
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00005FD8 File Offset: 0x000041D8
		private void SetLineBasisValue(PXCache cache, INKitSpecStkDet row)
		{
			bool flag = row.CompInventoryID == null;
			if (!flag)
			{
				InventoryItem inventoryItemByID = this._itemDataProvider.GetInventoryItemByID(row.CompInventoryID);
				bool flag2 = inventoryItemByID == null;
				if (!flag2)
				{
					ASCJSMINInventoryItemExt extension = PXCache<InventoryItem>.GetExtension<ASCJSMINInventoryItemExt>(inventoryItemByID);
					bool flag3;
					if (extension != null && extension.UsrBasisValue != null)
					{
						decimal? usrBasisValue = extension.UsrBasisValue;
						decimal d = 0m;
						flag3 = !(usrBasisValue.GetValueOrDefault() == d & usrBasisValue != null);
					}
					else
					{
						flag3 = false;
					}
					bool flag4 = flag3;
					if (flag4)
					{
						cache.SetValueExt<ASCJSMINKitSpecStkDetExt.usrBasisValue>(row, extension.UsrBasisValue);
					}
				}
			}
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00006080 File Offset: 0x00004280
		private void UpdateHeaderFields()
		{
			decimal? num = new decimal?(0m);
			PXResultset<INKitSpecStkDet> pxresultset = base.Base.StockDet.Select(Array.Empty<object>());
			foreach (PXResult<INKitSpecStkDet> r in pxresultset)
			{
				INKitSpecStkDet inkitSpecStkDet = r;
				bool flag;
				if (inkitSpecStkDet.DfltCompQty != null)
				{
					decimal? dfltCompQty = inkitSpecStkDet.DfltCompQty;
					decimal d = 0m;
					flag = (dfltCompQty.GetValueOrDefault() == d & dfltCompQty != null);
				}
				else
				{
					flag = true;
				}
				bool flag2 = flag;
				if (!flag2)
				{
					ASCJSMINKitSpecStkDetExt extension = inkitSpecStkDet.GetExtension<ASCJSMINKitSpecStkDetExt>();
					num += extension.UsrContractIncrement * inkitSpecStkDet.DfltCompQty;
				}
			}
			base.Base.Hdr.Current.GetExtension<ASCJSMINKitSpecHdrExt>().UsrContractIncrement = num;
			this.UpdateHeaderBasisValue();
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000061E4 File Offset: 0x000043E4
		private void SortAndAddStkDetailLines()
		{
			INKitSpecHdr inkitSpecHdr = this.Hdr.Current;
			ASCJSMINKitSpecHdrExt extension = inkitSpecHdr.GetExtension<ASCJSMINKitSpecHdrExt>();
			decimal? num = new decimal?(0m);
			decimal? num2 = new decimal?(0m);
			decimal? num3 = new decimal?(0m);
			decimal? num4 = new decimal?(0m);
			decimal? num5 = new decimal?(0m);
			decimal? num6 = new decimal?(0m);
			decimal? num7 = new decimal?(0m);
			PXResultset<INKitSpecStkDet> pxresultset = base.Base.StockDet.Select(Array.Empty<object>());
			foreach (PXResult<INKitSpecStkDet> r in pxresultset)
			{
				INKitSpecStkDet item = r;
				ASCJSMINKitSpecStkDetExt extension2 = item.GetExtension<ASCJSMINKitSpecStkDetExt>();
				num += extension2.UsrPreciousMetalCost;
				num2 += extension2.UsrFabricationCost;
				num3 += extension2.UsrOtherMaterialsCost;
				num4 += extension2.UsrPackagingCost;
				num5 += extension2.UsrPackagingLaborCost;
				num7 += extension2.UsrHandlingCost;
			}
			base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrPreciousMetalCost>(inkitSpecHdr, num);
			base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrFabricationCost>(inkitSpecHdr, num2);
			base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrOtherMaterialsCost>(inkitSpecHdr, num3);
			base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrPackagingCost>(inkitSpecHdr, num4);
			base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrPackagingLaborCost>(inkitSpecHdr, num5);
			base.Base.Hdr.Cache.SetValueExt<ASCJSMINKitSpecHdrExt.usrHandlingCost>(inkitSpecHdr, num7);
			base.Base.Hdr.Update(inkitSpecHdr);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00006528 File Offset: 0x00004728
		protected virtual void SendEmailNotification(INKitSpecHdr inKitSpecHdr)
		{
			bool flag = this.VendorItems.Current == null;
			if (flag)
			{
				PXSelectBase<POVendorInventory> vendorItems = this.VendorItems;
				PXResultset<POVendorInventory> pxresultset = this.VendorItems.Select(Array.Empty<object>());
				POVendorInventory value;
				if (pxresultset == null)
				{
					value = null;
				}
				else
				{
					value = pxresultset.RowCast<POVendorInventory>().FirstOrDefault((POVendorInventory x) => x.IsDefault.GetValueOrDefault());
				}
				vendorItems.Current = value;
			}
			bool flag2 = this.VendorItems.Current == null;
			if (flag2)
			{
				throw new PXException("To proceed, please add a default vendor or select one on the Vendors tab.");
			}
			BAccount bAccount = BAccount.PK.Find(base.Base, this.VendorItems.Current.VendorID, PKFindOptions.None);
			InventoryItem inventoryItem = InventoryItem.PK.Find(base.Base, inKitSpecHdr.KitInventoryID, PKFindOptions.None);
			NotificationGenerator notificationGenerator = new NotificationGenerator
			{
				To = this.GetVendorEmail(bAccount),
				Subject = string.Format("Purchase Item request: {0}", (inventoryItem != null) ? inventoryItem.InventoryCD : null),
				Body = this.CreateEmailBody(inventoryItem, bAccount),
				BodyFormat = "H"
			};
			this.AddAttachmentsToEmail(notificationGenerator);
			notificationGenerator.Send();
		}

		// Token: 0x06000056 RID: 86 RVA: 0x0000663E File Offset: 0x0000483E
		private string GetVendorEmail(BAccount bAccount)
		{
			PXResultset<Contact> pxresultset = PXSelectBase<Contact, PXViewOf<Contact>.BasedOn<SelectFromBase<Contact, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<Contact.contactID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				bAccount.PrimaryContactID
			});
			string result;
			if (pxresultset == null)
			{
				result = null;
			}
			else
			{
				Contact topFirst = pxresultset.TopFirst;
				result = ((topFirst != null) ? topFirst.EMail : null);
			}
			return result;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00006678 File Offset: 0x00004878
		private string CreateEmailBody(InventoryItem inventoryItem, BAccount bAccount)
		{
			PXResultset<Location> pxresultset = PXSelectBase<Location, PXViewOf<Location>.BasedOn<SelectFromBase<Location, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<Location.bAccountID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				bAccount.BAccountID
			});
			Location location = (pxresultset != null) ? pxresultset.TopFirst : null;
			string text = PXLogin.ExtractCompany(PXContext.PXIdentity.IdentityName);
			short? num;
			return string.Format("Dear supplier, {0}\r\nPlease, find in attachment specification for Item Style: {1} - {2} \r\nProvide {0}'s most favorable quote for its manufacture. \r\nExpectation delivery time frame is {3} days from Purcahse Order date. \r\n\r\nBest regards, \r\n{4}\r\n{5}", new object[]
			{
				bAccount.AcctName,
				inventoryItem.InventoryCD,
				inventoryItem.Descr,
				(location != null) ? ((location.VLeadTime != null) ? num.GetValueOrDefault().ToString() : null) : null,
				text,
				PXAccess.GetUserDisplayName()
			});
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00006730 File Offset: 0x00004930
		private void AddAttachmentsToEmail(NotificationGenerator sender)
		{
			UploadFileMaintenance uploadFileMaintenance = PXGraph.CreateInstance<UploadFileMaintenance>();
			Guid[] fileNotes = PXNoteAttribute.GetFileNotes(base.Base.Hdr.Cache, base.Base.Hdr.Current);
			foreach (Guid fileID in fileNotes)
			{
				FileInfo file = uploadFileMaintenance.GetFile(fileID);
				sender.AddAttachment(file.Name, file.BinData);
			}
		}

		// Token: 0x04000004 RID: 4
		private const decimal One_Gram = 1m;

		// Token: 0x04000005 RID: 5
		[PXCopyPasteHiddenView]
		public PXSelect<INKitSpecHdr, Where<INKitSpecHdr.kitInventoryID, Equal<Optional<INKitSpecHdr.kitInventoryID>>>> Hdr;

		// Token: 0x04000006 RID: 6
		[PXCopyPasteHiddenView]
		public PXSelect<POVendorInventory, Where<POVendorInventory.inventoryID, Equal<Current<INKitSpecHdr.kitInventoryID>>>> VendorItems;

		// Token: 0x04000007 RID: 7
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			1,
			0,
			0,
			1,
			1,
			0,
			1,
			1,
			0,
			1,
			1,
			0
		})]
		public FbqlSelect<SelectFromBase<ASCJSMINKitSpecJewelryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<ASCJSMINKitSpecJewelryItem.kitInventoryID, Equal<BqlField<INKitSpecHdr.kitInventoryID, IBqlInt>.FromCurrent>>>>>.And<BqlOperand<ASCJSMINKitSpecJewelryItem.revisionID, IBqlString>.IsEqual<BqlField<INKitSpecHdr.revisionID, IBqlString>.FromCurrent>>>, ASCJSMINKitSpecJewelryItem>.View JewelryItemView;

		// Token: 0x04000008 RID: 8
		[PXCopyPasteHiddenView]
		public PXSetup<INSetup> ASCIStarINSetup;

		// Token: 0x04000009 RID: 9
		[PXCopyPasteHiddenView]
		public PXSelect<InventoryItem, Where<InventoryItem.inventoryID, Equal<Current<INKitSpecHdr.kitInventoryID>>>> ASCIStarInventoryItem;

		// Token: 0x0400000A RID: 10
		[PXCopyPasteHiddenView]
		public PXSelect<ASCJSMINJewelryItem, Where<ASCJSMINJewelryItem.inventoryID, Equal<Current<INKitSpecHdr.kitInventoryID>>>> ASCIStarJewelryItem;

		// Token: 0x0400000B RID: 11
		[PXCopyPasteHiddenView]
		public FbqlSelect<SelectFromBase<InventoryItemCurySettings, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<InventoryItemCurySettings.inventoryID, Equal<P.AsInt>>>>>.And<BqlOperand<InventoryItemCurySettings.curyID, IBqlString>.IsEqual<BqlField<AccessInfo.baseCuryID, IBqlString>.AsOptional>>>, InventoryItemCurySettings>.View ASCIStarItemCurySettings;

		// Token: 0x0400000C RID: 12
		[PXCopyPasteHiddenView]
		public FbqlSelect<SelectFromBase<InventoryItemCurySettings, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItemCurySettings.inventoryID, IBqlInt>.IsEqual<P.AsInt>>, InventoryItemCurySettings>.View ASCIStarAllItemCurySettings;

		// Token: 0x0400000F RID: 15
		public PXAction<INKitSpecHdr> SendEmailToVendor;

		// Token: 0x04000010 RID: 16
		public PXAction<INKitSpecHdr> ASCIStarCreateProdItem;

		// Token: 0x04000011 RID: 17
		public PXAction<INKitSpecHdr> ASCIStarUpdateMetalCost;

		// Token: 0x0200004B RID: 75
		// (Invoke) Token: 0x06000453 RID: 1107
		public delegate void PersistDelegate();
	}
}
