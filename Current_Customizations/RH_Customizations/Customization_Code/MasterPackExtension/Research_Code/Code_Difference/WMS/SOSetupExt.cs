using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000016 RID: 22
	public class SOSetupExt : PXCacheExtension<SOSetup>
	{
		// Token: 0x060000D1 RID: 209 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000D2 RID: 210 RVA: 0x0000452D File Offset: 0x0000272D
		// (set) Token: 0x060000D3 RID: 211 RVA: 0x00004535 File Offset: 0x00002735
		[PXDBString(10, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Carton Numbering Sequence", Visibility = 7)]
		[PXSelector(typeof(Numbering.numberingID))]
		public virtual string UsrCartonNumberingSequence { get; set; }

		// Token: 0x0200006C RID: 108
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrCartonNumberingSequence : BqlType<IBqlString, string>.Field<SOSetupExt.usrCartonNumberingSequence>
		{
		}
	}
}
