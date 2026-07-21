using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CS;

namespace WMS.AR.GraphExt
{
	// Token: 0x0200001E RID: 30
	[PXCacheName("CustomerBoxesDAC")]
	[Serializable]
	public class CustomerBoxesDAC : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600011B RID: 283 RVA: 0x0000979D File Offset: 0x0000799D
		// (set) Token: 0x0600011C RID: 284 RVA: 0x000097A5 File Offset: 0x000079A5
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Box ID")]
		[PXSelector(typeof(Search<CSBox.boxID>))]
		public virtual string BoxID { get; set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600011D RID: 285 RVA: 0x000097AE File Offset: 0x000079AE
		// (set) Token: 0x0600011E RID: 286 RVA: 0x000097B6 File Offset: 0x000079B6
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(Customer.bAccountID))]
		public virtual int? CustomerID { get; set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600011F RID: 287 RVA: 0x000097BF File Offset: 0x000079BF
		// (set) Token: 0x06000120 RID: 288 RVA: 0x000097C7 File Offset: 0x000079C7
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = 7, IsReadOnly = true)]
		public virtual string Description { get; set; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000121 RID: 289 RVA: 0x000097D0 File Offset: 0x000079D0
		// (set) Token: 0x06000122 RID: 290 RVA: 0x000097D8 File Offset: 0x000079D8
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXUIField(DisplayName = "Box Weight", Visibility = 7, IsReadOnly = true)]
		public virtual decimal? BoxWeight { get; set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000123 RID: 291 RVA: 0x000097E1 File Offset: 0x000079E1
		// (set) Token: 0x06000124 RID: 292 RVA: 0x000097E9 File Offset: 0x000079E9
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXUIField(DisplayName = "Max. Weight", Visibility = 7, IsReadOnly = true)]
		public virtual decimal? MaxWeight { get; set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000125 RID: 293 RVA: 0x000097F2 File Offset: 0x000079F2
		// (set) Token: 0x06000126 RID: 294 RVA: 0x000097FA File Offset: 0x000079FA
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXUIField(DisplayName = "Weight UOM", Visibility = 3, IsReadOnly = true)]
		public virtual string WeightOUM { get; set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000127 RID: 295 RVA: 0x00009803 File Offset: 0x00007A03
		// (set) Token: 0x06000128 RID: 296 RVA: 0x0000980B File Offset: 0x00007A0B
		[PXDBDecimal(4, MinValue = 0.0)]
		[PXUIField(DisplayName = "Max Volume", Visibility = 7, IsReadOnly = true)]
		public virtual decimal? MaxVolume { get; set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000129 RID: 297 RVA: 0x00009814 File Offset: 0x00007A14
		// (set) Token: 0x0600012A RID: 298 RVA: 0x0000981C File Offset: 0x00007A1C
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXUIField(DisplayName = "Volume UOM", Visibility = 3, IsReadOnly = true)]
		public virtual string VolumeUOM { get; set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600012B RID: 299 RVA: 0x00009825 File Offset: 0x00007A25
		// (set) Token: 0x0600012C RID: 300 RVA: 0x0000982D File Offset: 0x00007A2D
		[PXDBInt(MinValue = 0)]
		[PXUIField(DisplayName = "Length", Visibility = 7, IsReadOnly = true)]
		public virtual int? Length { get; set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600012D RID: 301 RVA: 0x00009836 File Offset: 0x00007A36
		// (set) Token: 0x0600012E RID: 302 RVA: 0x0000983E File Offset: 0x00007A3E
		[PXDBInt(MinValue = 0)]
		[PXUIField(DisplayName = "Width", Visibility = 7, IsReadOnly = true)]
		public virtual int? Width { get; set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x0600012F RID: 303 RVA: 0x00009847 File Offset: 0x00007A47
		// (set) Token: 0x06000130 RID: 304 RVA: 0x0000984F File Offset: 0x00007A4F
		[PXDBInt(MinValue = 0)]
		[PXUIField(DisplayName = "Height", Visibility = 7, IsReadOnly = true)]
		public virtual int? Height { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000131 RID: 305 RVA: 0x00009858 File Offset: 0x00007A58
		// (set) Token: 0x06000132 RID: 306 RVA: 0x00009860 File Offset: 0x00007A60
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXUIField(DisplayName = "Linear UOM", Visibility = 3, IsReadOnly = true)]
		public virtual string LinearUOM { get; set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000133 RID: 307 RVA: 0x00009869 File Offset: 0x00007A69
		// (set) Token: 0x06000134 RID: 308 RVA: 0x00009871 File Offset: 0x00007A71
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Carrier's Package", Visibility = 7, IsReadOnly = true)]
		public virtual string CarrierBox { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000135 RID: 309 RVA: 0x0000987A File Offset: 0x00007A7A
		// (set) Token: 0x06000136 RID: 310 RVA: 0x00009882 File Offset: 0x00007A82
		[PXDBBool]
		[PXDefault(true, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Active for this customer")]
		public virtual bool? Active { get; set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000137 RID: 311 RVA: 0x0000988B File Offset: 0x00007A8B
		// (set) Token: 0x06000138 RID: 312 RVA: 0x00009893 File Offset: 0x00007A93
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000139 RID: 313 RVA: 0x0000989C File Offset: 0x00007A9C
		// (set) Token: 0x0600013A RID: 314 RVA: 0x000098A4 File Offset: 0x00007AA4
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600013B RID: 315 RVA: 0x000098AD File Offset: 0x00007AAD
		// (set) Token: 0x0600013C RID: 316 RVA: 0x000098B5 File Offset: 0x00007AB5
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x0600013D RID: 317 RVA: 0x000098BE File Offset: 0x00007ABE
		// (set) Token: 0x0600013E RID: 318 RVA: 0x000098C6 File Offset: 0x00007AC6
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x0600013F RID: 319 RVA: 0x000098CF File Offset: 0x00007ACF
		// (set) Token: 0x06000140 RID: 320 RVA: 0x000098D7 File Offset: 0x00007AD7
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000141 RID: 321 RVA: 0x000098E0 File Offset: 0x00007AE0
		// (set) Token: 0x06000142 RID: 322 RVA: 0x000098E8 File Offset: 0x00007AE8
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000143 RID: 323 RVA: 0x000098F1 File Offset: 0x00007AF1
		// (set) Token: 0x06000144 RID: 324 RVA: 0x000098F9 File Offset: 0x00007AF9
		[PXDBTimestamp]
		public virtual byte[] Tstamp { get; set; }

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000145 RID: 325 RVA: 0x00009902 File Offset: 0x00007B02
		// (set) Token: 0x06000146 RID: 326 RVA: 0x0000990A File Offset: 0x00007B0A
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x0200007D RID: 125
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

		// Token: 0x0200007E RID: 126
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class customerID : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.customerID>
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
		public abstract class description : BqlType<IBqlString, string>.Field<CustomerBoxesDAC.description>
		{
		}

		// Token: 0x02000080 RID: 128
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class boxWeight : BqlType<IBqlDecimal, decimal>.Field<CustomerBoxesDAC.boxWeight>
		{
		}

		// Token: 0x02000081 RID: 129
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class maxWeight : BqlType<IBqlDecimal, decimal>.Field<CustomerBoxesDAC.maxWeight>
		{
		}

		// Token: 0x02000082 RID: 130
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

		// Token: 0x02000083 RID: 131
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class maxVolume : BqlType<IBqlDecimal, decimal>.Field<CustomerBoxesDAC.maxVolume>
		{
		}

		// Token: 0x02000084 RID: 132
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

		// Token: 0x02000085 RID: 133
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class length : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.length>
		{
		}

		// Token: 0x02000086 RID: 134
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class width : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.width>
		{
		}

		// Token: 0x02000087 RID: 135
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class height : BqlType<IBqlInt, int>.Field<CustomerBoxesDAC.height>
		{
		}

		// Token: 0x02000088 RID: 136
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

		// Token: 0x02000089 RID: 137
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

		// Token: 0x0200008A RID: 138
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class active : BqlType<IBqlBool, bool>.Field<CustomerBoxesDAC.active>
		{
		}

		// Token: 0x0200008B RID: 139
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerBoxesDAC.createdDateTime>
		{
		}

		// Token: 0x0200008C RID: 140
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<CustomerBoxesDAC.createdByID>
		{
		}

		// Token: 0x0200008D RID: 141
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

		// Token: 0x0200008E RID: 142
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerBoxesDAC.lastModifiedDateTime>
		{
		}

		// Token: 0x0200008F RID: 143
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<CustomerBoxesDAC.lastModifiedByID>
		{
		}

		// Token: 0x02000090 RID: 144
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

		// Token: 0x02000091 RID: 145
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

		// Token: 0x02000092 RID: 146
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
