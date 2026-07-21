using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000017 RID: 23
	public class SOPackageDetailExExt : PXCacheExtension<SOPackageDetailEx>
	{
		// Token: 0x060000D5 RID: 213 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000D6 RID: 214 RVA: 0x00004725 File Offset: 0x00002925
		// (set) Token: 0x060000D7 RID: 215 RVA: 0x0000472D File Offset: 0x0000292D
		[PXDBString(BqlField = typeof(SOPackageDetailExt.usrCartonNbr), IsUnicode = true)]
		[PXUIField(DisplayName = "Carton #", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		[AutoNumber(typeof(SOSetupExt.usrCartonNumberingSequence), typeof(SOPackageDetail.createdDateTime))]
		public string UsrCartonNbr { get; set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x00004736 File Offset: 0x00002936
		// (set) Token: 0x060000D9 RID: 217 RVA: 0x0000473E File Offset: 0x0000293E
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Est Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrEstPackageQuantity { get; set; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060000DA RID: 218 RVA: 0x00004747 File Offset: 0x00002947
		// (set) Token: 0x060000DB RID: 219 RVA: 0x0000474F File Offset: 0x0000294F
		[PXDBDecimal]
		[PXUIField(DisplayName = "Total Qty of Contents of Package", IsReadOnly = true)]
		[PXDefault(PersistingCheck = 2)]
		public decimal? UsrContentPackageQuantity { get; set; }

		// Token: 0x0200006B RID: 107
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

		// Token: 0x0200006C RID: 108
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrEstPackageQuantity : BqlType<IBqlDecimal, decimal>.Field<SOPackageDetailExExt.usrEstPackageQuantity>
		{
		}

		// Token: 0x0200006D RID: 109
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
