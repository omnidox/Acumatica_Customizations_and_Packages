using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PX.Api;
using PX.Api.Models;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.WorkflowAPI;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.Common.Discount;
using PX.Objects.Common.GraphExtensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN.DAC.Unbound;
using PX.Objects.IN.GraphExtensions.InventoryItemMaintExt;
using PX.Objects.IN.InventoryRelease;
using PX.Objects.IN.InventoryRelease.Accumulators.QtyAllocated;
using PX.Objects.IN.InventoryRelease.Accumulators.Statistics.Item;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.TX;
using PX.SM;

namespace PX.Objects.IN
{
	// Token: 0x02000C9A RID: 3226
	public class InventoryItemMaint : InventoryItemMaintBase
	{
		// Token: 0x170049EA RID: 18922
		// (get) Token: 0x0600D77A RID: 55162 RVA: 0x00314CA3 File Offset: 0x00312EA3
		public override bool IsStockItemFlag
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600D77B RID: 55163 RVA: 0x00314CA8 File Offset: 0x00312EA8
		public InventoryItemMaint()
		{
			this.Item.View = new PXView(this, false, new SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<InventoryItem.stkItem, Equal<True>>>>, And<BqlOperand<InventoryItem.itemStatus, IBqlString>.IsNotEqual<InventoryItemStatus.unknown>>>, And<BqlOperand<InventoryItem.isTemplate, IBqlBool>.IsEqual<False>>>>.And<MatchUser>>());
			this.Views["Item"] = this.Item.View;
			PXUIFieldAttribute.SetVisible<Vendor.curyID>(this.Caches[typeof(Vendor)], null, PXAccess.FeatureInstalled<FeaturesSet.multicurrency>());
			PXUIFieldAttribute.SetVisible<InventoryItem.pPVAcctID>(this.Item.Cache, null, true);
			PXUIFieldAttribute.SetVisible<InventoryItem.pPVSubID>(this.Item.Cache, null, true);
			this.itemsiterecords.Cache.AllowInsert = false;
			this.itemsiterecords.Cache.AllowDelete = false;
			PXUIFieldAttribute.SetEnabled(this.itemsiterecords.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INItemSite.isDefault>(this.itemsiterecords.Cache, null, true);
			PXUIFieldAttribute.SetEnabled<INItemSite.siteStatus>(this.itemsiterecords.Cache, null, true);
			PXUIFieldAttribute.SetEnabled(this.Groups.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<RelationGroup.included>(this.Groups.Cache, null, true);
			bool allowSelect = PXAccess.FeatureInstalled<FeaturesSet.replenishment>() && PXAccess.FeatureInstalled<FeaturesSet.subItem>();
			this.subreplenishment.AllowSelect = allowSelect;
			PXDBDefaultAttribute.SetDefaultForInsert<INItemXRef.inventoryID>(this.itemxrefrecords.Cache, null, true);
			base.FieldDefaulting.AddHandler<BAccountR.type>(delegate(PXCache sender, PXFieldDefaultingEventArgs e)
			{
				if (e.Row != null)
				{
					e.NewValue = "VE";
				}
			});
			this.Item.Cache.Fields.Add("LotSerNumVal");
			base.FieldSelecting.AddHandler(typeof(InventoryItem), "LotSerNumVal", new PXFieldSelecting(this.LotSerNumValueFieldSelecting));
			base.FieldUpdating.AddHandler(typeof(InventoryItem), "LotSerNumVal", new PXFieldUpdating(this.LotSerNumValueFieldUpdating));
		}

		// Token: 0x0600D77C RID: 55164 RVA: 0x00314E78 File Offset: 0x00313078
		public sealed override void Configure(PXScreenConfiguration config)
		{
			InventoryItemMaint.Configure(config.GetScreenConfigurationContext<InventoryItemMaint, InventoryItem>());
		}

		// Token: 0x0600D77D RID: 55165 RVA: 0x00314E88 File Offset: 0x00313088
		protected static void Configure(WorkflowContext<InventoryItemMaint, InventoryItem> context)
		{
			BoundedTo<InventoryItemMaint, InventoryItem>.Condition isKit = context.Conditions.FromBql<BqlOperand<InventoryItem.kitItem, IBqlBool>.IsEqual<True>>().WithSharedName("IsKit");
			BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.IConfigured pricesCategory = context.Categories.CreateNew("Prices Category", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.IAllowOptionalConfigCategory category) => category.DisplayName("Prices"));
			BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.IConfigured otherCategory = CommonActionCategories.Get<InventoryItemMaint, InventoryItem>(context).Other;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__5;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__7;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__9;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__11;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__13;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__15;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> <>9__32;
			Action<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IContainerFillerActions> <>9__2;
			Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.ConfiguratorCategory, BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.ConfiguratorCategory> <>9__55;
			Action<BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.IContainerFillerCategories> <>9__3;
			context.AddScreenConfigurationFor(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.ScreenConfiguration.IStartConfigScreen screen)
			{
				Action<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IContainerFillerActions> containerFiller;
				if ((containerFiller = <>9__2) == null)
				{
					containerFiller = (<>9__2 = delegate(BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IContainerFillerActions actions)
					{
						Expression<Func<InventoryItemMaint, PXAction<InventoryItem>>> actionSelector = (InventoryItemMaint g) => g.viewSalesPrices;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config;
						if ((config = <>9__5) == null)
						{
							config = (<>9__5 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(pricesCategory)));
						}
						actions.Add(actionSelector, config);
						Expression<Func<InventoryItemMaint, PXAction<InventoryItem>>> actionSelector2 = (InventoryItemMaint g) => g.viewVendorPrices;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config2;
						if ((config2 = <>9__7) == null)
						{
							config2 = (<>9__7 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(pricesCategory)));
						}
						actions.Add(actionSelector2, config2);
						Expression<Func<InventoryItemMaint, PXAction<InventoryItem>>> actionSelector3 = (InventoryItemMaint g) => g.updateCost;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config3;
						if ((config3 = <>9__9) == null)
						{
							config3 = (<>9__9 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(otherCategory)));
						}
						actions.Add(actionSelector3, config3);
						Expression<Func<InventoryItemMaint, PXAction<InventoryItem>>> actionSelector4 = (InventoryItemMaint g) => g.ChangeID;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config4;
						if ((config4 = <>9__11) == null)
						{
							config4 = (<>9__11 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(otherCategory)));
						}
						actions.Add(actionSelector4, config4);
						Expression<Func<InventoryItemMaint, PXAction<InventoryItem>>> actionSelector5 = (InventoryItemMaint g) => g.viewRestrictionGroups;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config5;
						if ((config5 = <>9__13) == null)
						{
							config5 = (<>9__13 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(otherCategory)));
						}
						actions.Add(actionSelector5, config5);
						Expression<Func<ConvertStockToNonStockExt, PXAction<InventoryItem>>> actionSelector6 = (ConvertStockToNonStockExt g) => g.convert;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config6;
						if ((config6 = <>9__15) == null)
						{
							config6 = (<>9__15 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(otherCategory)));
						}
						actions.Add<ConvertStockToNonStockExt>(actionSelector6, config6);
						actions.Add((InventoryItemMaint g) => g.viewSummary, (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add((InventoryItemMaint g) => g.viewAllocationDetails, (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add((InventoryItemMaint g) => g.viewTransactionSummary, (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add((InventoryItemMaint g) => g.viewTransactionDetails, (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add((InventoryItemMaint g) => g.viewTransactionHistory, (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.WithCategory(PredefinedCategory.Inquiries));
						actions.AddNew("ShowItemSalesPrices", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Item Sales Prices").IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<ARSalesPriceMaint>().WithIcon("account_balance").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
						{
							ass.Add<ARSalesPriceFilter.inventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
						})));
						actions.AddNew("ShowItemVendorPrices", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Item Vendor Prices").IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<APVendorPriceMaint>().WithIcon("local_offer").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
						{
							ass.Add<APVendorPriceFilter.inventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
						})));
						actions.AddNew("ShowInventorySummary", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Inventory Summary").IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<InventorySummaryEnq>().WithIcon("business").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
						{
							ass.Add<InventorySummaryEnqFilter.inventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
						})));
						actions.AddNew("ShowInventoryAllocationDetails", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Inventory Allocation Details").IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<InventoryAllocDetEnq>().WithIcon("account_details").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
						{
							ass.Add<InventoryAllocDetEnqFilter.inventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
						})));
						actions.AddNew("ShowInventoryTransactionHistory", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Inventory Transaction History").IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<InventoryTranHistEnq>().WithIcon("archive").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
						{
							ass.Add<InventoryTranHistEnqFilter.inventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
						})));
						actions.AddNew("ShowDeadStock", (BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Dead Stock").IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<INDeadStockEnq>().WithIcon("trending_down").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
						{
							ass.Add<INDeadStockEnqFilter.siteID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField("DefaultSiteID"));
							ass.Add<INDeadStockEnqFilter.inventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
						})));
						string actionName = "ShowKitSpecifications";
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction, BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IConfigured> config7;
						if ((config7 = <>9__32) == null)
						{
							config7 = (<>9__32 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionDefinition.IAllowOptionalConfigAction a) => a.DisplayName("Kit Specifications").IsHiddenWhen(!isKit).IsSidePanelScreen((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationDefinition.ISidePanelNeedScreen sp) => sp.NavigateToScreen<INKitSpecMaint>().WithIcon("description").WithAssignments(delegate(BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.IContainerFillerNavigationActionParameters ass)
							{
								ass.Add<INKitSpecHdr.kitInventoryID>((BoundedTo<InventoryItemMaint, InventoryItem>.NavigationParameter.INeedRightOperand e) => e.SetFromField<InventoryItem.inventoryID>());
							}))));
						}
						actions.AddNew(actionName, config7);
					});
				}
				BoundedTo<InventoryItemMaint, InventoryItem>.ScreenConfiguration.IAllowOptionalConfig allowOptionalConfig = screen.WithActions(containerFiller);
				Action<BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.IContainerFillerCategories> containerFiller2;
				if ((containerFiller2 = <>9__3) == null)
				{
					containerFiller2 = (<>9__3 = delegate(BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.IContainerFillerCategories categories)
					{
						categories.Add(pricesCategory);
						categories.Add(otherCategory);
						FolderType folderType = FolderType.InquiriesFolder;
						Func<BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.ConfiguratorCategory, BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.ConfiguratorCategory> config;
						if ((config = <>9__55) == null)
						{
							config = (<>9__55 = ((BoundedTo<InventoryItemMaint, InventoryItem>.ActionCategory.ConfiguratorCategory category) => category.PlaceAfter(otherCategory)));
						}
						categories.Update(folderType, config);
					});
				}
				return allowOptionalConfig.WithCategories(containerFiller2);
			});
		}

		// Token: 0x0600D77E RID: 55166 RVA: 0x00314F11 File Offset: 0x00313111
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<INItemClass.stkItem> e)
		{
		}

		// Token: 0x0600D77F RID: 55167 RVA: 0x00314F13 File Offset: 0x00313113
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault("F", typeof(SearchFor<INItemClass.itemType>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemClass.itemClassID, Equal<BqlField<INItemClass.parentItemClassID, IBqlInt>.FromCurrent>>>>>.And<BqlOperand<INItemClass.stkItem, IBqlBool>.IsEqual<True>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<INItemClass.itemType> e)
		{
		}

		// Token: 0x0600D780 RID: 55168 RVA: 0x00314F15 File Offset: 0x00313115
		[PXDBString(255)]
		[PXUIField(DisplayName = "Specific Type")]
		[PXStringList(new string[]
		{
			"PX.Objects.CS.SegmentValue",
			"PX.Objects.IN.InventoryItem"
		}, new string[]
		{
			"Subitem",
			"Inventory Item Restriction"
		})]
		protected virtual void _(Events.CacheAttached<RelationGroup.specificType> e)
		{
		}

		// Token: 0x0600D781 RID: 55169 RVA: 0x00314F17 File Offset: 0x00313117
		[PXDefault]
		[InventoryRaw(typeof(Where<BqlOperand<InventoryItem.stkItem, IBqlBool>.IsEqual<True>>), IsKey = true, DisplayName = "Inventory ID", Filterable = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.inventoryCD> e)
		{
		}

		// Token: 0x0600D782 RID: 55170 RVA: 0x00314F19 File Offset: 0x00313119
		[PXDBString(1, IsFixed = true, BqlField = typeof(InventoryItem.itemType))]
		[PXDefault("F", typeof(SelectFromBase<INItemClass, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemClass.itemClassID, IBqlInt>.IsEqual<BqlField<InventoryItem.itemClassID, IBqlInt>.FromCurrent>>), SourceField = typeof(INItemClass.itemType), CacheGlobal = true)]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible)]
		[INItemTypes.StockListAttribute]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemType> e)
		{
		}

		// Token: 0x0600D783 RID: 55171 RVA: 0x00314F1B File Offset: 0x0031311B
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault(typeof(SelectFromBase<INItemClass, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemClass.itemClassID, IBqlInt>.IsEqual<BqlField<InventoryItem.itemClassID, IBqlInt>.FromCurrent>>), SourceField = typeof(INItemClass.lotSerClassID), CacheGlobal = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.lotSerClassID> e)
		{
		}

		// Token: 0x0600D784 RID: 55172 RVA: 0x00314F1D File Offset: 0x0031311D
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXSelector(typeof(INPostClass.postClassID), DescriptionField = typeof(INPostClass.descr))]
		[PXDefault(typeof(SelectFromBase<INItemClass, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemClass.itemClassID, IBqlInt>.IsEqual<BqlField<InventoryItem.itemClassID, IBqlInt>.FromCurrent>>), SourceField = typeof(INItemClass.postClassID), CacheGlobal = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.postClassID> e)
		{
		}

		// Token: 0x0600D785 RID: 55173 RVA: 0x00314F1F File Offset: 0x0031311F
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(typeof(Where<INItemClass.stkItem, Equal<True>>), "The class you have selected can not be assigned to a stock item, because the Stock Item check box is cleared for this class on the Item Classes(IN201000) form.Select another item class which is designated to group stock items.", new Type[]
		{

		})]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemClassID> e)
		{
		}

		// Token: 0x0600D786 RID: 55174 RVA: 0x00314F21 File Offset: 0x00313121
		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), "ValidateValueOnPersisting", true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.defaultSubItemID> e)
		{
		}

		// Token: 0x0600D787 RID: 55175 RVA: 0x00314F23 File Offset: 0x00313123
		[PXDBString]
		[PXDefault("P")]
		protected virtual void _(Events.CacheAttached<InventoryItem.postToExpenseAccount> e)
		{
		}

		// Token: 0x0600D788 RID: 55176 RVA: 0x00314F25 File Offset: 0x00313125
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[CommodityCodeTypes.StockCommodityCodeListAttribute]
		protected virtual void _(Events.CacheAttached<InventoryItem.commodityCodeType> e)
		{
		}

		// Token: 0x0600D789 RID: 55177 RVA: 0x00314F27 File Offset: 0x00313127
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<NotExists<Select2<TaxCategoryDet, InnerJoin<Tax, On<TaxCategoryDet.taxID, Equal<Tax.taxID>>>, Where<TaxCategory.taxCategoryID, Equal<TaxCategoryDet.taxCategoryID>, And<TaxCategory.taxCatFlag, Equal<False>, And<Tax.directTax, Equal<True>>>>>>>), null, new Type[]
		{

		})]
		protected virtual void _(Events.CacheAttached<InventoryItem.taxCategoryID> e)
		{
		}

		// Token: 0x0600D78A RID: 55178 RVA: 0x00314F29 File Offset: 0x00313129
		[AnyInventory(IsKey = true, DirtyRead = true, CacheGlobal = false)]
		[PXRestrictor(typeof(Where<InventoryItem.stkItem, Equal<boolTrue>>), "The inventory item is not a stock item.", new Type[]
		{

		})]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<SiteStatusByCostCenter.inventoryID> e)
		{
		}

		// Token: 0x0600D78B RID: 55179 RVA: 0x00314F2B File Offset: 0x0031312B
		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), "ValidateValueOnPersisting", true)]
		protected virtual void _(Events.CacheAttached<POVendorInventory.subItemID> e)
		{
		}

		// Token: 0x0600D78C RID: 55180 RVA: 0x00314F2D File Offset: 0x0031312D
		[StockItem(IsKey = true, DirtyRead = true, CacheGlobal = false, TabOrder = 1)]
		[PXParent(typeof(INItemSite.FK.InventoryItem))]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<INItemSite.inventoryID> e)
		{
		}

		// Token: 0x0600D78D RID: 55181 RVA: 0x00314F2F File Offset: 0x0031312F
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[ItemSite]
		[PXUIField(DisplayName = "Warehouse", Enabled = false, TabOrder = 2)]
		protected virtual void _(Events.CacheAttached<INItemSite.siteID> e)
		{
		}

		// Token: 0x0600D78E RID: 55182 RVA: 0x00314F31 File Offset: 0x00313131
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[INDefaultWarehouse(typeof(INItemSite.siteID), typeof(INItemSite.inventoryID))]
		protected virtual void _(Events.CacheAttached<INItemSite.isDefault> e)
		{
		}

		// Token: 0x0600D78F RID: 55183 RVA: 0x00314F33 File Offset: 0x00313133
		[StockItem(IsKey = true, DirtyRead = true)]
		[PXParent(typeof(INItemCategory.FK.InventoryItem))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		protected virtual void _(Events.CacheAttached<INItemCategory.inventoryID> e)
		{
		}

		// Token: 0x0600D790 RID: 55184 RVA: 0x00314F35 File Offset: 0x00313135
		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), "ValidateValueOnPersisting", true)]
		protected virtual void _(Events.CacheAttached<INItemXRef.subItemID> e)
		{
		}

		// Token: 0x0600D791 RID: 55185 RVA: 0x00314F37 File Offset: 0x00313137
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXBool]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<PMCostCode.isProjectOverride> e)
		{
		}

		// Token: 0x0600D792 RID: 55186 RVA: 0x00314F39 File Offset: 0x00313139
		protected IEnumerable groups()
		{
			return this.GetGroups();
		}

		// Token: 0x0600D793 RID: 55187 RVA: 0x00314F44 File Offset: 0x00313144
		public override void InitCacheMapping(Dictionary<Type, Type> map)
		{
			base.InitCacheMapping(map);
			this.Caches.AddCacheMapping(typeof(INSiteStatusByCostCenter), typeof(INSiteStatusByCostCenter));
			this.Caches.AddCacheMapping(typeof(SiteStatusByCostCenter), typeof(SiteStatusByCostCenter));
		}

		// Token: 0x0600D794 RID: 55188 RVA: 0x00314F98 File Offset: 0x00313198
		protected virtual void LotSerNumValueFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			InventoryItemLotSerNumVal inventoryItemLotSerNumVal = this.lotSerNumVal.Current = (InventoryItemLotSerNumVal)this.lotSerNumVal.View.SelectSingleBound(new object[]
			{
				e.Row
			}, Array.Empty<object>());
			e.ReturnState = this.lotSerNumVal.Cache.GetStateExt<InventoryItemLotSerNumVal.lotSerNumVal>(inventoryItemLotSerNumVal);
			INLotSerClass inlotSerClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(sender, e.Row);
			if (inlotSerClass != null && inlotSerClass.LotSerTrack != "N")
			{
				object returnValue;
				if (!inlotSerClass.LotSerNumShared.GetValueOrDefault())
				{
					returnValue = ((inventoryItemLotSerNumVal != null) ? inventoryItemLotSerNumVal.LotSerNumVal : null);
				}
				else
				{
					INLotSerClassLotSerNumVal inlotSerClassLotSerNumVal = INLotSerClassLotSerNumVal.PK.Find(sender.Graph, inlotSerClass.LotSerClassID, PKFindOptions.None);
					returnValue = ((inlotSerClassLotSerNumVal != null) ? inlotSerClassLotSerNumVal.LotSerNumVal : null);
				}
				e.ReturnValue = returnValue;
			}
		}

		// Token: 0x0600D795 RID: 55189 RVA: 0x00315060 File Offset: 0x00313260
		protected virtual void LotSerNumValueFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if ((InventoryItem)e.Row == null)
			{
				return;
			}
			string text = (string)e.NewValue;
			InventoryItemLotSerNumVal inventoryItemLotSerNumVal = (InventoryItemLotSerNumVal)this.lotSerNumVal.View.SelectSingleBound(new object[]
			{
				e.Row
			}, Array.Empty<object>());
			string a = (inventoryItemLotSerNumVal != null) ? inventoryItemLotSerNumVal.LotSerNumVal : null;
			if (!sender.ObjectsEqual(a, text))
			{
				if ((INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(sender, e.Row) == null)
				{
					return;
				}
				this.SetLotSerNumber(inventoryItemLotSerNumVal, text);
			}
		}

		// Token: 0x0600D796 RID: 55190 RVA: 0x003150E4 File Offset: 0x003132E4
		private void SetLotSerNumber(InventoryItemLotSerNumVal inventoryNumVal, string newNumber)
		{
			if (inventoryNumVal == null)
			{
				if (!string.IsNullOrEmpty(newNumber))
				{
					this.lotSerNumVal.Insert(new InventoryItemLotSerNumVal
					{
						LotSerNumVal = newNumber
					});
					return;
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(newNumber))
				{
					this.lotSerNumVal.Delete(inventoryNumVal);
					return;
				}
				InventoryItemLotSerNumVal inventoryItemLotSerNumVal = (InventoryItemLotSerNumVal)this.lotSerNumVal.Cache.CreateCopy(inventoryNumVal);
				inventoryItemLotSerNumVal.LotSerNumVal = newNumber;
				this.lotSerNumVal.Cache.Update(inventoryItemLotSerNumVal);
			}
		}

		// Token: 0x0600D797 RID: 55191 RVA: 0x0031515C File Offset: 0x0031335C
		protected override void _(Events.RowSelected<InventoryItem> e)
		{
			base._(e);
			if (e.Row == null)
			{
				return;
			}
			INLotSerClass inlotSerClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(e.Cache, e.Row);
			if (inlotSerClass == null)
			{
				PXUIFieldAttribute.SetEnabled<InventoryItemLotSerNumVal.lotSerNumVal>(this.lotSerNumVal.Cache, null, false);
			}
			else
			{
				PXUIFieldAttribute.SetEnabled<InventoryItemLotSerNumVal.lotSerNumVal>(this.lotSerNumVal.Cache, this.lotSerNumVal.Current, !inlotSerClass.LotSerNumShared.GetValueOrDefault() && inlotSerClass.LotSerTrack != "N");
			}
			e.Cache.AdjustUI(e.Row).For<InventoryItem.valMethod>(delegate(PXUIFieldAttribute fa)
			{
				fa.Enabled = (e.Row.TemplateItemID == null);
			}).SameFor<InventoryItem.kitItem>();
			PXCache cache = e.Cache;
			object row = e.Row;
			bool isEnabled;
			if (this.postclass.Current != null)
			{
				bool? cogssubFromSales = this.postclass.Current.COGSSubFromSales;
				bool flag = false;
				isEnabled = (cogssubFromSales.GetValueOrDefault() == flag & cogssubFromSales != null);
			}
			else
			{
				isEnabled = false;
			}
			PXUIFieldAttribute.SetEnabled<InventoryItem.cOGSSubID>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstVarAcctID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == "T");
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstVarSubID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == "T");
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstRevAcctID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == "T");
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstRevSubID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == "T");
			PXUIFieldAttribute.SetVisible<InventoryItem.defaultSubItemOnEntry>(e.Cache, null, this.insetup.Current.UseInventorySubItem.GetValueOrDefault());
			PXUIFieldAttribute.SetEnabled<InventoryItem.defaultSubItemID>(e.Cache, e.Row, this.insetup.Current.UseInventorySubItem.GetValueOrDefault());
			INAcctSubDefault.Required(e.Cache, e.Args);
			bool flag2 = this.nonemptysitestatuses.SelectSingle(Array.Empty<object>()) != null;
			PXUIFieldAttribute.SetEnabled<InventoryItem.baseUnit>(e.Cache, e.Row, !flag2 && e.Row.TemplateItemID == null);
			this.Boxes.Cache.AllowInsert = (e.Row.PackageOption != "N" && PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>());
			this.Boxes.Cache.AllowUpdate = (e.Row.PackageOption != "N" && PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>());
			this.Boxes.Cache.AllowSelect = (e.Row.PackageOption != "N" && PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>());
			PXUIFieldAttribute.SetEnabled<InventoryItem.packSeparately>(this.Item.Cache, this.Item.Current, e.Row.PackageOption == "W");
			PXUIFieldAttribute.SetVisible<INItemBoxEx.qty>(this.Boxes.Cache, null, e.Row.PackageOption == "Q");
			PXUIFieldAttribute.SetVisible<INItemBoxEx.uOM>(this.Boxes.Cache, null, e.Row.PackageOption == "Q");
			PXUIFieldAttribute.SetVisible<INItemBoxEx.maxQty>(this.Boxes.Cache, null, e.Row.PackageOption.IsIn("W", "V"));
			PXUIFieldAttribute.SetVisible<INItemBoxEx.maxWeight>(this.Boxes.Cache, null, e.Row.PackageOption.IsIn("W", "V"));
			PXUIFieldAttribute.SetVisible<INItemBoxEx.maxVolume>(this.Boxes.Cache, null, e.Row.PackageOption == "V");
			if (PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>())
			{
				this.ValidatePackaging(e.Row);
			}
			this.SetLastCostEnabled();
		}

		// Token: 0x0600D798 RID: 55192 RVA: 0x00315640 File Offset: 0x00313840
		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.lotSerClassID> e)
		{
			INLotSerClass inlotSerClass = INLotSerClass.PK.Find(this, e.Row.OrigLotSerClassID, PKFindOptions.None);
			if (inlotSerClass == null)
			{
				return;
			}
			INLotSerClass inlotSerClass2 = INLotSerClass.PK.Find(this, (string)e.NewValue, PKFindOptions.None);
			if (inlotSerClass.LotSerAssign == "U" && inlotSerClass.LotSerTrack.IsIn("S", "L") && (((inlotSerClass2 != null) ? inlotSerClass2.LotSerTrack : null) == "N" || ((inlotSerClass2 != null) ? inlotSerClass2.LotSerAssign : null) == "R"))
			{
				if (this.IsWhenUsedQtyStillPresent())
				{
					throw new PXSetPropertyException("Lot/serial class cannot be changed when its tracking method is not compatible with the previous class and the item is in use.");
				}
			}
			else
			{
				if (inlotSerClass2 != null && !(inlotSerClass.LotSerTrack != inlotSerClass2.LotSerTrack))
				{
					bool? lotSerTrackExpiration = inlotSerClass.LotSerTrackExpiration;
					bool? lotSerTrackExpiration2 = inlotSerClass2.LotSerTrackExpiration;
					if ((lotSerTrackExpiration.GetValueOrDefault() == lotSerTrackExpiration2.GetValueOrDefault() & lotSerTrackExpiration != null == (lotSerTrackExpiration2 != null)) && !(inlotSerClass.LotSerAssign != inlotSerClass2.LotSerAssign))
					{
						return;
					}
				}
				if (InventoryItemMaint.IsQtyStillPresent(this, e.Row.InventoryID))
				{
					throw new PXSetPropertyException("Lot/serial class cannot be changed when its tracking method is not compatible with the previous class and the item is in use.");
				}
			}
		}

		// Token: 0x0600D799 RID: 55193 RVA: 0x0031575E File Offset: 0x0031395E
		private bool IsWhenUsedQtyStillPresent()
		{
			return PXSelectBase<INSiteStatusByCostCenter, PXViewOf<INSiteStatusByCostCenter>.BasedOn<SelectFromBase<INSiteStatusByCostCenter, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INSiteStatusByCostCenter.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INSiteStatusByCostCenter>, InventoryItem, INSiteStatusByCostCenter>.SameAsCurrent.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INSiteStatusByCostCenter.qtyOnHand, NotEqual<decimal0>>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINReceipts, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyInTransit, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINIssues, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINAssemblyDemand, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINAssemblySupply, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtySOShipped, IBqlDecimal>.IsNotEqual<decimal0>>>>.Or<BqlOperand<INSiteStatusByCostCenter.qtySOShipping, IBqlDecimal>.IsNotEqual<decimal0>>>>>.ReadOnly.Config>.Select(this, Array.Empty<object>()) != null;
		}

		// Token: 0x0600D79A RID: 55194 RVA: 0x00315774 File Offset: 0x00313974
		public static bool IsQtyStillPresent(PXGraph graph, int? inventoryID)
		{
			INItemLotSerial initemLotSerial = PXSelectBase<INItemLotSerial, PXViewOf<INItemLotSerial>.BasedOn<SelectFromBase<INItemLotSerial, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemLotSerial.inventoryID, Equal<P.AsInt>>>>>.And<BqlOperand<INItemLotSerial.qtyOnHand, IBqlDecimal>.IsNotEqual<decimal0>>>>.Config>.SelectWindowed(graph, 0, 1, new object[]
			{
				inventoryID
			});
			INSiteStatusByCostCenter insiteStatusByCostCenter = PXSelectBase<INSiteStatusByCostCenter, PXViewOf<INSiteStatusByCostCenter>.BasedOn<SelectFromBase<INSiteStatusByCostCenter, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INSiteStatusByCostCenter.inventoryID, Equal<P.AsInt>>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INSiteStatusByCostCenter.qtyOnHand, NotEqual<decimal0>>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINReceipts, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyInTransit, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINIssues, IBqlDecimal>.IsNotEqual<decimal0>>>, Or<BqlOperand<INSiteStatusByCostCenter.qtyINAssemblyDemand, IBqlDecimal>.IsNotEqual<decimal0>>>>.Or<BqlOperand<INSiteStatusByCostCenter.qtyINAssemblySupply, IBqlDecimal>.IsNotEqual<decimal0>>>>>.Config>.SelectWindowed(graph, 0, 1, new object[]
			{
				inventoryID
			});
			return initemLotSerial != null || insiteStatusByCostCenter != null;
		}

		// Token: 0x0600D79B RID: 55195 RVA: 0x003157C4 File Offset: 0x003139C4
		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.defaultSubItemID> e)
		{
			if (this.IsImport)
			{
				e.Cancel = true;
			}
		}

		// Token: 0x0600D79C RID: 55196 RVA: 0x003157D8 File Offset: 0x003139D8
		protected virtual void _(Events.RowUpdated<InventoryItem> e)
		{
			this.UpdateItemSite(e.Row, e.OldRow);
			if (!e.Cache.ObjectsEqual<InventoryItem.lotSerClassID>(e.Row, e.OldRow))
			{
				INLotSerClass inlotSerClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(e.Cache, e.Row);
				if (inlotSerClass != null)
				{
					InventoryItemLotSerNumVal inventoryItemLotSerNumVal;
					if ((inventoryItemLotSerNumVal = this.lotSerNumVal.Current) == null)
					{
						inventoryItemLotSerNumVal = (InventoryItemLotSerNumVal)this.lotSerNumVal.View.SelectSingleBound(new object[]
						{
							inlotSerClass
						}, Array.Empty<object>());
					}
					InventoryItemLotSerNumVal inventoryItemLotSerNumVal2 = inventoryItemLotSerNumVal;
					if (inlotSerClass.LotSerTrack == "N")
					{
						this.SetLotSerNumber(inventoryItemLotSerNumVal2, null);
						return;
					}
					if (inventoryItemLotSerNumVal2 == null)
					{
						InventoryItemLotSerNumVal inventoryItemLotSerNumVal3 = this.lotSerNumVal.Cache.Deleted.OfType<InventoryItemLotSerNumVal>().FirstOrDefault<InventoryItemLotSerNumVal>();
						if (inventoryItemLotSerNumVal3 != null)
						{
							this.SetLotSerNumber(inventoryItemLotSerNumVal2, inventoryItemLotSerNumVal3.LotSerNumVal);
							return;
						}
					}
					else if (!string.IsNullOrEmpty(inventoryItemLotSerNumVal2.LotSerNumVal))
					{
						return;
					}
					this.SetLotSerNumber(inventoryItemLotSerNumVal2, "000000");
				}
			}
		}

		// Token: 0x0600D79D RID: 55197 RVA: 0x003158C8 File Offset: 0x00313AC8
		protected virtual void UpdateItemSite(InventoryItem item, InventoryItem oldItem)
		{
			if (this.Item.Cache.ObjectsEqual<InventoryItem.valMethod, InventoryItem.markupPct, InventoryItem.aBCCodeID, InventoryItem.aBCCodeIsFixed, InventoryItem.movementClassID, InventoryItem.movementClassIsFixed, InventoryItem.salesUnit>(item, oldItem) && this.Item.Cache.ObjectsEqual<InventoryItem.purchaseUnit, InventoryItem.productManagerID, InventoryItem.productWorkgroupID, InventoryItem.itemClassID, InventoryItem.planningMethod>(item, oldItem) && !this.itemsiterecords.Cache.Inserted.Any_())
			{
				return;
			}
			foreach (PXResult<INItemSite> r in PXSelectBase<INItemSite, PXViewOf<INItemSite>.BasedOn<SelectFromBase<INItemSite, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INItemSite.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemSite>, InventoryItem, INItemSite>.SameAsCurrent>>.Config>.Select(this, Array.Empty<object>()))
			{
				INItemSite initemSite = r;
				bool flag = false;
				if (!string.Equals(item.ValMethod, oldItem.ValMethod) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.ValMethod = item.ValMethod;
					flag = true;
				}
				bool? flag2 = initemSite.MarkupPctOverride;
				bool flag3 = false;
				if ((flag2.GetValueOrDefault() == flag3 & flag2 != null) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.MarkupPct = item.MarkupPct;
					flag = true;
				}
				flag2 = initemSite.ABCCodeOverride;
				flag3 = false;
				if ((flag2.GetValueOrDefault() == flag3 & flag2 != null) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.ABCCodeID = item.ABCCodeID;
					initemSite.ABCCodeIsFixed = item.ABCCodeIsFixed;
					flag = true;
				}
				flag2 = initemSite.MovementClassOverride;
				flag3 = false;
				if ((flag2.GetValueOrDefault() == flag3 & flag2 != null) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.MovementClassID = item.MovementClassID;
					initemSite.MovementClassIsFixed = item.MovementClassIsFixed;
					flag = true;
				}
				if (!string.Equals(item.SalesUnit, oldItem.SalesUnit) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.DfltSalesUnit = item.SalesUnit;
					flag = true;
				}
				if (!string.Equals(item.PurchaseUnit, oldItem.PurchaseUnit) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.DfltPurchaseUnit = item.PurchaseUnit;
					flag = true;
				}
				if (!initemSite.ProductManagerOverride.GetValueOrDefault())
				{
					int? num = initemSite.ProductManagerID;
					int? num2 = item.ProductManagerID;
					if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
					{
						num2 = initemSite.ProductWorkgroupID;
						num = item.ProductWorkgroupID;
						if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
						{
							goto IL_277;
						}
					}
					initemSite.ProductManagerID = item.ProductManagerID;
					initemSite.ProductWorkgroupID = item.ProductWorkgroupID;
					flag = true;
				}
				IL_277:
				if (!string.Equals(item.PlanningMethod, oldItem.PlanningMethod))
				{
					initemSite.PlanningMethod = item.PlanningMethod;
					flag = true;
				}
				if (flag)
				{
					this.itemsiterecords.Cache.MarkUpdated(initemSite, true);
				}
			}
		}

		// Token: 0x0600D79E RID: 55198 RVA: 0x00315BB8 File Offset: 0x00313DB8
		protected virtual void _(Events.RowInserted<InventoryItemCurySettings> e)
		{
			this.UpdateItemSiteByCurySettings(e.Row);
		}

		// Token: 0x0600D79F RID: 55199 RVA: 0x00315BC8 File Offset: 0x00313DC8
		protected virtual void _(Events.RowUpdated<InventoryItemCurySettings> e)
		{
			if (this.ItemCurySettings.Cache.ObjectsEqual<InventoryItemCurySettings.pendingStdCost, InventoryItemCurySettings.pendingStdCostDate, InventoryItemCurySettings.stdCost, InventoryItemCurySettings.basePrice, InventoryItemCurySettings.recPrice, InventoryItemCurySettings.preferredVendorID, InventoryItemCurySettings.preferredVendorLocationID>(e.Row, e.OldRow) && !this.itemsiterecords.Cache.Inserted.Any_())
			{
				return;
			}
			this.UpdateItemSiteByCurySettings(e.Row);
		}

		// Token: 0x0600D7A0 RID: 55200 RVA: 0x00315C1C File Offset: 0x00313E1C
		protected virtual void UpdateItemSiteByCurySettings(InventoryItemCurySettings itemCurySettings)
		{
			foreach (PXResult<INItemSite> pxresult in this.itemsiterecords.Select(new object[]
			{
				itemCurySettings.CuryID
			}))
			{
				INItemSite initemSite = (PXResult<INItemSite, INSite, INSiteStatusSummary>)pxresult;
				bool flag = false;
				bool? flag2;
				bool flag3;
				if (initemSite.ValMethod == "T")
				{
					flag2 = initemSite.StdCostOverride;
					flag3 = false;
					if (flag2.GetValueOrDefault() == flag3 & flag2 != null)
					{
						if (itemCurySettings.PendingStdCostDate != null)
						{
							initemSite.PendingStdCost = itemCurySettings.PendingStdCost;
							initemSite.PendingStdCostDate = itemCurySettings.PendingStdCostDate;
							initemSite.PendingStdCostReset = new bool?(false);
						}
						else
						{
							decimal? stdCost = itemCurySettings.StdCost;
							decimal? stdCost2 = initemSite.StdCost;
							bool flag4 = stdCost.GetValueOrDefault() == stdCost2.GetValueOrDefault() & stdCost != null == (stdCost2 != null);
							initemSite.PendingStdCost = (flag4 ? itemCurySettings.PendingStdCost : itemCurySettings.StdCost);
							initemSite.PendingStdCostDate = null;
							initemSite.PendingStdCostReset = new bool?(!flag4);
						}
						flag = true;
					}
				}
				flag2 = initemSite.BasePriceOverride;
				flag3 = false;
				if ((flag2.GetValueOrDefault() == flag3 & flag2 != null) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.BasePrice = itemCurySettings.BasePrice;
					flag = true;
				}
				flag2 = initemSite.RecPriceOverride;
				flag3 = false;
				if ((flag2.GetValueOrDefault() == flag3 & flag2 != null) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.RecPrice = itemCurySettings.RecPrice;
					flag = true;
				}
				flag2 = initemSite.PreferredVendorOverride;
				flag3 = false;
				if ((flag2.GetValueOrDefault() == flag3 & flag2 != null) || this.itemsiterecords.Cache.GetStatus(initemSite) == PXEntryStatus.Inserted)
				{
					initemSite.PreferredVendorID = itemCurySettings.PreferredVendorID;
					initemSite.PreferredVendorLocationID = itemCurySettings.PreferredVendorLocationID;
					flag = true;
				}
				if (flag)
				{
					this.itemsiterecords.Cache.MarkUpdated(initemSite, true);
				}
			}
		}

		// Token: 0x0600D7A1 RID: 55201 RVA: 0x00315E58 File Offset: 0x00314058
		protected virtual void _(Events.FieldUpdated<InventoryItemCurySettings, InventoryItemCurySettings.dfltSiteID> e)
		{
			INItemSite initemSite = PXSelectBase<INItemSite, PXViewOf<INItemSite>.BasedOn<SelectFromBase<INItemSite, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemSite.inventoryID, Equal<BqlField<InventoryItem.inventoryID, IBqlInt>.FromCurrent>>>>>.And<BqlOperand<INItemSite.siteID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(this, new object[]
			{
				e.Row.DfltSiteID
			});
			INSite insite = INSite.PK.Find(this, e.Row.DfltSiteID, PKFindOptions.None);
			if (initemSite != null)
			{
				initemSite = PXCache<INItemSite>.CreateCopy(initemSite);
				initemSite.IsDefault = new bool?(true);
				this.itemsiterecords.Update(initemSite);
				e.Row.DfltShipLocationID = initemSite.DfltShipLocationID;
				e.Row.DfltReceiptLocationID = initemSite.DfltReceiptLocationID;
				e.Row.DfltPutawayLocationID = initemSite.DfltPutawayLocationID;
				return;
			}
			if (insite != null)
			{
				initemSite = new INItemSite();
				initemSite.InventoryID = e.Row.InventoryID;
				initemSite.SiteID = e.Row.DfltSiteID;
				INItemSiteMaint.DefaultItemSiteByItem(this, initemSite, this.Item.Current, insite, this.postclass.Current, e.Row);
				initemSite.IsDefault = new bool?(true);
				initemSite.StdCostOverride = new bool?(false);
				initemSite.DfltReceiptLocationID = insite.ReceiptLocationID;
				initemSite.DfltShipLocationID = insite.ShipLocationID;
				initemSite.DfltPutawayLocationID = null;
				this.itemsiterecords.Insert(initemSite);
				e.Row.DfltShipLocationID = initemSite.DfltShipLocationID;
				e.Row.DfltReceiptLocationID = initemSite.DfltReceiptLocationID;
				return;
			}
			e.Row.DfltShipLocationID = null;
			e.Row.DfltReceiptLocationID = null;
			e.Row.DfltPutawayLocationID = null;
			foreach (PXResult<INItemSite> r in this.itemsiterecords.Select(new object[]
			{
				e.Row.CuryID
			}))
			{
				INItemSite initemSite2 = r;
				if (initemSite2.IsDefault.GetValueOrDefault())
				{
					initemSite2.IsDefault = new bool?(false);
					this.itemsiterecords.Cache.MarkUpdated(initemSite2, true);
				}
			}
		}

		// Token: 0x0600D7A2 RID: 55202 RVA: 0x0031607C File Offset: 0x0031427C
		protected override void _(Events.RowPersisting<InventoryItem> e)
		{
			base._(e);
			INAcctSubDefault.Required(e.Cache, e.Args);
			if (e.Row.IsSplitted.GetValueOrDefault())
			{
				if (string.IsNullOrEmpty(e.Row.DeferredCode) && e.Cache.RaiseExceptionHandling<InventoryItem.deferredCode>(e.Row, e.Row.DeferredCode, new PXSetPropertyException("'{0}' cannot be empty.", new object[]
				{
					"[deferredCode]"
				})))
				{
					throw new PXRowPersistingException("deferredCode", e.Row.DeferredCode, "'{0}' cannot be empty.", new object[]
					{
						"deferredCode"
					});
				}
				List<INComponent> components = this.Components.Select(Array.Empty<object>()).RowCast<INComponent>().ToList<INComponent>();
				InventoryItemMaintBase.VerifyComponentPercentages(e.Cache, e.Row, components);
				InventoryItemMaintBase.VerifyOnlyOneResidualComponent(this.Components.Cache, components);
				InventoryItemMaintBase.CheckSameTermOnAllComponents(this.Components.Cache, components);
			}
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update) && e.Row.ValMethod == "S" && this.lotserclass.Current != null && (this.lotserclass.Current.LotSerTrack == "N" || this.lotserclass.Current.LotSerAssign != "R") && e.Cache.RaiseExceptionHandling<InventoryItem.valMethod>(e.Row, "S", new PXSetPropertyException("Specific valuated items should be lot or serial numbered during receipt.")))
			{
				throw new PXRowPersistingException(typeof(InventoryItem.valMethod).Name, "S", "Specific valuated items should be lot or serial numbered during receipt.", new object[]
				{
					typeof(InventoryItem.valMethod).Name
				});
			}
			if (e.Operation.Command() == PXDBOperation.Delete)
			{
				PXDatabase.Delete<INSiteStatusByCostCenter>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<INSiteStatusByCostCenter.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INSiteStatusByCostCenter.qtyOnHand>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ),
					new PXDataFieldRestrict<INSiteStatusByCostCenter.qtyAvail>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ)
				});
				PXDatabase.Delete<INLocationStatusByCostCenter>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<INLocationStatusByCostCenter.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INLocationStatusByCostCenter.qtyOnHand>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ),
					new PXDataFieldRestrict<INLocationStatusByCostCenter.qtyAvail>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ)
				});
				PXDatabase.Delete<INLotSerialStatusByCostCenter>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<INLotSerialStatusByCostCenter.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INLotSerialStatusByCostCenter.qtyOnHand>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ),
					new PXDataFieldRestrict<INLotSerialStatusByCostCenter.qtyAvail>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ)
				});
				PXDatabase.Delete<INCostStatus>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<INCostStatus.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INCostStatus.qtyOnHand>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ)
				});
				PXDatabase.Delete<INItemCostHist>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<INItemCostHist.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INItemCostHist.finYtdQty>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ),
					new PXDataFieldRestrict<INItemCostHist.finYtdCost>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ)
				});
				PXDatabase.Delete<INItemSiteHist>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<INItemSiteHist.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INItemSiteHist.finYtdQty>(PXDbType.Decimal, new int?(8), 0m, PXComp.EQ)
				});
				PXDatabase.Delete<CSAnswers>(new PXDataFieldRestrict[]
				{
					new PXDataFieldRestrict<CSAnswers.refNoteID>(PXDbType.UniqueIdentifier, e.Row.NoteID)
				});
			}
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INLotSerClass inlotSerClass = this.lotserclass.Current;
				if (inlotSerClass != null && inlotSerClass.LotSerTrack != "N")
				{
					bool? lotSerNumShared = inlotSerClass.LotSerNumShared;
					bool flag = false;
					if (lotSerNumShared.GetValueOrDefault() == flag & lotSerNumShared != null)
					{
						PXStringState pxstringState = (PXStringState)e.Cache.GetValueExt(e.Row, "LotSerNumVal");
						if ((pxstringState == null || pxstringState.Value == null) && PXSelectBase<INLotSerSegment, PXViewOf<INLotSerSegment>.BasedOn<SelectFromBase<INLotSerSegment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INLotSerSegment.lotSerClassID, Equal<P.AsString>>>>>.And<BqlOperand<INLotSerSegment.segmentType, IBqlString>.IsEqual<P.AsString.ASCII>>>>.ReadOnly.Config>.Select(this, new object[]
						{
							inlotSerClass.LotSerClassID,
							"N"
						}) != null)
						{
							PXSetPropertyException ex = new PXSetPropertyException("'{0}' cannot be empty.", new object[]
							{
								pxstringState.DisplayName
							});
							PXUIFieldAttribute.SetError<InventoryItemLotSerNumVal.lotSerNumVal>(this.lotSerNumVal.Cache, null, ex.Message);
						}
					}
				}
			}
			if (e.Operation.Command() == PXDBOperation.Update && e.Row.LotSerClassID != e.Row.OrigLotSerClassID && this.lotserclass.Current != null && this.lotserclass.Current.LotSerAssign != "U")
			{
				INLotSerClass inlotSerClass2 = INLotSerClass.PK.Find(this, e.Row.OrigLotSerClassID, PKFindOptions.None);
				if (((inlotSerClass2 != null) ? inlotSerClass2.LotSerAssign : null) == "U")
				{
					PXDatabase.Delete<INItemLotSerial>(new PXDataFieldRestrict[]
					{
						new PXDataFieldRestrict<INItemLotSerial.inventoryID>(PXDbType.Int, e.Row.InventoryID),
						new PXDataFieldRestrict<INItemLotSerial.lotSerAssign>(PXDbType.Char, new int?(1), "U", PXComp.EQ)
					});
					PXDatabase.Delete<INSiteLotSerial>(new PXDataFieldRestrict[]
					{
						new PXDataFieldRestrict<INSiteLotSerial.inventoryID>(PXDbType.Int, e.Row.InventoryID),
						new PXDataFieldRestrict<INSiteLotSerial.lotSerAssign>(PXDbType.Char, new int?(1), "U", PXComp.EQ)
					});
				}
			}
		}

		// Token: 0x0600D7A3 RID: 55203 RVA: 0x00316642 File Offset: 0x00314842
		protected virtual void _(Events.FieldUpdated<InventoryItemCurySettings, InventoryItemCurySettings.dfltReceiptLocationID> e)
		{
			this.UpdateItemSiteDefaultField<InventoryItemCurySettings.dfltReceiptLocationID, INItemSite.dfltReceiptLocationID>(e.Row);
		}

		// Token: 0x0600D7A4 RID: 55204 RVA: 0x00316650 File Offset: 0x00314850
		protected virtual void _(Events.FieldUpdated<InventoryItemCurySettings, InventoryItemCurySettings.dfltPutawayLocationID> e)
		{
			this.UpdateItemSiteDefaultField<InventoryItemCurySettings.dfltPutawayLocationID, INItemSite.dfltPutawayLocationID>(e.Row);
		}

		// Token: 0x0600D7A5 RID: 55205 RVA: 0x0031665E File Offset: 0x0031485E
		protected virtual void _(Events.FieldUpdated<InventoryItemCurySettings, InventoryItemCurySettings.dfltShipLocationID> e)
		{
			this.UpdateItemSiteDefaultField<InventoryItemCurySettings.dfltShipLocationID, INItemSite.dfltShipLocationID>(e.Row);
		}

		// Token: 0x0600D7A6 RID: 55206 RVA: 0x0031666C File Offset: 0x0031486C
		protected virtual void UpdateItemSiteDefaultField<TSourceField, TDestinationField>(InventoryItemCurySettings itemCurySettings) where TSourceField : BqlType<IBqlInt, int>.Field<TSourceField> where TDestinationField : BqlType<IBqlInt, int>.Field<TDestinationField>
		{
			int? dfltSiteID = itemCurySettings.DfltSiteID;
			if (dfltSiteID != null)
			{
				int valueOrDefault = dfltSiteID.GetValueOrDefault();
				INItemSite initemSite = PXSelectBase<INItemSite, PXViewOf<INItemSite>.BasedOn<SelectFromBase<INItemSite, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemSite.inventoryID, Equal<BqlField<InventoryItem.inventoryID, IBqlInt>.FromCurrent>>>>>.And<BqlOperand<INItemSite.siteID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(this, new object[]
				{
					valueOrDefault
				});
				if (initemSite != null)
				{
					this.itemsiterecords.Cache.SetValue<TDestinationField>(initemSite, this.Caches<InventoryItemCurySettings>().GetValue<TSourceField>(itemCurySettings));
					this.itemsiterecords.Cache.MarkUpdated(initemSite, true);
				}
				return;
			}
		}

		// Token: 0x0600D7A7 RID: 55207 RVA: 0x003166E1 File Offset: 0x003148E1
		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.defaultSubItemID> e)
		{
			this.AddVendorDetail(e.Row, null);
		}

		// Token: 0x0600D7A8 RID: 55208 RVA: 0x003166F1 File Offset: 0x003148F1
		protected virtual void _(Events.FieldUpdated<InventoryItemCurySettings, InventoryItemCurySettings.preferredVendorLocationID> e)
		{
			this.AddVendorDetail(null, e.Row);
		}

		// Token: 0x0600D7A9 RID: 55209 RVA: 0x00316704 File Offset: 0x00314904
		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.planningMethod> e)
		{
			if (e.Cache.ObjectsEqual<InventoryItem.planningMethod>(e.OldValue, e.NewValue))
			{
				return;
			}
			foreach (PXResult<INItemSite> r in this.itemsiterecords.Select(Array.Empty<object>()))
			{
				INItemSite initemSite = r;
				if (e.NewValue.Equals("N"))
				{
					initemSite.ReplenishmentMethod = "N";
				}
				initemSite.PlanningMethod = (string)e.NewValue;
				this.itemsiterecords.Update(initemSite);
			}
		}

		// Token: 0x0600D7AA RID: 55210 RVA: 0x003167B0 File Offset: 0x003149B0
		private POVendorInventory AddVendorDetail(InventoryItem row, InventoryItemCurySettings curySettings)
		{
			if (row == null)
			{
				row = this.Item.Current;
			}
			if (row != null && curySettings == null)
			{
				curySettings = this.ItemCurySettings.SelectSingle(new object[]
				{
					row.InventoryID
				});
			}
			if (curySettings == null || curySettings.PreferredVendorID == null || (row == null || row.DefaultSubItemID == null))
			{
				return null;
			}
			POVendorInventory povendorInventory = PXSelectBase<POVendorInventory, PXViewOf<POVendorInventory>.BasedOn<SelectFromBase<POVendorInventory, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<POVendorInventory.inventoryID, Equal<P.AsInt>>>>, And<BqlOperand<POVendorInventory.subItemID, IBqlInt>.IsEqual<P.AsInt>>>, And<BqlOperand<POVendorInventory.vendorID, IBqlInt>.IsEqual<P.AsInt>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<POVendorInventory.vendorLocationID, Equal<P.AsInt>>>>>.Or<BqlOperand<POVendorInventory.vendorLocationID, IBqlInt>.IsNull>>>>.Config>.SelectWindowed(this, 0, 1, new object[]
			{
				row.InventoryID,
				row.DefaultSubItemID,
				curySettings.PreferredVendorID,
				curySettings.PreferredVendorLocationID
			});
			if (povendorInventory == null)
			{
				povendorInventory = new POVendorInventory
				{
					InventoryID = row.InventoryID,
					SubItemID = row.DefaultSubItemID,
					PurchaseUnit = row.PurchaseUnit,
					VendorID = curySettings.PreferredVendorID,
					VendorLocationID = curySettings.PreferredVendorLocationID
				};
				povendorInventory = (POVendorInventory)this.VendorItems.Cache.Insert(povendorInventory);
			}
			return povendorInventory;
		}

		// Token: 0x0600D7AB RID: 55211 RVA: 0x003168D4 File Offset: 0x00314AD4
		protected override void _(Events.FieldUpdated<InventoryItem, InventoryItem.itemClassID> e)
		{
			base._(e);
			InventoryItem row = e.Row;
			bool flag = row != null && row.IsConversionMode.GetValueOrDefault();
			if (flag)
			{
				e.Cache.SetDefaultExt<InventoryItem.itemType>(e.Row);
			}
			if (this.doResetDefaultsOnItemClassChange || flag)
			{
				e.Cache.SetDefaultExt<InventoryItem.lotSerClassID>(e.Row);
				if (this.doResetDefaultsOnItemClassChange)
				{
					if (flag)
					{
						InventoryItem row2 = e.Row;
						if (((row2 != null) ? row2.ValMethod : null) != null)
						{
							goto IL_84;
						}
					}
					e.Cache.SetDefaultExt<InventoryItem.valMethod>(e.Row);
				}
				IL_84:
				e.Cache.SetDefaultExt<InventoryItem.countryOfOrigin>(e.Row);
				e.Cache.SetDefaultExt<InventoryItem.hSTariffCode>(e.Row);
				e.Cache.SetDefaultExt<InventoryItem.planningMethod>(e.Row);
				e.Cache.SetDefaultExt<InventoryItem.replenishmentSource>(e.Row);
			}
			this.AppendGroupMask(e.Row.ItemClassID, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted || flag);
			if (e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted || flag)
			{
				foreach (PXResult<INItemRep> r in PXSelectBase<INItemRep, PXViewOf<INItemRep>.BasedOn<SelectFromBase<INItemRep, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemRep.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(this, new object[]
				{
					e.Row.InventoryID
				}))
				{
					INItemRep item = r;
					this.replenishment.Delete(item);
				}
				foreach (PXResult<INItemClassRep> r2 in PXSelectBase<INItemClassRep, PXViewOf<INItemClassRep>.BasedOn<SelectFromBase<INItemClassRep, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemClassRep.itemClassID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(this, new object[]
				{
					e.Row.ParentItemClassID
				}))
				{
					INItemClassRep initemClassRep = r2;
					INItemRep item2 = new INItemRep
					{
						ReplenishmentClassID = initemClassRep.ReplenishmentClassID,
						ReplenishmentMethod = initemClassRep.ReplenishmentMethod,
						ReplenishmentPolicyID = initemClassRep.ReplenishmentPolicyID,
						LaunchDate = initemClassRep.LaunchDate,
						TerminationDate = initemClassRep.TerminationDate,
						CuryID = initemClassRep.CuryID
					};
					this.replenishment.Insert(item2);
				}
			}
		}

		// Token: 0x0600D7AC RID: 55212 RVA: 0x00316B0C File Offset: 0x00314D0C
		protected override void _(Events.RowInserted<InventoryItem> e)
		{
			e.Row.TotalPercentage = new decimal?(100);
			using (new ReadOnlyScope(new PXCache[]
			{
				this.replenishment.Cache
			}))
			{
				foreach (PXResult<INItemClassRep> r in PXSelectBase<INItemClassRep, PXViewOf<INItemClassRep>.BasedOn<SelectFromBase<INItemClassRep, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemClassRep.itemClassID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(this, new object[]
				{
					e.Row.ParentItemClassID
				}))
				{
					INItemClassRep initemClassRep = r;
					INItemRep item = new INItemRep
					{
						ReplenishmentClassID = initemClassRep.ReplenishmentClassID,
						ReplenishmentMethod = initemClassRep.ReplenishmentMethod,
						ReplenishmentPolicyID = initemClassRep.ReplenishmentPolicyID,
						LaunchDate = initemClassRep.LaunchDate,
						TerminationDate = initemClassRep.TerminationDate,
						CuryID = initemClassRep.CuryID
					};
					this.replenishment.Insert(item);
				}
			}
			base._(e);
			this.AppendGroupMask(e.Row.ItemClassID, true);
			this._JustInserted = true;
		}

		// Token: 0x0600D7AD RID: 55213 RVA: 0x00316C34 File Offset: 0x00314E34
		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.purchaseUnit> e)
		{
			if (e.Row == null || string.Equals(e.Row.PurchaseUnit, (string)e.OldValue, StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}
			IEnumerable<POVendorInventory> enumerable = PXSelectBase<POVendorInventory, PXViewOf<POVendorInventory>.BasedOn<SelectFromBase<POVendorInventory, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<POVendorInventory.inventoryID, Equal<P.AsInt>>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<POVendorInventory.purchaseUnit, Equal<P.AsString>>>>>.Or<BqlOperand<POVendorInventory.purchaseUnit, IBqlString>.IsEqual<P.AsString>>>>>.Config>.Select(this, new object[]
			{
				e.Row.InventoryID,
				e.Row.PurchaseUnit,
				e.OldValue
			}).AsEnumerable<PXResult<POVendorInventory>>().RowCast<POVendorInventory>();
			IEnumerable<POVendorInventory> source = enumerable;
			Func<POVendorInventory, bool> <>9__0;
			Func<POVendorInventory, bool> predicate;
			if ((predicate = <>9__0) == null)
			{
				predicate = (<>9__0 = ((POVendorInventory x) => x.PurchaseUnit == (string)e.OldValue));
			}
			using (IEnumerator<POVendorInventory> enumerator = source.Where(predicate).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					POVendorInventory detailWithOldPurchaseUnit = enumerator.Current;
					if (enumerable.FirstOrDefault(delegate(POVendorInventory x)
					{
						if (x.PurchaseUnit == e.Row.PurchaseUnit)
						{
							int? vendorID = x.VendorID;
							int? vendorID2 = detailWithOldPurchaseUnit.VendorID;
							return vendorID.GetValueOrDefault() == vendorID2.GetValueOrDefault() & vendorID != null == (vendorID2 != null);
						}
						return false;
					}) == null)
					{
						if (detailWithOldPurchaseUnit.LastPrice != null)
						{
							detailWithOldPurchaseUnit.LastPrice = new decimal?(POItemCostManager.ConvertUOM(this, e.Row, (string)e.OldValue, detailWithOldPurchaseUnit.LastPrice.Value, e.Row.PurchaseUnit));
						}
						detailWithOldPurchaseUnit.PurchaseUnit = e.Row.PurchaseUnit;
						this.VendorItems.Update(detailWithOldPurchaseUnit);
					}
				}
			}
		}

		// Token: 0x0600D7AE RID: 55214 RVA: 0x00316E14 File Offset: 0x00315014
		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.valMethod> e)
		{
			if (e.Row.ValMethod != null && !string.Equals(e.Row.ValMethod, (string)e.NewValue) && (!e.Row.IsConversionMode.GetValueOrDefault() || e.NewValue != null))
			{
				INCostStatus incostStatus = (from layer in PXSelectBase<INCostStatus, PXViewOf<INCostStatus>.BasedOn<SelectFromBase<INCostStatus, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INCostStatus.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INCostStatus>, InventoryItem, INCostStatus>.SameAsCurrent.And<BqlOperand<INCostStatus.qtyOnHand, IBqlDecimal>.IsNotEqual<decimal0>>>>.ReadOnly.Config>.Select(this, Array.Empty<object>())
				orderby (((INCostStatus)layer).CostSiteID == this.insetup.Current.TransitSiteID) ? 1 : 0
				select layer).FirstOrDefault<PXResult<INCostStatus>>();
				if (incostStatus != null)
				{
					INValMethod.ListAttribute listAttribute = e.Cache.GetAttributesReadonly<InventoryItem.valMethod>(e.Row).OfType<INValMethod.ListAttribute>().First<INValMethod.ListAttribute>();
					string text;
					listAttribute.ValueLabelDic.TryGetValue(e.Row.ValMethod, out text);
					string text2;
					listAttribute.ValueLabelDic.TryGetValue((string)e.NewValue, out text2);
					int? costSiteID = incostStatus.CostSiteID;
					int? transitSiteID = this.insetup.Current.TransitSiteID;
					throw new PXSetPropertyException((costSiteID.GetValueOrDefault() == transitSiteID.GetValueOrDefault() & costSiteID != null == (transitSiteID != null)) ? "The valuation method cannot be changed from {0} to {1} because the item is in transit." : "Valuation method cannot be changed from '{0}' to '{1}' while stock is not zero.", new object[]
					{
						text,
						text2
					});
				}
			}
		}

		// Token: 0x0600D7AF RID: 55215 RVA: 0x00317018 File Offset: 0x00315218
		protected virtual void _(Events.RowDeleting<InventoryItem> e)
		{
			if (e.Row == null)
			{
				return;
			}
			INSiteStatusByCostCenter insiteStatusByCostCenter = this.nonemptysitestatuses.SelectSingle(Array.Empty<object>());
			if (insiteStatusByCostCenter != null)
			{
				throw new PXException("There is a non-zero quantity of the '{0}' item at the '{1}' warehouse.", new object[]
				{
					e.Row.InventoryCD,
					this.nonemptysitestatuses.GetValueExt<INSiteStatusByCostCenter.siteID>(insiteStatusByCostCenter)
				});
			}
		}

		// Token: 0x0600D7B0 RID: 55216 RVA: 0x00317070 File Offset: 0x00315270
		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.packageOption> e)
		{
			if (e.Row != null && e.NewValue.ToString() == "Q" && this.Boxes.Select(Array.Empty<object>()).Count == 0)
			{
				e.Cache.RaiseExceptionHandling<InventoryItem.packageOption>(e.Row, e.NewValue, new PXSetPropertyException("At least one box must be specified in the Boxes grid for the given packaging option.", PXErrorLevel.Warning));
			}
		}

		// Token: 0x0600D7B1 RID: 55217 RVA: 0x003170D8 File Offset: 0x003152D8
		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.taxCategoryID> e)
		{
			string text = e.NewValue as string;
			if (e.Row == null || string.IsNullOrEmpty(text))
			{
				return;
			}
			TaxCategoryDet taxCategoryDet = PXSelectBase<TaxCategoryDet, PXViewOf<TaxCategoryDet>.BasedOn<SelectFromBase<TaxCategoryDet, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<TaxCategory>.On<BqlOperand<TaxCategory.taxCategoryID, IBqlString>.IsEqual<TaxCategoryDet.taxCategoryID>>>, FbqlJoins.Inner<Tax>.On<BqlOperand<Tax.taxID, IBqlString>.IsEqual<TaxCategoryDet.taxID>>>>.Where<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<TaxCategoryDet.taxCategoryID, Equal<P.AsString>>>>, And<BqlOperand<TaxCategory.taxCatFlag, IBqlBool>.IsEqual<False>>>>.And<BqlOperand<Tax.directTax, IBqlBool>.IsEqual<True>>>>.Config>.Select(this, new object[]
			{
				text
			});
			if (taxCategoryDet != null)
			{
				throw new PXSetPropertyException("The {0} tax category containing the {1} direct-entry tax cannot be selected for a stock item.", new object[]
				{
					taxCategoryDet.TaxCategoryID,
					taxCategoryDet.TaxID
				});
			}
		}

		// Token: 0x0600D7B2 RID: 55218 RVA: 0x00317140 File Offset: 0x00315340
		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.packageOption> e)
		{
			if (e.Row == null)
			{
				return;
			}
			if (e.Row.PackageOption == "Q")
			{
				e.Row.PackSeparately = new bool?(true);
				return;
			}
			if (e.Row.PackageOption == "V")
			{
				e.Row.PackSeparately = new bool?(false);
				return;
			}
			if (e.Row.PackageOption == "N")
			{
				e.Row.PackSeparately = new bool?(false);
				foreach (PXResult<INItemBoxEx> r in this.Boxes.Select(Array.Empty<object>()))
				{
					INItemBoxEx item = r;
					this.Boxes.Delete(item);
				}
			}
		}

		// Token: 0x0600D7B3 RID: 55219 RVA: 0x00317228 File Offset: 0x00315428
		protected virtual void _(Events.RowPersisted<InventoryItem> e)
		{
			if (e.TranStatus == PXTranStatus.Completed)
			{
				DiscountEngine.RemoveFromCachedInventoryPriceClasses(e.Row.InventoryID);
			}
		}

		// Token: 0x0600D7B4 RID: 55220 RVA: 0x00317243 File Offset: 0x00315443
		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.valMethod> e)
		{
			this.ItemCurySettings.Cache.RaiseRowSelected(null);
		}

		// Token: 0x0600D7B5 RID: 55221 RVA: 0x00317258 File Offset: 0x00315458
		protected virtual void _(Events.RowSelected<INItemCost> e)
		{
			this.SetLastCostEnabled();
			INItemCost row = e.Row;
			if (row != null)
			{
				decimal? avgCost = row.AvgCost;
				decimal d = 0m;
				if (avgCost.GetValueOrDefault() < d & avgCost != null)
				{
					this.ItemCosts.Cache.RaiseExceptionHandling<INItemCost.avgCost>(row, row.AvgCost, new PXSetPropertyException("Negative average cost is caused by a negative quantity of the item in one of its warehouses.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x0600D7B6 RID: 55222 RVA: 0x003172C4 File Offset: 0x003154C4
		protected virtual void SetLastCostEnabled()
		{
			if (this.Item.Current == null)
			{
				return;
			}
			Lazy<bool> lazy = Lazy.By<bool>(delegate
			{
				object[] currents = new InventoryItem[]
				{
					this.Item.Current
				};
				return PXSelectBase<INItemSite, PXViewOf<INItemSite>.BasedOn<SelectFromBase<INItemSite, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INItemSite.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemSite>, InventoryItem, INItemSite>.SameAsCurrent>>.Config>.SelectSingleBound(this, currents, Array.Empty<object>()).AsEnumerable<PXResult<INItemSite>>().Any<PXResult<INItemSite>>();
			});
			bool isEnabled = this.Item.Current.ValMethod.IsNotIn("T", "S") && (!PXAccess.FeatureInstalled<FeaturesSet.warehouse>() || lazy.Value);
			PXUIFieldAttribute.SetEnabled<INItemCost.lastCost>(this.ItemCosts.Cache, null, isEnabled);
		}

		// Token: 0x0600D7B7 RID: 55223 RVA: 0x00317338 File Offset: 0x00315538
		protected virtual void _(Events.RowInserted<INItemCost> e)
		{
			if (e.Row != null)
			{
				decimal? lastCost = e.Row.LastCost;
				decimal d = 0m;
				if (!(lastCost.GetValueOrDefault() == d & lastCost != null) && e.Row.LastCost != null)
				{
					this.UpdateLastCost(e.Row);
				}
			}
		}

		// Token: 0x0600D7B8 RID: 55224 RVA: 0x0031739C File Offset: 0x0031559C
		protected virtual void _(Events.RowUpdated<INItemCost> e)
		{
			if (e.Row != null && e.OldRow != null)
			{
				decimal? lastCost = e.Row.LastCost;
				decimal? lastCost2 = e.OldRow.LastCost;
				if (!(lastCost.GetValueOrDefault() == lastCost2.GetValueOrDefault() & lastCost != null == (lastCost2 != null)) && e.Row.LastCost != null)
				{
					this.UpdateLastCost(e.Row);
				}
			}
		}

		// Token: 0x0600D7B9 RID: 55225 RVA: 0x00317418 File Offset: 0x00315618
		private void UpdateLastCost(INItemCost row)
		{
			foreach (object obj in this.itemstats.Cache.Inserted)
			{
				ItemStats item = (ItemStats)obj;
				this.itemstats.Cache.Delete(item);
			}
			DateTime lastCostTime = INReleaseProcess.GetLastCostTime(this.itemstats.Cache);
			foreach (PXResult<INItemSite> r in this.itemsiterecords.Select(Array.Empty<object>()))
			{
				INItemSite initemSite = r;
				ItemStats itemStats = new ItemStats
				{
					InventoryID = initemSite.InventoryID,
					SiteID = initemSite.SiteID
				};
				itemStats = this.itemstats.Insert(itemStats);
				itemStats.LastCost = row.LastCost;
				itemStats.LastCostDate = new DateTime?(lastCostTime);
			}
			foreach (object obj2 in this.itemcost.Cache.Inserted)
			{
				ItemCost item2 = (ItemCost)obj2;
				this.itemstats.Cache.Delete(item2);
			}
			ItemCost itemCost = new ItemCost
			{
				InventoryID = row.InventoryID,
				CuryID = row.CuryID
			};
			itemCost = this.itemcost.Insert(itemCost);
			itemCost.LastCost = row.LastCost;
			itemCost.LastCostDate = new DateTime?(lastCostTime);
		}

		// Token: 0x0600D7BA RID: 55226 RVA: 0x003175D4 File Offset: 0x003157D4
		protected virtual void _(Events.RowInserted<INSubItemRep> e)
		{
			this.UpdateSubItemSiteReplenishment(e.Row, PXDBOperation.Insert);
		}

		// Token: 0x0600D7BB RID: 55227 RVA: 0x003175E3 File Offset: 0x003157E3
		protected virtual void _(Events.RowUpdated<INSubItemRep> e)
		{
			this.UpdateSubItemSiteReplenishment(e.Row, PXDBOperation.Update);
		}

		// Token: 0x0600D7BC RID: 55228 RVA: 0x003175F2 File Offset: 0x003157F2
		protected virtual void _(Events.RowDeleted<INSubItemRep> e)
		{
			this.UpdateSubItemSiteReplenishment(e.Row, PXDBOperation.Delete);
		}

		// Token: 0x0600D7BD RID: 55229 RVA: 0x00317604 File Offset: 0x00315804
		private void UpdateSubItemSiteReplenishment(INSubItemRep row, PXDBOperation operation)
		{
			if (row == null || row.InventoryID == null || row.SubItemID == null)
			{
				return;
			}
			foreach (PXResult<INItemSite> r in PXSelectBase<INItemSite, PXViewOf<INItemSite>.BasedOn<SelectFromBase<INItemSite, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemSite.inventoryID, Equal<P.AsInt>>>>, And<BqlOperand<INItemSite.replenishmentClassID, IBqlString>.IsEqual<P.AsString>>>>.And<BqlOperand<INItemSite.subItemOverride, IBqlBool>.IsEqual<False>>>.Order<By<BqlField<INItemSite.inventoryID, IBqlInt>.Asc>>>.Config>.Select(this, new object[]
			{
				row.InventoryID,
				row.ReplenishmentClassID
			}))
			{
				INItemSite initemSite = r;
				PXCache pxcache = this.Caches[typeof(INItemSiteReplenishment)];
				INItemSiteReplenishment initemSiteReplenishment = PXSelectBase<INItemSiteReplenishment, PXViewOf<INItemSiteReplenishment>.BasedOn<SelectFromBase<INItemSiteReplenishment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemSiteReplenishment.inventoryID, Equal<P.AsInt>>>>, And<BqlOperand<INItemSiteReplenishment.siteID, IBqlInt>.IsEqual<P.AsInt>>>>.And<BqlOperand<INItemSiteReplenishment.subItemID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.SelectWindowed(this, 0, 1, new object[]
				{
					row.InventoryID,
					initemSite.SiteID,
					row.SubItemID
				});
				if (initemSiteReplenishment == null)
				{
					if (operation == PXDBOperation.Delete)
					{
						continue;
					}
					operation = PXDBOperation.Insert;
					initemSiteReplenishment = new INItemSiteReplenishment
					{
						InventoryID = row.InventoryID,
						SiteID = initemSite.SiteID,
						SubItemID = row.SubItemID
					};
				}
				else
				{
					initemSiteReplenishment = PXCache<INItemSiteReplenishment>.CreateCopy(initemSiteReplenishment);
				}
				initemSiteReplenishment.SafetyStock = row.SafetyStock;
				initemSiteReplenishment.MinQty = row.MinQty;
				initemSiteReplenishment.MaxQty = row.MaxQty;
				initemSiteReplenishment.TransferERQ = row.TransferERQ;
				initemSiteReplenishment.ItemStatus = row.ItemStatus;
				switch (operation)
				{
				case PXDBOperation.Update:
					pxcache.Update(initemSiteReplenishment);
					break;
				case PXDBOperation.Insert:
					pxcache.Insert(initemSiteReplenishment);
					break;
				case PXDBOperation.Delete:
					pxcache.Delete(initemSiteReplenishment);
					break;
				}
			}
		}

		// Token: 0x0600D7BE RID: 55230 RVA: 0x003177B4 File Offset: 0x003159B4
		protected virtual void _(Events.RowPersisting<ItemStats> e)
		{
			if (e.Operation.Command() == PXDBOperation.Insert)
			{
				int? inventoryID = e.Row.InventoryID;
				int num = 0;
				if ((inventoryID.GetValueOrDefault() < num & inventoryID != null) && this.Item.Current != null)
				{
					int? num2 = (int?)this.Item.Cache.GetValue<InventoryItem.inventoryID>(this.Item.Current);
					if (!this._persisted.ContainsKey(num2))
					{
						this._persisted.Add(num2, e.Row.InventoryID);
					}
					e.Row.InventoryID = num2;
					e.Cache.Normalize();
				}
			}
		}

		// Token: 0x0600D7BF RID: 55231 RVA: 0x00317860 File Offset: 0x00315A60
		protected virtual void _(Events.RowPersisted<ItemStats> e)
		{
			int? inventoryID;
			if (e.TranStatus == PXTranStatus.Aborted && e.Operation.Command() == PXDBOperation.Insert && this._persisted.TryGetValue(e.Row.InventoryID, out inventoryID))
			{
				e.Row.InventoryID = inventoryID;
			}
		}

		// Token: 0x0600D7C0 RID: 55232 RVA: 0x003178AA File Offset: 0x00315AAA
		protected virtual void _(Events.FieldSelecting<Vendor, Vendor.curyID> e)
		{
			if (e.ReturnValue == null)
			{
				e.ReturnValue = this.Company.Current.BaseCuryID;
			}
		}

		// Token: 0x0600D7C1 RID: 55233 RVA: 0x003178CC File Offset: 0x00315ACC
		protected virtual void _(Events.ExceptionHandling<INItemXRef, INItemXRef.bAccountID> e)
		{
			if (e.Row != null && e.Row.BAccountID == null)
			{
				object newValue = e.NewValue;
				if (newValue is int)
				{
					int num = (int)newValue;
					if (num == 0)
					{
						goto IL_52;
					}
				}
				string text = e.NewValue as string;
				if (text == null || !(text == "0"))
				{
					return;
				}
				IL_52:
				e.Row.BAccountID = new int?(0);
				e.Cancel = true;
			}
		}

		// Token: 0x0600D7C2 RID: 55234 RVA: 0x00317944 File Offset: 0x00315B44
		protected virtual void _(Events.RowInserted<INItemSite> e)
		{
			if (e.Row.IsDefault.GetValueOrDefault())
			{
				this.SetSiteDefault(e.Row);
			}
			if (e.Row != null && !this.insetup.Current.UseInventorySubItem.GetValueOrDefault() && e.Row.InventoryID != null && e.Row.SiteID != null)
			{
				SiteStatusByCostCenter item = new SiteStatusByCostCenter
				{
					InventoryID = e.Row.InventoryID,
					SiteID = e.Row.SiteID,
					CostCenterID = new int?(0),
					PersistEvenZero = new bool?(true)
				};
				this.sitestatusbycostcenter.Insert(item);
			}
		}

		// Token: 0x0600D7C3 RID: 55235 RVA: 0x00317A10 File Offset: 0x00315C10
		protected virtual void _(Events.RowUpdated<INItemSite> e)
		{
			bool? isDefault = e.OldRow.IsDefault;
			bool? isDefault2 = e.Row.IsDefault;
			if (!(isDefault.GetValueOrDefault() == isDefault2.GetValueOrDefault() & isDefault != null == (isDefault2 != null)))
			{
				this.SetSiteDefault(e.Row);
			}
			if (e.Row != null && !this.insetup.Current.UseInventorySubItem.GetValueOrDefault() && e.Row.InventoryID != null && e.Row.SiteID != null)
			{
				SiteStatusByCostCenter item = new SiteStatusByCostCenter
				{
					InventoryID = e.Row.InventoryID,
					SiteID = e.Row.SiteID,
					CostCenterID = new int?(0),
					PersistEvenZero = new bool?(true)
				};
				this.sitestatusbycostcenter.Insert(item);
			}
		}

		// Token: 0x0600D7C4 RID: 55236 RVA: 0x00317B00 File Offset: 0x00315D00
		protected virtual void _(Events.RowSelected<INItemSite> e)
		{
			if (e.Row != null)
			{
				if (e.Row != null && INReplenishmentSource.IsTransfer(e.Row.ReplenishmentSource))
				{
					int? replenishmentSourceSiteID = e.Row.ReplenishmentSourceSiteID;
					int? siteID = e.Row.SiteID;
					if (replenishmentSourceSiteID.GetValueOrDefault() == siteID.GetValueOrDefault() & replenishmentSourceSiteID != null == (siteID != null))
					{
						e.Cache.RaiseExceptionHandling<INItemSite.replenishmentSourceSiteID>(e.Row, e.Row.ReplenishmentSourceSiteID, new PXSetPropertyException("Replenishment Source Warehouse must be different from current Warehouse", PXErrorLevel.Warning));
						goto IL_B5;
					}
				}
				e.Cache.RaiseExceptionHandling<INItemSite.replenishmentSourceSiteID>(e.Row, e.Row.ReplenishmentSourceSiteID, null);
			}
			IL_B5:
			if (e.Row != null && e.Row.InvtAcctID == null)
			{
				INSite site = INSite.PK.Find(this, e.Row.SiteID, PKFindOptions.None);
				try
				{
					INItemSiteMaint.DefaultInvtAcctSub(this, e.Row, this.Item.Current, site, this.postclass.Current);
				}
				catch (PXMaskArgumentException)
				{
				}
			}
		}

		// Token: 0x0600D7C5 RID: 55237 RVA: 0x00317C2C File Offset: 0x00315E2C
		protected virtual void _(Events.CommandPreparing<INItemSite.invtAcctID> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update) && !((INItemSite)e.Row).OverrideInvtAcctSub.GetValueOrDefault())
			{
				e.Args.ExcludeFromInsertUpdate();
			}
		}

		// Token: 0x0600D7C6 RID: 55238 RVA: 0x00317C74 File Offset: 0x00315E74
		protected virtual void _(Events.CommandPreparing<INItemSite.invtSubID> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update) && !((INItemSite)e.Row).OverrideInvtAcctSub.GetValueOrDefault())
			{
				e.Args.ExcludeFromInsertUpdate();
			}
		}

		// Token: 0x0600D7C7 RID: 55239 RVA: 0x00317CBA File Offset: 0x00315EBA
		protected virtual void _(Events.FieldVerifying<INItemSite, INItemSite.inventoryID> e)
		{
			e.Cancel = true;
		}

		// Token: 0x0600D7C8 RID: 55240 RVA: 0x00317CC4 File Offset: 0x00315EC4
		protected virtual void _(Events.RowSelected<RelationGroup> e)
		{
			if (this.Item.Current != null && e.Row != null && this.Groups.Cache.GetStatus(e.Row) == PXEntryStatus.Notchanged)
			{
				e.Row.Included = new bool?(UserAccess.IsIncluded(this.Item.Current.GroupMask, e.Row));
			}
		}

		// Token: 0x0600D7C9 RID: 55241 RVA: 0x00317D29 File Offset: 0x00315F29
		protected virtual void _(Events.RowPersisting<RelationGroup> e)
		{
			e.Cancel = true;
		}

		// Token: 0x0600D7CA RID: 55242 RVA: 0x00317D34 File Offset: 0x00315F34
		protected virtual void _(Events.RowSelected<INItemRep> e)
		{
			if (e.Row != null)
			{
				bool flag = INReplenishmentSource.IsTransfer(e.Row.ReplenishmentSource);
				PXUIFieldAttribute.SetEnabled<INItemRep.replenishmentMethod>(e.Cache, e.Row, e.Row.ReplenishmentSource.IsNotIn("O", "D"));
				PXUIFieldAttribute.SetEnabled<INItemRep.replenishmentSourceSiteID>(e.Cache, e.Row, e.Row.ReplenishmentSource.IsIn("O", "D", "T", "P"));
				PXUIFieldAttribute.SetEnabled<INItemRep.maxShelfLife>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.launchDate>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.terminationDate>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.serviceLevelPct>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.safetyStock>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.minQty>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.maxQty>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.forecastModelType>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.forecastPeriodType>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.historyDepth>(e.Cache, e.Row, e.Row.ReplenishmentMethod != "N");
				PXUIFieldAttribute.SetEnabled<INItemRep.transferERQ>(e.Cache, e.Row, flag && e.Row.ReplenishmentMethod == "F");
				PXUIFieldAttribute.SetEnabled<INSubItemRep.transferERQ>(this.subreplenishment.Cache, null, flag && e.Row.ReplenishmentMethod == "F");
			}
			this.subreplenishment.Cache.AllowInsert = (e.Row != null && !string.IsNullOrEmpty(e.Row.ReplenishmentClassID) && this.insetup.Current.UseInventorySubItem.GetValueOrDefault());
		}

		// Token: 0x0600D7CB RID: 55243 RVA: 0x00317FD6 File Offset: 0x003161D6
		protected virtual void _(Events.RowInserted<INItemRep> e)
		{
			if (e.Row != null && e.Row.ReplenishmentClassID != null)
			{
				this.UpdateItemSiteReplenishment(e.Row);
			}
		}

		// Token: 0x0600D7CC RID: 55244 RVA: 0x00317FFC File Offset: 0x003161FC
		protected virtual void _(Events.FieldUpdated<INItemRep, INItemRep.replenishmentSource> e)
		{
			if (e.Row == null)
			{
				return;
			}
			if (e.Row.ReplenishmentSource.IsIn("O", "D"))
			{
				e.Cache.SetValueExt<INItemRep.replenishmentMethod>(e.Row, "N");
			}
			if (PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && e.Row.ReplenishmentSource.IsNotIn("O", "D", "T"))
			{
				e.Cache.SetDefaultExt<INItemRep.replenishmentSourceSiteID>(e.Row);
			}
		}

		// Token: 0x0600D7CD RID: 55245 RVA: 0x00318080 File Offset: 0x00316280
		protected virtual void _(Events.FieldUpdated<INItemRep, INItemRep.replenishmentMethod> e)
		{
			if (e.Row == null)
			{
				return;
			}
			if (e.Row.ReplenishmentMethod == "N")
			{
				e.Cache.SetDefaultExt<INItemRep.maxShelfLife>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.launchDate>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.terminationDate>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.serviceLevelPct>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.safetyStock>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.minQty>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.maxQty>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.forecastModelType>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.forecastPeriodType>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.historyDepth>(e.Row);
			}
		}

		// Token: 0x0600D7CE RID: 55246 RVA: 0x0031815C File Offset: 0x0031635C
		protected virtual void _(Events.RowUpdated<INItemRep> e)
		{
			if (e.Row == null)
			{
				return;
			}
			if (!INReplenishmentSource.IsTransfer(e.Row.ReplenishmentSource))
			{
				e.Row.ReplenishmentSourceSiteID = null;
			}
			this.UpdateItemSiteReplenishment(e.Row);
		}

		// Token: 0x0600D7CF RID: 55247 RVA: 0x003181A4 File Offset: 0x003163A4
		protected virtual void _(Events.RowDeleted<INItemRep> e)
		{
			if (e.Row == null)
			{
				return;
			}
			INItemRep rep = new INItemRep
			{
				ReplenishmentClassID = e.Row.ReplenishmentClassID,
				CuryID = e.Row.CuryID
			};
			this.UpdateItemSiteReplenishment(rep);
		}

		// Token: 0x0600D7D0 RID: 55248 RVA: 0x003181EC File Offset: 0x003163EC
		private void UpdateItemSiteReplenishment(INItemRep rep)
		{
			foreach (INItemSite initemSite in this.itemsiterecords.SelectMain(new object[]
			{
				rep.CuryID
			}))
			{
				bool flag = false;
				if (initemSite.ReplenishmentClassID == null)
				{
					initemSite.ReplenishmentClassID = rep.ReplenishmentClassID;
					flag = true;
				}
				if (!(initemSite.ReplenishmentClassID != rep.ReplenishmentClassID) && (this.UpdateItemSiteReplenishment(initemSite, rep) || flag))
				{
					this.itemsiterecords.Cache.MarkUpdated(initemSite, true);
				}
			}
		}

		// Token: 0x0600D7D1 RID: 55249 RVA: 0x0031826F File Offset: 0x0031646F
		protected virtual bool UpdateItemSiteReplenishment(INItemSite itemSite, INItemRep rep)
		{
			return INItemSiteMaint.UpdateItemSiteReplenishment(itemSite, rep);
		}

		// Token: 0x0600D7D2 RID: 55250 RVA: 0x00318278 File Offset: 0x00316478
		protected virtual void _(Events.RowSelected<INItemBoxEx> e)
		{
			if (e.Row == null || this.Item.Current == null)
			{
				return;
			}
			if (this.Item.Current.PackageOption.IsIn("W", "V"))
			{
				e.Row.MaxQty = this.CalculateMaxQtyInBox(this.Item.Current, e.Row);
			}
		}

		// Token: 0x0600D7D3 RID: 55251 RVA: 0x003182E0 File Offset: 0x003164E0
		protected virtual void _(Events.RowInserted<INItemBoxEx> e)
		{
			if (e.Row == null)
			{
				return;
			}
			CSBox csbox = CSBox.PK.Find(this, e.Row.BoxID, PKFindOptions.None);
			if (csbox != null)
			{
				e.Row.MaxWeight = csbox.MaxWeight;
				e.Row.MaxVolume = csbox.MaxVolume;
				e.Row.BoxWeight = csbox.BoxWeight;
				e.Row.Description = csbox.Description;
			}
			if (this.Item.Current.PackageOption.IsIn("W", "V"))
			{
				e.Row.MaxQty = this.CalculateMaxQtyInBox(this.Item.Current, e.Row);
			}
		}

		// Token: 0x0600D7D4 RID: 55252 RVA: 0x00318394 File Offset: 0x00316594
		protected virtual void _(Events.FieldVerifying<INItemBoxEx, INItemBoxEx.uOM> e)
		{
			if (e.Row != null && e.NewValue != null)
			{
				InventoryItem inventoryItem = this.Item.Current;
				if (inventoryItem != null && INUnit.UK.ByInventory.FindDirty(e.Cache.Graph, inventoryItem.InventoryID, inventoryItem.BaseUnit) == null)
				{
					throw new PXSetPropertyException("{0} '{1}' cannot be found in the system. Please verify whether you have proper access rights to this object.", new object[]
					{
						"uOM",
						e.NewValue
					});
				}
			}
			e.Cancel = true;
		}

		// Token: 0x0600D7D5 RID: 55253 RVA: 0x00318408 File Offset: 0x00316608
		protected virtual void _(Events.FieldVerifying<POVendorInventory, POVendorInventory.purchaseUnit> e)
		{
			if (e.Row == null)
			{
				return;
			}
			foreach (object obj in this.Caches[typeof(INUnit)].Inserted)
			{
				INUnit inunit = (INUnit)obj;
				short? unitType = inunit.UnitType;
				int? num = (unitType != null) ? new int?((int)unitType.GetValueOrDefault()) : null;
				int num2 = 1;
				if (num.GetValueOrDefault() == num2 & num != null)
				{
					num = inunit.InventoryID;
					int? inventoryID = e.Row.InventoryID;
					if ((num.GetValueOrDefault() == inventoryID.GetValueOrDefault() & num != null == (inventoryID != null)) && string.Equals(inunit.FromUnit, (string)e.NewValue, StringComparison.InvariantCultureIgnoreCase))
					{
						e.Cancel = true;
					}
				}
			}
		}

		// Token: 0x0600D7D6 RID: 55254 RVA: 0x00318518 File Offset: 0x00316718
		protected virtual void _(Events.RowSelected<InventoryItemCurySettings> eventArgs)
		{
			eventArgs.Cache.AdjustUI(null).For<InventoryItemCurySettings.pendingStdCost>(delegate(PXUIFieldAttribute a)
			{
				InventoryItem inventoryItem = this.Item.Current;
				a.Enabled = (((inventoryItem != null) ? inventoryItem.ValMethod : null) == "T");
			}).SameFor<InventoryItemCurySettings.pendingStdCostDate>();
		}

		// Token: 0x0600D7D7 RID: 55255 RVA: 0x00318550 File Offset: 0x00316750
		protected virtual void AppendGroupMask(int? itemClassID, bool clear)
		{
			if (itemClassID.GetValueOrDefault() != 0)
			{
				INItemClass initemClass = PXSelectBase<INItemClass, PXViewOf<INItemClass>.BasedOn<SelectFromBase<INItemClass, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemClass.itemClassID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(this, new object[]
				{
					itemClassID
				});
				if (initemClass != null && initemClass.GroupMask != null)
				{
					if (clear)
					{
						this.Groups.Cache.Clear();
					}
					foreach (PXResult<RelationGroup> r in this.Groups.Select(Array.Empty<object>()))
					{
						RelationGroup relationGroup = r;
						int num = 0;
						while (num < relationGroup.GroupMask.Length && num < initemClass.GroupMask.Length)
						{
							if (!relationGroup.Included.GetValueOrDefault() && relationGroup.GroupMask[num] != 0 && (initemClass.GroupMask[num] & relationGroup.GroupMask[num]) == relationGroup.GroupMask[num])
							{
								relationGroup.Included = new bool?(true);
								this.Groups.Cache.MarkUpdated(relationGroup, true);
								this.Groups.Cache.IsDirty = true;
								break;
							}
							num++;
						}
					}
				}
			}
		}

		// Token: 0x170049EB RID: 18923
		// (get) Token: 0x0600D7D8 RID: 55256 RVA: 0x00318680 File Offset: 0x00316880
		public override bool IsDirty
		{
			get
			{
				return (!this._JustInserted || this.IsContractBasedAPI) && base.IsDirty;
			}
		}

		// Token: 0x0600D7D9 RID: 55257 RVA: 0x0031869C File Offset: 0x0031689C
		protected virtual void SetSiteDefault(INItemSite itemsite)
		{
			InventoryItem inventoryItem = InventoryItem.PK.FindDirty(this, itemsite.InventoryID);
			INSite insite = INSite.PK.Find(this, itemsite.SiteID, PKFindOptions.None);
			if (inventoryItem != null)
			{
				InventoryItemCurySettings curySettings = this.GetCurySettings(inventoryItem.InventoryID, insite.BaseCuryID);
				curySettings.DfltSiteID = (itemsite.IsDefault.GetValueOrDefault() ? itemsite.SiteID : null);
				curySettings.DfltReceiptLocationID = (itemsite.IsDefault.GetValueOrDefault() ? itemsite.DfltReceiptLocationID : null);
				curySettings.DfltShipLocationID = (itemsite.IsDefault.GetValueOrDefault() ? itemsite.DfltShipLocationID : null);
				this.ItemCurySettings.Update(curySettings);
			}
			bool flag = false;
			foreach (PXResult<INItemSite> r in this.itemsiterecords.Select(new object[]
			{
				insite.BaseCuryID
			}))
			{
				INItemSite initemSite = r;
				if (!object.Equals(initemSite.SiteID, itemsite.SiteID) && initemSite.IsDefault.Value)
				{
					initemSite.IsDefault = new bool?(false);
					this.itemsiterecords.Cache.MarkUpdated(initemSite, true);
					flag = true;
				}
			}
			if (flag)
			{
				this.itemsiterecords.View.RequestRefresh();
			}
		}

		// Token: 0x0600D7DA RID: 55258 RVA: 0x00318824 File Offset: 0x00316A24
		public override void Persist()
		{
			if (this.Item.Current != null)
			{
				if (string.IsNullOrEmpty(this.Item.Current.LotSerClassID) && !PXAccess.FeatureInstalled<FeaturesSet.lotSerialTracking>())
				{
					this.Item.Current.LotSerClassID = INLotSerClass.GetDefaultLotSerClass(this);
				}
				if (this.Groups.Cache.IsDirty)
				{
					UserAccess.PopulateNeighbours<InventoryItem>(this.Item.Cache, this.Item.Current, new PXDataFieldValue[]
					{
						new PXDataFieldValue(typeof(InventoryItem.inventoryID).Name, PXDbType.Int, new int?(4), this.Item.Current.InventoryID, PXComp.NE)
					}, this.Groups, new Type[]
					{
						typeof(SegmentValue)
					});
					PXSelectorAttribute.ClearGlobalCache<InventoryItem>();
				}
			}
			foreach (object obj in this.itemsitereplenihments.Cache.Inserted)
			{
				INItemSiteReplenishment initemSiteReplenishment = (INItemSiteReplenishment)obj;
				this.sitestatusbycostcenter.Insert(new SiteStatusByCostCenter
				{
					InventoryID = initemSiteReplenishment.InventoryID,
					SubItemID = initemSiteReplenishment.SubItemID,
					SiteID = initemSiteReplenishment.SiteID,
					CostCenterID = new int?(0),
					PersistEvenZero = new bool?(true)
				});
			}
			base.Persist();
			this.Groups.Cache.Clear();
			GroupHelper.Clear();
		}

		// Token: 0x0600D7DB RID: 55259 RVA: 0x003189B4 File Offset: 0x00316BB4
		public override void CopyPasteGetScript(bool isImportSimple, List<Command> script, List<Container> containers)
		{
			base.CopyPasteGetScript(isImportSimple, script, containers);
			if (this.DisableCopyPastingSubitems())
			{
				foreach (int index in script.SelectIndexesWhere((Command _) => this.IsMatchingPatternWithTrailingNumber(_.ObjectName, "SubItem_")).Reverse<int>())
				{
					script.RemoveAt(index);
					containers.RemoveAt(index);
				}
			}
			(from _ in script
			where _.ObjectName == "itemxrefrecords"
			select _).ForEach(delegate(Command _)
			{
				_.Commit = false;
			});
			(from _ in script
			where _.ObjectName == "itemxrefrecords"
			select _).Last<Command>().Commit = true;
			foreach (SubItemAttribute subItemAttribute in this.ItemSettings.Cache.GetAttributesReadonly<InventoryItem.defaultSubItemID>().Concat(this.VendorItems.Cache.GetAttributesReadonly<POVendorInventory.subItemID>()).Concat(this.itemxrefrecords.Cache.GetAttributesReadonly<INItemXRef.subItemID>()).OfType<SubItemAttribute>())
			{
				subItemAttribute.ValidateValueOnFieldUpdating = false;
			}
		}

		// Token: 0x0600D7DC RID: 55260 RVA: 0x00318B18 File Offset: 0x00316D18
		protected virtual bool DisableCopyPastingSubitems()
		{
			int? segmentsNumber = this.SegmentValues.SegmentsNumber;
			int num = 1;
			return segmentsNumber.GetValueOrDefault() > num & segmentsNumber != null;
		}

		// Token: 0x0600D7DD RID: 55261 RVA: 0x00318B48 File Offset: 0x00316D48
		protected virtual bool IsMatchingPatternWithTrailingNumber(string input, string pattern)
		{
			int? num = (input != null) ? new int?(input.Length) : null;
			int length = pattern.Length;
			return (num.GetValueOrDefault() > length & num != null) && Regex.IsMatch(input, string.Format("^{0}[0-9]+$", pattern));
		}

		// Token: 0x0600D7DE RID: 55262 RVA: 0x00318BA0 File Offset: 0x00316DA0
		protected virtual void ValidatePackaging(InventoryItem row)
		{
			PXUIFieldAttribute.SetError<InventoryItem.weightUOM>(this.Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.baseItemWeight>(this.Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.volumeUOM>(this.Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.baseItemVolume>(this.Item.Cache, row, null);
			if (row.PackageOption.IsIn("W", "V"))
			{
				if (string.IsNullOrEmpty(row.WeightUOM))
				{
					this.Item.Cache.RaiseExceptionHandling<InventoryItem.weightUOM>(row, row.WeightUOM, new PXSetPropertyException("Value is required for Auto packaging to work correctly.", PXErrorLevel.Warning));
				}
				decimal? num = row.BaseItemWeight;
				decimal d = 0m;
				if (num.GetValueOrDefault() <= d & num != null)
				{
					this.Item.Cache.RaiseExceptionHandling<InventoryItem.baseItemWeight>(row, row.BaseItemWeight, new PXSetPropertyException("Value is required for Auto packaging to work correctly.", PXErrorLevel.Warning));
				}
				if (row.PackageOption == "V")
				{
					if (string.IsNullOrEmpty(row.VolumeUOM))
					{
						this.Item.Cache.RaiseExceptionHandling<InventoryItem.volumeUOM>(row, row.VolumeUOM, new PXSetPropertyException("Value is required for Auto packaging to work correctly.", PXErrorLevel.Warning));
					}
					num = row.BaseItemVolume;
					d = 0m;
					if (num.GetValueOrDefault() <= d & num != null)
					{
						this.Item.Cache.RaiseExceptionHandling<InventoryItem.baseItemVolume>(row, row.BaseItemVolume, new PXSetPropertyException("Value is required for Auto packaging to work correctly.", PXErrorLevel.Warning));
					}
				}
			}
			foreach (PXResult<INItemBoxEx> r in this.Boxes.Select(Array.Empty<object>()))
			{
				INItemBoxEx initemBoxEx = r;
				PXUIFieldAttribute.SetError<INItemBoxEx.boxID>(this.Boxes.Cache, initemBoxEx, null);
				PXUIFieldAttribute.SetError<INItemBoxEx.maxQty>(this.Boxes.Cache, initemBoxEx, null);
				if (row.PackageOption.IsIn("W", "V") && initemBoxEx.MaxWeight.GetValueOrDefault() == 0m)
				{
					this.Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(initemBoxEx, initemBoxEx.BoxID, new PXSetPropertyException("Box Max. Weight must be defined for Auto Packaging to work correctly.", PXErrorLevel.Warning));
				}
				if (row.PackageOption == "V" && initemBoxEx.MaxVolume.GetValueOrDefault() == 0m)
				{
					this.Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(initemBoxEx, initemBoxEx.BoxID, new PXSetPropertyException("Box Max. Volume must be defined for Auto Packaging to work correctly.", PXErrorLevel.Warning));
				}
				if (row.PackageOption.IsIn("W", "V"))
				{
					if (!(initemBoxEx.MaxWeight.GetValueOrDefault() < row.BaseItemWeight.GetValueOrDefault()))
					{
						decimal? num = initemBoxEx.MaxVolume;
						decimal d = 0m;
						if (!(num.GetValueOrDefault() > d & num != null))
						{
							continue;
						}
						num = row.BaseItemVolume;
						decimal? maxVolume = initemBoxEx.MaxVolume;
						if (!(num.GetValueOrDefault() > maxVolume.GetValueOrDefault() & (num != null & maxVolume != null)))
						{
							continue;
						}
					}
					this.Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(initemBoxEx, initemBoxEx.BoxID, new PXSetPropertyException("The item can't fit the given Box.", PXErrorLevel.Warning));
				}
			}
		}

		// Token: 0x0600D7DF RID: 55263 RVA: 0x00318EF8 File Offset: 0x003170F8
		private IEnumerable<RelationGroup> GetGroups()
		{
			if (this.IsImport)
			{
				this.Groups.View.Clear();
			}
			foreach (PXResult<RelationGroup> r in PXSelectBase<RelationGroup, PXViewOf<RelationGroup>.BasedOn<SelectFromBase<RelationGroup, TypeArrayOf<IFbqlJoin>.Empty>>.Config>.Select(this, Array.Empty<object>()))
			{
				RelationGroup relationGroup = r;
				if ((relationGroup.SpecificModule.IsIn(null, typeof(InventoryItem).Namespace) && relationGroup.SpecificType.IsIn(null, typeof(SegmentValue).FullName, typeof(InventoryItem).FullName)) || (this.Item.Current != null && UserAccess.IsIncluded(this.Item.Current.GroupMask, relationGroup)))
				{
					this.Groups.Current = relationGroup;
					yield return relationGroup;
				}
			}
			IEnumerator<PXResult<RelationGroup>> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600D7E0 RID: 55264 RVA: 0x00318F08 File Offset: 0x00317108
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Inventory Summary", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewSummary(PXAdapter adapter)
		{
			if (this.Item.Current != null)
			{
				InventorySummaryEnq inventorySummaryEnq = PXGraph.CreateInstance<InventorySummaryEnq>();
				inventorySummaryEnq.Filter.Current.InventoryID = this.Item.Current.InventoryID;
				inventorySummaryEnq.Filter.Select(Array.Empty<object>());
				throw new PXRedirectRequiredException(inventorySummaryEnq, "Inventory Summary")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E1 RID: 55265 RVA: 0x00318F70 File Offset: 0x00317170
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Inventory Allocation Details", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewAllocationDetails(PXAdapter adapter)
		{
			if (this.Item.Current != null)
			{
				InventoryAllocDetEnq inventoryAllocDetEnq = PXGraph.CreateInstance<InventoryAllocDetEnq>();
				inventoryAllocDetEnq.Filter.Current.InventoryID = this.Item.Current.InventoryID;
				inventoryAllocDetEnq.Filter.Select(Array.Empty<object>());
				throw new PXRedirectRequiredException(inventoryAllocDetEnq, "Inventory Allocation Details")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E2 RID: 55266 RVA: 0x00318FD8 File Offset: 0x003171D8
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Inventory Transaction Summary", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewTransactionSummary(PXAdapter adapter)
		{
			if (this.Item.Current != null)
			{
				InventoryTranSumEnq inventoryTranSumEnq = PXGraph.CreateInstance<InventoryTranSumEnq>();
				inventoryTranSumEnq.Filter.Current.InventoryID = this.Item.Current.InventoryID;
				inventoryTranSumEnq.Filter.Select(Array.Empty<object>());
				throw new PXRedirectRequiredException(inventoryTranSumEnq, "Inventory Transaction Summary")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E3 RID: 55267 RVA: 0x00319040 File Offset: 0x00317240
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Inventory Transaction Details", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewTransactionDetails(PXAdapter adapter)
		{
			if (this.Item.Current != null)
			{
				InventoryTranDetEnq inventoryTranDetEnq = PXGraph.CreateInstance<InventoryTranDetEnq>();
				inventoryTranDetEnq.Filter.Current.InventoryID = this.Item.Current.InventoryID;
				inventoryTranDetEnq.Filter.Select(Array.Empty<object>());
				throw new PXRedirectRequiredException(inventoryTranDetEnq, "Inventory Transaction Details")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E4 RID: 55268 RVA: 0x003190A8 File Offset: 0x003172A8
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Inventory Transaction History", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewTransactionHistory(PXAdapter adapter)
		{
			if (this.Item.Current != null)
			{
				InventoryTranHistEnq inventoryTranHistEnq = PXGraph.CreateInstance<InventoryTranHistEnq>();
				inventoryTranHistEnq.Filter.Current.InventoryID = this.Item.Current.InventoryID;
				inventoryTranHistEnq.Filter.Select(Array.Empty<object>());
				throw new PXRedirectRequiredException(inventoryTranHistEnq, "Inventory Transaction History")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E5 RID: 55269 RVA: 0x00319110 File Offset: 0x00317310
		[PXUIField(DisplayName = "Add Warehouse Detail", MapEnableRights = PXCacheRights.Select)]
		[PXInsertButton]
		protected virtual IEnumerable AddWarehouseDetail(PXAdapter adapter)
		{
			foreach (object obj in adapter.Get())
			{
				InventoryItem inventoryItem = (InventoryItem)obj;
				int? inventoryID = inventoryItem.InventoryID;
				int num = 0;
				if (inventoryID.GetValueOrDefault() > num & inventoryID != null)
				{
					INItemSiteMaint initemSiteMaint = PXGraph.CreateInstance<INItemSiteMaint>();
					PXCache cache = initemSiteMaint.itemsiterecord.Cache;
					INItemSite initemSite = (INItemSite)cache.CreateCopy(cache.Insert());
					initemSite.InventoryID = inventoryItem.InventoryID;
					cache.Update(initemSite);
					cache.IsDirty = false;
					throw new PXRedirectRequiredException(initemSiteMaint, "Add Warehouse Detail");
				}
				yield return inventoryItem;
			}
			IEnumerator enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600D7E6 RID: 55270 RVA: 0x00319120 File Offset: 0x00317320
		[PXButton(ImageKey = "DataEntry")]
		[PXUIField(DisplayName = "Reset to Default", MapEnableRights = PXCacheRights.Update)]
		protected virtual IEnumerable UpdateReplenishment(PXAdapter adapter)
		{
			if (this.replenishment.Current != null && this.insetup.Current.UseInventorySubItem.GetValueOrDefault())
			{
				foreach (PXResult<INSubItemRep> r in this.subreplenishment.Select(Array.Empty<object>()))
				{
					INSubItemRep insubItemRep = PXCache<INSubItemRep>.CreateCopy(r);
					insubItemRep.SafetyStock = this.replenishment.Current.SafetyStock;
					insubItemRep.MinQty = this.replenishment.Current.MinQty;
					insubItemRep.MaxQty = this.replenishment.Current.MaxQty;
					this.subreplenishment.Update(insubItemRep);
				}
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E7 RID: 55271 RVA: 0x003191FC File Offset: 0x003173FC
		[PXButton(ImageKey = "AddNew")]
		[PXUIField(DisplayName = "Generate Subitems", MapEnableRights = PXCacheRights.Update)]
		protected virtual IEnumerable GenerateSubitems(PXAdapter adapter)
		{
			if (this.replenishment.Current != null && this.insetup.Current.UseInventorySubItem.GetValueOrDefault())
			{
				ParameterExpression parameterExpression;
				List<Segment> list = PXSelectBase<Segment, PXViewOf<Segment>.BasedOn<SelectFromBase<Segment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<Segment.dimensionID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(this, new object[]
				{
					"INSUBITEM"
				}).Select(Expression.Lambda<Func<PXResult<Segment>, Segment>>(Expression.Call(parameterExpression, methodof(PXResult.GetItem()), Array.Empty<Expression>()), new ParameterExpression[]
				{
					parameterExpression
				})).ToList<Segment>();
				Dictionary<short?, List<string>> dictionary = list.ToDictionary((Segment segment) => segment.SegmentID, (Segment segement) => new List<string>());
				foreach (PXResult<INSubItemSegmentValue> r in PXSelectBase<INSubItemSegmentValue, PXViewOf<INSubItemSegmentValue>.BasedOn<SelectFromBase<INSubItemSegmentValue, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SegmentValue>.On<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SegmentValue.segmentID, Equal<INSubItemSegmentValue.segmentID>>>>, And<BqlOperand<SegmentValue.value, IBqlString>.IsEqual<INSubItemSegmentValue.value>>>>.And<BqlOperand<SegmentValue.dimensionID, IBqlString>.IsEqual<SubItemAttribute.dimensionName>>>>>.Where<KeysRelation<Field<INSubItemSegmentValue.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INSubItemSegmentValue>, InventoryItem, INSubItemSegmentValue>.SameAsCurrent>>.Config>.Select(this, Array.Empty<object>()))
				{
					INSubItemSegmentValue insubItemSegmentValue = r;
					dictionary[insubItemSegmentValue.SegmentID].Add(insubItemSegmentValue.Value);
				}
				foreach (Segment segment2 in list)
				{
					if (!dictionary[segment2.SegmentID].Any<string>())
					{
						if (segment2.Validate.GetValueOrDefault())
						{
							throw new PXException("At least one value in each segment that requires validation should be selected on the SUBITEMS tab.");
						}
						dictionary[segment2.SegmentID].Add(new string(' ', (int)segment2.Length.GetValueOrDefault(1)));
					}
				}
				List<string> list2 = dictionary.First<KeyValuePair<short?, List<string>>>().Value;
				foreach (List<string> inner in from kvp in dictionary.Skip(1)
				select kvp.Value)
				{
					list2 = list2.Join(inner, (string s) => 0, (string s) => 0, (string subItemId, string segment) => subItemId + segment).ToList<string>();
				}
				foreach (string text in list2)
				{
					if (!text.All(new Func<char, bool>(char.IsWhiteSpace)))
					{
						INSubItemRep insubItemRep = new INSubItemRep();
						insubItemRep.InventoryID = this.Item.Current.InventoryID;
						insubItemRep.ReplenishmentClassID = this.replenishment.Current.ReplenishmentClassID;
						this.subreplenishment.SetValueExt<INSubItemRep.subItemID>(insubItemRep, text);
						this.subreplenishment.Insert(insubItemRep);
					}
				}
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E8 RID: 55272 RVA: 0x00319554 File Offset: 0x00317754
		[PXUIField(DisplayName = "Group Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ViewGroupDetails(PXAdapter adapter)
		{
			if (this.Groups.Current != null)
			{
				RelationGroups relationGroups = PXGraph.CreateInstance<RelationGroups>();
				relationGroups.HeaderGroup.Current = relationGroups.HeaderGroup.Search<RelationGroup.groupName>(this.Groups.Current.GroupName, Array.Empty<object>());
				throw new PXRedirectRequiredException(relationGroups, false, "Group Details");
			}
			return adapter.Get();
		}

		// Token: 0x0600D7E9 RID: 55273 RVA: 0x003195B7 File Offset: 0x003177B7
		public static void Redirect(int? inventoryID)
		{
			InventoryItemMaint.Redirect(inventoryID, false);
		}

		// Token: 0x0600D7EA RID: 55274 RVA: 0x003195C0 File Offset: 0x003177C0
		public static void Redirect(int? inventoryID, bool newWindow)
		{
			InventoryItemMaint inventoryItemMaint = PXGraph.CreateInstance<InventoryItemMaint>();
			inventoryItemMaint.Item.Current = inventoryItemMaint.Item.Search<InventoryItem.inventoryID>(inventoryID, Array.Empty<object>());
			if (inventoryItemMaint.Item.Current == null)
			{
				return;
			}
			if (newWindow)
			{
				throw new PXRedirectRequiredException(inventoryItemMaint, true, "Inventory Item")
				{
					Mode = PXBaseRedirectException.WindowMode.NewWindow
				};
			}
			throw new PXRedirectRequiredException(inventoryItemMaint, "Inventory Item");
		}

		// Token: 0x0600D7EB RID: 55275 RVA: 0x0031962C File Offset: 0x0031782C
		protected virtual decimal? CalculateMaxQtyInBox(InventoryItem item, INItemBoxEx box)
		{
			decimal? result = null;
			decimal? result2 = null;
			decimal? num = item.BaseWeight;
			decimal d = 0m;
			if (num.GetValueOrDefault() > d & num != null)
			{
				num = box.MaxWeight;
				d = 0m;
				if (num.GetValueOrDefault() > d & num != null)
				{
					result = new decimal?(Math.Floor((box.MaxWeight.Value - box.BoxWeight.GetValueOrDefault()) / item.BaseWeight.Value));
				}
			}
			if (item.PackageOption == "W")
			{
				return result;
			}
			num = item.BaseVolume;
			d = 0m;
			if (num.GetValueOrDefault() > d & num != null)
			{
				num = box.MaxVolume;
				d = 0m;
				if (num.GetValueOrDefault() > d & num != null)
				{
					result2 = new decimal?(Math.Floor(box.MaxVolume.Value / item.BaseVolume.Value));
				}
			}
			if (result != null && result2 != null)
			{
				return new decimal?(Math.Min(result.Value, result2.Value));
			}
			if (result != null)
			{
				return result;
			}
			if (result2 != null)
			{
				return result2;
			}
			return null;
		}

		// Token: 0x04006C51 RID: 27729
		private const string lotSerNumValueFieldName = "LotSerNumVal";

		// Token: 0x04006C52 RID: 27730
		[PXHidden]
		public FbqlSelect<SelectFromBase<INLotSerClass, TypeArrayOf<IFbqlJoin>.Empty>, INLotSerClass>.View lotSerClass;

		// Token: 0x04006C53 RID: 27731
		[PXHidden]
		public FbqlSelect<SelectFromBase<Location, TypeArrayOf<IFbqlJoin>.Empty>, Location>.View location;

		// Token: 0x04006C54 RID: 27732
		public FbqlSelect<SelectFromBase<BAccount, TypeArrayOf<IFbqlJoin>.Empty>, BAccount>.View baccount;

		// Token: 0x04006C55 RID: 27733
		public FbqlSelect<SelectFromBase<Vendor, TypeArrayOf<IFbqlJoin>.Empty>, Vendor>.View vendor;

		// Token: 0x04006C56 RID: 27734
		public FbqlSelect<SelectFromBase<Customer, TypeArrayOf<IFbqlJoin>.Empty>, Customer>.View customer;

		// Token: 0x04006C57 RID: 27735
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
		[PXCopyPasteHiddenFields(new Type[]
		{
			typeof(INItemCost.lastCost)
		})]
		public FbqlSelect<SelectFromBase<INItemCost, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemCost.inventoryID, Equal<BqlField<InventoryItem.inventoryID, IBqlInt>.FromCurrent>>>>>.And<BqlOperand<INItemCost.curyID, IBqlString>.IsEqual<BqlField<AccessInfo.baseCuryID, IBqlString>.FromCurrent>>>, INItemCost>.View ItemCosts;

		// Token: 0x04006C58 RID: 27736
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
		[PXCopyPasteHiddenView]
		public FbqlSelect<SelectFromBase<INItemSite, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<INSite>.On<KeysRelation<Field<INItemSite.siteID>.IsRelatedTo<INSite.siteID>.AsSimpleKey.WithTablesOf<INSite, INItemSite>, INSite, INItemSite>.And<CurrentMatch<INSite, AccessInfo.userName>>>>, FbqlJoins.Left<INSiteStatusSummary>.On<INSiteStatusSummary.FK.ItemSite>>>.Where<KeysRelation<Field<INItemSite.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemSite>, InventoryItem, INItemSite>.SameAsCurrent.And<BqlOperand<INSite.baseCuryID, IBqlString>.IsEqual<BqlField<AccessInfo.baseCuryID, IBqlString>.AsOptional>>>, INItemSite>.View itemsiterecords;

		// Token: 0x04006C59 RID: 27737
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			1,
			1,
			0,
			1,
			1
		})]
		public PXSetup<INPostClass>.Where<BqlOperand<INPostClass.postClassID, IBqlString>.IsEqual<BqlField<InventoryItem.postClassID, IBqlString>.FromCurrent>> postclass;

		// Token: 0x04006C5A RID: 27738
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			1,
			1,
			0,
			1,
			1
		})]
		public PXSetup<INLotSerClass>.Where<BqlOperand<INLotSerClass.lotSerClassID, IBqlString>.IsEqual<BqlField<InventoryItem.lotSerClassID, IBqlString>.FromCurrent>> lotserclass;

		// Token: 0x04006C5B RID: 27739
		public FbqlSelect<SelectFromBase<InventoryItemLotSerNumVal, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<InventoryItemLotSerNumVal.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, InventoryItemLotSerNumVal>, InventoryItem, InventoryItemLotSerNumVal>.SameAsCurrent>, InventoryItemLotSerNumVal>.View lotSerNumVal;

		// Token: 0x04006C5C RID: 27740
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
		public FbqlSelect<SelectFromBase<INItemRep, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INItemRep.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemRep>, InventoryItem, INItemRep>.SameAsCurrent.And<BqlOperand<INItemRep.curyID, IBqlString>.IsEqual<BqlField<AccessInfo.baseCuryID, IBqlString>.FromCurrent>>>, INItemRep>.View replenishment;

		// Token: 0x04006C5D RID: 27741
		[PXCopyPasteHiddenView]
		public FbqlSelect<SelectFromBase<INSubItemRep, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<CompositeKey<Field<INSubItemRep.inventoryID>.IsRelatedTo<INItemRep.inventoryID>, Field<INSubItemRep.curyID>.IsRelatedTo<INItemRep.curyID>, Field<INSubItemRep.replenishmentClassID>.IsRelatedTo<INItemRep.replenishmentClassID>>.WithTablesOf<INItemRep, INSubItemRep>, INItemRep, INSubItemRep>.SameAsCurrent>, INSubItemRep>.View subreplenishment;

		// Token: 0x04006C5E RID: 27742
		public FbqlSelect<SelectFromBase<INItemSiteReplenishment, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INItemSiteReplenishment.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemSiteReplenishment>, InventoryItem, INItemSiteReplenishment>.SameAsCurrent>, INItemSiteReplenishment>.View itemsitereplenihments;

		// Token: 0x04006C5F RID: 27743
		public FbqlSelect<SelectFromBase<INItemBoxEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INItemBoxEx.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemBoxEx>, InventoryItem, INItemBoxEx>.SameAsCurrent>, INItemBoxEx>.View Boxes;

		// Token: 0x04006C60 RID: 27744
		public FbqlSelect<SelectFromBase<SiteStatusByCostCenter, TypeArrayOf<IFbqlJoin>.Empty>, SiteStatusByCostCenter>.View sitestatusbycostcenter;

		// Token: 0x04006C61 RID: 27745
		public FbqlSelect<SelectFromBase<ItemStats, TypeArrayOf<IFbqlJoin>.Empty>, ItemStats>.View itemstats;

		// Token: 0x04006C62 RID: 27746
		public FbqlSelect<SelectFromBase<ItemCost, TypeArrayOf<IFbqlJoin>.Empty>, ItemCost>.View itemcost;

		// Token: 0x04006C63 RID: 27747
		[PXCopyPasteHiddenView]
		public FbqlSelect<SelectFromBase<INItemPlan, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INItemPlan.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INItemPlan>, InventoryItem, INItemPlan>.SameAsCurrent>, INItemPlan>.View itemplans;

		// Token: 0x04006C64 RID: 27748
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
			0,
			1,
			1,
			0,
			0
		})]
		[PXCopyPasteHiddenView]
		public FbqlSelect<SelectFromBase<INSiteStatusByCostCenter, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INSiteStatusByCostCenter.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INSiteStatusByCostCenter>, InventoryItem, INSiteStatusByCostCenter>.SameAsCurrent.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INSiteStatusByCostCenter.qtyOnHand, NotEqual<decimal0>>>>>.Or<BqlOperand<INSiteStatusByCostCenter.qtyAvail, IBqlDecimal>.IsNotEqual<decimal0>>>>, INSiteStatusByCostCenter>.View nonemptysitestatuses;

		// Token: 0x04006C65 RID: 27749
		public FbqlSelect<SelectFromBase<INPIClassItem, TypeArrayOf<IFbqlJoin>.Empty>, INPIClassItem>.View inpiclassitems;

		// Token: 0x04006C66 RID: 27750
		public FbqlSelect<SelectFromBase<PMBudget, TypeArrayOf<IFbqlJoin>.Empty>, PMBudget>.View projectBudget;

		// Token: 0x04006C67 RID: 27751
		[PXDependToCache(new Type[]
		{
			typeof(InventoryItem)
		})]
		public FbqlSelect<SelectFromBase<RelationGroup, TypeArrayOf<IFbqlJoin>.Empty>, RelationGroup>.View Groups;

		// Token: 0x04006C68 RID: 27752
		private Dictionary<int?, int?> _persisted = new Dictionary<int?, int?>();

		// Token: 0x04006C69 RID: 27753
		protected bool _JustInserted;

		// Token: 0x04006C6A RID: 27754
		public PXAction<InventoryItem> viewSummary;

		// Token: 0x04006C6B RID: 27755
		public PXAction<InventoryItem> viewAllocationDetails;

		// Token: 0x04006C6C RID: 27756
		public PXAction<InventoryItem> viewTransactionSummary;

		// Token: 0x04006C6D RID: 27757
		public PXAction<InventoryItem> viewTransactionDetails;

		// Token: 0x04006C6E RID: 27758
		public PXAction<InventoryItem> viewTransactionHistory;

		// Token: 0x04006C6F RID: 27759
		public PXAction<InventoryItem> addWarehouseDetail;

		// Token: 0x04006C70 RID: 27760
		public PXAction<InventoryItem> updateReplenishment;

		// Token: 0x04006C71 RID: 27761
		public PXAction<InventoryItem> generateSubitems;

		// Token: 0x04006C72 RID: 27762
		public PXAction<InventoryItem> viewGroupDetails;

		// Token: 0x0200718C RID: 29068
		public class CurySettings : CurySettingsExtension<InventoryItemMaint, InventoryItem, InventoryItemCurySettings>
		{
			// Token: 0x060256B3 RID: 153267 RVA: 0x007B78FA File Offset: 0x007B5AFA
			public static bool IsActive()
			{
				return true;
			}

			// Token: 0x060256B4 RID: 153268 RVA: 0x007B7900 File Offset: 0x007B5B00
			[PXButton(CommitChanges = true)]
			[PXUIField(DisplayName = "Update Cost", MapEnableRights = PXCacheRights.Update)]
			protected virtual IEnumerable UpdateCost(PXAdapter adapter)
			{
				InventoryItemCurySettings inventoryItemCurySettings = (InventoryItemCurySettings)this.curySettings.SelectSingle(Array.Empty<object>());
				if (inventoryItemCurySettings != null && inventoryItemCurySettings.PendingStdCostDate != null)
				{
					InventoryItem inventoryItem = base.Base.ItemSettings.Current;
					if (((inventoryItem != null) ? inventoryItem.ValMethod : null) == "T")
					{
						if (PXSelectBase<INCostStatus, PXViewOf<INCostStatus>.BasedOn<SelectFromBase<INCostStatus, TypeArrayOf<IFbqlJoin>.Empty>.Where<KeysRelation<Field<INCostStatus.inventoryID>.IsRelatedTo<InventoryItem.inventoryID>.AsSimpleKey.WithTablesOf<InventoryItem, INCostStatus>, InventoryItem, INCostStatus>.SameAsCurrent.And<BqlOperand<INCostStatus.qtyOnHand, IBqlDecimal>.IsNotEqual<decimal0>>>>.Config>.SelectWindowed(base.Base, 0, 1, Array.Empty<object>()) != null)
						{
							throw new PXException("There is non zero Quantity on Hand for this item. You can only change Cost when the Qty on Hand is equal to zero");
						}
						decimal valueOrDefault = inventoryItemCurySettings.PendingStdCost.GetValueOrDefault();
						DateTime value = inventoryItemCurySettings.PendingStdCostDate ?? base.Base.Accessinfo.BusinessDate.Value;
						inventoryItemCurySettings.LastStdCost = inventoryItemCurySettings.StdCost;
						inventoryItemCurySettings.StdCost = new decimal?(valueOrDefault);
						inventoryItemCurySettings.StdCostDate = new DateTime?(value);
						inventoryItemCurySettings.PendingStdCost = new decimal?(0m);
						inventoryItemCurySettings.PendingStdCostDate = null;
						this.curySettings.Cache.MarkUpdated(inventoryItemCurySettings, true);
						string curyID = inventoryItemCurySettings.CuryID;
						Currency baseCurrency = CurrencyCollection.GetBaseCurrency();
						if (string.Equals(curyID, (baseCurrency != null) ? baseCurrency.CuryID : null, StringComparison.OrdinalIgnoreCase))
						{
							InventoryItem inventoryItem2 = base.Base.ItemSettings.Current;
							inventoryItem2.LastStdCost = inventoryItemCurySettings.LastStdCost;
							inventoryItem2.StdCost = inventoryItemCurySettings.StdCost;
							inventoryItem2.StdCostDate = inventoryItemCurySettings.StdCostDate;
							inventoryItem2.PendingStdCost = inventoryItemCurySettings.PendingStdCost;
							inventoryItem2.PendingStdCostDate = inventoryItemCurySettings.PendingStdCostDate;
							base.Base.ItemSettings.Cache.MarkUpdated(inventoryItem2, true);
						}
						foreach (PXResult<INItemSite> r in base.Base.itemsiterecords.Select(Array.Empty<object>()))
						{
							INItemSite initemSite = r;
							if (!initemSite.StdCostOverride.GetValueOrDefault())
							{
								initemSite.LastStdCost = initemSite.StdCost;
								initemSite.StdCost = new decimal?(valueOrDefault);
								initemSite.StdCostDate = new DateTime?(value);
								initemSite.PendingStdCost = new decimal?(0m);
								initemSite.PendingStdCostDate = null;
								initemSite.PendingStdCostReset = new bool?(false);
								base.Base.itemsiterecords.Cache.MarkUpdated(initemSite, true);
							}
						}
						base.Base.Save.Press();
					}
				}
				return adapter.Get();
			}
		}

		// Token: 0x0200718D RID: 29069
		public class DefaultSiteID : PXFieldAttachedTo<InventoryItem>.By<InventoryItemMaint>.AsInteger.Named<InventoryItemMaint.DefaultSiteID>
		{
			// Token: 0x060256B6 RID: 153270 RVA: 0x007B7BAC File Offset: 0x007B5DAC
			public static bool IsActive()
			{
				return PXAccess.FeatureInstalled<FeaturesSet.warehouse>();
			}

			// Token: 0x060256B7 RID: 153271 RVA: 0x007B7BB3 File Offset: 0x007B5DB3
			public override int? GetValue(InventoryItem Row)
			{
				return Row.With((InventoryItem ii) => base.Base.GetCurySettings(ii.InventoryID, null)).With((InventoryItemCurySettings iici) => iici.DfltSiteID);
			}
		}
	}
}
