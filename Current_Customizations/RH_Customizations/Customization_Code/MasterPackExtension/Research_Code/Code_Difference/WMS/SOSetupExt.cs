using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000018 RID: 24
	public class SOSetupExt : PXCacheExtension<SOSetup>
	{
		// Token: 0x060000DD RID: 221 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060000DE RID: 222 RVA: 0x00004761 File Offset: 0x00002961
		// (set) Token: 0x060000DF RID: 223 RVA: 0x00004769 File Offset: 0x00002969
		[PXDBString(10, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Carton Numbering Sequence", Visibility = 7)]
		[PXSelector(typeof(Numbering.numberingID))]
		public virtual string UsrCartonNumberingSequence { get; set; }

		// Token: 0x0200006E RID: 110
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
