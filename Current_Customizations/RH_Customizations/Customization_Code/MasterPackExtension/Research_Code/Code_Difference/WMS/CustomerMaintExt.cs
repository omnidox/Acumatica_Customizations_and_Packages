using System;
using System.Runtime.CompilerServices;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using WMS.AR.GraphExt;

namespace WMS
{
	// Token: 0x02000005 RID: 5
	public class CustomerMaintExt : PXGraphExtension<CustomerMaint>
	{
		// Token: 0x06000008 RID: 8 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000020A0 File Offset: 0x000002A0
		protected void _(Events.RowSelected<Customer> args)
		{
			bool flag = args == null;
			if (!flag)
			{
				bool flag2 = string.IsNullOrEmpty(base.Base.BAccount.Current.AcctCD);
				if (flag2)
				{
					this.CustomerBoxesDAC.AllowInsert = false;
					this.CustomerBoxesDAC.AllowUpdate = false;
					this.CustomerBoxesDAC.AllowDelete = false;
				}
				else
				{
					this.CustomerBoxesDAC.AllowInsert = true;
					this.CustomerBoxesDAC.AllowUpdate = true;
					this.CustomerBoxesDAC.AllowDelete = true;
				}
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000212C File Offset: 0x0000032C
		protected void _(Events.RowSelected<CustomerPackaging> e)
		{
			bool flag = e == null || e.Row == null;
			if (!flag)
			{
				PXUIFieldAttribute.SetReadOnly<CustomerPackaging.packSeparately>(e.Cache, this.CustomerPackagingView.Current, false);
				bool flag2 = e.Row.DefaultPackaging == "N";
				if (flag2)
				{
					PXUIFieldAttribute.SetReadOnly<CustomerPackaging.packSeparately>(e.Cache, this.CustomerPackagingView.Current, true);
					e.Row.PackSeparately = new bool?(false);
				}
				bool flag3 = e.Row.DefaultPackaging == "Q";
				if (flag3)
				{
					PXUIFieldAttribute.SetReadOnly<CustomerPackaging.packSeparately>(e.Cache, this.CustomerPackagingView.Current, true);
					e.Row.PackSeparately = new bool?(true);
				}
				bool flag4 = e.Row.DefaultPackaging == "V";
				if (flag4)
				{
					PXUIFieldAttribute.SetReadOnly<CustomerPackaging.packSeparately>(e.Cache, this.CustomerPackagingView.Current, true);
					e.Row.PackSeparately = new bool?(false);
				}
				bool valueOrDefault = e.Row.UseCustomersPackagingOption.GetValueOrDefault();
				if (valueOrDefault)
				{
					PXUIFieldAttribute.SetReadOnly<CustomerPackaging.defaultPackaging>(e.Cache, this.CustomerPackagingView.Current, false);
				}
				else
				{
					PXUIFieldAttribute.SetReadOnly<CustomerPackaging.defaultPackaging>(e.Cache, this.CustomerPackagingView.Current, true);
				}
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002287 File Offset: 0x00000487
		[PXDBDefault(typeof(BAccount.bAccountID))]
		[PXParent(typeof(Select<Customer, Where<Customer.bAccountID, Equal<Current<CustomerPackaging.customer>>>>))]
		[PXMergeAttributes(Method = 0)]
		public virtual void _(Events.CacheAttached<CustomerPackaging.customer> e)
		{
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000228C File Offset: 0x0000048C
		protected void _(Events.FieldUpdated<CustomerBoxesDAC, CustomerBoxesDAC.boxID> e)
		{
			bool flag = e.Row == null || e.Row.BoxID == null;
			if (!flag)
			{
				CommonSetup commonSetup = PXSelectBase<CommonSetup, PXViewOf<CommonSetup>.BasedOn<SelectFromBase<CommonSetup, TypeArrayOf<IFbqlJoin>.Empty>>.Config>.Select(base.Base, Array.Empty<object>()).TopFirst;
				CSBox matchingRecord = CSBox.PK.Find(base.Base, e.Row.BoxID, 0);
				e.Row.Description = matchingRecord.Description;
				e.Row.BoxWeight = matchingRecord.BoxWeight;
				e.Row.MaxWeight = matchingRecord.MaxWeight;
				e.Row.MaxVolume = matchingRecord.MaxVolume;
				CustomerBoxesDAC row = e.Row;
				decimal? num = matchingRecord.Length;
				row.Length = ((num != null) ? new int?((int)num.GetValueOrDefault()) : null);
				CustomerBoxesDAC row2 = e.Row;
				num = matchingRecord.Width;
				row2.Width = ((num != null) ? new int?((int)num.GetValueOrDefault()) : null);
				CustomerBoxesDAC row3 = e.Row;
				num = matchingRecord.Height;
				row3.Height = ((num != null) ? new int?((int)num.GetValueOrDefault()) : null);
				bool flag2 = commonSetup != null;
				if (flag2)
				{
					e.Row.WeightOUM = commonSetup.WeightUOM;
					e.Row.VolumeUOM = commonSetup.VolumeUOM;
					e.Row.LinearUOM = commonSetup.LinearUOM;
				}
			}
		}

		// Token: 0x04000003 RID: 3
		[Nullable(new byte[]
		{
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
			1,
			0
		})]
		public FbqlSelect<SelectFromBase<CustomerBoxesDAC, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CustomerBoxesDAC.customerID, IBqlInt>.IsEqual<BqlField<Customer.bAccountID, IBqlInt>.FromCurrent>>, CustomerBoxesDAC>.View CustomerBoxesDAC;

		// Token: 0x04000004 RID: 4
		[Nullable(new byte[]
		{
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
			1,
			0
		})]
		public FbqlSelect<SelectFromBase<CustomerPackaging, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CustomerPackaging.customer, IBqlInt>.IsEqual<BqlField<Customer.bAccountID, IBqlInt>.FromCurrent>>, CustomerPackaging>.View CustomerPackagingView;
	}
}
