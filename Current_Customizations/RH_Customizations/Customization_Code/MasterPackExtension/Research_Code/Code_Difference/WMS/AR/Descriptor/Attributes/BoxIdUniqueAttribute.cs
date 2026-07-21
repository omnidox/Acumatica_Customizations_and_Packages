using System;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using WMS.AR.GraphExt;

namespace WMS.AR.Descriptor.Attributes
{
	// Token: 0x0200001F RID: 31
	public class BoxIdUniqueAttribute : PXEventSubscriberAttribute, IPXFieldUpdatedSubscriber
	{
		// Token: 0x06000148 RID: 328 RVA: 0x00009914 File Offset: 0x00007B14
		public void FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			CustomerBoxesDAC box = e.Row as CustomerBoxesDAC;
			bool flag = box != null;
			if (flag)
			{
				this.ValidateBox(cache, box);
			}
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00009944 File Offset: 0x00007B44
		private void ValidateBox(PXCache cache, CustomerBoxesDAC box)
		{
			bool flag = !string.IsNullOrWhiteSpace(box.BoxID) && this.IsBoxExist(cache.Graph, box);
			if (flag)
			{
				cache.RaiseExceptionHandling<CustomerBoxesDAC.boxID>(box, null, new PXSetPropertyException("Already Exist", 4));
			}
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000998C File Offset: 0x00007B8C
		private bool IsBoxExist(PXGraph graph, CustomerBoxesDAC box)
		{
			return PXSelectBase<CustomerBoxesDAC, PXViewOf<CustomerBoxesDAC>.BasedOn<SelectFromBase<CustomerBoxesDAC, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CustomerBoxesDAC.boxID, IBqlString>.IsEqual<P.AsString>>>.ReadOnly.Config>.Select(graph, new object[]
			{
				box.BoxID
			}).Any<PXResult<CustomerBoxesDAC>>();
		}
	}
}
