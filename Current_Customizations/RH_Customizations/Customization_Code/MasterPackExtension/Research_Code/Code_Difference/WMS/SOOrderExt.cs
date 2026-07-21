using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000015 RID: 21
	public class SOOrderExt : PXCacheExtension<SOOrder>
	{
		// Token: 0x060000BC RID: 188 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000BD RID: 189 RVA: 0x00004658 File Offset: 0x00002858
		// (set) Token: 0x060000BE RID: 190 RVA: 0x00004660 File Offset: 0x00002860
		[PXDBBool]
		[PXDefault(true, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Pack This Order Separately")]
		public bool? UsrPackOrderSeparately { get; set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000BF RID: 191 RVA: 0x00004669 File Offset: 0x00002869
		// (set) Token: 0x060000C0 RID: 192 RVA: 0x00004671 File Offset: 0x00002871
		[PXDBString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "EDI Store Number")]
		public string UsrTCStoreNumber { get; set; }

		// Token: 0x02000060 RID: 96
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrPackOrderSeparately : BqlType<IBqlBool, bool>.Field<SOOrderExt.usrPackOrderSeparately>
		{
		}

		// Token: 0x02000061 RID: 97
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
