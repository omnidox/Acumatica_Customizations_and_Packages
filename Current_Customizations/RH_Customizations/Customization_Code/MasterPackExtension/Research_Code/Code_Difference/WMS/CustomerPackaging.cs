using System;
using System.Runtime.CompilerServices;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;

namespace WMS
{
	// Token: 0x02000006 RID: 6
	[PXCacheName("CustomerPackaging")]
	[Serializable]
	public class CustomerPackaging : PXBqlTable, IBqlTable, IBqlTableSystemDataStorage
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000E RID: 14 RVA: 0x00002429 File Offset: 0x00000629
		// (set) Token: 0x0600000F RID: 15 RVA: 0x00002431 File Offset: 0x00000631
		[PXDBInt(IsKey = true)]
		public int? Customer { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000010 RID: 16 RVA: 0x0000243A File Offset: 0x0000063A
		// (set) Token: 0x06000011 RID: 17 RVA: 0x00002442 File Offset: 0x00000642
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Use Customer’s Packaging Option")]
		public bool? UseCustomersPackagingOption { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000012 RID: 18 RVA: 0x0000244B File Offset: 0x0000064B
		// (set) Token: 0x06000013 RID: 19 RVA: 0x00002453 File Offset: 0x00000653
		[PXDBString(1, IsFixed = true)]
		[PXDefault("W")]
		[PXUIField(DisplayName = "Default Packaging option", IsReadOnly = true)]
		[INPackageOption.ListAttribute]
		public virtual string DefaultPackaging { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000014 RID: 20 RVA: 0x0000245C File Offset: 0x0000065C
		// (set) Token: 0x06000015 RID: 21 RVA: 0x00002464 File Offset: 0x00000664
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Pack Separately")]
		public bool? PackSeparately { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000016 RID: 22 RVA: 0x0000246D File Offset: 0x0000066D
		// (set) Token: 0x06000017 RID: 23 RVA: 0x00002475 File Offset: 0x00000675
		[PXDBBool]
		[PXDefault(false, PersistingCheck = 2)]
		[PXUIField(DisplayName = "Use only Customer’s Boxes")]
		public bool? UseOnlyCustomerBoxes { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000018 RID: 24 RVA: 0x0000247E File Offset: 0x0000067E
		// (set) Token: 0x06000019 RID: 25 RVA: 0x00002486 File Offset: 0x00000686
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600001A RID: 26 RVA: 0x0000248F File Offset: 0x0000068F
		// (set) Token: 0x0600001B RID: 27 RVA: 0x00002497 File Offset: 0x00000697
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600001C RID: 28 RVA: 0x000024A0 File Offset: 0x000006A0
		// (set) Token: 0x0600001D RID: 29 RVA: 0x000024A8 File Offset: 0x000006A8
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600001E RID: 30 RVA: 0x000024B1 File Offset: 0x000006B1
		// (set) Token: 0x0600001F RID: 31 RVA: 0x000024B9 File Offset: 0x000006B9
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000020 RID: 32 RVA: 0x000024C2 File Offset: 0x000006C2
		// (set) Token: 0x06000021 RID: 33 RVA: 0x000024CA File Offset: 0x000006CA
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000024D3 File Offset: 0x000006D3
		// (set) Token: 0x06000023 RID: 35 RVA: 0x000024DB File Offset: 0x000006DB
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000024E4 File Offset: 0x000006E4
		// (set) Token: 0x06000025 RID: 37 RVA: 0x000024EC File Offset: 0x000006EC
		[PXDBTimestamp]
		public virtual byte[] Tstamp { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000024F5 File Offset: 0x000006F5
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000024FD File Offset: 0x000006FD
		[PXNote]
		public virtual Guid? NoteID { get; set; }

		// Token: 0x0200001F RID: 31
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class customer : BqlType<IBqlInt, int>.Field<CustomerPackaging.customer>
		{
		}

		// Token: 0x02000020 RID: 32
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class useCustomersPackagingOption : BqlType<IBqlBool, bool>.Field<CustomerPackaging.useCustomersPackagingOption>
		{
		}

		// Token: 0x02000021 RID: 33
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

		// Token: 0x02000022 RID: 34
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class packSeparately : BqlType<IBqlBool, bool>.Field<CustomerPackaging.packSeparately>
		{
		}

		// Token: 0x02000023 RID: 35
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class useOnlyCustomerBoxes : BqlType<IBqlBool, bool>.Field<CustomerPackaging.useOnlyCustomerBoxes>
		{
		}

		// Token: 0x02000024 RID: 36
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerPackaging.createdDateTime>
		{
		}

		// Token: 0x02000025 RID: 37
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class createdByID : BqlType<IBqlGuid, Guid>.Field<CustomerPackaging.createdByID>
		{
		}

		// Token: 0x02000026 RID: 38
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

		// Token: 0x02000027 RID: 39
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedDateTime : BqlType<IBqlDateTime, DateTime>.Field<CustomerPackaging.lastModifiedDateTime>
		{
		}

		// Token: 0x02000028 RID: 40
		[Nullable(new byte[]
		{
			0,
			1,
			0
		})]
		public abstract class lastModifiedByID : BqlType<IBqlGuid, Guid>.Field<CustomerPackaging.lastModifiedByID>
		{
		}

		// Token: 0x02000029 RID: 41
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

		// Token: 0x0200002A RID: 42
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

		// Token: 0x0200002B RID: 43
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
