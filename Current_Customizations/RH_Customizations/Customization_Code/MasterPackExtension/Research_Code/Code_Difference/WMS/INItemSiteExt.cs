using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x02000007 RID: 7
	public class INItemSiteExt : PXCacheExtension<INItemSite>
	{
		// Token: 0x06000029 RID: 41 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600002A RID: 42 RVA: 0x0000250F File Offset: 0x0000070F
		// (set) Token: 0x0600002B RID: 43 RVA: 0x00002517 File Offset: 0x00000717
		[PXDBString(30, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Default Put Away To:")]
		[PXSelector(typeof(Search<INLocation.locationCD, Where<INLocation.siteID, Equal<Current<INItemSite.siteID>>, And<INLocation.active, Equal<True>>>>))]
		public virtual string UsrDefaultPutAway { get; set; }

		// Token: 0x0200002C RID: 44
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class usrDefaultPutAway : BqlType<IBqlString, string>.Field<INItemSiteExt.usrDefaultPutAway>
		{
		}
	}
}
