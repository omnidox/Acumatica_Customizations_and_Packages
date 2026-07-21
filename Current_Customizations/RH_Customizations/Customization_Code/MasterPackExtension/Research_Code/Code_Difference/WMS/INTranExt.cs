using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x0200000B RID: 11
	public class INTranExt : PXCacheExtension<INTran>
	{
		// Token: 0x0600003D RID: 61 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00002777 File Offset: 0x00000977
		// (set) Token: 0x0600003F RID: 63 RVA: 0x0000277F File Offset: 0x0000097F
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Tracking Nbr field", IsReadOnly = true)]
		public string UsrRefNbr { get; set; }

		// Token: 0x02000030 RID: 48
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
