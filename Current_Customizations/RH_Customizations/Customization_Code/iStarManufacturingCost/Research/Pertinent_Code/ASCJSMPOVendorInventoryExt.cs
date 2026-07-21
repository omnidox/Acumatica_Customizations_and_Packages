using System;
using System.Runtime.CompilerServices;
using ASCJSMCustom.AP.CacheExt;
using ASCJSMCustom.Common.Descriptor;
using ASCJSMCustom.Common.DTO.Interfaces;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.IN;
using PX.Objects.PO;

namespace ASCJSMCustom.PO.CacheExt
{
	// Token: 0x02000013 RID: 19
	public class ASCJSMPOVendorInventoryExt : PXCacheExtension<POVendorInventory>, IASCJSMItemCostSpecDTO
	{
		// Token: 0x0600018D RID: 397 RVA: 0x00008696 File Offset: 0x00006896
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600018E RID: 398 RVA: 0x00008699 File Offset: 0x00006899
		// (set) Token: 0x0600018F RID: 399 RVA: 0x000086A1 File Offset: 0x000068A1
		[Inventory(Filterable = true, DirtyRead = true, Enabled = false)]
		[PXParent(typeof(SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<BqlField<ASCJSMPOVendorInventoryExt.inventoryID, IBqlInt>.FromCurrent>>))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		public virtual int? InventoryID { get; set; }

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000190 RID: 400 RVA: 0x000086AA File Offset: 0x000068AA
		// (set) Token: 0x06000191 RID: 401 RVA: 0x000086B2 File Offset: 0x000068B2
		[PXDBInt]
		[PXUIField(DisplayName = "Market")]
		[PXSelector(typeof(Search2<Vendor.bAccountID, InnerJoin<VendorClass, On<Vendor.vendorClassID, Equal<VendorClass.vendorClassID>>>, Where<VendorClass.vendorClassID, Equal<ASCJSMConstants.MarketClass>>>), new Type[]
		{
			typeof(Vendor.acctCD),
			typeof(Vendor.acctName)
		}, SubstituteKey = typeof(Vendor.acctCD), DescriptionField = typeof(Vendor.acctName))]
		[PXDefault(typeof(Search<ASCJSMVendorExt.usrMarketID, Where<Vendor.bAccountID, Equal<Current<POVendorInventory.vendorID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public int? UsrMarketID { get; set; }

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000192 RID: 402 RVA: 0x000086BB File Offset: 0x000068BB
		// (set) Token: 0x06000193 RID: 403 RVA: 0x000086C3 File Offset: 0x000068C3
		[PXDBInt]
		[PXUIField(DisplayName = "Metal")]
		[PXSelector(typeof(Search2<InventoryItem.inventoryID, InnerJoin<INItemClass, On<InventoryItem.itemClassID, Equal<INItemClass.itemClassID>>>, Where<INItemClass.itemClassCD, Equal<ASCJSMConstants.CommodityClass>>>), new Type[]
		{
			typeof(InventoryItem.inventoryCD),
			typeof(InventoryItem.descr)
		}, SubstituteKey = typeof(InventoryItem.inventoryCD), DescriptionField = typeof(InventoryItem.descr))]
		public int? UsrCommodityID { get; set; }

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000194 RID: 404 RVA: 0x000086CC File Offset: 0x000068CC
		// (set) Token: 0x06000195 RID: 405 RVA: 0x000086D4 File Offset: 0x000068D4
		[PXDBBool]
		[PXUIField(DisplayName = "Override Vendor", Visible = false)]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? UsrIsOverrideVendor { get; set; }

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000196 RID: 406 RVA: 0x000086DD File Offset: 0x000068DD
		// (set) Token: 0x06000197 RID: 407 RVA: 0x000086E5 File Offset: 0x000068E5
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Custom Price", Visible = false)]
		public decimal? UsrCommodityVendorPrice { get; set; }

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000198 RID: 408 RVA: 0x000086EE File Offset: 0x000068EE
		// (set) Token: 0x06000199 RID: 409 RVA: 0x000086F6 File Offset: 0x000068F6
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Basis Price", IsReadOnly = true)]
		public decimal? UsrBasisPrice { get; set; }

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x0600019A RID: 410 RVA: 0x000086FF File Offset: 0x000068FF
		// (set) Token: 0x0600019B RID: 411 RVA: 0x00008707 File Offset: 0x00006907
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Price / TOZ @ Basis", IsReadOnly = true)]
		public decimal? UsrBasisValue { get; set; }

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x0600019C RID: 412 RVA: 0x00008710 File Offset: 0x00006910
		// (set) Token: 0x0600019D RID: 413 RVA: 0x00008718 File Offset: 0x00006918
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Increment")]
		public decimal? UsrContractIncrement { get; set; }

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x0600019E RID: 414 RVA: 0x00008721 File Offset: 0x00006921
		// (set) Token: 0x0600019F RID: 415 RVA: 0x00008729 File Offset: 0x00006929
		[PXDBDecimal(2, MinValue = 0.0, MaxValue = 10.0)]
		[PXUIField(DisplayName = "Matrix Step")]
		[PXDefault(TypeCode.Decimal, "0.500000", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? UsrMatrixStep { get; set; }

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060001A0 RID: 416 RVA: 0x00008732 File Offset: 0x00006932
		// (set) Token: 0x060001A1 RID: 417 RVA: 0x0000873A File Offset: 0x0000693A
		[PXDBDecimal(4)]
		[PXUIField(DisplayName = "Floor", IsReadOnly = true)]
		public decimal? UsrFloor { get; set; }

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x00008743 File Offset: 0x00006943
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x0000874B File Offset: 0x0000694B
		[PXDBDecimal(4)]
		[PXUIField(DisplayName = "Ceiling", IsReadOnly = true)]
		public decimal? UsrCeiling { get; set; }

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060001A4 RID: 420 RVA: 0x00008754 File Offset: 0x00006954
		// (set) Token: 0x060001A5 RID: 421 RVA: 0x0000875C File Offset: 0x0000695C
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Metal Loss %", Visible = false)]
		public decimal? UsrContractLossPct { get; set; }

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x00008765 File Offset: 0x00006965
		// (set) Token: 0x060001A7 RID: 423 RVA: 0x0000876D File Offset: 0x0000696D
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Surcharge, %", Visible = false)]
		public decimal? UsrContractSurcharge { get; set; }

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060001A8 RID: 424 RVA: 0x00008776 File Offset: 0x00006976
		// (set) Token: 0x060001A9 RID: 425 RVA: 0x0000877E File Offset: 0x0000697E
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Surcharge, $", Visible = false)]
		public decimal? UsrContractSurchargeAmount { get; set; }

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060001AA RID: 426 RVA: 0x00008787 File Offset: 0x00006987
		// (set) Token: 0x060001AB RID: 427 RVA: 0x0000878F File Offset: 0x0000698F
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Precious Metal Cost")]
		public decimal? UsrPreciousMetalCost { get; set; }

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060001AC RID: 428 RVA: 0x00008798 File Offset: 0x00006998
		// (set) Token: 0x060001AD RID: 429 RVA: 0x000087A0 File Offset: 0x000069A0
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Other Material Cost")]
		public decimal? UsrOtherMaterialsCost { get; set; }

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060001AE RID: 430 RVA: 0x000087A9 File Offset: 0x000069A9
		// (set) Token: 0x060001AF RID: 431 RVA: 0x000087B1 File Offset: 0x000069B1
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Fabrication / Value Add")]
		public decimal? UsrFabricationCost { get; set; }

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060001B0 RID: 432 RVA: 0x000087BA File Offset: 0x000069BA
		// (set) Token: 0x060001B1 RID: 433 RVA: 0x000087C2 File Offset: 0x000069C2
		[PXDBDecimal(4, MinValue = 0.0, MaxValue = 1000.0)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Packaging Cost")]
		public decimal? UsrPackagingCost { get; set; }

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060001B2 RID: 434 RVA: 0x000087CB File Offset: 0x000069CB
		// (set) Token: 0x060001B3 RID: 435 RVA: 0x000087D3 File Offset: 0x000069D3
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Labor packaging")]
		public decimal? UsrPackagingLaborCost { get; set; }

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060001B4 RID: 436 RVA: 0x000087DC File Offset: 0x000069DC
		// (set) Token: 0x060001B5 RID: 437 RVA: 0x000087E4 File Offset: 0x000069E4
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "In-house Labor Cost", Visible = false)]
		public decimal? UsrLaborCost { get; set; }

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060001B6 RID: 438 RVA: 0x000087ED File Offset: 0x000069ED
		// (set) Token: 0x060001B7 RID: 439 RVA: 0x000087F5 File Offset: 0x000069F5
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Handling Cost", Visible = false)]
		public decimal? UsrHandlingCost { get; set; }

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060001B8 RID: 440 RVA: 0x000087FE File Offset: 0x000069FE
		// (set) Token: 0x060001B9 RID: 441 RVA: 0x00008806 File Offset: 0x00006A06
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Freight Cost", Visible = false)]
		public decimal? UsrFreightCost { get; set; }

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060001BA RID: 442 RVA: 0x0000880F File Offset: 0x00006A0F
		// (set) Token: 0x060001BB RID: 443 RVA: 0x00008817 File Offset: 0x00006A17
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Duty Cost", Visible = false)]
		public decimal? UsrDutyCost { get; set; }

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060001BC RID: 444 RVA: 0x00008820 File Offset: 0x00006A20
		// (set) Token: 0x060001BD RID: 445 RVA: 0x00008828 File Offset: 0x00006A28
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.000000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Other Cost", Enabled = false, Visible = false)]
		public decimal? UsrOtherCost { get; set; }

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00008831 File Offset: 0x00006A31
		// (set) Token: 0x060001BF RID: 447 RVA: 0x00008839 File Offset: 0x00006A39
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Unit Cost", IsReadOnly = true)]
		[PXFormula(typeof(Add<Add<Add<Add<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost, ASCJSMPOVendorInventoryExt.usrOtherMaterialsCost>, ASCJSMPOVendorInventoryExt.usrFabricationCost>, ASCJSMPOVendorInventoryExt.usrPackagingCost>, ASCJSMPOVendorInventoryExt.usrPackagingLaborCost>))]
		public decimal? UsrUnitCost { get; set; }

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00008842 File Offset: 0x00006A42
		// (set) Token: 0x060001C1 RID: 449 RVA: 0x0000884A File Offset: 0x00006A4A
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.00", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Est. Landed Cost", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXFormula(typeof(Add<Add<Add<Add<ASCJSMPOVendorInventoryExt.usrUnitCost, ASCJSMPOVendorInventoryExt.usrHandlingCost>, ASCJSMPOVendorInventoryExt.usrFreightCost>, ASCJSMPOVendorInventoryExt.usrLaborCost>, ASCJSMPOVendorInventoryExt.usrDutyCost>))]
		public decimal? UsrEstLandedCost { get; set; }

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060001C2 RID: 450 RVA: 0x00008853 File Offset: 0x00006A53
		// (set) Token: 0x060001C3 RID: 451 RVA: 0x0000885B File Offset: 0x00006A5B
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Fabrication / Weight", Visibility = PXUIVisibility.Visible)]
		public decimal? UsrFabricationWeight { get; set; }

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060001C4 RID: 452 RVA: 0x00008864 File Offset: 0x00006A64
		// (set) Token: 0x060001C5 RID: 453 RVA: 0x0000886C File Offset: 0x00006A6C
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0000", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Fabrication / Piece", Visibility = PXUIVisibility.Visible)]
		public decimal? UsrFabricationPiece { get; set; }

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060001C6 RID: 454 RVA: 0x00008875 File Offset: 0x00006A75
		// (set) Token: 0x060001C7 RID: 455 RVA: 0x0000887D File Offset: 0x00006A7D
		[PXDecimal(6)]
		public decimal? UsrActualGRAMGold { get; set; }

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x060001C8 RID: 456 RVA: 0x00008886 File Offset: 0x00006A86
		// (set) Token: 0x060001C9 RID: 457 RVA: 0x0000888E File Offset: 0x00006A8E
		[PXDecimal(6)]
		public decimal? UsrPricingGRAMSilver { get; set; }

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x060001CA RID: 458 RVA: 0x00008897 File Offset: 0x00006A97
		// (set) Token: 0x060001CB RID: 459 RVA: 0x0000889F File Offset: 0x00006A9F
		[PXDecimal(6)]
		public decimal? UsrPricingGRAMGold { get; set; }

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060001CC RID: 460 RVA: 0x000088A8 File Offset: 0x00006AA8
		// (set) Token: 0x060001CD RID: 461 RVA: 0x000088B0 File Offset: 0x00006AB0
		[PXDecimal(6)]
		public decimal? UsrActualGRAMSilver { get; set; }

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060001CE RID: 462 RVA: 0x000088B9 File Offset: 0x00006AB9
		// (set) Token: 0x060001CF RID: 463 RVA: 0x000088C1 File Offset: 0x00006AC1
		[PXDecimal(6)]
		public decimal? UsrDutyCostPct { get; set; }

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x060001D0 RID: 464 RVA: 0x000088CA File Offset: 0x00006ACA
		// (set) Token: 0x060001D1 RID: 465 RVA: 0x000088D2 File Offset: 0x00006AD2
		[PXString]
		public string UsrCostingType { get; set; }

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x060001D2 RID: 466 RVA: 0x000088DB File Offset: 0x00006ADB
		// (set) Token: 0x060001D3 RID: 467 RVA: 0x000088E3 File Offset: 0x00006AE3
		[PXString]
		public string UsrCommodityType { get; set; }

		// Token: 0x020000D9 RID: 217
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class inventoryID : BqlType<IBqlInt, int>.Field<ASCJSMPOVendorInventoryExt.inventoryID>
		{
		}

		// Token: 0x020000DA RID: 218
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMarketID : BqlType<IBqlInt, int>.Field<ASCJSMPOVendorInventoryExt.usrMarketID>
		{
		}

		// Token: 0x020000DB RID: 219
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrCommodityID : BqlType<IBqlInt, int>.Field<ASCJSMPOVendorInventoryExt.usrCommodityID>
		{
		}

		// Token: 0x020000DC RID: 220
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrIsOverrideVendor : BqlType<IBqlBool, bool>.Field<ASCJSMPOVendorInventoryExt.usrIsOverrideVendor>
		{
		}

		// Token: 0x020000DD RID: 221
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrCommodityVendorPrice : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrCommodityVendorPrice>
		{
		}

		// Token: 0x020000DE RID: 222
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrBasisPrice : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrBasisPrice>
		{
		}

		// Token: 0x020000DF RID: 223
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrBasisValue : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrBasisValue>
		{
		}

		// Token: 0x020000E0 RID: 224
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractIncrement : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrContractIncrement>
		{
		}

		// Token: 0x020000E1 RID: 225
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrMatrixStep : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrMatrixStep>
		{
		}

		// Token: 0x020000E2 RID: 226
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFloor : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrFloor>
		{
		}

		// Token: 0x020000E3 RID: 227
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrCeiling : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrCeiling>
		{
		}

		// Token: 0x020000E4 RID: 228
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractLossPct : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrContractLossPct>
		{
		}

		// Token: 0x020000E5 RID: 229
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractSurcharge : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrContractSurcharge>
		{
		}

		// Token: 0x020000E6 RID: 230
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContractSurchargeAmount : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrContractSurchargeAmount>
		{
		}

		// Token: 0x020000E7 RID: 231
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPreciousMetalCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrPreciousMetalCost>
		{
		}

		// Token: 0x020000E8 RID: 232
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrOtherMaterialsCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrOtherMaterialsCost>
		{
		}

		// Token: 0x020000E9 RID: 233
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFabricationCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrFabricationCost>
		{
		}

		// Token: 0x020000EA RID: 234
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPackagingCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrPackagingCost>
		{
		}

		// Token: 0x020000EB RID: 235
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPackagingLaborCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrPackagingLaborCost>
		{
		}

		// Token: 0x020000EC RID: 236
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrLaborCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrLaborCost>
		{
		}

		// Token: 0x020000ED RID: 237
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrHandlingCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrHandlingCost>
		{
		}

		// Token: 0x020000EE RID: 238
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFreightCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrFreightCost>
		{
		}

		// Token: 0x020000EF RID: 239
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrDutyCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrDutyCost>
		{
		}

		// Token: 0x020000F0 RID: 240
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrOtherCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrOtherCost>
		{
		}

		// Token: 0x020000F1 RID: 241
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrUnitCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrUnitCost>
		{
		}

		// Token: 0x020000F2 RID: 242
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEstLandedCost : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrEstLandedCost>
		{
		}

		// Token: 0x020000F3 RID: 243
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFabricationWeight : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrFabricationWeight>
		{
		}

		// Token: 0x020000F4 RID: 244
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrFabricationPiece : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrFabricationPiece>
		{
		}

		// Token: 0x020000F5 RID: 245
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrActualGRAMGold : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrActualGRAMGold>
		{
		}

		// Token: 0x020000F6 RID: 246
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrActualGRAMSilver : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrActualGRAMSilver>
		{
		}

		// Token: 0x020000F7 RID: 247
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPricingGRAMGold : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrPricingGRAMGold>
		{
		}

		// Token: 0x020000F8 RID: 248
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPricingGRAMSilver : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrPricingGRAMSilver>
		{
		}

		// Token: 0x020000F9 RID: 249
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrDutyCostPct : BqlType<IBqlDecimal, decimal>.Field<ASCJSMPOVendorInventoryExt.usrDutyCostPct>
		{
		}

		// Token: 0x020000FA RID: 250
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCostingType : BqlType<IBqlString, string>.Field<ASCJSMPOVendorInventoryExt.usrCostingType>
		{
		}

		// Token: 0x020000FB RID: 251
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCommodityType : BqlType<IBqlString, string>.Field<ASCJSMPOVendorInventoryExt.usrCommodityType>
		{
		}
	}
}
