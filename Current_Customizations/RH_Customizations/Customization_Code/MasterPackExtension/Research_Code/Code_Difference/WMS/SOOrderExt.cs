using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000013 RID: 19
	public class SOOrderExt : PXCacheExtension<SOOrder>
	{
		// Token: 0x060000B0 RID: 176 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000B1 RID: 177 RVA: 0x00004424 File Offset: 0x00002624
		// (set) Token: 0x060000B2 RID: 178 RVA: 0x0000442C File Offset: 0x0000262C
		[PXDBBool]
		[PXDefault(true, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Pack This Order Separately")]
		public bool? UsrPackOrderSeparately { get; set; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000B3 RID: 179 RVA: 0x00004435 File Offset: 0x00002635
		// (set) Token: 0x060000B4 RID: 180 RVA: 0x0000443D File Offset: 0x0000263D
		[PXDBString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "EDI Store Number")]
		public string UsrTCStoreNumber { get; set; }

		// Token: 0x0200005E RID: 94
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPackOrderSeparately : BqlType<IBqlBool, bool>.Field<SOOrderExt.usrPackOrderSeparately>
		{
		}

		// Token: 0x0200005F RID: 95
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrTCStoreNumber : BqlType<IBqlString, string>.Field<SOOrderExt.usrTCStoreNumber>
		{
		}
	}
}
