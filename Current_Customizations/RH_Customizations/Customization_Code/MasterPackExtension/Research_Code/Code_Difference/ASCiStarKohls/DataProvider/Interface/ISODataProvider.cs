using System;
using System.Collections.Generic;
using PX.Objects.SO;

namespace ASCiStarKohls.DataProvider.Interface
{
	// Token: 0x02000009 RID: 9
	public interface ISODataProvider
	{
		// Token: 0x06000021 RID: 33
		IEnumerable<SOLine> GetSOLines(string orderType, string orderNbr);

		// Token: 0x06000022 RID: 34
		SOLine GetSOLine(string orderType, string orderNbr, int? lineNbr);
	}
}
