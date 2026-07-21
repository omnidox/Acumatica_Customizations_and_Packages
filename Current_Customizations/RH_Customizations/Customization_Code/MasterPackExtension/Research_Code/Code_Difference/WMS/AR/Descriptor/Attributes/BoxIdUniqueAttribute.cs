using System;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using WMS.AR.GraphExt;

namespace WMS.AR.Descriptor.Attributes
{
	// Token: 0x0200001D RID: 29
	public class BoxIdUniqueAttribute : PXEventSubscriberAttribute, IPXFieldUpdatedSubscriber
	{
		// Token: 0x06000131 RID: 305 RVA: 0x00008404 File Offset: 0x00006604
		public void FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			CustomerBoxesDAC box = e.Row as CustomerBoxesDAC;
			bool flag = box != null;
			if (flag)
			{
				this.ValidateBox(cache, box);
			}
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00008434 File Offset: 0x00006634
		private void ValidateBox(PXCache cache, CustomerBoxesDAC box)
		{
			bool flag = !string.IsNullOrWhiteSpace(box.BoxID) && this.IsBoxExist(cache.Graph, box);
			if (flag)
			{
				cache.RaiseExceptionHandling<CustomerBoxesDAC.boxID>(box, null, new PXSetPropertyException("Already Exist", 4));
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x0000847C File Offset: 0x0000667C
		private bool IsBoxExist(PXGraph graph, CustomerBoxesDAC box)
		{
			return PXSelectBase<CustomerBoxesDAC, PXViewOf<CustomerBoxesDAC>.BasedOn<SelectFromBase<CustomerBoxesDAC, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CustomerBoxesDAC.boxID, IBqlString>.IsEqual<P.AsString>>>.ReadOnly.Config>.Select(graph, new object[]
			{
				box.BoxID
			}).Any<PXResult<CustomerBoxesDAC>>();
		}
	}
}
