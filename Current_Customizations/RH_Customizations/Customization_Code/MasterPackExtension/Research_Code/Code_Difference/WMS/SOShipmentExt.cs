using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000019 RID: 25
	public class SOShipmentExt : PXCacheExtension<SOShipment>
	{
		// Token: 0x060000F9 RID: 249 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000FA RID: 250 RVA: 0x000080DF File Offset: 0x000062DF
		// (set) Token: 0x060000FB RID: 251 RVA: 0x000080E7 File Offset: 0x000062E7
		[PXDBString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "EDI Store Number")]
		public string UsrTCStoreNumber { get; set; }

		// Token: 0x02000071 RID: 113
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
