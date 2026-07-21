using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000014 RID: 20
	[PXCacheName("SelectedPackageContents")]
	[Serializable]
	public class SelectedPackageContents : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600008B RID: 139 RVA: 0x000044C0 File Offset: 0x000026C0
		// (set) Token: 0x0600008C RID: 140 RVA: 0x000044C8 File Offset: 0x000026C8
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID { get; set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600008D RID: 141 RVA: 0x000044D1 File Offset: 0x000026D1
		// (set) Token: 0x0600008E RID: 142 RVA: 0x000044D9 File Offset: 0x000026D9
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXDBDefault(typeof(SOShipment.shipmentNbr))]
		public virtual string ShipmentNbr { get; set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600008F RID: 143 RVA: 0x000044E2 File Offset: 0x000026E2
		// (set) Token: 0x06000090 RID: 144 RVA: 0x000044EA File Offset: 0x000026EA
		[PXDBInt]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.lineNbr>))]
		public int? ShipmentLineNbr { get; set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000091 RID: 145 RVA: 0x000044F3 File Offset: 0x000026F3
		// (set) Token: 0x06000092 RID: 146 RVA: 0x000044FB File Offset: 0x000026FB
		[PXDBInt]
		[PXUIField(DisplayName = "Shipment Split Line Nbr.", IsReadOnly = true)]
		[PXSelector(typeof(Search<SOShipLineSplit.splitLineNbr, Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOPackageDetail.shipmentNbr>>>>), new Type[]
		{
			typeof(SOShipLineSplit.lineNbr),
			typeof(SOShipLineSplit.splitLineNbr),
			typeof(SOShipLineSplit.origOrderType),
			typeof(SOShipLineSplit.origOrderNbr),
			typeof(SOShipLineSplit.inventoryID),
			typeof(SOShipLineSplit.lotSerialNbr),
			typeof(SOShipLineSplit.qty),
			typeof(SOShipLineSplit.packedQty),
			typeof(SOShipLineSplit.uOM)
		}, DirtyRead = true)]
		public virtual int? ShipmentSplitLineNbr { get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000093 RID: 147 RVA: 0x00004504 File Offset: 0x00002704
		// (set) Token: 0x06000094 RID: 148 RVA: 0x0000450C File Offset: 0x0000270C
		[PXDBInt]
		[PXDBDefault(typeof(SOPackageDetail.lineNbr))]
		public virtual int? PackageLineNbr { get; set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000095 RID: 149 RVA: 0x00004515 File Offset: 0x00002715
		// (set) Token: 0x06000096 RID: 150 RVA: 0x0000451D File Offset: 0x0000271D
		[Inventory(Enabled = false)]
		[PXDefault(PersistingCheck = 2)]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.inventoryID>))]
		public int? InventoryID { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000097 RID: 151 RVA: 0x00004526 File Offset: 0x00002726
		// (set) Token: 0x06000098 RID: 152 RVA: 0x0000452E File Offset: 0x0000272E
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Lot/Serial Nbr.", FieldClass = "LotSerial", Enabled = false)]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.lotSerialNbr>))]
		public virtual string LotSerialNbr { get; set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000099 RID: 153 RVA: 0x00004537 File Offset: 0x00002737
		// (set) Token: 0x0600009A RID: 154 RVA: 0x0000453F File Offset: 0x0000273F
		[INUnit(typeof(SelectedPackageContents.inventoryID), DisplayName = "UOM", Enabled = false)]
		[PXDefault(PersistingCheck = 2)]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.uOM>))]
		public virtual string UOM { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600009B RID: 155 RVA: 0x00004548 File Offset: 0x00002748
		// (set) Token: 0x0600009C RID: 156 RVA: 0x00004550 File Offset: 0x00002750
		[PXDBQuantity(typeof(SelectedPackageContents.uOM), typeof(SelectedPackageContents.basePackedQty))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		[PXUIField(DisplayName = "Quantity", IsReadOnly = true)]
		public virtual decimal? PackedQty { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600009D RID: 157 RVA: 0x00004559 File Offset: 0x00002759
		// (set) Token: 0x0600009E RID: 158 RVA: 0x00004561 File Offset: 0x00002761
		[PXDBDecimal(6)]
		[PXDefault(PersistingCheck = 2)]
		public virtual decimal? BasePackedQty { get; set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600009F RID: 159 RVA: 0x0000456A File Offset: 0x0000276A
		// (set) Token: 0x060000A0 RID: 160 RVA: 0x00004572 File Offset: 0x00002772
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		[PXUIField(DisplayName = "Remaining Qty")]
		public virtual decimal? RemainingQty { get; set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000A1 RID: 161 RVA: 0x0000457B File Offset: 0x0000277B
		// (set) Token: 0x060000A2 RID: 162 RVA: 0x00004583 File Offset: 0x00002783
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Skipped Status")]
		public bool? SkippedStatus { get; set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000A3 RID: 163 RVA: 0x0000458C File Offset: 0x0000278C
		// (set) Token: 0x060000A4 RID: 164 RVA: 0x00004594 File Offset: 0x00002794
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Order Nbr", IsReadOnly = true, FieldClass = "OrderNbr")]
		public virtual string OrderNbr { get; set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000A5 RID: 165 RVA: 0x0000459D File Offset: 0x0000279D
		// (set) Token: 0x060000A6 RID: 166 RVA: 0x000045A5 File Offset: 0x000027A5
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Order Line Nbr", IsReadOnly = true)]
		public virtual string OrderLineNbr { get; set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000A7 RID: 167 RVA: 0x000045AE File Offset: 0x000027AE
		// (set) Token: 0x060000A8 RID: 168 RVA: 0x000045B6 File Offset: 0x000027B6
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Store #", IsReadOnly = true, FieldClass = "StoreNbr")]
		public virtual string StoreNbr { get; set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000A9 RID: 169 RVA: 0x000045BF File Offset: 0x000027BF
		// (set) Token: 0x060000AA RID: 170 RVA: 0x000045C7 File Offset: 0x000027C7
		[PXUIField(DisplayName = "Default Issue From", IsReadOnly = true)]
		[PXDBInt]
		[PXSelector(typeof(INLocation.locationID), SubstituteKey = typeof(INLocation.locationCD))]
		public int? DefaultIssueFrom { get; set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000AB RID: 171 RVA: 0x000045D0 File Offset: 0x000027D0
		// (set) Token: 0x060000AC RID: 172 RVA: 0x000045D8 File Offset: 0x000027D8
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000AD RID: 173 RVA: 0x000045E1 File Offset: 0x000027E1
		// (set) Token: 0x060000AE RID: 174 RVA: 0x000045E9 File Offset: 0x000027E9
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000AF RID: 175 RVA: 0x000045F2 File Offset: 0x000027F2
		// (set) Token: 0x060000B0 RID: 176 RVA: 0x000045FA File Offset: 0x000027FA
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000B1 RID: 177 RVA: 0x00004603 File Offset: 0x00002803
		// (set) Token: 0x060000B2 RID: 178 RVA: 0x0000460B File Offset: 0x0000280B
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000B3 RID: 179 RVA: 0x00004614 File Offset: 0x00002814
		// (set) Token: 0x060000B4 RID: 180 RVA: 0x0000461C File Offset: 0x0000281C
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000B5 RID: 181 RVA: 0x00004625 File Offset: 0x00002825
		// (set) Token: 0x060000B6 RID: 182 RVA: 0x0000462D File Offset: 0x0000282D
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000B7 RID: 183 RVA: 0x00004636 File Offset: 0x00002836
		// (set) Token: 0x060000B8 RID: 184 RVA: 0x0000463E File Offset: 0x0000283E
		[PXDBTimestamp]
		public virtual byte[] Tstamp { get; set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000B9 RID: 185 RVA: 0x00004647 File Offset: 0x00002847
		// (set) Token: 0x060000BA RID: 186 RVA: 0x0000464F File Offset: 0x0000284F
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x02000048 RID: 72
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class recordID : BqlType<IBqlInt, int>.Field<SelectedPackageContents.recordID>
		{
		}

		// Token: 0x02000049 RID: 73
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class shipmentNbr : BqlType<IBqlString, string>.Field<SelectedPackageContents.shipmentNbr>
		{
		}

		// Token: 0x0200004A RID: 74
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class shipmentLineNbr : BqlType<IBqlInt, int>.Field<SelectedPackageContents.shipmentLineNbr>
		{
		}

		// Token: 0x0200004B RID: 75
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class shipmentSplitLineNbr : BqlType<IBqlInt, int>.Field<SelectedPackageContents.shipmentSplitLineNbr>
		{
		}

		// Token: 0x0200004C RID: 76
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class packageLineNbr : BqlType<IBqlInt, int>.Field<SelectedPackageContents.packageLineNbr>
		{
		}

		// Token: 0x0200004D RID: 77
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class inventoryID : BqlType<IBqlInt, int>.Field<SelectedPackageContents.inventoryID>
		{
		}

		// Token: 0x0200004E RID: 78
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class lotSerialNbr : BqlType<IBqlString, string>.Field<SelectedPackageContents.lotSerialNbr>
		{
		}

		// Token: 0x0200004F RID: 79
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class uOM : BqlType<IBqlString, string>.Field<SelectedPackageContents.uOM>
		{
		}

		// Token: 0x02000050 RID: 80
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class packedQty : BqlType<IBqlDecimal, decimal>.Field<SelectedPackageContents.packedQty>
		{
		}

		// Token: 0x02000051 RID: 81
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class basePackedQty : BqlType<IBqlDecimal, decimal>.Field<SelectedPackageContents.basePackedQty>
		{
		}

		// Token: 0x02000052 RID: 82
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class remainingQty : BqlType<IBqlDecimal, decimal>.Field<SelectedPackageContents.remainingQty>
		{
		}

		// Token: 0x02000053 RID: 83
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class skippedStatus : BqlType<IBqlBool, bool>.Field<SelectedPackageContents.skippedStatus>
		{
		}

		// Token: 0x02000054 RID: 84
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class orderNbr : BqlType<IBqlString, string>.Field<SelectedPackageContents.orderNbr>
		{
		}

		// Token: 0x02000055 RID: 85
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class orderLineNbr : BqlType<IBqlString, string>.Field<SelectedPackageContents.orderLineNbr>
		{
		}

		// Token: 0x02000056 RID: 86
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class storeNbr : BqlType<IBqlString, string>.Field<SelectedPackageContents.storeNbr>
		{
		}

		// Token: 0x02000057 RID: 87
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class defaultIssueFrom : BqlType<IBqlInt, int>.Field<SelectedPackageContents.defaultIssueFrom>
		{
		}

		// Token: 0x02000058 RID: 88
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<SelectedPackageContents.createdDateTime>
		{
		}

		// Token: 0x02000059 RID: 89
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<SelectedPackageContents.createdByID>
		{
		}

		// Token: 0x0200005A RID: 90
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class createdByScreenID : BqlType<IBqlString, string>.Field<SelectedPackageContents.createdByScreenID>
		{
		}

		// Token: 0x0200005B RID: 91
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<SelectedPackageContents.lastModifiedDateTime>
		{
		}

		// Token: 0x0200005C RID: 92
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<SelectedPackageContents.lastModifiedByID>
		{
		}

		// Token: 0x0200005D RID: 93
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class lastModifiedByScreenID : BqlType<IBqlString, string>.Field<SelectedPackageContents.lastModifiedByScreenID>
		{
		}

		// Token: 0x0200005E RID: 94
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class tstamp : BqlType<IBqlByteArray, byte[]>.Field<SelectedPackageContents.tstamp>
		{
		}

		// Token: 0x0200005F RID: 95
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class noteID : BqlType<IBqlGuid, Guid>.Field<SelectedPackageContents.noteID>
		{
		}
	}
}
