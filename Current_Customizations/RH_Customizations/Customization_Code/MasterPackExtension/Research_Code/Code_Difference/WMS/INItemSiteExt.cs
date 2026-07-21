using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x02000009 RID: 9
	public class INItemSiteExt : PXCacheExtension<INItemSite>
	{
		// Token: 0x06000035 RID: 53 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000036 RID: 54 RVA: 0x00002743 File Offset: 0x00000943
		// (set) Token: 0x06000037 RID: 55 RVA: 0x0000274B File Offset: 0x0000094B
		[PXDBString(30, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Default Put Away To:")]
		[PXSelector(typeof(Search<INLocation.locationCD, Where<INLocation.siteID, Equal<Current<INItemSite.siteID>>, And<INLocation.active, Equal<True>>>>))]
		public virtual string UsrDefaultPutAway { get; set; }

		// Token: 0x0200002E RID: 46
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
