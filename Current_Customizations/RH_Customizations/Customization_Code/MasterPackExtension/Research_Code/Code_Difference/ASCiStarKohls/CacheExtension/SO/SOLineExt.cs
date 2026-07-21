using System;
using System.Runtime.CompilerServices;
using ASCiStarKohls.Common.Interfaces;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace ASCiStarKohls.CacheExtension.SO
{
	// Token: 0x0200000B RID: 11
	public sealed class SOLineExt : PXCacheExtension<SOLine>, ICustomLineNumber
	{
		// Token: 0x06000025 RID: 37 RVA: 0x0000280E File Offset: 0x00000A0E
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00002811 File Offset: 0x00000A11
		// (set) Token: 0x06000027 RID: 39 RVA: 0x00002819 File Offset: 0x00000A19
		[PXDBInt(MinValue = 0, MaxValue = 999)]
		[PXUIField(DisplayName = "Usr Line Nbr.", Visibility = 7, Visible = false, Enabled = false)]
		public int? UsrLineNbr { get; set; }

		// Token: 0x02000015 RID: 21
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrLineNbr : BqlType<IBqlInt, int>.Field<SOLineExt.usrLineNbr>
		{
		}
	}
}
