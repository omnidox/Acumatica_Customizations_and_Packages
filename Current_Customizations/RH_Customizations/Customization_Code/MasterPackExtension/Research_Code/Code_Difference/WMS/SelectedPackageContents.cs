using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;
using PX.Objects.SO;

namespace WMS
{
	// Token: 0x02000012 RID: 18
	[PXCacheName("SelectedPackageContents")]
	[Serializable]
	public class SelectedPackageContents : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600007F RID: 127 RVA: 0x0000428C File Offset: 0x0000248C
		// (set) Token: 0x06000080 RID: 128 RVA: 0x00004294 File Offset: 0x00002494
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000081 RID: 129 RVA: 0x0000429D File Offset: 0x0000249D
		// (set) Token: 0x06000082 RID: 130 RVA: 0x000042A5 File Offset: 0x000024A5
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXDBDefault(typeof(SOShipment.shipmentNbr))]
		public virtual string ShipmentNbr { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000083 RID: 131 RVA: 0x000042AE File Offset: 0x000024AE
		// (set) Token: 0x06000084 RID: 132 RVA: 0x000042B6 File Offset: 0x000024B6
		[PXDBInt]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.lineNbr>))]
		public int? ShipmentLineNbr { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000085 RID: 133 RVA: 0x000042BF File Offset: 0x000024BF
		// (set) Token: 0x06000086 RID: 134 RVA: 0x000042C7 File Offset: 0x000024C7
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

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000087 RID: 135 RVA: 0x000042D0 File Offset: 0x000024D0
		// (set) Token: 0x06000088 RID: 136 RVA: 0x000042D8 File Offset: 0x000024D8
		[PXDBInt]
		[PXDBDefault(typeof(SOPackageDetail.lineNbr))]
		public virtual int? PackageLineNbr { get; set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000089 RID: 137 RVA: 0x000042E1 File Offset: 0x000024E1
		// (set) Token: 0x0600008A RID: 138 RVA: 0x000042E9 File Offset: 0x000024E9
		[Inventory(Enabled = false)]
		[PXDefault(PersistingCheck = 2)]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.inventoryID>))]
		public int? InventoryID { get; set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600008B RID: 139 RVA: 0x000042F2 File Offset: 0x000024F2
		// (set) Token: 0x0600008C RID: 140 RVA: 0x000042FA File Offset: 0x000024FA
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Lot/Serial Nbr.", FieldClass = "LotSerial", Enabled = false)]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.lotSerialNbr>))]
		public virtual string LotSerialNbr { get; set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600008D RID: 141 RVA: 0x00004303 File Offset: 0x00002503
		// (set) Token: 0x0600008E RID: 142 RVA: 0x0000430B File Offset: 0x0000250B
		[INUnit(typeof(SelectedPackageContents.inventoryID), DisplayName = "UOM", Enabled = false)]
		[PXDefault(PersistingCheck = 2)]
		[PXFormula(typeof(Selector<SelectedPackageContents.shipmentSplitLineNbr, SOShipLineSplit.uOM>))]
		public virtual string UOM { get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600008F RID: 143 RVA: 0x00004314 File Offset: 0x00002514
		// (set) Token: 0x06000090 RID: 144 RVA: 0x0000431C File Offset: 0x0000251C
		[PXDBQuantity(typeof(SelectedPackageContents.uOM), typeof(SelectedPackageContents.basePackedQty))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		[PXUIField(DisplayName = "Quantity", IsReadOnly = true)]
		public virtual decimal? PackedQty { get; set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000091 RID: 145 RVA: 0x00004325 File Offset: 0x00002525
		// (set) Token: 0x06000092 RID: 146 RVA: 0x0000432D File Offset: 0x0000252D
		[PXDBDecimal(6)]
		[PXDefault(PersistingCheck = 2)]
		public virtual decimal? BasePackedQty { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000093 RID: 147 RVA: 0x00004336 File Offset: 0x00002536
		// (set) Token: 0x06000094 RID: 148 RVA: 0x0000433E File Offset: 0x0000253E
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = 2)]
		[PXUIField(DisplayName = "Remaining Qty")]
		public virtual decimal? RemainingQty { get; set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000095 RID: 149 RVA: 0x00004347 File Offset: 0x00002547
		// (set) Token: 0x06000096 RID: 150 RVA: 0x0000434F File Offset: 0x0000254F
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Skipped Status")]
		public bool? SkippedStatus { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000097 RID: 151 RVA: 0x00004358 File Offset: 0x00002558
		// (set) Token: 0x06000098 RID: 152 RVA: 0x00004360 File Offset: 0x00002560
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Order Nbr", IsReadOnly = true, FieldClass = "OrderNbr")]
		public virtual string OrderNbr { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000099 RID: 153 RVA: 0x00004369 File Offset: 0x00002569
		// (set) Token: 0x0600009A RID: 154 RVA: 0x00004371 File Offset: 0x00002571
		[PXDBString(15, IsUnicode = true)]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Order Line Nbr", IsReadOnly = true)]
		public virtual string OrderLineNbr { get; set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600009B RID: 155 RVA: 0x0000437A File Offset: 0x0000257A
		// (set) Token: 0x0600009C RID: 156 RVA: 0x00004382 File Offset: 0x00002582
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXDefault(PersistingCheck = 2)]
		[PXUIField(DisplayName = "Store #", IsReadOnly = true, FieldClass = "StoreNbr")]
		public virtual string StoreNbr { get; set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x0600009D RID: 157 RVA: 0x0000438B File Offset: 0x0000258B
		// (set) Token: 0x0600009E RID: 158 RVA: 0x00004393 File Offset: 0x00002593
		[PXUIField(DisplayName = "Default Issue From", IsReadOnly = true)]
		[PXDBInt]
		[PXSelector(typeof(INLocation.locationID), SubstituteKey = typeof(INLocation.locationCD))]
		public int? DefaultIssueFrom { get; set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600009F RID: 159 RVA: 0x0000439C File Offset: 0x0000259C
		// (set) Token: 0x060000A0 RID: 160 RVA: 0x000043A4 File Offset: 0x000025A4
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000A1 RID: 161 RVA: 0x000043AD File Offset: 0x000025AD
		// (set) Token: 0x060000A2 RID: 162 RVA: 0x000043B5 File Offset: 0x000025B5
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000A3 RID: 163 RVA: 0x000043BE File Offset: 0x000025BE
		// (set) Token: 0x060000A4 RID: 164 RVA: 0x000043C6 File Offset: 0x000025C6
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000A5 RID: 165 RVA: 0x000043CF File Offset: 0x000025CF
		// (set) Token: 0x060000A6 RID: 166 RVA: 0x000043D7 File Offset: 0x000025D7
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000A7 RID: 167 RVA: 0x000043E0 File Offset: 0x000025E0
		// (set) Token: 0x060000A8 RID: 168 RVA: 0x000043E8 File Offset: 0x000025E8
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000A9 RID: 169 RVA: 0x000043F1 File Offset: 0x000025F1
		// (set) Token: 0x060000AA RID: 170 RVA: 0x000043F9 File Offset: 0x000025F9
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000AB RID: 171 RVA: 0x00004402 File Offset: 0x00002602
		// (set) Token: 0x060000AC RID: 172 RVA: 0x0000440A File Offset: 0x0000260A
		[PXDBTimestamp]
		public virtual byte[] Tstamp { get; set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000AD RID: 173 RVA: 0x00004413 File Offset: 0x00002613
		// (set) Token: 0x060000AE RID: 174 RVA: 0x0000441B File Offset: 0x0000261B
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x02000046 RID: 70
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class recordID : BqlType<IBqlInt, int>.Field<SelectedPackageContents.recordID>
		{
		}

		// Token: 0x02000047 RID: 71
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

		// Token: 0x02000048 RID: 72
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class shipmentLineNbr : BqlType<IBqlInt, int>.Field<SelectedPackageContents.shipmentLineNbr>
		{
		}

		// Token: 0x02000049 RID: 73
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class shipmentSplitLineNbr : BqlType<IBqlInt, int>.Field<SelectedPackageContents.shipmentSplitLineNbr>
		{
		}

		// Token: 0x0200004A RID: 74
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class packageLineNbr : BqlType<IBqlInt, int>.Field<SelectedPackageContents.packageLineNbr>
		{
		}

		// Token: 0x0200004B RID: 75
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class inventoryID : BqlType<IBqlInt, int>.Field<SelectedPackageContents.inventoryID>
		{
		}

		// Token: 0x0200004C RID: 76
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

		// Token: 0x0200004D RID: 77
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

		// Token: 0x0200004E RID: 78
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class packedQty : BqlType<IBqlDecimal, decimal>.Field<SelectedPackageContents.packedQty>
		{
		}

		// Token: 0x0200004F RID: 79
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class basePackedQty : BqlType<IBqlDecimal, decimal>.Field<SelectedPackageContents.basePackedQty>
		{
		}

		// Token: 0x02000050 RID: 80
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class remainingQty : BqlType<IBqlDecimal, decimal>.Field<SelectedPackageContents.remainingQty>
		{
		}

		// Token: 0x02000051 RID: 81
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class skippedStatus : BqlType<IBqlBool, bool>.Field<SelectedPackageContents.skippedStatus>
		{
		}

		// Token: 0x02000052 RID: 82
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

		// Token: 0x02000053 RID: 83
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

		// Token: 0x02000054 RID: 84
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

		// Token: 0x02000055 RID: 85
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class defaultIssueFrom : BqlType<IBqlInt, int>.Field<SelectedPackageContents.defaultIssueFrom>
		{
		}

		// Token: 0x02000056 RID: 86
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<SelectedPackageContents.createdDateTime>
		{
		}

		// Token: 0x02000057 RID: 87
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<SelectedPackageContents.createdByID>
		{
		}

		// Token: 0x02000058 RID: 88
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

		// Token: 0x02000059 RID: 89
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<SelectedPackageContents.lastModifiedDateTime>
		{
		}

		// Token: 0x0200005A RID: 90
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<SelectedPackageContents.lastModifiedByID>
		{
		}

		// Token: 0x0200005B RID: 91
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

		// Token: 0x0200005C RID: 92
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

		// Token: 0x0200005D RID: 93
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
