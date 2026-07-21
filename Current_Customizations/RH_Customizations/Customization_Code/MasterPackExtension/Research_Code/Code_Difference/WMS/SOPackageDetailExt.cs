using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000014 RID: 20
	public sealed class SOPackageDetailExt : PXCacheExtension<SOPackageDetail>
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x0000444F File Offset: 0x0000264F
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x00004457 File Offset: 0x00002657
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Master Pack Carton")]
		public bool? UsrIsParentBox { get; set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x00004460 File Offset: 0x00002660
		// (set) Token: 0x060000B9 RID: 185 RVA: 0x00004468 File Offset: 0x00002668
		[PXDBString(15, IsUnicode = true)]
		[PXSelector(typeof(Search<SOPackageDetailExt.usrCartonNbr, Where<SOPackageDetailExt.usrIsParentBox, Equal<True>, And<SOPackageDetail.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>>>), new Type[]
		{
			typeof(SOPackageDetailExt.usrCartonNbr),
			typeof(SOPackageDetail.boxID)
		})]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Contains In Master Pack Carton #")]
		public string UsrSelectedParentBox { get; set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000BA RID: 186 RVA: 0x00004471 File Offset: 0x00002671
		// (set) Token: 0x060000BB RID: 187 RVA: 0x00004479 File Offset: 0x00002679
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		public string UsrSepareteOrderNbr { get; set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00004482 File Offset: 0x00002682
		// (set) Token: 0x060000BD RID: 189 RVA: 0x0000448A File Offset: 0x0000268A
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Order Nbr")]
		public string UsrOrderNbr { get; set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00004493 File Offset: 0x00002693
		// (set) Token: 0x060000BF RID: 191 RVA: 0x0000449B File Offset: 0x0000269B
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Store #")]
		public string UsrStoreNbr { get; set; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x000044A4 File Offset: 0x000026A4
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x000044AC File Offset: 0x000026AC
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Carton #", IsReadOnly = true)]
		[AutoNumber(typeof(SOSetupExt.usrCartonNumberingSequence), typeof(SOPackageDetail.createdDateTime))]
		public string UsrCartonNbr { get; set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x000044B5 File Offset: 0x000026B5
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x000044BD File Offset: 0x000026BD
		[PXDBString(100, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "SO Box Nbr", IsReadOnly = true)]
		public string UsrSOBoxNbrStr { get; set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x000044C6 File Offset: 0x000026C6
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x000044CE File Offset: 0x000026CE
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Est Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrEstPackageQuantity { get; set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x000044D7 File Offset: 0x000026D7
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x000044DF File Offset: 0x000026DF
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Contents of Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrContentPackageQuantity { get; set; }

		// Token: 0x02000060 RID: 96
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrIsParentBox : BqlType<IBqlBool, bool>.Field<SOPackageDetailExt.usrIsParentBox>
		{
		}

		// Token: 0x02000061 RID: 97
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

		// Token: 0x02000062 RID: 98
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

		// Token: 0x02000063 RID: 99
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

		// Token: 0x02000064 RID: 100
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

		// Token: 0x02000065 RID: 101
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

		// Token: 0x02000066 RID: 102
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

		// Token: 0x02000067 RID: 103
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEstPackageQuantity : BqlType<IBqlDecimal, decimal>.Field<SOPackageDetailExt.usrEstPackageQuantity>
		{
		}

		// Token: 0x02000068 RID: 104
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
