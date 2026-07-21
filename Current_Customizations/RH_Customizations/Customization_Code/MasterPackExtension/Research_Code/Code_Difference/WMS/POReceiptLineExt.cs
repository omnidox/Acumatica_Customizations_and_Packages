using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.PO;

namespace WMS
{
	// Token: 0x0200000E RID: 14
	public class POReceiptLineExt : PXCacheExtension<POReceiptLine>
	{
		// Token: 0x06000067 RID: 103 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000068 RID: 104 RVA: 0x0000412E File Offset: 0x0000232E
		// (set) Token: 0x06000069 RID: 105 RVA: 0x00004136 File Offset: 0x00002336
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		public decimal? UsrINTRANINSPQty { get; set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600006A RID: 106 RVA: 0x0000413F File Offset: 0x0000233F
		// (set) Token: 0x0600006B RID: 107 RVA: 0x00004147 File Offset: 0x00002347
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		public decimal? UsrNSPRECQty { get; set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600006C RID: 108 RVA: 0x00004150 File Offset: 0x00002350
		// (set) Token: 0x0600006D RID: 109 RVA: 0x00004158 File Offset: 0x00002358
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		public decimal? UsrRECSTOCKQty { get; set; }

		// Token: 0x0200003E RID: 62
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrINTRANINSPQty : BqlType<IBqlDecimal, decimal>.Field<POReceiptLineExt.usrINTRANINSPQty>
		{
		}

		// Token: 0x0200003F RID: 63
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrNSPRECQty : BqlType<IBqlDecimal, decimal>.Field<POReceiptLineExt.usrNSPRECQty>
		{
		}

		// Token: 0x02000040 RID: 64
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
