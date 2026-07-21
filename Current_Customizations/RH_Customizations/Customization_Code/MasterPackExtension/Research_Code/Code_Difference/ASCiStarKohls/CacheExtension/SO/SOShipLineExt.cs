using System;
using System.Runtime.CompilerServices;
using ASCiStarKohls.Common.Interfaces;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace ASCiStarKohls.CacheExtension.SO
{
	// Token: 0x0200000D RID: 13
	public sealed class SOShipLineExt : PXCacheExtension<SOShipLine>, ICustomLineNumber
	{
		// Token: 0x06000031 RID: 49 RVA: 0x0000286A File Offset: 0x00000A6A
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000032 RID: 50 RVA: 0x0000286D File Offset: 0x00000A6D
		// (set) Token: 0x06000033 RID: 51 RVA: 0x00002875 File Offset: 0x00000A75
		[PXDBInt(MinValue = 0, MaxValue = 999)]
		[PXUIField(DisplayName = "Usr Line Nbr.", Visibility = 7, Visible = false, Enabled = false)]
		public int? UsrLineNbr { get; set; }

		// Token: 0x02000019 RID: 25
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrLineNbr : BqlType<IBqlInt, int>.Field<SOShipLineExt.usrLineNbr>
		{
		}
	}
}
