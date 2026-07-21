using System;
using System.Collections.Generic;
using ASCiStarKohls.DataProvider.Interface;
using PX.Data;
using PX.Objects.SO;

namespace ASCiStarKohls.DataProvider
{
	// Token: 0x02000008 RID: 8
	public class SODataProvider : ISODataProvider
	{
		// Token: 0x0600001E RID: 30 RVA: 0x0000278D File Offset: 0x0000098D
		public SODataProvider(PXGraph graph)
		{
			this._graph = graph;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000027A0 File Offset: 0x000009A0
		public SOLine GetSOLine(string orderType, string orderNbr, int? lineNbr)
		{
			return PXSelectBase<SOLine, PXSelect<SOLine, Where<SOLine.orderType, Equal<Required<SOLine.orderType>>, And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>, And<SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>>.Config>.Select(this._graph, new object[]
			{
				orderType,
				orderNbr,
				lineNbr
			});
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000027DC File Offset: 0x000009DC
		public IEnumerable<SOLine> GetSOLines(string orderType, string orderNbr)
		{
			return PXSelectBase<SOLine, PXSelect<SOLine, Where<SOLine.orderType, Equal<Required<SOLine.orderType>>, And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>>>>.Config>.Select(this._graph, new object[]
			{
				orderType,
				orderNbr
			}).FirstTableItems;
		}

		// Token: 0x04000004 RID: 4
		private readonly PXGraph _graph;
	}
}
