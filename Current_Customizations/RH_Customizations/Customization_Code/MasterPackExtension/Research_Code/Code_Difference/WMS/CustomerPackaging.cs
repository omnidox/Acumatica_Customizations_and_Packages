using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x02000008 RID: 8
	[PXCacheName("CustomerPackaging")]
	[Serializable]
	public class CustomerPackaging : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600001A RID: 26 RVA: 0x0000265D File Offset: 0x0000085D
		// (set) Token: 0x0600001B RID: 27 RVA: 0x00002665 File Offset: 0x00000865
		[PXDBInt(IsKey = true)]
		public int? Customer { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600001C RID: 28 RVA: 0x0000266E File Offset: 0x0000086E
		// (set) Token: 0x0600001D RID: 29 RVA: 0x00002676 File Offset: 0x00000876
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Use Customer’s Packaging Option")]
		public bool? UseCustomersPackagingOption { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600001E RID: 30 RVA: 0x0000267F File Offset: 0x0000087F
		// (set) Token: 0x0600001F RID: 31 RVA: 0x00002687 File Offset: 0x00000887
		[PXDBString(1, IsFixed = true)]
		[PXDefault("W")]
		[PXUIField(DisplayName = "Default Packaging option", IsReadOnly = true)]
		[INPackageOption.ListAttribute]
		public virtual string DefaultPackaging { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000020 RID: 32 RVA: 0x00002690 File Offset: 0x00000890
		// (set) Token: 0x06000021 RID: 33 RVA: 0x00002698 File Offset: 0x00000898
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Pack Separately")]
		public bool? PackSeparately { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000026A1 File Offset: 0x000008A1
		// (set) Token: 0x06000023 RID: 35 RVA: 0x000026A9 File Offset: 0x000008A9
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Use only Customer’s Boxes")]
		public bool? UseOnlyCustomerBoxes { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000026B2 File Offset: 0x000008B2
		// (set) Token: 0x06000025 RID: 37 RVA: 0x000026BA File Offset: 0x000008BA
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000026C3 File Offset: 0x000008C3
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000026CB File Offset: 0x000008CB
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000028 RID: 40 RVA: 0x000026D4 File Offset: 0x000008D4
		// (set) Token: 0x06000029 RID: 41 RVA: 0x000026DC File Offset: 0x000008DC
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600002A RID: 42 RVA: 0x000026E5 File Offset: 0x000008E5
		// (set) Token: 0x0600002B RID: 43 RVA: 0x000026ED File Offset: 0x000008ED
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600002C RID: 44 RVA: 0x000026F6 File Offset: 0x000008F6
		// (set) Token: 0x0600002D RID: 45 RVA: 0x000026FE File Offset: 0x000008FE
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002707 File Offset: 0x00000907
		// (set) Token: 0x0600002F RID: 47 RVA: 0x0000270F File Offset: 0x0000090F
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000030 RID: 48 RVA: 0x00002718 File Offset: 0x00000918
		// (set) Token: 0x06000031 RID: 49 RVA: 0x00002720 File Offset: 0x00000920
		[PXDBTimestamp]
		public virtual byte[] Tstamp { get; set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000032 RID: 50 RVA: 0x00002729 File Offset: 0x00000929
		// (set) Token: 0x06000033 RID: 51 RVA: 0x00002731 File Offset: 0x00000931
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x02000021 RID: 33
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class customer : BqlType<IBqlInt, int>.Field<CustomerPackaging.customer>
		{
		}

		// Token: 0x02000022 RID: 34
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class useCustomersPackagingOption : BqlType<IBqlBool, bool>.Field<CustomerPackaging.useCustomersPackagingOption>
		{
		}

		// Token: 0x02000023 RID: 35
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class defaultPackaging : BqlType<IBqlString, string>.Field<CustomerPackaging.defaultPackaging>
		{
		}

		// Token: 0x02000024 RID: 36
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class packSeparately : BqlType<IBqlBool, bool>.Field<CustomerPackaging.packSeparately>
		{
		}

		// Token: 0x02000025 RID: 37
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class useOnlyCustomerBoxes : BqlType<IBqlBool, bool>.Field<CustomerPackaging.useOnlyCustomerBoxes>
		{
		}

		// Token: 0x02000026 RID: 38
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerPackaging.createdDateTime>
		{
		}

		// Token: 0x02000027 RID: 39
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<CustomerPackaging.createdByID>
		{
		}

		// Token: 0x02000028 RID: 40
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class createdByScreenID : BqlType<IBqlString, string>.Field<CustomerPackaging.createdByScreenID>
		{
		}

		// Token: 0x02000029 RID: 41
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerPackaging.lastModifiedDateTime>
		{
		}

		// Token: 0x0200002A RID: 42
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<CustomerPackaging.lastModifiedByID>
		{
		}

		// Token: 0x0200002B RID: 43
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class lastModifiedByScreenID : BqlType<IBqlString, string>.Field<CustomerPackaging.lastModifiedByScreenID>
		{
		}

		// Token: 0x0200002C RID: 44
		[Nullable(new byte[]
		{
			0,
			1,
			1,
			0
		})]
		public abstract class tstamp : BqlType<IBqlByteArray, byte[]>.Field<CustomerPackaging.tstamp>
		{
		}

		// Token: 0x0200002D RID: 45
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class noteID : BqlType<IBqlGuid, Guid>.Field<CustomerPackaging.noteID>
		{
		}
	}
}
