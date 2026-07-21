using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.PO;

namespace WMS
{
	// Token: 0x0200000F RID: 15
	public class POReceiptLineSplitExt : PXCacheExtension<POReceiptLineSplit>
	{
		// Token: 0x0600006F RID: 111 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000070 RID: 112 RVA: 0x0000416A File Offset: 0x0000236A
		// (set) Token: 0x06000071 RID: 113 RVA: 0x00004172 File Offset: 0x00002372
		[PXDBString(30, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Default Put Away To Location", IsReadOnly = true)]
		public virtual string UsrDefaultPutAwayLocation { get; set; }

		// Token: 0x02000041 RID: 65
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrDefaultPutAwayLocation : BqlType<IBqlString, string>.Field<POReceiptLineSplitExt.usrDefaultPutAwayLocation>
		{
		}
	}
}
