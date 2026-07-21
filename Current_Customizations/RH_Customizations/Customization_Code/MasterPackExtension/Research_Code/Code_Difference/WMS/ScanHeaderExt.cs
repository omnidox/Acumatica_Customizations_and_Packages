using System;
using System.Runtime.CompilerServices;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Data.BQL;

namespace WMS
{
	// Token: 0x02000013 RID: 19
	public sealed class ScanHeaderExt : PXCacheExtension<ScanHeader>
	{
		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000082 RID: 130 RVA: 0x00004473 File Offset: 0x00002673
		// (set) Token: 0x06000083 RID: 131 RVA: 0x0000447B File Offset: 0x0000267B
		[PXString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Master Pack Carton Nbr")]
		public string UsrMasterPackCartonNbr { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000084 RID: 132 RVA: 0x00004484 File Offset: 0x00002684
		// (set) Token: 0x06000085 RID: 133 RVA: 0x0000448C File Offset: 0x0000268C
		[PXString(240, IsUnicode = true)]
		public string UsrItemBarcode { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000086 RID: 134 RVA: 0x00004495 File Offset: 0x00002695
		// (set) Token: 0x06000087 RID: 135 RVA: 0x0000449D File Offset: 0x0000269D
		[PXString(4000, IsUnicode = true)]
		public string BoxPrompt { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000088 RID: 136 RVA: 0x000044A6 File Offset: 0x000026A6
		// (set) Token: 0x06000089 RID: 137 RVA: 0x000044AE File Offset: 0x000026AE
		[PXBool]
		public bool? CanConfirm { get; set; }

		// Token: 0x02000044 RID: 68
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrMasterPackCartonNbr : BqlType<IBqlString, string>.Field<ScanHeaderExt.usrMasterPackCartonNbr>
		{
		}

		// Token: 0x02000045 RID: 69
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrItemBarcode : BqlType<IBqlString, string>.Field<ScanHeaderExt.usrItemBarcode>
		{
		}

		// Token: 0x02000046 RID: 70
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class boxPrompt : BqlType<IBqlString, string>.Field<ScanHeaderExt.boxPrompt>
		{
		}

		// Token: 0x02000047 RID: 71
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class canConfirm : BqlType<IBqlBool, bool>.Field<ScanHeaderExt.canConfirm>
		{
		}
	}
}
