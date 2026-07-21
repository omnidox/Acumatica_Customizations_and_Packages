using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.PO;

namespace WMS
{
	// Token: 0x02000010 RID: 16
	public class POReceiptLineExt : PXCacheExtension<POReceiptLine>
	{
		// Token: 0x06000073 RID: 115 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00004362 File Offset: 0x00002562
		// (set) Token: 0x06000075 RID: 117 RVA: 0x0000436A File Offset: 0x0000256A
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		public decimal? UsrINTRANINSPQty { get; set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00004373 File Offset: 0x00002573
		// (set) Token: 0x06000077 RID: 119 RVA: 0x0000437B File Offset: 0x0000257B
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		public decimal? UsrNSPRECQty { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000078 RID: 120 RVA: 0x00004384 File Offset: 0x00002584
		// (set) Token: 0x06000079 RID: 121 RVA: 0x0000438C File Offset: 0x0000258C
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		public decimal? UsrRECSTOCKQty { get; set; }

		// Token: 0x02000040 RID: 64
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrINTRANINSPQty : BqlType<IBqlDecimal, decimal>.Field<POReceiptLineExt.usrINTRANINSPQty>
		{
		}

		// Token: 0x02000041 RID: 65
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrNSPRECQty : BqlType<IBqlDecimal, decimal>.Field<POReceiptLineExt.usrNSPRECQty>
		{
		}

		// Token: 0x02000042 RID: 66
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrRECSTOCKQty : BqlType<IBqlDecimal, decimal>.Field<POReceiptLineExt.usrRECSTOCKQty>
		{
		}
	}
}
