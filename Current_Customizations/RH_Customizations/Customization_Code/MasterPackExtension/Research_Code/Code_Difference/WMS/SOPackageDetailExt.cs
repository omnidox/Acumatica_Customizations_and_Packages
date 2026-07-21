using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000016 RID: 22
	public sealed class SOPackageDetailExt : PXCacheExtension<SOPackageDetail>
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00004683 File Offset: 0x00002883
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x0000468B File Offset: 0x0000288B
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Master Pack Carton")]
		public bool? UsrIsParentBox { get; set; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x00004694 File Offset: 0x00002894
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x0000469C File Offset: 0x0000289C
		[PXDBString(15, IsUnicode = true)]
		[PXSelector(typeof(Search<SOPackageDetailExt.usrCartonNbr, Where<SOPackageDetailExt.usrIsParentBox, Equal<True>, And<SOPackageDetail.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>>>), new Type[]
		{
			typeof(SOPackageDetailExt.usrCartonNbr),
			typeof(SOPackageDetail.boxID)
		})]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Contains In Master Pack Carton #")]
		public string UsrSelectedParentBox { get; set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x000046A5 File Offset: 0x000028A5
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x000046AD File Offset: 0x000028AD
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		public string UsrSepareteOrderNbr { get; set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x000046B6 File Offset: 0x000028B6
		// (set) Token: 0x060000C9 RID: 201 RVA: 0x000046BE File Offset: 0x000028BE
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Order Nbr")]
		public string UsrOrderNbr { get; set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000CA RID: 202 RVA: 0x000046C7 File Offset: 0x000028C7
		// (set) Token: 0x060000CB RID: 203 RVA: 0x000046CF File Offset: 0x000028CF
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Store #")]
		public string UsrStoreNbr { get; set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000CC RID: 204 RVA: 0x000046D8 File Offset: 0x000028D8
		// (set) Token: 0x060000CD RID: 205 RVA: 0x000046E0 File Offset: 0x000028E0
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Carton #", IsReadOnly = true)]
		[AutoNumber(typeof(SOSetupExt.usrCartonNumberingSequence), typeof(SOPackageDetail.createdDateTime))]
		public string UsrCartonNbr { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000CE RID: 206 RVA: 0x000046E9 File Offset: 0x000028E9
		// (set) Token: 0x060000CF RID: 207 RVA: 0x000046F1 File Offset: 0x000028F1
		[PXDBString(100, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "SO Box Nbr", IsReadOnly = true)]
		public string UsrSOBoxNbrStr { get; set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000D0 RID: 208 RVA: 0x000046FA File Offset: 0x000028FA
		// (set) Token: 0x060000D1 RID: 209 RVA: 0x00004702 File Offset: 0x00002902
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Est Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrEstPackageQuantity { get; set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000D2 RID: 210 RVA: 0x0000470B File Offset: 0x0000290B
		// (set) Token: 0x060000D3 RID: 211 RVA: 0x00004713 File Offset: 0x00002913
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Contents of Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrContentPackageQuantity { get; set; }

		// Token: 0x02000062 RID: 98
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrIsParentBox : BqlType<IBqlBool, bool>.Field<SOPackageDetailExt.usrIsParentBox>
		{
		}

		// Token: 0x02000063 RID: 99
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrSelectedParentBox : BqlType<IBqlString, string>.Field<SOPackageDetailExt.usrSelectedParentBox>
		{
		}

		// Token: 0x02000064 RID: 100
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrSepareteOrderNbr : BqlType<IBqlString, string>.Field<SOPackageDetailExt.usrSelectedParentBox>
		{
		}

		// Token: 0x02000065 RID: 101
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrOrderNbr : BqlType<IBqlString, string>.Field<SOPackageDetailExt.usrOrderNbr>
		{
		}

		// Token: 0x02000066 RID: 102
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrStoreNbr : BqlType<IBqlString, string>.Field<SOPackageDetailExt.usrStoreNbr>
		{
		}

		// Token: 0x02000067 RID: 103
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCartonNbr : BqlType<IBqlString, string>.Field<SOPackageDetailExt.usrCartonNbr>
		{
		}

		// Token: 0x02000068 RID: 104
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrSOBoxNbrstr : BqlType<IBqlString, string>.Field<SOPackageDetailExt.usrSOBoxNbrstr>
		{
		}

		// Token: 0x02000069 RID: 105
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEstPackageQuantity : BqlType<IBqlDecimal, decimal>.Field<SOPackageDetailExt.usrEstPackageQuantity>
		{
		}

		// Token: 0x0200006A RID: 106
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContentPackageQuantity : BqlType<IBqlDecimal, decimal>.Field<SOPackageDetailExt.usrContentPackageQuantity>
		{
		}
	}
}
