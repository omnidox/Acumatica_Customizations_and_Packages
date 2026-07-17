using System;
using System.Runtime.CompilerServices;
using ASCJSMCustom.Common.Descriptor;
using ASCJSMCustom.Common.DTO.Interfaces;
using ASCJSMCustom.IN.Descriptor.Constants;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;
using PX.Objects.IN;

namespace ASCJSMCustom.IN.CacheExt
{
	// Token: 0x0200001B RID: 27
	public class ASCJSMINInventoryItemExt : PXCacheExtension<InventoryItem>, IASCJSMItemCostSpecDTO
	{
		// Token: 0x06000289 RID: 649 RVA: 0x0000C6B5 File Offset: 0x0000A8B5
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x0600028A RID: 650 RVA: 0x0000C6B8 File Offset: 0x0000A8B8
		// (set) Token: 0x0600028B RID: 651 RVA: 0x0000C6C0 File Offset: 0x0000A8C0
		[PXDBIdentity]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible, Visible = false)]
		[PXReferentialIntegrityCheck]
		public virtual int? InventoryID { get; set; }

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x0600028C RID: 652 RVA: 0x0000C6C9 File Offset: 0x0000A8C9
		// (set) Token: 0x0600028D RID: 653 RVA: 0x0000C6D1 File Offset: 0x0000A8D1
		[PXDBString(2, IsFixed = true)]
		[PXDefault("NP")]
		[PXUIField(DisplayName = "Item Status", Visibility = PXUIVisibility.SelectorVisible)]
		[ASCJSMINConstants.InventoryItemStatusExt.ListAttribute]
		public virtual string ItemStatus { get; set; }

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x0600028E RID: 654 RVA: 0x0000C6DA File Offset: 0x0000A8DA
		// (set) Token: 0x0600028F RID: 655 RVA: 0x0000C6E2 File Offset: 0x0000A8E2
		[PXDBString(30)]
		[PXUIField(DisplayName = "Legacy Short Ref")]
		public string UsrLegacyShortRef { get; set; }

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000290 RID: 656 RVA: 0x0000C6EB File Offset: 0x0000A8EB
		// (set) Token: 0x06000291 RID: 657 RVA: 0x0000C6F3 File Offset: 0x0000A8F3
		[PXDBString(30)]
		[PXUIField(DisplayName = "Legacy ID")]
		public string UsrLegacyID { get; set; }

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000292 RID: 658 RVA: 0x0000C6FC File Offset: 0x0000A8FC
		// (set) Token: 0x06000293 RID: 659 RVA: 0x0000C704 File Offset: 0x0000A904
		[PXDBInt]
		[PXUIField(DisplayName = "Price as Item", Visible = false, Enabled = false)]
		[PXSelector(typeof(Search2<InventoryItem.inventoryID, LeftJoin<INItemClass, On<InventoryItem.itemClassID, Equal<INItemClass.itemClassID>>>, Where<INItemClass.itemClassCD, Equal<ASCJSMConstants.CommodityClass>>>), new Type[]
		{
			typeof(InventoryItem.inventoryCD),
			typeof(InventoryItem.descr)
		}, SubstituteKey = typeof(InventoryItem.inventoryCD), DescriptionField = typeof(InventoryItem.descr))]
		public int? UsrPriceAsID { get; set; }

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000294 RID: 660 RVA: 0x0000C70D File Offset: 0x0000A90D
		// (set) Token: 0x06000295 RID: 661 RVA: 0x0000C715 File Offset: 0x0000A915
		[PXString]
		public string UsrPriceToUnit { get; set; }

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x06000296 RID: 662 RVA: 0x0000C71E File Offset: 0x0000A91E
		// (set) Token: 0x06000297 RID: 663 RVA: 0x0000C726 File Offset: 0x0000A926
		[PXDBDecimal(28)]
		[PXUIField(DisplayName = "Fine Gold, Grams")]
		public decimal? UsrPricingGRAMGold { get; set; }

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x06000298 RID: 664 RVA: 0x0000C72F File Offset: 0x0000A92F
		// (set) Token: 0x06000299 RID: 665 RVA: 0x0000C737 File Offset: 0x0000A937
		[PXDBDecimal(28)]
		[PXUIField(DisplayName = "Fine Silver, Grams")]
		public decimal? UsrPricingGRAMSilver { get; set; }

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x0600029A RID: 666 RVA: 0x0000C740 File Offset: 0x0000A940
		// (set) Token: 0x0600029B RID: 667 RVA: 0x0000C748 File Offset: 0x0000A948
		[PXDBDecimal(28)]
		[PXUIField(DisplayName = "Gold, Grams")]
		public decimal? UsrActualGRAMGold { get; set; }

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x0600029C RID: 668 RVA: 0x0000C751 File Offset: 0x0000A951
		// (set) Token: 0x0600029D RID: 669 RVA: 0x0000C759 File Offset: 0x0000A959
		[PXDBDecimal(28)]
		[PXUIField(DisplayName = "Silver, Grams")]
		public decimal? UsrActualGRAMSilver { get; set; }

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x0600029E RID: 670 RVA: 0x0000C762 File Offset: 0x0000A962
		// (set) Token: 0x0600029F RID: 671 RVA: 0x0000C76A File Offset: 0x0000A96A
		[PXDBString(1, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Costing Type", Required = true)]
		[ASCJSMConstants.CostingType.ListAttribute]
		[PXDefault("C")]
		public string UsrCostingType { get; set; }

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x060002A0 RID: 672 RVA: 0x0000C773 File Offset: 0x0000A973
		// (set) Token: 0x060002A1 RID: 673 RVA: 0x0000C77B File Offset: 0x0000A97B
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Price / TOZ @ Basis", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrBasisValue { get; set; }

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x060002A2 RID: 674 RVA: 0x0000C784 File Offset: 0x0000A984
		// (set) Token: 0x060002A3 RID: 675 RVA: 0x0000C78C File Offset: 0x0000A98C
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Price / TOZ Add-On", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrBasisValueAddOn { get; set; }

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x060002A4 RID: 676 RVA: 0x0000C795 File Offset: 0x0000A995
		// (set) Token: 0x060002A5 RID: 677 RVA: 0x0000C79D File Offset: 0x0000A99D
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Market Price per Gram", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMarketPriceGram { get; set; }

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x060002A6 RID: 678 RVA: 0x0000C7A6 File Offset: 0x0000A9A6
		// (set) Token: 0x060002A7 RID: 679 RVA: 0x0000C7AE File Offset: 0x0000A9AE
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Market Price per TOZ Add-On", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMarketPriceAddOn { get; set; }

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x060002A8 RID: 680 RVA: 0x0000C7B7 File Offset: 0x0000A9B7
		// (set) Token: 0x060002A9 RID: 681 RVA: 0x0000C7BF File Offset: 0x0000A9BF
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Market Price per TOZ", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMarketPriceTOZ { get; set; }

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x060002AA RID: 682 RVA: 0x0000C7C8 File Offset: 0x0000A9C8
		// (set) Token: 0x060002AB RID: 683 RVA: 0x0000C7D0 File Offset: 0x0000A9D0
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Matrix Price per Gram", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMatrixPriceGram { get; set; }

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x060002AC RID: 684 RVA: 0x0000C7D9 File Offset: 0x0000A9D9
		// (set) Token: 0x060002AD RID: 685 RVA: 0x0000C7E1 File Offset: 0x0000A9E1
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Matrix Price per TOZ", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMatrixPriceTOZ { get; set; }

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x060002AE RID: 686 RVA: 0x0000C7EA File Offset: 0x0000A9EA
		// (set) Token: 0x060002AF RID: 687 RVA: 0x0000C7F2 File Offset: 0x0000A9F2
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 100.0)]
		[PXUIField(DisplayName = "Metal Loss, %")]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrContractLossPct { get; set; }

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x060002B0 RID: 688 RVA: 0x0000C7FB File Offset: 0x0000A9FB
		// (set) Token: 0x060002B1 RID: 689 RVA: 0x0000C803 File Offset: 0x0000AA03
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Vendor Increment")]
		public decimal? UsrContractIncrement { get; set; }

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x060002B2 RID: 690 RVA: 0x0000C80C File Offset: 0x0000AA0C
		// (set) Token: 0x060002B3 RID: 691 RVA: 0x0000C814 File Offset: 0x0000AA14
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Enable Vendor Increment")]
		public bool? UsrEnableVendorIncrement { get; set; }

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x060002B4 RID: 692 RVA: 0x0000C81D File Offset: 0x0000AA1D
		// (set) Token: 0x060002B5 RID: 693 RVA: 0x0000C825 File Offset: 0x0000AA25
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Increment", Enabled = false)]
		public decimal? UsrIncrement { get; set; }

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x060002B6 RID: 694 RVA: 0x0000C82E File Offset: 0x0000AA2E
		// (set) Token: 0x060002B7 RID: 695 RVA: 0x0000C836 File Offset: 0x0000AA36
		[PXDBDecimal(2, MinValue = 0.0, MaxValue = 10.0)]
		[PXUIField(DisplayName = "Matrix Step")]
		[PXDefault(TypeCode.Decimal, "0.500000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMatrixStep { get; set; }

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x060002B8 RID: 696 RVA: 0x0000C83F File Offset: 0x0000AA3F
		// (set) Token: 0x060002B9 RID: 697 RVA: 0x0000C847 File Offset: 0x0000AA47
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Floor", Enabled = false, Visible = false)]
		public decimal? UsrFloor { get; set; }

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x060002BA RID: 698 RVA: 0x0000C850 File Offset: 0x0000AA50
		// (set) Token: 0x060002BB RID: 699 RVA: 0x0000C858 File Offset: 0x0000AA58
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Ceiling", Enabled = false, Visible = false)]
		public decimal? UsrCeiling { get; set; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x060002BC RID: 700 RVA: 0x0000C861 File Offset: 0x0000AA61
		// (set) Token: 0x060002BD RID: 701 RVA: 0x0000C869 File Offset: 0x0000AA69
		[PXDBDecimal(4, MinValue = -100.0, MaxValue = 100.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Surcharge, %")]
		public decimal? UsrContractSurcharge { get; set; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x060002BE RID: 702 RVA: 0x0000C872 File Offset: 0x0000AA72
		// (set) Token: 0x060002BF RID: 703 RVA: 0x0000C87A File Offset: 0x0000AA7A
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Surcharge, $")]
		public decimal? UsrContractSurchargeAmount { get; set; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x060002C0 RID: 704 RVA: 0x0000C883 File Offset: 0x0000AA83
		// (set) Token: 0x060002C1 RID: 705 RVA: 0x0000C88B File Offset: 0x0000AA8B
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Precious Metal Cost")]
		public decimal? UsrPreciousMetalCost { get; set; }

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x060002C2 RID: 706 RVA: 0x0000C894 File Offset: 0x0000AA94
		// (set) Token: 0x060002C3 RID: 707 RVA: 0x0000C89C File Offset: 0x0000AA9C
		[PXUIField(DisplayName = "Other Materials Cost")]
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrOtherMaterialsCost { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x060002C4 RID: 708 RVA: 0x0000C8A5 File Offset: 0x0000AAA5
		// (set) Token: 0x060002C5 RID: 709 RVA: 0x0000C8AD File Offset: 0x0000AAAD
		[PXDBDecimal(4)]
		[PXUIField(DisplayName = "Fabrication/Value Add")]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrFabricationCost { get; set; }

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x060002C6 RID: 710 RVA: 0x0000C8B6 File Offset: 0x0000AAB6
		// (set) Token: 0x060002C7 RID: 711 RVA: 0x0000C8BE File Offset: 0x0000AABE
		[PXUIField(DisplayName = "In-house Labor Cost")]
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrLaborCost { get; set; }

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x060002C8 RID: 712 RVA: 0x0000C8C7 File Offset: 0x0000AAC7
		// (set) Token: 0x060002C9 RID: 713 RVA: 0x0000C8CF File Offset: 0x0000AACF
		[PXDBDecimal(4)]
		[PXUIField(DisplayName = "Handling Cost")]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrHandlingCost { get; set; }

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x060002CA RID: 714 RVA: 0x0000C8D8 File Offset: 0x0000AAD8
		// (set) Token: 0x060002CB RID: 715 RVA: 0x0000C8E0 File Offset: 0x0000AAE0
		[PXDBDecimal(4)]
		[PXUIField(DisplayName = "Freight Cost")]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrFreightCost { get; set; }

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x060002CC RID: 716 RVA: 0x0000C8E9 File Offset: 0x0000AAE9
		// (set) Token: 0x060002CD RID: 717 RVA: 0x0000C8F1 File Offset: 0x0000AAF1
		[PXUIField(DisplayName = "Duty Cost")]
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrDutyCost { get; set; }

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x060002CE RID: 718 RVA: 0x0000C8FA File Offset: 0x0000AAFA
		// (set) Token: 0x060002CF RID: 719 RVA: 0x0000C902 File Offset: 0x0000AB02
		[PXUIField(DisplayName = "Duty, %")]
		[PXDBDecimal(2, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrDutyCostPct { get; set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x060002D0 RID: 720 RVA: 0x0000C90B File Offset: 0x0000AB0B
		// (set) Token: 0x060002D1 RID: 721 RVA: 0x0000C913 File Offset: 0x0000AB13
		[PXUIField(DisplayName = "Other Cost", Visible = false)]
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrOtherCost { get; set; }

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x060002D2 RID: 722 RVA: 0x0000C91C File Offset: 0x0000AB1C
		// (set) Token: 0x060002D3 RID: 723 RVA: 0x0000C924 File Offset: 0x0000AB24
		[PXUIField(DisplayName = "Packaging Cost")]
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrPackagingCost { get; set; }

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x060002D4 RID: 724 RVA: 0x0000C92D File Offset: 0x0000AB2D
		// (set) Token: 0x060002D5 RID: 725 RVA: 0x0000C935 File Offset: 0x0000AB35
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Labor Packaging")]
		public decimal? UsrPackagingLaborCost { get; set; }

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x060002D6 RID: 726 RVA: 0x0000C93E File Offset: 0x0000AB3E
		// (set) Token: 0x060002D7 RID: 727 RVA: 0x0000C946 File Offset: 0x0000AB46
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Unit Cost", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXFormula(typeof(Add<Add<Add<Add<Add<Add<ASCJSMINInventoryItemExt.usrPackagingLaborCost, ASCJSMINInventoryItemExt.usrOtherMaterialsCost>, ASCJSMINInventoryItemExt.usrFabricationCost>, ASCJSMINInventoryItemExt.usrPackagingCost>, ASCJSMINInventoryItemExt.usrPreciousMetalCost>, ASCJSMINInventoryItemExt.usrLaborCost>, ASCJSMINInventoryItemExt.usrHandlingCost>))]
		public decimal? UsrUnitCost { get; set; }

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x060002D8 RID: 728 RVA: 0x0000C94F File Offset: 0x0000AB4F
		// (set) Token: 0x060002D9 RID: 729 RVA: 0x0000C957 File Offset: 0x0000AB57
		[PXDecimal(4)]
		[PXUIField(DisplayName = "Est. Landed Cost", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXFormula(typeof(Add<Add<ASCJSMINInventoryItemExt.usrDutyCost, ASCJSMINInventoryItemExt.usrFreightCost>, ASCJSMINInventoryItemExt.usrUnitCost>))]
		public decimal? UsrEstLandedCost { get; set; }

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x060002DA RID: 730 RVA: 0x0000C960 File Offset: 0x0000AB60
		// (set) Token: 0x060002DB RID: 731 RVA: 0x0000C968 File Offset: 0x0000AB68
		[PXDBString(1)]
		[PXUIField(DisplayName = "Commodity Type")]
		[ASCJSMConstants.CommodityType.ListAttribute]
		public virtual string UsrCommodityType { get; set; }

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x060002DC RID: 732 RVA: 0x0000C971 File Offset: 0x0000AB71
		// (set) Token: 0x060002DD RID: 733 RVA: 0x0000C979 File Offset: 0x0000AB79
		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "Default Vendor Country of Origin", Enabled = false)]
		[Country]
		public string UsrDefaultVendorCountryOfOrigin { get; set; }

		// Token: 0x0200012E RID: 302
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class inventoryID : BqlType<IBqlInt, int>.Field<ASCJSMINInventoryItemExt.inventoryID>
		{
		}

		// Token: 0x0200012F RID: 303
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class itemStatus : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.itemStatus>
		{
		}

		// Token: 0x02000130 RID: 304
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrLegacyShortRef : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.usrLegacyShortRef>
		{
		}

		// Token: 0x02000131 RID: 305
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrLegacyID : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.usrLegacyID>
		{
		}

		// Token: 0x02000132 RID: 306
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPriceAsID : BqlType<IBqlInt, int>.Field<ASCJSMINInventoryItemExt.usrPriceAsID>
		{
		}

		// Token: 0x02000133 RID: 307
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrPriceToUnit : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.usrPriceToUnit>
		{
		}

		// Token: 0x02000134 RID: 308
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPricingGRAMGold : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrPricingGRAMGold>
		{
		}

		// Token: 0x02000135 RID: 309
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPricingGRAMSilver : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrPricingGRAMSilver>
		{
		}

		// Token: 0x02000136 RID: 310
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrActualGRAMGold : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrActualGRAMGold>
		{
		}

		// Token: 0x02000137 RID: 311
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrActualGRAMSilver : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrActualGRAMSilver>
		{
		}

		// Token: 0x02000138 RID: 312
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCostingType : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.usrCostingType>
		{
		}

		// Token: 0x02000139 RID: 313
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrBasisValue : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrBasisValue>
		{
		}

		// Token: 0x0200013A RID: 314
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrBasisValueAddOn : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrBasisValueAddOn>
		{
		}

		// Token: 0x0200013B RID: 315
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMarketPriceGram : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrMarketPriceGram>
		{
		}

		// Token: 0x0200013C RID: 316
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMarketPriceAddOn : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrMarketPriceAddOn>
		{
		}

		// Token: 0x0200013D RID: 317
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMarketPriceTOZ : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrMarketPriceTOZ>
		{
		}

		// Token: 0x0200013E RID: 318
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMatrixPriceGram : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrMatrixPriceGram>
		{
		}

		// Token: 0x0200013F RID: 319
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMatrixPriceTOZ : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrMatrixPriceTOZ>
		{
		}

		// Token: 0x02000140 RID: 320
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractLossPct : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrContractLossPct>
		{
		}

		// Token: 0x02000141 RID: 321
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractIncrement : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrContractIncrement>
		{
		}

		// Token: 0x02000142 RID: 322
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEnableVendorIncrement : BqlType<IBqlBool, bool>.Field<ASCJSMINInventoryItemExt.usrEnableVendorIncrement>
		{
		}

		// Token: 0x02000143 RID: 323
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrIncrement : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrIncrement>
		{
		}

		// Token: 0x02000144 RID: 324
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMatrixStep : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrMatrixStep>
		{
		}

		// Token: 0x02000145 RID: 325
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFloor : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrFloor>
		{
		}

		// Token: 0x02000146 RID: 326
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrCeiling : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrCeiling>
		{
		}

		// Token: 0x02000147 RID: 327
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractSurcharge : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrContractSurcharge>
		{
		}

		// Token: 0x02000148 RID: 328
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractSurchargeAmount : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrContractSurchargeAmount>
		{
		}

		// Token: 0x02000149 RID: 329
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPreciousMetalCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrPreciousMetalCost>
		{
		}

		// Token: 0x0200014A RID: 330
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrOtherMaterialsCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrOtherMaterialsCost>
		{
		}

		// Token: 0x0200014B RID: 331
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFabricationCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrFabricationCost>
		{
		}

		// Token: 0x0200014C RID: 332
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrLaborCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrLaborCost>
		{
		}

		// Token: 0x0200014D RID: 333
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrHandlingCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrHandlingCost>
		{
		}

		// Token: 0x0200014E RID: 334
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFreightCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrFreightCost>
		{
		}

		// Token: 0x0200014F RID: 335
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrDutyCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrDutyCost>
		{
		}

		// Token: 0x02000150 RID: 336
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrDutyCostPct : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrDutyCostPct>
		{
		}

		// Token: 0x02000151 RID: 337
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrOtherCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrOtherCost>
		{
		}

		// Token: 0x02000152 RID: 338
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPackagingCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrPackagingCost>
		{
		}

		// Token: 0x02000153 RID: 339
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPackagingLaborCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrPackagingLaborCost>
		{
		}

		// Token: 0x02000154 RID: 340
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrUnitCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrUnitCost>
		{
		}

		// Token: 0x02000155 RID: 341
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEstLandedCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMINInventoryItemExt.usrEstLandedCost>
		{
		}

		// Token: 0x02000156 RID: 342
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCommodityType : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.usrCommodityType>
		{
		}

		// Token: 0x02000157 RID: 343
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrDefaultVendorCountryOfOrigin : BqlType<IBqlString, string>.Field<ASCJSMINInventoryItemExt.usrDefaultVendorCountryOfOrigin>
		{
		}
	}
}
