using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x02000008 RID: 8
	public class INSiteStatusFilterExt : PXCacheExtension<INSiteStatusFilter>
	{
		// Token: 0x0600002D RID: 45 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002529 File Offset: 0x00000729
		// (set) Token: 0x0600002F RID: 47 RVA: 0x00002531 File Offset: 0x00000731
		[PXBool]
		[PXDefault(true, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Bulk Transfer")]
		public bool? UsrUseActualQty { get; set; }

		// Token: 0x0200002D RID: 45
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrUseActualQty : BqlType<IBqlBool, bool>.Field<INSiteStatusFilterExt.usrUseActualQty>
		{
		}
	}
}
