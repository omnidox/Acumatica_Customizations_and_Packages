using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x0200001B RID: 27
	public class SOShipmentExt : PXCacheExtension<SOShipment>
	{
		// Token: 0x06000110 RID: 272 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000111 RID: 273 RVA: 0x000095EF File Offset: 0x000077EF
		// (set) Token: 0x06000112 RID: 274 RVA: 0x000095F7 File Offset: 0x000077F7
		[PXDBString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "EDI Store Number")]
		public string UsrTCStoreNumber { get; set; }

		// Token: 0x0200007B RID: 123
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrTCStoreNumber : BqlType<IBqlString, string>.Field<SOShipmentExt.usrTCStoreNumber>
		{
		}
	}
}
