using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x02000009 RID: 9
	public class INTranExt : PXCacheExtension<INTran>
	{
		// Token: 0x06000031 RID: 49 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000032 RID: 50 RVA: 0x00002543 File Offset: 0x00000743
		// (set) Token: 0x06000033 RID: 51 RVA: 0x0000254B File Offset: 0x0000074B
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Tracking Nbr field", IsReadOnly = true)]
		public string UsrRefNbr { get; set; }

		// Token: 0x0200002E RID: 46
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrRefNbr : BqlType<IBqlString, string>.Field<INTranExt.usrRefNbr>
		{
		}
	}
}
