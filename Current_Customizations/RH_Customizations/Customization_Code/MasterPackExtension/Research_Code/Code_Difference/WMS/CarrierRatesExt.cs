using System;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.CarrierRates;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;

namespace WMS
{
	// Token: 0x02000019 RID: 25
	[PXProtectedAccess(null)]
	public abstract class CarrierRatesExt : PXGraphExtension<CreateShipmentExtension, SOShipmentEntry.CarrierRates, SOShipmentEntry>
	{
		// Token: 0x060000E1 RID: 225 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000477B File Offset: 0x0000297B
		[PXOverride]
		public void RecalculatePackagesForOrder(Document doc, CarrierRatesExt.RecalculatePackagesForOrderDelegate baseMethod)
		{
		}

		// Token: 0x0200006F RID: 111
		// (Invoke) Token: 0x060001CB RID: 459
		public delegate void RecalculatePackagesForOrderDelegate(Document doc);
	}
}
