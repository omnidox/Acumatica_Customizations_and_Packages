using System;
using System.Runtime.CompilerServices;
using ASCiStarKohls.Common.Interfaces;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;

namespace ASCiStarKohls.CacheExtension.AR
{
	// Token: 0x0200000E RID: 14
	public sealed class ARTranExt : PXCacheExtension<ARTran>, ICustomLineNumber
	{
		// Token: 0x06000035 RID: 53 RVA: 0x00002887 File Offset: 0x00000A87
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000036 RID: 54 RVA: 0x0000288A File Offset: 0x00000A8A
		// (set) Token: 0x06000037 RID: 55 RVA: 0x00002892 File Offset: 0x00000A92
		[PXDBInt(MinValue = 0, MaxValue = 999)]
		[PXUIField(DisplayName = "Usr Line Nbr.", Visibility = 7, Visible = false, Enabled = false)]
		public int? UsrLineNbr { get; set; }

		// Token: 0x0200001A RID: 26
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrLineNbr : BqlType<IBqlInt, int>.Field<ARTranExt.usrLineNbr>
		{
		}
	}
}
