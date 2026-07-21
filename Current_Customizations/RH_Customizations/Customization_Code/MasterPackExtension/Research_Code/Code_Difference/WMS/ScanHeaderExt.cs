using System;
using System.Runtime.CompilerServices;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Data.BQL;

namespace WMS
{
	// Token: 0x02000011 RID: 17
	public sealed class ScanHeaderExt : PXCacheExtension<ScanHeader>
	{
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000076 RID: 118 RVA: 0x0000423F File Offset: 0x0000243F
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00004247 File Offset: 0x00002447
		[PXString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Master Pack Carton Nbr")]
		public string UsrMasterPackCartonNbr { get; set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000078 RID: 120 RVA: 0x00004250 File Offset: 0x00002450
		// (set) Token: 0x06000079 RID: 121 RVA: 0x00004258 File Offset: 0x00002458
		[PXString(240, IsUnicode = true)]
		public string UsrItemBarcode { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00004261 File Offset: 0x00002461
		// (set) Token: 0x0600007B RID: 123 RVA: 0x00004269 File Offset: 0x00002469
		[PXString(4000, IsUnicode = true)]
		public string BoxPrompt { get; set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600007C RID: 124 RVA: 0x00004272 File Offset: 0x00002472
		// (set) Token: 0x0600007D RID: 125 RVA: 0x0000427A File Offset: 0x0000247A
		[PXBool]
		public bool? CanConfirm { get; set; }

		// Token: 0x02000042 RID: 66
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

		// Token: 0x02000043 RID: 67
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

		// Token: 0x02000044 RID: 68
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

		// Token: 0x02000045 RID: 69
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
