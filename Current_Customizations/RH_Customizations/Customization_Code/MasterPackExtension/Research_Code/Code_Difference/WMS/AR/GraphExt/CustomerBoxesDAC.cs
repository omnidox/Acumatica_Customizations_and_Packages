using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CS;

namespace WMS.AR.GraphExt
{
	// Token: 0x0200001C RID: 28
	[PXCacheName("CustomerBoxesDAC")]
	[Serializable]
	public class CustomerBoxesDAC : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000104 RID: 260 RVA: 0x0000828D File Offset: 0x0000648D
		// (set) Token: 0x06000105 RID: 261 RVA: 0x00008295 File Offset: 0x00006495
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Box ID")]
		[PXSelector(typeof(Search<CSBox.boxID>))]
		public virtual string BoxID { get; set; }

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000106 RID: 262 RVA: 0x0000829E File Offset: 0x0000649E
		// (set) Token: 0x06000107 RID: 263 RVA: 0x000082A6 File Offset: 0x000064A6
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(Customer.bAccountID))]
		public virtual int? CustomerID { get; set; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000108 RID: 264 RVA: 0x000082AF File Offset: 0x000064AF
		// (set) Token: 0x06000109 RID: 265 RVA: 0x000082B7 File Offset: 0x000064B7
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = 7, IsReadOnly = true)]
		public virtual string Description { get; set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x0600010A RID: 266 RVA: 0x000082C0 File Offset: 0x000064C0
		// (set) Token: 0x0600010B RID: 267 RVA: 0x000082C8 File Offset: 0x000064C8
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXUIField(DisplayName = "Box Weight", Visibility = 7, IsReadOnly = true)]
		public virtual decimal? BoxWeight { get; set; }

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600010C RID: 268 RVA: 0x000082D1 File Offset: 0x000064D1
		// (set) Token: 0x0600010D RID: 269 RVA: 0x000082D9 File Offset: 0x000064D9
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXUIField(DisplayName = "Max. Weight", Visibility = 7, IsReadOnly = true)]
		public virtual decimal? MaxWeight { get; set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600010E RID: 270 RVA: 0x000082E2 File Offset: 0x000064E2
		// (set) Token: 0x0600010F RID: 271 RVA: 0x000082EA File Offset: 0x000064EA
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXUIField(DisplayName = "Weight UOM", Visibility = 3, IsReadOnly = true)]
		public virtual string WeightOUM { get; set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000110 RID: 272 RVA: 0x000082F3 File Offset: 0x000064F3
		// (set) Token: 0x06000111 RID: 273 RVA: 0x000082FB File Offset: 0x000064FB
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXUIField(DisplayName = "Max Volume", Visibility = 7, IsReadOnly = true)]
		public virtual decimal? MaxVolume { get; set; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000112 RID: 274 RVA: 0x00008304 File Offset: 0x00006504
		// (set) Token: 0x06000113 RID: 275 RVA: 0x0000830C File Offset: 0x0000650C
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXUIField(DisplayName = "Volume UOM", Visibility = 3, IsReadOnly = true)]
		public virtual string VolumeUOM { get; set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000114 RID: 276 RVA: 0x00008315 File Offset: 0x00006515
		// (set) Token: 0x06000115 RID: 277 RVA: 0x0000831D File Offset: 0x0000651D
		[PXDBInt(MinValue = 0)]
		[PXUIField(DisplayName = "Length", Visibility = 7, IsReadOnly = true)]
		public virtual int? Length { get; set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000116 RID: 278 RVA: 0x00008326 File Offset: 0x00006526
		// (set) Token: 0x06000117 RID: 279 RVA: 0x0000832E File Offset: 0x0000652E
		[PXDBInt(MinValue = 0)]
		[PXUIField(DisplayName = "Width", Visibility = 7, IsReadOnly = true)]
		public virtual int? Width { get; set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000118 RID: 280 RVA: 0x00008337 File Offset: 0x00006537
		// (set) Token: 0x06000119 RID: 281 RVA: 0x0000833F File Offset: 0x0000653F
		[PXDBInt(MinValue = 0)]
		[PXUIField(DisplayName = "Height", Visibility = 7, IsReadOnly = true)]
		public virtual int? Height { get; set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600011A RID: 282 RVA: 0x00008348 File Offset: 0x00006548
		// (set) Token: 0x0600011B RID: 283 RVA: 0x00008350 File Offset: 0x00006550
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXUIField(DisplayName = "Linear UOM", Visibility = 3, IsReadOnly = true)]
		public virtual string LinearUOM { get; set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600011C RID: 284 RVA: 0x00008359 File Offset: 0x00006559
		// (set) Token: 0x0600011D RID: 285 RVA: 0x00008361 File Offset: 0x00006561
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Carrier's Package", Visibility = 7, IsReadOnly = true)]
		public virtual string CarrierBox { get; set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600011E RID: 286 RVA: 0x0000836A File Offset: 0x0000656A
		// (set) Token: 0x0600011F RID: 287 RVA: 0x00008372 File Offset: 0x00006572
		[PXDBBool]
		[PXDefault(true, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Active for this customer")]
		public virtual bool? Active { get; set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000120 RID: 288 RVA: 0x0000837B File Offset: 0x0000657B
		// (set) Token: 0x06000121 RID: 289 RVA: 0x00008383 File Offset: 0x00006583
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000122 RID: 290 RVA: 0x0000838C File Offset: 0x0000658C
		// (set) Token: 0x06000123 RID: 291 RVA: 0x00008394 File Offset: 0x00006594
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000124 RID: 292 RVA: 0x0000839D File Offset: 0x0000659D
		// (set) Token: 0x06000125 RID: 293 RVA: 0x000083A5 File Offset: 0x000065A5
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000126 RID: 294 RVA: 0x000083AE File Offset: 0x000065AE
		// (set) Token: 0x06000127 RID: 295 RVA: 0x000083B6 File Offset: 0x000065B6
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000128 RID: 296 RVA: 0x000083BF File Offset: 0x000065BF
		// (set) Token: 0x06000129 RID: 297 RVA: 0x000083C7 File Offset: 0x000065C7
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x0600012A RID: 298 RVA: 0x000083D0 File Offset: 0x000065D0
		// (set) Token: 0x0600012B RID: 299 RVA: 0x000083D8 File Offset: 0x000065D8
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600012C RID: 300 RVA: 0x000083E1 File Offset: 0x000065E1
		// (set) Token: 0x0600012D RID: 301 RVA: 0x000083E9 File Offset: 0x000065E9
		[PXDBTimestamp]
		public virtual byte[] Tstamp { get; set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x0600012E RID: 302 RVA: 0x000083F2 File Offset: 0x000065F2
		// (set) Token: 0x0600012F RID: 303 RVA: 0x000083FA File Offset: 0x000065FA
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x02000073 RID: 115
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class boxID : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.boxID>
		{
		}

		// Token: 0x02000074 RID: 116
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class customerID : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.customerID>
		{
		}

		// Token: 0x02000075 RID: 117
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class description : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.description>
		{
		}

		// Token: 0x02000076 RID: 118
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class boxWeight : BqlType<IBqlDecimal, decimal>.Field<CustomerBoxesDAC.boxWeight>
		{
		}

		// Token: 0x02000077 RID: 119
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class maxWeight : BqlType<IBqlDecimal, decimal>.Field<CustomerBoxesDAC.maxWeight>
		{
		}

		// Token: 0x02000078 RID: 120
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class weightOUM : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.weightOUM>
		{
		}

		// Token: 0x02000079 RID: 121
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class maxVolume : BqlType<IBqlDecimal, decimal>.Field<CustomerBoxesDAC.maxVolume>
		{
		}

		// Token: 0x0200007A RID: 122
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class volumeUOM : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.volumeUOM>
		{
		}

		// Token: 0x0200007B RID: 123
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class length : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.length>
		{
		}

		// Token: 0x0200007C RID: 124
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class width : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.width>
		{
		}

		// Token: 0x0200007D RID: 125
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class height : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.height>
		{
		}

		// Token: 0x0200007E RID: 126
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class linearUOM : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.linearUOM>
		{
		}

		// Token: 0x0200007F RID: 127
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class carrierBox : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.carrierBox>
		{
		}

		// Token: 0x02000080 RID: 128
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class active : BqlType<IBqlBool, bool>.Field<CustomerBoxesDAC.active>
		{
		}

		// Token: 0x02000081 RID: 129
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerBoxesDAC.createdDateTime>
		{
		}

		// Token: 0x02000082 RID: 130
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<CustomerBoxesDAC.createdByID>
		{
		}

		// Token: 0x02000083 RID: 131
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class createdByScreenID : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.createdByScreenID>
		{
		}

		// Token: 0x02000084 RID: 132
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerBoxesDAC.lastModifiedDateTime>
		{
		}

		// Token: 0x02000085 RID: 133
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<CustomerBoxesDAC.lastModifiedByID>
		{
		}

		// Token: 0x02000086 RID: 134
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class lastModifiedByScreenID : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.lastModifiedByScreenID>
		{
		}

		// Token: 0x02000087 RID: 135
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class tstamp : BqlType<IBqlByteArray, byte[]>.Field<CustomerBoxesDAC.tstamp>
		{
		}

		// Token: 0x02000088 RID: 136
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class noteID : BqlType<IBqlGuid, Guid>.Field<CustomerBoxesDAC.noteID>
		{
		}
	}
}
