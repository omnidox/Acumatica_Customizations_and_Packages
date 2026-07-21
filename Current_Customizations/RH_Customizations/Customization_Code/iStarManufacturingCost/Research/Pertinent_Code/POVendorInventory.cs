using System;
using System.Runtime.CompilerServices;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CR.Standalone;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;

namespace PX.Objects.PO
{
	// Token: 0x02000421 RID: 1057
	[PXCacheName("Inventory Item Vendor Details")]
	[Serializable]
	public class POVendorInventory : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x17001BFA RID: 7162
		// (get) Token: 0x06004DBA RID: 19898 RVA: 0x00111138 File Offset: 0x0010F338
		// (set) Token: 0x06004DBB RID: 19899 RVA: 0x00111140 File Offset: 0x0010F340
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID
		{
			get
			{
				return this._RecordID;
			}
			set
			{
				this._RecordID = value;
			}
		}

		// Token: 0x17001BFB RID: 7163
		// (get) Token: 0x06004DBC RID: 19900 RVA: 0x00111149 File Offset: 0x0010F349
		// (set) Token: 0x06004DBD RID: 19901 RVA: 0x00111151 File Offset: 0x0010F351
		[VendorNonEmployeeActiveOrHoldPayments(Visibility = PXUIVisibility.SelectorVisible, DescriptionField = typeof(Vendor.acctName), CacheGlobal = true, Filterable = true, DisplayName = "Vendor ID")]
		[PXDefault(typeof(Vendor.bAccountID))]
		[PXForeignReference(typeof(POVendorInventory.FK.Vendor))]
		public virtual int? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}

		// Token: 0x17001BFC RID: 7164
		// (get) Token: 0x06004DBE RID: 19902 RVA: 0x0011115A File Offset: 0x0010F35A
		// (set) Token: 0x06004DBF RID: 19903 RVA: 0x00111162 File Offset: 0x0010F362
		[LocationID(typeof(Where<PX.Objects.CR.Location.bAccountID, Equal<Current<POVendorInventory.vendorID>>>), DescriptionField = typeof(PX.Objects.CR.Location.descr), Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Location")]
		[PXFormula(typeof(Default<POVendorInventory.vendorID>))]
		[PXParent(typeof(POVendorInventory.FK.VendorLocation))]
		public virtual int? VendorLocationID
		{
			get
			{
				return this._VendorLocationID;
			}
			set
			{
				this._VendorLocationID = value;
			}
		}

		// Token: 0x17001BFD RID: 7165
		// (get) Token: 0x06004DC0 RID: 19904 RVA: 0x0011116B File Offset: 0x0010F36B
		// (set) Token: 0x06004DC1 RID: 19905 RVA: 0x00111173 File Offset: 0x0010F373
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "All Locations")]
		[PXDBCalced(typeof(Switch<Case<Where<POVendorInventory.vendorLocationID, IsNull>, True>, False>), typeof(bool))]
		public virtual bool? AllLocations
		{
			get
			{
				return this._AllLocations;
			}
			set
			{
				this._AllLocations = value;
			}
		}

		// Token: 0x17001BFE RID: 7166
		// (get) Token: 0x06004DC2 RID: 19906 RVA: 0x0011117C File Offset: 0x0010F37C
		// (set) Token: 0x06004DC3 RID: 19907 RVA: 0x00111184 File Offset: 0x0010F384
		[Inventory(Filterable = true, DirtyRead = true, Enabled = false)]
		[PXParent(typeof(POVendorInventory.FK.InventoryItem))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		public virtual int? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}

		// Token: 0x17001BFF RID: 7167
		// (get) Token: 0x06004DC4 RID: 19908 RVA: 0x0011118D File Offset: 0x0010F38D
		// (set) Token: 0x06004DC5 RID: 19909 RVA: 0x00111195 File Offset: 0x0010F395
		[SubItem(typeof(POVendorInventory.inventoryID), DisplayName = "Subitem")]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? SubItemID
		{
			get
			{
				return this._SubItemID;
			}
			set
			{
				this._SubItemID = value;
			}
		}

		// Token: 0x17001C00 RID: 7168
		// (get) Token: 0x06004DC6 RID: 19910 RVA: 0x0011119E File Offset: 0x0010F39E
		// (set) Token: 0x06004DC7 RID: 19911 RVA: 0x001111A6 File Offset: 0x0010F3A6
		[PXDefault(typeof(Search<InventoryItem.purchaseUnit, Where<InventoryItem.inventoryID, Equal<Current<POVendorInventory.inventoryID>>>>))]
		[PXFormula(typeof(Default<POVendorInventory.inventoryID>))]
		[INUnit(typeof(POVendorInventory.inventoryID), DisplayName = "Purchase Unit", Visibility = PXUIVisibility.Visible)]
		[PXCheckUnique(new Type[]
		{
			typeof(POVendorInventory.vendorID),
			typeof(POVendorInventory.vendorLocationID),
			typeof(POVendorInventory.inventoryID),
			typeof(POVendorInventory.subItemID),
			typeof(POVendorInventory.purchaseUnit)
		}, IgnoreNulls = false, ClearOnDuplicate = true)]
		public virtual string PurchaseUnit
		{
			get
			{
				return this._PurchaseUnit;
			}
			set
			{
				this._PurchaseUnit = value;
			}
		}

		// Token: 0x17001C01 RID: 7169
		// (get) Token: 0x06004DC8 RID: 19912 RVA: 0x001111AF File Offset: 0x0010F3AF
		// (set) Token: 0x06004DC9 RID: 19913 RVA: 0x001111B7 File Offset: 0x0010F3B7
		[PXDBString(50, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Vendor Inventory ID", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string VendorInventoryID
		{
			get
			{
				return this._VendorInventoryID;
			}
			set
			{
				this._VendorInventoryID = value;
			}
		}

		// Token: 0x17001C02 RID: 7170
		// (get) Token: 0x06004DCA RID: 19914 RVA: 0x001111C0 File Offset: 0x0010F3C0
		// (set) Token: 0x06004DCB RID: 19915 RVA: 0x001111C8 File Offset: 0x0010F3C8
		[PXShort(MinValue = 0, MaxValue = 100000)]
		[PXUIField(DisplayName = "Vendor Lead Time (Days)", Enabled = false)]
		[PXDBScalar(typeof(Search<PX.Objects.CR.Standalone.Location.vLeadTime, Where<PX.Objects.CR.Standalone.Location.bAccountID, Equal<POVendorInventory.vendorID>, And<PX.Objects.CR.Standalone.Location.locationID, Equal<POVendorInventory.vendorLocationID>>>>))]
		public virtual short? VLeadTime
		{
			get
			{
				return this._VLeadTime;
			}
			set
			{
				this._VLeadTime = value;
			}
		}

		// Token: 0x17001C03 RID: 7171
		// (get) Token: 0x06004DCC RID: 19916 RVA: 0x001111D1 File Offset: 0x0010F3D1
		// (set) Token: 0x06004DCD RID: 19917 RVA: 0x001111D9 File Offset: 0x0010F3D9
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Override")]
		public virtual bool? OverrideSettings
		{
			get
			{
				return this._OverrideSettings;
			}
			set
			{
				this._OverrideSettings = value;
			}
		}

		// Token: 0x17001C04 RID: 7172
		// (get) Token: 0x06004DCE RID: 19918 RVA: 0x001111E2 File Offset: 0x0010F3E2
		// (set) Token: 0x06004DCF RID: 19919 RVA: 0x001111EA File Offset: 0x0010F3EA
		[PXDefault(0)]
		[PXDBShort]
		[PXUIField(DisplayName = "Add. Lead Time (Days)")]
		public virtual short? AddLeadTimeDays
		{
			get
			{
				return this._AddLeadTimeDays;
			}
			set
			{
				this._AddLeadTimeDays = value;
			}
		}

		// Token: 0x17001C05 RID: 7173
		// (get) Token: 0x06004DD0 RID: 19920 RVA: 0x001111F3 File Offset: 0x0010F3F3
		// (set) Token: 0x06004DD1 RID: 19921 RVA: 0x001111FB File Offset: 0x0010F3FB
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Active")]
		[PXUIVerify(typeof(Where<POVendorInventory.active, Equal<True>, Or<POVendorInventory.isDefault, NotEqual<True>>>), PXErrorLevel.Error, "Default Vendor cannot be deactivated.", new Type[]
		{

		})]
		public virtual bool? Active
		{
			get
			{
				return this._Active;
			}
			set
			{
				this._Active = value;
			}
		}

		// Token: 0x17001C06 RID: 7174
		// (get) Token: 0x06004DD2 RID: 19922 RVA: 0x00111204 File Offset: 0x0010F404
		// (set) Token: 0x06004DD3 RID: 19923 RVA: 0x0011120C File Offset: 0x0010F40C
		[PXDBInt]
		[PXUIField(DisplayName = "Min. Order Freq.(Days)")]
		[PXDefault(0)]
		public virtual int? MinOrdFreq
		{
			get
			{
				return this._MinOrdFreq;
			}
			set
			{
				this._MinOrdFreq = value;
			}
		}

		// Token: 0x17001C07 RID: 7175
		// (get) Token: 0x06004DD4 RID: 19924 RVA: 0x00111215 File Offset: 0x0010F415
		// (set) Token: 0x06004DD5 RID: 19925 RVA: 0x0011121D File Offset: 0x0010F41D
		[PXDBQuantity]
		[PXUIField(DisplayName = "Min. Order Qty.")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? MinOrdQty
		{
			get
			{
				return this._MinOrdQty;
			}
			set
			{
				this._MinOrdQty = value;
			}
		}

		// Token: 0x17001C08 RID: 7176
		// (get) Token: 0x06004DD6 RID: 19926 RVA: 0x00111226 File Offset: 0x0010F426
		// (set) Token: 0x06004DD7 RID: 19927 RVA: 0x0011122E File Offset: 0x0010F42E
		[PXDBQuantity]
		[PXUIField(DisplayName = "Max Order Qty.")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? MaxOrdQty
		{
			get
			{
				return this._MaxOrdQty;
			}
			set
			{
				this._MaxOrdQty = value;
			}
		}

		// Token: 0x17001C09 RID: 7177
		// (get) Token: 0x06004DD8 RID: 19928 RVA: 0x00111237 File Offset: 0x0010F437
		// (set) Token: 0x06004DD9 RID: 19929 RVA: 0x0011123F File Offset: 0x0010F43F
		[PXDBQuantity]
		[PXUIField(DisplayName = "Lot Size")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? LotSize
		{
			get
			{
				return this._LotSize;
			}
			set
			{
				this._LotSize = value;
			}
		}

		// Token: 0x17001C0A RID: 7178
		// (get) Token: 0x06004DDA RID: 19930 RVA: 0x00111248 File Offset: 0x0010F448
		// (set) Token: 0x06004DDB RID: 19931 RVA: 0x00111250 File Offset: 0x0010F450
		[PXDBQuantity]
		[PXUIField(DisplayName = "EOQ")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? ERQ
		{
			get
			{
				return this._ERQ;
			}
			set
			{
				this._ERQ = value;
			}
		}

		// Token: 0x17001C0B RID: 7179
		// (get) Token: 0x06004DDC RID: 19932 RVA: 0x00111259 File Offset: 0x0010F459
		// (set) Token: 0x06004DDD RID: 19933 RVA: 0x00111261 File Offset: 0x0010F461
		[PXDBPriceCost]
		[PXUIField(DisplayName = "Last Vendor Price", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? LastPrice
		{
			get
			{
				return this._LastPrice;
			}
			set
			{
				this._LastPrice = value;
			}
		}

		// Token: 0x17001C0C RID: 7180
		// (get) Token: 0x06004DDE RID: 19934 RVA: 0x0011126A File Offset: 0x0010F46A
		// (set) Token: 0x06004DDF RID: 19935 RVA: 0x00111272 File Offset: 0x0010F472
		[PXDBString(5, IsUnicode = true)]
		[PXSelector(typeof(PX.Objects.CM.Extensions.Currency.curyID), CacheGlobal = true)]
		[PXDefault(typeof(Coalesce<Search<Vendor.curyID, Where<Vendor.bAccountID, Equal<Current<POVendorInventory.vendorID>>>>, Search<Company.baseCuryID>>), PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXFormula(typeof(Default<POVendorInventory.vendorID>))]
		[PXUIField(DisplayName = "Currency ID", Enabled = false)]
		public virtual string CuryID
		{
			get
			{
				return this._CuryID;
			}
			set
			{
				this._CuryID = value;
			}
		}

		// Token: 0x17001C0D RID: 7181
		// (get) Token: 0x06004DE0 RID: 19936 RVA: 0x0011127B File Offset: 0x0010F47B
		// (set) Token: 0x06004DE1 RID: 19937 RVA: 0x00111283 File Offset: 0x0010F483
		[PXBool]
		[PXUIField(DisplayName = "Default", Enabled = false)]
		[PXDependsOnFields(new Type[]
		{
			typeof(POVendorInventory.inventoryID),
			typeof(POVendorInventory.vendorID),
			typeof(POVendorInventory.vendorLocationID)
		})]
		[PODefaultVendor(typeof(POVendorInventory.inventoryID), typeof(POVendorInventory.vendorID), typeof(POVendorInventory.vendorLocationID))]
		public virtual bool? IsDefault
		{
			get
			{
				return this._IsDefault;
			}
			set
			{
				this._IsDefault = value;
			}
		}

		// Token: 0x17001C0E RID: 7182
		// (get) Token: 0x06004DE2 RID: 19938 RVA: 0x0011128C File Offset: 0x0010F48C
		// (set) Token: 0x06004DE3 RID: 19939 RVA: 0x00111294 File Offset: 0x0010F494
		[PXDBDecimal(6, MinValue = 0.0, MaxValue = 100.0)]
		[PXUIField(DisplayName = "Prepayment Percent")]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual decimal? PrepaymentPct { get; set; }

		// Token: 0x17001C0F RID: 7183
		// (get) Token: 0x06004DE4 RID: 19940 RVA: 0x0011129D File Offset: 0x0010F49D
		// (set) Token: 0x06004DE5 RID: 19941 RVA: 0x001112A5 File Offset: 0x0010F4A5
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x17001C10 RID: 7184
		// (get) Token: 0x06004DE6 RID: 19942 RVA: 0x001112AE File Offset: 0x0010F4AE
		// (set) Token: 0x06004DE7 RID: 19943 RVA: 0x001112B6 File Offset: 0x0010F4B6
		[PXDBTimestamp]
		public virtual byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}

		// Token: 0x17001C11 RID: 7185
		// (get) Token: 0x06004DE8 RID: 19944 RVA: 0x001112BF File Offset: 0x0010F4BF
		// (set) Token: 0x06004DE9 RID: 19945 RVA: 0x001112C7 File Offset: 0x0010F4C7
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}

		// Token: 0x17001C12 RID: 7186
		// (get) Token: 0x06004DEA RID: 19946 RVA: 0x001112D0 File Offset: 0x0010F4D0
		// (set) Token: 0x06004DEB RID: 19947 RVA: 0x001112D8 File Offset: 0x0010F4D8
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}

		// Token: 0x17001C13 RID: 7187
		// (get) Token: 0x06004DEC RID: 19948 RVA: 0x001112E1 File Offset: 0x0010F4E1
		// (set) Token: 0x06004DED RID: 19949 RVA: 0x001112E9 File Offset: 0x0010F4E9
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}

		// Token: 0x17001C14 RID: 7188
		// (get) Token: 0x06004DEE RID: 19950 RVA: 0x001112F2 File Offset: 0x0010F4F2
		// (set) Token: 0x06004DEF RID: 19951 RVA: 0x001112FA File Offset: 0x0010F4FA
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}

		// Token: 0x17001C15 RID: 7189
		// (get) Token: 0x06004DF0 RID: 19952 RVA: 0x00111303 File Offset: 0x0010F503
		// (set) Token: 0x06004DF1 RID: 19953 RVA: 0x0011130B File Offset: 0x0010F50B
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}

		// Token: 0x17001C16 RID: 7190
		// (get) Token: 0x06004DF2 RID: 19954 RVA: 0x00111314 File Offset: 0x0010F514
		// (set) Token: 0x06004DF3 RID: 19955 RVA: 0x0011131C File Offset: 0x0010F51C
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}

		// Token: 0x040024C8 RID: 9416
		protected int? _RecordID;

		// Token: 0x040024C9 RID: 9417
		protected int? _VendorID;

		// Token: 0x040024CA RID: 9418
		protected int? _VendorLocationID;

		// Token: 0x040024CB RID: 9419
		protected bool? _AllLocations;

		// Token: 0x040024CC RID: 9420
		protected int? _InventoryID;

		// Token: 0x040024CD RID: 9421
		protected int? _SubItemID;

		// Token: 0x040024CE RID: 9422
		protected string _PurchaseUnit;

		// Token: 0x040024CF RID: 9423
		protected string _VendorInventoryID;

		// Token: 0x040024D0 RID: 9424
		protected short? _VLeadTime;

		// Token: 0x040024D1 RID: 9425
		protected bool? _OverrideSettings;

		// Token: 0x040024D2 RID: 9426
		protected short? _AddLeadTimeDays;

		// Token: 0x040024D3 RID: 9427
		protected bool? _Active;

		// Token: 0x040024D4 RID: 9428
		protected int? _MinOrdFreq;

		// Token: 0x040024D5 RID: 9429
		protected decimal? _MinOrdQty;

		// Token: 0x040024D6 RID: 9430
		protected decimal? _MaxOrdQty;

		// Token: 0x040024D7 RID: 9431
		protected decimal? _LotSize;

		// Token: 0x040024D8 RID: 9432
		protected decimal? _ERQ;

		// Token: 0x040024D9 RID: 9433
		protected decimal? _LastPrice;

		// Token: 0x040024DA RID: 9434
		protected string _CuryID;

		// Token: 0x040024DB RID: 9435
		protected bool? _IsDefault;

		// Token: 0x040024DE RID: 9438
		protected byte[] _tstamp;

		// Token: 0x040024DF RID: 9439
		protected Guid? _CreatedByID;

		// Token: 0x040024E0 RID: 9440
		protected string _CreatedByScreenID;

		// Token: 0x040024E1 RID: 9441
		protected DateTime? _CreatedDateTime;

		// Token: 0x040024E2 RID: 9442
		protected Guid? _LastModifiedByID;

		// Token: 0x040024E3 RID: 9443
		protected string _LastModifiedByScreenID;

		// Token: 0x040024E4 RID: 9444
		protected DateTime? _LastModifiedDateTime;

		// Token: 0x02003C05 RID: 15365
		public class PK : PrimaryKeyOf<POVendorInventory>.By<POVendorInventory.recordID>
		{
			// Token: 0x0601FDEA RID: 130538 RVA: 0x00716DE4 File Offset: 0x00714FE4
			public static POVendorInventory Find(PXGraph graph, int? recordID, PKFindOptions options = PKFindOptions.None)
			{
				return PrimaryKeyOf<POVendorInventory>.By<POVendorInventory.recordID>.FindBy(graph, recordID, options);
			}
		}

		// Token: 0x02003C06 RID: 15366
		public static class FK
		{
			// Token: 0x0200D514 RID: 54548
			public class InventoryItem : PrimaryKeyOf<PX.Objects.IN.InventoryItem>.By<PX.Objects.IN.InventoryItem.inventoryID>.ForeignKeyOf<POVendorInventory>.By<POVendorInventory.inventoryID>
			{
			}

			// Token: 0x0200D515 RID: 54549
			public class SubItem : PrimaryKeyOf<INSubItem>.By<INSubItem.subItemID>.ForeignKeyOf<POVendorInventory>.By<POVendorInventory.subItemID>
			{
			}

			// Token: 0x0200D516 RID: 54550
			public class Vendor : PrimaryKeyOf<PX.Objects.AP.Vendor>.By<PX.Objects.AP.Vendor.bAccountID>.ForeignKeyOf<POVendorInventory>.By<POVendorInventory.vendorID>
			{
			}

			// Token: 0x0200D517 RID: 54551
			public class VendorLocation : PrimaryKeyOf<PX.Objects.CR.Location>.By<PX.Objects.CR.Location.bAccountID, PX.Objects.CR.Location.locationID>.ForeignKeyOf<POVendorInventory>.By<POVendorInventory.vendorID, POVendorInventory.vendorLocationID>
			{
			}

			// Token: 0x0200D518 RID: 54552
			public class Currency : PrimaryKeyOf<PX.Objects.CM.Currency>.By<PX.Objects.CM.Currency.curyID>.ForeignKeyOf<POVendorInventory>.By<POVendorInventory.curyID>
			{
			}
		}

		// Token: 0x02003C07 RID: 15367
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class recordID : BqlType<IBqlInt, int>.Field<POVendorInventory.recordID>
		{
		}

		// Token: 0x02003C08 RID: 15368
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class vendorID : BqlType<IBqlInt, int>.Field<POVendorInventory.vendorID>
		{
			// Token: 0x0200D519 RID: 54553
			[Nullable(new byte[]
			{
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				1,
				1,
				0,
				1,
				1
			})]
			public class PreventEditBAccountVOrgBAccountID<TGraph> : EditPreventor<TypeArrayOf<IBqlField>.FilledWith<BAccount.vOrgBAccountID>>.On<TGraph>.IfExists<SelectFromBase<POVendorInventory, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<POVendorInventory.vendorID, IBqlInt>.IsEqual<BqlField<BAccount.bAccountID, IBqlInt>.FromCurrent>>> where TGraph : PXGraph
			{
				// Token: 0x060301BF RID: 197055 RVA: 0x008EF084 File Offset: 0x008ED284
				protected override string CreateEditPreventingReason(GetEditPreventingReasonArgs arg, object firstPreventingEntity, string fieldName, string currentTableName, string foreignTableName)
				{
					BAccount baccount = arg.Row as BAccount;
					int? bAccountID = arg.NewValue as int?;
					PXAccess.MasterCollection.Branch branchByBAccountID = PXAccess.GetBranchByBAccountID(bAccountID);
					string a;
					if ((a = ((branchByBAccountID != null) ? branchByBAccountID.BaseCuryID : null)) == null)
					{
						PXAccess.MasterCollection.Organization organizationByBAccountID = PXAccess.GetOrganizationByBAccountID(bAccountID);
						a = ((organizationByBAccountID != null) ? organizationByBAccountID.BaseCuryID : null);
					}
					PXGraph graph = base.Base;
					POVendorInventory povendorInventory = firstPreventingEntity as POVendorInventory;
					PX.Objects.IN.InventoryItem inventoryItem = PX.Objects.IN.InventoryItem.PK.Find(graph, (povendorInventory != null) ? povendorInventory.InventoryID : null, PKFindOptions.None);
					if (a == ((baccount != null) ? baccount.BaseCuryID : null))
					{
						return null;
					}
					if (baccount != null && baccount.BaseCuryID != null)
					{
						return PXMessages.LocalizeFormatNoPrefix("A branch with the base currency other than {0} cannot be associated with the {1} vendor because {1} is added to the vendor's list of the {2} item.", new object[]
						{
							baccount.BaseCuryID,
							baccount.AcctCD,
							(inventoryItem != null) ? inventoryItem.InventoryCD : null
						});
					}
					return PXMessages.LocalizeFormatNoPrefix("This box must remain blank because {0} is added to the list of vendors in the settings of the {1} item.", new object[]
					{
						baccount.AcctCD,
						(inventoryItem != null) ? inventoryItem.InventoryCD : null
					});
				}
			}

			// Token: 0x0200D51A RID: 54554
			public class PreventEditBAccountVOrgBAccountIDOnVendorMaint : POVendorInventory.vendorID.PreventEditBAccountVOrgBAccountID<VendorMaint>
			{
				// Token: 0x060301C1 RID: 197057 RVA: 0x008EF184 File Offset: 0x008ED384
				public static bool IsActive()
				{
					return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
				}
			}

			// Token: 0x0200D51B RID: 54555
			public class PreventEditBAccountVOrgBAccountIDOnCustomerMaint : POVendorInventory.vendorID.PreventEditBAccountVOrgBAccountID<CustomerMaint>
			{
				// Token: 0x060301C3 RID: 197059 RVA: 0x008EF193 File Offset: 0x008ED393
				public static bool IsActive()
				{
					return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
				}
			}
		}

		// Token: 0x02003C09 RID: 15369
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class vendorLocationID : BqlType<IBqlInt, int>.Field<POVendorInventory.vendorLocationID>
		{
		}

		// Token: 0x02003C0A RID: 15370
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class allLocations : BqlType<IBqlBool, bool>.Field<POVendorInventory.allLocations>
		{
		}

		// Token: 0x02003C0B RID: 15371
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class inventoryID : BqlType<IBqlInt, int>.Field<POVendorInventory.inventoryID>
		{
		}

		// Token: 0x02003C0C RID: 15372
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class subItemID : BqlType<IBqlInt, int>.Field<POVendorInventory.subItemID>
		{
		}

		// Token: 0x02003C0D RID: 15373
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class purchaseUnit : BqlType<IBqlString, string>.Field<POVendorInventory.purchaseUnit>
		{
		}

		// Token: 0x02003C0E RID: 15374
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class vendorInventoryID : BqlType<IBqlString, string>.Field<POVendorInventory.vendorInventoryID>
		{
		}

		// Token: 0x02003C0F RID: 15375
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class vLeadTime : BqlType<IBqlShort, short>.Field<POVendorInventory.vLeadTime>
		{
		}

		// Token: 0x02003C10 RID: 15376
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class overrideSettings : BqlType<IBqlBool, bool>.Field<POVendorInventory.overrideSettings>
		{
		}

		// Token: 0x02003C11 RID: 15377
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class addLeadTimeDays : BqlType<IBqlShort, short>.Field<POVendorInventory.addLeadTimeDays>
		{
		}

		// Token: 0x02003C12 RID: 15378
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class active : BqlType<IBqlBool, bool>.Field<POVendorInventory.active>
		{
		}

		// Token: 0x02003C13 RID: 15379
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class minOrdFreq : BqlType<IBqlInt, int>.Field<POVendorInventory.minOrdFreq>
		{
		}

		// Token: 0x02003C14 RID: 15380
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class minOrdQty : BqlType<IBqlDecimal, decimal>.Field<POVendorInventory.minOrdQty>
		{
		}

		// Token: 0x02003C15 RID: 15381
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class maxOrdQty : BqlType<IBqlDecimal, decimal>.Field<POVendorInventory.maxOrdQty>
		{
		}

		// Token: 0x02003C16 RID: 15382
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lotSize : BqlType<IBqlDecimal, decimal>.Field<POVendorInventory.lotSize>
		{
		}

		// Token: 0x02003C17 RID: 15383
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class eRQ : BqlType<IBqlDecimal, decimal>.Field<POVendorInventory.eRQ>
		{
		}

		// Token: 0x02003C18 RID: 15384
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastPrice : BqlType<IBqlDecimal, decimal>.Field<POVendorInventory.lastPrice>
		{
		}

		// Token: 0x02003C19 RID: 15385
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class curyID : BqlType<IBqlString, string>.Field<POVendorInventory.curyID>
		{
		}

		// Token: 0x02003C1A RID: 15386
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class isDefault : BqlType<IBqlBool, bool>.Field<POVendorInventory.isDefault>
		{
		}

		// Token: 0x02003C1B RID: 15387
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class prepaymentPct : BqlType<IBqlDecimal, decimal>.Field<POVendorInventory.prepaymentPct>
		{
		}

		// Token: 0x02003C1C RID: 15388
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class noteID : BqlType<IBqlGuid, Guid>.Field<POVendorInventory.noteID>
		{
		}

		// Token: 0x02003C1D RID: 15389
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class Tstamp : BqlType<IBqlByteArray, byte[]>.Field<POVendorInventory.Tstamp>
		{
		}

		// Token: 0x02003C1E RID: 15390
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<POVendorInventory.createdByID>
		{
		}

		// Token: 0x02003C1F RID: 15391
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class createdByScreenID : BqlType<IBqlString, string>.Field<POVendorInventory.createdByScreenID>
		{
		}

		// Token: 0x02003C20 RID: 15392
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<POVendorInventory.createdDateTime>
		{
		}

		// Token: 0x02003C21 RID: 15393
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<POVendorInventory.lastModifiedByID>
		{
		}

		// Token: 0x02003C22 RID: 15394
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class lastModifiedByScreenID : BqlType<IBqlString, string>.Field<POVendorInventory.lastModifiedByScreenID>
		{
		}

		// Token: 0x02003C23 RID: 15395
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<POVendorInventory.lastModifiedDateTime>
		{
		}
	}
}
