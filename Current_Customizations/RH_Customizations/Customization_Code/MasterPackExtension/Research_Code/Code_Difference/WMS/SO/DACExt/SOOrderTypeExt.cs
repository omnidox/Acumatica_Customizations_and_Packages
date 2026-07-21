using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace WMS.SO.DACExt
{
	// Token: 0x0200001D RID: 29
	public class SOOrderTypeExt : PXCacheExtension<SOOrderType>
	{
		// Token: 0x06000117 RID: 279 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000118 RID: 280 RVA: 0x00009783 File Offset: 0x00007983
		// (set) Token: 0x06000119 RID: 281 RVA: 0x0000978B File Offset: 0x0000798B
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "TC Order Type")]
		public bool? UsrIsTCOrderType { get; set; }

		// Token: 0x0200007C RID: 124
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrIsTCOrderType : BqlType<IBqlBool, bool>.Field<SOOrderTypeExt.usrIsTCOrderType>
		{
		}
	}
}
