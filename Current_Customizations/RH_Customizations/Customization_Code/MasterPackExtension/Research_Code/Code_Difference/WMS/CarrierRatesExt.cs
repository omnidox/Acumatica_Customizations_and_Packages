using System;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.CarrierRates;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;

namespace WMS
{
	// Token: 0x02000017 RID: 23
	[PXProtectedAccess(null)]
	public abstract class CarrierRatesExt : PXGraphExtension<CreateShipmentExtension, SOShipmentEntry.CarrierRates, SOShipmentEntry>
	{
		// Token: 0x060000D5 RID: 213 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00004547 File Offset: 0x00002747
		[PXOverride]
		public void RecalculatePackagesForOrder(Document doc, CarrierRatesExt.RecalculatePackagesForOrderDelegate baseMethod)
		{
		}

		// Token: 0x0200006D RID: 109
		// (Invoke) Token: 0x060001B4 RID: 436
		public delegate void RecalculatePackagesForOrderDelegate(Document doc);
	}
}
