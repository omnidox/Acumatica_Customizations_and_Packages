using System;

namespace ASCJSMCustom.Common.DTO.Interfaces
{
	// Token: 0x0200003B RID: 59
	public interface IASCJSMItemCostSpecDTO
	{
		// Token: 0x1700012B RID: 299
		// (get) Token: 0x0600039B RID: 923
		// (set) Token: 0x0600039C RID: 924
		int? InventoryID { get; set; }

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x0600039D RID: 925
		// (set) Token: 0x0600039E RID: 926
		decimal? UsrActualGRAMGold { get; set; }

		// Token: 0x1700012D RID: 301
		// (get) Token: 0x0600039F RID: 927
		// (set) Token: 0x060003A0 RID: 928
		decimal? UsrPricingGRAMSilver { get; set; }

		// Token: 0x1700012E RID: 302
		// (get) Token: 0x060003A1 RID: 929
		// (set) Token: 0x060003A2 RID: 930
		decimal? UsrPricingGRAMGold { get; set; }

		// Token: 0x1700012F RID: 303
		// (get) Token: 0x060003A3 RID: 931
		// (set) Token: 0x060003A4 RID: 932
		decimal? UsrActualGRAMSilver { get; set; }

		// Token: 0x17000130 RID: 304
		// (get) Token: 0x060003A5 RID: 933
		// (set) Token: 0x060003A6 RID: 934
		decimal? UsrContractLossPct { get; set; }

		// Token: 0x17000131 RID: 305
		// (get) Token: 0x060003A7 RID: 935
		// (set) Token: 0x060003A8 RID: 936
		decimal? UsrContractSurcharge { get; set; }

		// Token: 0x17000132 RID: 306
		// (get) Token: 0x060003A9 RID: 937
		// (set) Token: 0x060003AA RID: 938
		decimal? UsrContractIncrement { get; set; }

		// Token: 0x17000133 RID: 307
		// (get) Token: 0x060003AB RID: 939
		// (set) Token: 0x060003AC RID: 940
		decimal? UsrMatrixStep { get; set; }

		// Token: 0x17000134 RID: 308
		// (get) Token: 0x060003AD RID: 941
		// (set) Token: 0x060003AE RID: 942
		decimal? UsrUnitCost { get; set; }

		// Token: 0x17000135 RID: 309
		// (get) Token: 0x060003AF RID: 943
		// (set) Token: 0x060003B0 RID: 944
		decimal? UsrBasisValue { get; set; }

		// Token: 0x17000136 RID: 310
		// (get) Token: 0x060003B1 RID: 945
		// (set) Token: 0x060003B2 RID: 946
		decimal? UsrEstLandedCost { get; set; }

		// Token: 0x17000137 RID: 311
		// (get) Token: 0x060003B3 RID: 947
		// (set) Token: 0x060003B4 RID: 948
		decimal? UsrPreciousMetalCost { get; set; }

		// Token: 0x17000138 RID: 312
		// (get) Token: 0x060003B5 RID: 949
		// (set) Token: 0x060003B6 RID: 950
		decimal? UsrFabricationCost { get; set; }

		// Token: 0x17000139 RID: 313
		// (get) Token: 0x060003B7 RID: 951
		// (set) Token: 0x060003B8 RID: 952
		decimal? UsrPackagingCost { get; set; }

		// Token: 0x1700013A RID: 314
		// (get) Token: 0x060003B9 RID: 953
		// (set) Token: 0x060003BA RID: 954
		decimal? UsrPackagingLaborCost { get; set; }

		// Token: 0x1700013B RID: 315
		// (get) Token: 0x060003BB RID: 955
		// (set) Token: 0x060003BC RID: 956
		decimal? UsrOtherMaterialsCost { get; set; }

		// Token: 0x1700013C RID: 316
		// (get) Token: 0x060003BD RID: 957
		// (set) Token: 0x060003BE RID: 958
		decimal? UsrOtherCost { get; set; }

		// Token: 0x1700013D RID: 317
		// (get) Token: 0x060003BF RID: 959
		// (set) Token: 0x060003C0 RID: 960
		decimal? UsrFreightCost { get; set; }

		// Token: 0x1700013E RID: 318
		// (get) Token: 0x060003C1 RID: 961
		// (set) Token: 0x060003C2 RID: 962
		decimal? UsrHandlingCost { get; set; }

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x060003C3 RID: 963
		// (set) Token: 0x060003C4 RID: 964
		decimal? UsrLaborCost { get; set; }

		// Token: 0x17000140 RID: 320
		// (get) Token: 0x060003C5 RID: 965
		// (set) Token: 0x060003C6 RID: 966
		decimal? UsrDutyCost { get; set; }

		// Token: 0x17000141 RID: 321
		// (get) Token: 0x060003C7 RID: 967
		// (set) Token: 0x060003C8 RID: 968
		decimal? UsrDutyCostPct { get; set; }

		// Token: 0x17000142 RID: 322
		// (get) Token: 0x060003C9 RID: 969
		// (set) Token: 0x060003CA RID: 970
		string UsrCostingType { get; set; }

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x060003CB RID: 971
		// (set) Token: 0x060003CC RID: 972
		string UsrCommodityType { get; set; }
	}
}
