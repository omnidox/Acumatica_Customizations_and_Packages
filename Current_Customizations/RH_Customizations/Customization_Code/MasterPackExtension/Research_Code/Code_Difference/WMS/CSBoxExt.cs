using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;

namespace WMS
{
	// Token: 0x02000004 RID: 4
	public class CSBoxExt : PXCacheExtension<CSBox>
	{
		// Token: 0x06000004 RID: 4 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002085 File Offset: 0x00000285
		// (set) Token: 0x06000006 RID: 6 RVA: 0x0000208D File Offset: 0x0000028D
		[PXDBBool]
		[PXDBDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Can Be Used as Master Pack Carton")]
		public bool? UsrUseAsParentBox { get; set; }

		// Token: 0x0200001E RID: 30
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrUseAsParentBox : BqlType<IBqlBool, bool>.Field<CSBoxExt.usrUseAsParentBox>
		{
		}
	}
}
