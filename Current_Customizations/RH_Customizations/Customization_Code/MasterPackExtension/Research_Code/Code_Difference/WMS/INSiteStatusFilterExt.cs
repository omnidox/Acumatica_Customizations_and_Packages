using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x0200000A RID: 10
	public class INSiteStatusFilterExt : PXCacheExtension<INSiteStatusFilter>
	{
		// Token: 0x06000039 RID: 57 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600003A RID: 58 RVA: 0x0000275D File Offset: 0x0000095D
		// (set) Token: 0x0600003B RID: 59 RVA: 0x00002765 File Offset: 0x00000965
		[PXBool]
		[PXDefault(true, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Bulk Transfer")]
		public bool? UsrUseActualQty { get; set; }

		// Token: 0x0200002F RID: 47
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
