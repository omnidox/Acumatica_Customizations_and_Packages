using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.PO.WMS;

namespace WMS
{
	// Token: 0x02000012 RID: 18
	public class PutAwayExt : PXGraphExtension<ReceivePutAway.Host>
	{
		// Token: 0x0600007F RID: 127 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000043B8 File Offset: 0x000025B8
		public virtual void _(Events.RowSelecting<POReceiptLineSplit> args)
		{
			bool flag = args.Row == null || args.Row.ReceiptNbr == null;
			if (!flag)
			{
				InventoryItem item = InventoryItem.PK.Find(base.Base, args.Row.InventoryID, 0);
				bool flag2 = item == null;
				if (!flag2)
				{
					INItemSite inSite = INItemSite.PK.Find(base.Base, args.Row.InventoryID, args.Row.SiteID, 0);
					bool flag3 = inSite == null;
					if (!flag3)
					{
						string defaultPutAway = PXCacheEx.GetExtension<INItemSiteExt>(inSite).UsrDefaultPutAway;
						bool flag4 = defaultPutAway == null;
						if (!flag4)
						{
							args.Cache.SetValueExt<POReceiptLineSplitExt.usrDefaultPutAwayLocation>(args.Row, defaultPutAway);
						}
					}
				}
			}
		}
	}
}
