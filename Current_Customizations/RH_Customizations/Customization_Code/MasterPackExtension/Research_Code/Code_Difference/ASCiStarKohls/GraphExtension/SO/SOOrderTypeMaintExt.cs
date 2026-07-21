using System;
using ASCiStarKohls.CacheExtension.SO;
using PX.Data;
using PX.Objects.SO;

namespace ASCiStarKohls.GraphExtension.SO
{
	// Token: 0x02000006 RID: 6
	public class SOOrderTypeMaintExt : PXGraphExtension<SOOrderTypeMaint>
	{
		// Token: 0x0600000F RID: 15 RVA: 0x00002378 File Offset: 0x00000578
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000237C File Offset: 0x0000057C
		protected virtual void _(Events.RowSelected<SOOrderType> e, PXRowSelected baseMethod)
		{
			if (baseMethod != null)
			{
				baseMethod.Invoke(e.Cache, e.Args);
			}
			SOOrderType row = e.Row;
			bool flag = row != null;
			if (flag)
			{
				SOOrderTypeExt extension = PXCacheEx.GetExtension<SOOrderTypeExt>(row);
				PXUIFieldAttribute.SetRequired<SOOrderTypeExt.usrUnitPriceForHandling>(e.Cache, extension.UsrHandlingOrderType.GetValueOrDefault());
			}
		}
	}
}
