using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;

namespace ASCiStarKohls.Attributes
{
	// Token: 0x0200000F RID: 15
	[PXDBInt]
	[PXUIField(DisplayName = "Handling Item", Visibility = 3)]
	public class HandlingItemAttribute : InventoryAttribute
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000039 RID: 57 RVA: 0x000028A4 File Offset: 0x00000AA4
		public static Type Search
		{
			get
			{
				return typeof(Search<InventoryItem.inventoryID, Where<Match<Current<AccessInfo.userName>>>>);
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000028B0 File Offset: 0x00000AB0
		public static PXRestrictorAttribute CreateRestrictor()
		{
			return new PXRestrictorAttribute(typeof(Where<InventoryItem.stkItem, Equal<boolFalse>, And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.unknown>, And<InventoryItem.isTemplate, Equal<False>, And<InventoryItem.itemType, Equal<INItemTypes.nonStockItem>>>>>), "The inventory item is a stock item.", Array.Empty<Type>());
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000028DB File Offset: 0x00000ADB
		public HandlingItemAttribute() : base(HandlingItemAttribute.Search, typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr))
		{
			this._Attributes.Add(HandlingItemAttribute.CreateRestrictor());
		}
	}
}
