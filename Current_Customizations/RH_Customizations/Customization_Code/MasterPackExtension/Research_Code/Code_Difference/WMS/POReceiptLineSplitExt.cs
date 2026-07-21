using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.PO;

namespace WMS
{
	// Token: 0x02000011 RID: 17
	public class POReceiptLineSplitExt : PXCacheExtension<POReceiptLineSplit>
	{
		// Token: 0x0600007B RID: 123 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600007C RID: 124 RVA: 0x0000439E File Offset: 0x0000259E
		// (set) Token: 0x0600007D RID: 125 RVA: 0x000043A6 File Offset: 0x000025A6
		[PXDBString(30, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Default Put Away To Location", IsReadOnly = true)]
		public virtual string UsrDefaultPutAwayLocation { get; set; }

		// Token: 0x02000043 RID: 67
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
