using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;

namespace WMS
{
	// Token: 0x02000006 RID: 6
	public class CSBoxExt : PXCacheExtension<CSBox>
	{
		// Token: 0x06000010 RID: 16 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000022B7 File Offset: 0x000004B7
		// (set) Token: 0x06000012 RID: 18 RVA: 0x000022BF File Offset: 0x000004BF
		[PXDBBool]
		[PXDBDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Can Be Used as Master Pack Carton")]
		public bool? UsrUseAsParentBox { get; set; }

		// Token: 0x02000020 RID: 32
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
