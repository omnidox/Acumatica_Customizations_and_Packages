using System;
using System.Runtime.CompilerServices;
using ASCiStarKohls.Attributes;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;
using PX.Objects.SO;

namespace ASCiStarKohls.CacheExtension.SO
{
	// Token: 0x0200000C RID: 12
	public sealed class SOOrderTypeExt : PXCacheExtension<SOOrderType>
	{
		// Token: 0x06000029 RID: 41 RVA: 0x0000282B File Offset: 0x00000A2B
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600002A RID: 42 RVA: 0x0000282E File Offset: 0x00000A2E
		// (set) Token: 0x0600002B RID: 43 RVA: 0x00002836 File Offset: 0x00000A36
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Handling Order Type")]
		public bool? UsrHandlingOrderType { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600002C RID: 44 RVA: 0x0000283F File Offset: 0x00000A3F
		// (set) Token: 0x0600002D RID: 45 RVA: 0x00002847 File Offset: 0x00000A47
		[HandlingItem(DescriptionField = typeof(InventoryItem.descr))]
		[PXDefault(PersistingCheck = 2)]
		[PXUIRequired(typeof(Where<SOOrderTypeExt.usrHandlingOrderType, Equal<True>>))]
		[PXUIEnabled(typeof(Where<SOOrderTypeExt.usrHandlingOrderType, Equal<True>>))]
		public int? UsrHandlingItem { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002850 File Offset: 0x00000A50
		// (set) Token: 0x0600002F RID: 47 RVA: 0x00002858 File Offset: 0x00000A58
		[PXDBPriceCost(MinValue = 0.0)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Unit Price for Handling")]
		[PXUIEnabled(typeof(Where<SOOrderTypeExt.usrHandlingOrderType, Equal<True>>))]
		public decimal? UsrUnitPriceForHandling { get; set; }

		// Token: 0x02000016 RID: 22
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrHandlingOrderType : BqlType<IBqlBool, bool>.Field<SOOrderTypeExt.usrHandlingOrderType>
		{
		}

		// Token: 0x02000017 RID: 23
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrHandlingItem : BqlType<IBqlInt, int>.Field<SOOrderTypeExt.usrHandlingItem>
		{
		}

		// Token: 0x02000018 RID: 24
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class usrUnitPriceForHandling : BqlType<IBqlDecimal, decimal>.Field<SOOrderTypeExt.usrUnitPriceForHandling>
		{
		}
	}
}
