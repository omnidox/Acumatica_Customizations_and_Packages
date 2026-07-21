using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace WMS.SO.DACExt
{
	// Token: 0x0200001B RID: 27
	public class SOOrderTypeExt : PXCacheExtension<SOOrderType>
	{
		// Token: 0x06000100 RID: 256 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000101 RID: 257 RVA: 0x00008273 File Offset: 0x00006473
		// (set) Token: 0x06000102 RID: 258 RVA: 0x0000827B File Offset: 0x0000647B
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "TC Order Type")]
		public bool? UsrIsTCOrderType { get; set; }

		// Token: 0x02000072 RID: 114
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
