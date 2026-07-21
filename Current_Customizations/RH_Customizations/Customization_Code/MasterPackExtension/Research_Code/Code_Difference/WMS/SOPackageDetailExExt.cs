using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000015 RID: 21
	public class SOPackageDetailExExt : PXCacheExtension<SOPackageDetailEx>
	{
		// Token: 0x060000C9 RID: 201 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000CA RID: 202 RVA: 0x000044F1 File Offset: 0x000026F1
		// (set) Token: 0x060000CB RID: 203 RVA: 0x000044F9 File Offset: 0x000026F9
		[PXDBString(BqlField = typeof(SOPackageDetailExt.usrCartonNbr), IsUnicode = true)]
		[PXUIField(DisplayName = "Carton #", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		[AutoNumber(typeof(SOSetupExt.usrCartonNumberingSequence), typeof(SOPackageDetail.createdDateTime))]
		public string UsrCartonNbr { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000CC RID: 204 RVA: 0x00004502 File Offset: 0x00002702
		// (set) Token: 0x060000CD RID: 205 RVA: 0x0000450A File Offset: 0x0000270A
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Est Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrEstPackageQuantity { get; set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000CE RID: 206 RVA: 0x00004513 File Offset: 0x00002713
		// (set) Token: 0x060000CF RID: 207 RVA: 0x0000451B File Offset: 0x0000271B
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Contents of Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrContentPackageQuantity { get; set; }

		// Token: 0x02000069 RID: 105
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCartonNbr : BqlType<IBqlString, string>.Field<SOPackageDetailExExt.usrCartonNbr>
		{
		}

		// Token: 0x0200006A RID: 106
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEstPackageQuantity : BqlType<IBqlDecimal, decimal>.Field<SOPackageDetailExExt.usrEstPackageQuantity>
		{
		}

		// Token: 0x0200006B RID: 107
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrContentPackageQuantity : BqlType<IBqlDecimal, decimal>.Field<SOPackageDetailExExt.usrContentPackageQuantity>
		{
		}
	}
}
