using System;
using ASCJSMCustom.Common.Descriptor;
using ASCJSMCustom.Common.DTO.Interfaces;
using ASCJSMCustom.Common.Helper;
using ASCJSMCustom.IN.CacheExt;
using ASCJSMCustom.IN.DAC;
using ASCJSMCustom.PO.CacheExt;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.IN;
using PX.Objects.PO;

namespace ASCJSMCustom.Common.Builder
{
	// Token: 0x02000041 RID: 65
	public class ASCJSMCostBuilder
	{
		// Token: 0x1700014B RID: 331
		// (get) Token: 0x060003DF RID: 991 RVA: 0x0000F024 File Offset: 0x0000D224
		// (set) Token: 0x060003E0 RID: 992 RVA: 0x0000F02C File Offset: 0x0000D22C
		private string Currency { get; set; } = "USD";

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x060003E1 RID: 993 RVA: 0x0000F035 File Offset: 0x0000D235
		// (set) Token: 0x060003E2 RID: 994 RVA: 0x0000F03D File Offset: 0x0000D23D
		private bool IsEnabledOverrideVendor { get; set; }

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x060003E3 RID: 995 RVA: 0x0000F046 File Offset: 0x0000D246
		// (set) Token: 0x060003E4 RID: 996 RVA: 0x0000F04E File Offset: 0x0000D24E
		private InventoryItem PreciousMetalItem { get; set; }

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x060003E5 RID: 997 RVA: 0x0000F057 File Offset: 0x0000D257
		// (set) Token: 0x060003E6 RID: 998 RVA: 0x0000F05F File Offset: 0x0000D25F
		public ASCJSMINJewelryItem INJewelryItem { get; set; }

		// Token: 0x1700014F RID: 335
		// (get) Token: 0x060003E7 RID: 999 RVA: 0x0000F068 File Offset: 0x0000D268
		// (set) Token: 0x060003E8 RID: 1000 RVA: 0x0000F070 File Offset: 0x0000D270
		public IASCJSMItemCostSpecDTO ItemCostSpecification { get; set; }

		// Token: 0x17000150 RID: 336
		// (get) Token: 0x060003E9 RID: 1001 RVA: 0x0000F079 File Offset: 0x0000D279
		// (set) Token: 0x060003EA RID: 1002 RVA: 0x0000F081 File Offset: 0x0000D281
		public POVendorInventory POVendorInventory { get; set; }

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x060003EB RID: 1003 RVA: 0x0000F08A File Offset: 0x0000D28A
		// (set) Token: 0x060003EC RID: 1004 RVA: 0x0000F092 File Offset: 0x0000D292
		public ASCJSMPOVendorInventoryExt POVendorInventoryExt { get; set; }

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x060003ED RID: 1005 RVA: 0x0000F09B File Offset: 0x0000D29B
		// (set) Token: 0x060003EE RID: 1006 RVA: 0x0000F0A3 File Offset: 0x0000D2A3
		public DateTime PricingDate { get; set; } = PXTimeZoneInfo.Today;

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x060003EF RID: 1007 RVA: 0x0000F0AC File Offset: 0x0000D2AC
		// (set) Token: 0x060003F0 RID: 1008 RVA: 0x0000F0B4 File Offset: 0x0000D2B4
		public decimal? PreciousMetalContractCostPerTOZ { get; private set; }

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x060003F1 RID: 1009 RVA: 0x0000F0BD File Offset: 0x0000D2BD
		// (set) Token: 0x060003F2 RID: 1010 RVA: 0x0000F0C5 File Offset: 0x0000D2C5
		public decimal? PreciousMetalMarketCostPerTOZ { get; private set; }

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x060003F3 RID: 1011 RVA: 0x0000F0CE File Offset: 0x0000D2CE
		// (set) Token: 0x060003F4 RID: 1012 RVA: 0x0000F0D6 File Offset: 0x0000D2D6
		public decimal? PreciousMetalAvrSilverMarketCostPerTOZ { get; set; }

		// Token: 0x17000156 RID: 342
		// (get) Token: 0x060003F5 RID: 1013 RVA: 0x0000F0DF File Offset: 0x0000D2DF
		// (set) Token: 0x060003F6 RID: 1014 RVA: 0x0000F0E7 File Offset: 0x0000D2E7
		public decimal? PreciousMetalContractCostPerGram { get; private set; }

		// Token: 0x17000157 RID: 343
		// (get) Token: 0x060003F7 RID: 1015 RVA: 0x0000F0F0 File Offset: 0x0000D2F0
		// (set) Token: 0x060003F8 RID: 1016 RVA: 0x0000F0F8 File Offset: 0x0000D2F8
		public decimal? PreciousMetalMarketCostPerGram { get; private set; }

		// Token: 0x17000158 RID: 344
		// (get) Token: 0x060003F9 RID: 1017 RVA: 0x0000F101 File Offset: 0x0000D301
		// (set) Token: 0x060003FA RID: 1018 RVA: 0x0000F109 File Offset: 0x0000D309
		public decimal? PreciousMetalUnitCost { get; private set; }

		// Token: 0x17000159 RID: 345
		// (get) Token: 0x060003FB RID: 1019 RVA: 0x0000F112 File Offset: 0x0000D312
		// (set) Token: 0x060003FC RID: 1020 RVA: 0x0000F11A File Offset: 0x0000D31A
		public decimal? AvrPreciousMetalUnitCost { get; private set; }

		// Token: 0x1700015A RID: 346
		// (get) Token: 0x060003FD RID: 1021 RVA: 0x0000F123 File Offset: 0x0000D323
		// (set) Token: 0x060003FE RID: 1022 RVA: 0x0000F12B File Offset: 0x0000D32B
		public decimal? Floor { get; private set; }

		// Token: 0x1700015B RID: 347
		// (get) Token: 0x060003FF RID: 1023 RVA: 0x0000F134 File Offset: 0x0000D334
		// (set) Token: 0x06000400 RID: 1024 RVA: 0x0000F13C File Offset: 0x0000D33C
		public decimal? Ceiling { get; private set; }

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x06000401 RID: 1025 RVA: 0x0000F145 File Offset: 0x0000D345
		// (set) Token: 0x06000402 RID: 1026 RVA: 0x0000F14D File Offset: 0x0000D34D
		public decimal? BasisValue { get; private set; } = new decimal?(0m);

		// Token: 0x1700015D RID: 349
		// (get) Token: 0x06000403 RID: 1027 RVA: 0x0000F156 File Offset: 0x0000D356
		// (set) Token: 0x06000404 RID: 1028 RVA: 0x0000F15E File Offset: 0x0000D35E
		public APVendorPrice APVendorPriceContract { get; set; }

		// Token: 0x06000405 RID: 1029 RVA: 0x0000F167 File Offset: 0x0000D367
		public ASCJSMCostBuilder(PXGraph graph)
		{
			this._graph = graph;
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0000F1A0 File Offset: 0x0000D3A0
		public ASCJSMCostBuilder WithInventoryItem(IASCJSMItemCostSpecDTO inventory)
		{
			this.ItemCostSpecification = inventory;
			return this;
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0000F1BC File Offset: 0x0000D3BC
		public ASCJSMCostBuilder WithJewelryAttrData(ASCJSMINJewelryItem jewelryItem = null)
		{
			bool flag = jewelryItem == null;
			if (flag)
			{
				this.INJewelryItem = this.GetASCIStarINJewelryItem(this.ItemCostSpecification.InventoryID);
			}
			else
			{
				this.INJewelryItem = jewelryItem;
			}
			return this;
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x0000F1FC File Offset: 0x0000D3FC
		public ASCJSMCostBuilder WithPOVendorInventory(POVendorInventory vendorInventory)
		{
			this.POVendorInventory = vendorInventory;
			this.POVendorInventoryExt = PXCache<POVendorInventory>.GetExtension<ASCJSMPOVendorInventoryExt>(vendorInventory);
			this.IsEnabledOverrideVendor = this.POVendorInventoryExt.UsrIsOverrideVendor.GetValueOrDefault();
			return this;
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x0000F240 File Offset: 0x0000D440
		public ASCJSMCostBuilder WithCurrency(string currency)
		{
			this.Currency = currency;
			return this;
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0000F25C File Offset: 0x0000D45C
		public ASCJSMCostBuilder WithPricingData(DateTime pricingData)
		{
			this.PricingDate = pricingData;
			return this;
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0000F278 File Offset: 0x0000D478
		public virtual ASCJSMCostBuilder Build()
		{
			bool flag = this.INJewelryItem == null || this.INJewelryItem.MetalType == null;
			if (flag)
			{
				this.INJewelryItem = this.GetASCIStarINJewelryItem(this.ItemCostSpecification.InventoryID);
				bool flag2 = this.INJewelryItem == null || this.INJewelryItem.MetalType == null;
				if (flag2)
				{
					return null;
				}
			}
			bool flag3 = ASCJSMMetalType.IsGold(this.INJewelryItem.MetalType);
			if (flag3)
			{
				this.PreciousMetalItem = ASCJSMMetalType.GetInventoryItemByInvenctoryCD(this._graph, "24K");
			}
			else
			{
				bool flag4 = ASCJSMMetalType.IsSilver(this.INJewelryItem.MetalType);
				if (flag4)
				{
					this.PreciousMetalItem = ASCJSMMetalType.GetInventoryItemByInvenctoryCD(this._graph, "SSS");
				}
			}
			bool flag5 = this.PreciousMetalItem == null;
			ASCJSMCostBuilder result;
			if (flag5)
			{
				result = null;
			}
			else
			{
				this.PreciousMetalContractCostPerTOZ = (this.IsEnabledOverrideVendor ? this.POVendorInventoryExt.UsrCommodityVendorPrice : this.GetVendorPricePerTOZ(this.POVendorInventory.VendorID, this.PreciousMetalItem.InventoryID, true));
				this.PreciousMetalMarketCostPerTOZ = this.GetVendorPricePerTOZ(this.POVendorInventoryExt.UsrMarketID, this.PreciousMetalItem.InventoryID, false);
				bool flag6 = ASCJSMMetalType.IsGold(this.INJewelryItem.MetalType);
				if (flag6)
				{
					this.PreciousMetalContractCostPerGram = this.PreciousMetalContractCostPerTOZ * ASCJSMMetalType.GetMultFactorConvertTOZtoGram("24K");
					this.PreciousMetalMarketCostPerGram = this.PreciousMetalMarketCostPerTOZ * ASCJSMMetalType.GetMultFactorConvertTOZtoGram("24K");
					this.BasisValue = this.PreciousMetalContractCostPerTOZ;
				}
				else
				{
					bool flag7 = ASCJSMMetalType.IsSilver(this.INJewelryItem.MetalType);
					if (flag7)
					{
						this.PreciousMetalContractCostPerGram = this.PreciousMetalContractCostPerTOZ * ASCJSMMetalType.GetMultFactorConvertTOZtoGram("SSS");
						this.PreciousMetalMarketCostPerGram = this.PreciousMetalMarketCostPerTOZ * ASCJSMMetalType.GetMultFactorConvertTOZtoGram("SSS");
						this.BasisValue = (this.PreciousMetalContractCostPerTOZ + (this.PreciousMetalContractCostPerTOZ + this.ItemCostSpecification.UsrMatrixStep)) / 2;
					}
				}
				this.AvrPreciousMetalUnitCost = this.GetPresiousMetalAvrCost();
				result = this;
			}
			return result;
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x0000F5D8 File Offset: 0x0000D7D8
		public virtual decimal? CalculatePreciousMetalCost(string costingType = null)
		{
			decimal? num = new decimal?(0m);
			decimal? num2 = new decimal?(0m);
			decimal? num3 = new decimal?(this.ItemCostSpecification.UsrContractSurcharge.GetValueOrDefault(0.0m) / 100.0m);
			decimal? num4 = new decimal?(this.POVendorInventoryExt.UsrContractSurchargeAmount.GetValueOrDefault(0.0m));
			decimal? num5 = new decimal?(this.ItemCostSpecification.UsrContractLossPct.GetValueOrDefault(0.0m) / 100.0m);
			ASCJSMINJewelryItem injewelryItem = this.INJewelryItem;
			bool flag = injewelryItem == null || injewelryItem.InventoryID == null;
			decimal? result;
			if (flag)
			{
				result = null;
			}
			else
			{
				InventoryItem inventoryItem = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(this._graph, new object[]
				{
					injewelryItem.InventoryID
				});
				bool flag2 = inventoryItem == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					ASCJSMINInventoryItemExt extension = inventoryItem.GetExtension<ASCJSMINInventoryItemExt>();
					ASCJSMINJewelryItem injewelryItem2 = this.INJewelryItem;
					bool flag3 = ASCJSMMetalType.IsGold((injewelryItem2 != null) ? injewelryItem2.MetalType : null);
					if (flag3)
					{
						num2 = new decimal?(extension.UsrPricingGRAMGold.GetValueOrDefault(0.0m));
						string text = costingType ?? this.ItemCostSpecification.UsrCostingType;
						string a = text;
						if (!(a == "C"))
						{
							if (!(a == "M"))
							{
								if (a == "S")
								{
									return this.AvrPreciousMetalUnitCost - this.ItemCostSpecification.UsrFabricationCost - this.ItemCostSpecification.UsrOtherMaterialsCost - this.ItemCostSpecification.UsrPackagingCost;
								}
							}
							else
							{
								num = this.PreciousMetalMarketCostPerTOZ;
							}
						}
						else
						{
							num = this.PreciousMetalContractCostPerTOZ;
						}
					}
					else
					{
						ASCJSMINJewelryItem injewelryItem3 = this.INJewelryItem;
						bool flag4 = ASCJSMMetalType.IsSilver((injewelryItem3 != null) ? injewelryItem3.MetalType : null);
						if (flag4)
						{
							num2 = new decimal?(extension.UsrPricingGRAMSilver.GetValueOrDefault(0.0m));
							string text2 = costingType ?? this.ItemCostSpecification.UsrCostingType;
							string a2 = text2;
							if (!(a2 == "C"))
							{
								if (!(a2 == "M"))
								{
									if (a2 == "S")
									{
										return this.AvrPreciousMetalUnitCost - this.ItemCostSpecification.UsrFabricationCost - this.ItemCostSpecification.UsrOtherMaterialsCost - this.ItemCostSpecification.UsrPackagingCost;
									}
								}
								else
								{
									this.PreciousMetalAvrSilverMarketCostPerTOZ = this.GetSilverMetalCostPerOZ(this.PreciousMetalContractCostPerTOZ, this.PreciousMetalMarketCostPerTOZ, this.ItemCostSpecification.UsrMatrixStep);
									num = this.PreciousMetalAvrSilverMarketCostPerTOZ;
								}
							}
							else
							{
								this.PreciousMetalAvrSilverMarketCostPerTOZ = this.GetSilverMetalCostPerOZ(this.PreciousMetalContractCostPerTOZ, this.PreciousMetalContractCostPerTOZ, this.ItemCostSpecification.UsrMatrixStep);
								num = this.PreciousMetalAvrSilverMarketCostPerTOZ;
							}
						}
					}
					decimal d = 0.032150743m;
					decimal? num6 = (num + num4) * (1 + num3);
					ASCJSMINJewelryItem injewelryItem4 = this.INJewelryItem;
					bool flag5 = ASCJSMMetalType.IsGold((injewelryItem4 != null) ? injewelryItem4.MetalType : null) && extension.UsrEnableVendorIncrement.GetValueOrDefault();
					if (flag5)
					{
						this.PreciousMetalUnitCost = num6 * (1 + num5) * extension.UsrContractIncrement.GetValueOrDefault();
					}
					else
					{
						this.PreciousMetalUnitCost = num6 * d * num2 * (1 + num5);
					}
					result = this.PreciousMetalUnitCost;
				}
			}
			return result;
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x0000FCF0 File Offset: 0x0000DEF0
		public virtual decimal? GetVendorPricePerTOZ(int? vendorID, int? inventoryID, bool isContract = false)
		{
			APVendorPrice apvendorPrice = ASCJSMCostBuilder.GetAPVendorPrice(this._graph, vendorID, inventoryID, ASCJSMConstants.TOZ.value, this.PricingDate);
			bool flag = apvendorPrice == null;
			decimal? result;
			if (flag)
			{
				result = new decimal?(0.0m);
			}
			else
			{
				if (isContract)
				{
					this.APVendorPriceContract = apvendorPrice;
				}
				result = apvendorPrice.SalesPrice;
			}
			return result;
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x0000FD4C File Offset: 0x0000DF4C
		public virtual decimal? CalculateIncrementValue(IASCJSMItemCostSpecDTO itemCostSpecification)
		{
			ASCJSMINJewelryItem injewelryItem = this.INJewelryItem;
			decimal multFactorConvertTOZtoGram = ASCJSMMetalType.GetMultFactorConvertTOZtoGram((injewelryItem != null) ? injewelryItem.MetalType : null);
			decimal? result = new decimal?(multFactorConvertTOZtoGram * (1.0m + itemCostSpecification.UsrContractSurcharge.GetValueOrDefault(0.0m) / 100.0m));
			return result;
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x0000FDC0 File Offset: 0x0000DFC0
		public static decimal? CalculateSurchargeValue(decimal? increment, string metalType)
		{
			decimal multFactorConvertTOZtoGram = ASCJSMMetalType.GetMultFactorConvertTOZtoGram(metalType);
			decimal? num = increment;
			decimal d = multFactorConvertTOZtoGram;
			return (num != null) ? new decimal?((num.GetValueOrDefault() / d - 1.0m) * 100.0m) : null;
		}

		// Token: 0x06000410 RID: 1040 RVA: 0x0000FE2C File Offset: 0x0000E02C
		public static decimal? CalculateDutyCost(IASCJSMItemCostSpecDTO costSpecDTO, decimal? newValue)
		{
			decimal? usrDutyCostPct = costSpecDTO.UsrDutyCostPct;
			decimal? num = newValue;
			return (usrDutyCostPct != null & num != null) ? new decimal?((usrDutyCostPct.GetValueOrDefault() + num.GetValueOrDefault()) / 100m) : null;
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x0000FE88 File Offset: 0x0000E088
		public static decimal? CalculateDutyCostPct(IASCJSMItemCostSpecDTO costSpecDTO, decimal? newValue)
		{
			decimal value = 0m;
			decimal? usrUnitCost = costSpecDTO.UsrUnitCost;
			decimal d = 0m;
			bool flag = !(usrUnitCost.GetValueOrDefault() == d & usrUnitCost != null);
			if (flag)
			{
				value = (newValue / costSpecDTO.UsrUnitCost).Value * 100m;
			}
			return new decimal?(value);
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x0000FF2C File Offset: 0x0000E12C
		public decimal? GetSilverMetalCostPerOZ(decimal? basisCost, decimal? marketCost, decimal? matrixStep)
		{
			bool flag;
			if (basisCost != null)
			{
				decimal? num = basisCost;
				decimal num2 = 0.0m;
				if (!(num.GetValueOrDefault() == num2 & num != null) && marketCost != null)
				{
					num = marketCost;
					num2 = 0.0m;
					flag = (num.GetValueOrDefault() == num2 & num != null);
					goto IL_60;
				}
			}
			flag = true;
			IL_60:
			bool flag2 = flag;
			decimal? result;
			if (flag2)
			{
				result = new decimal?(0.0m);
			}
			else
			{
				decimal? num = matrixStep;
				decimal num2 = 0.0m;
				bool flag3 = (num.GetValueOrDefault() <= num2 & num != null) || matrixStep == null;
				if (flag3)
				{
					this.Floor = marketCost;
					this.Ceiling = marketCost;
					result = marketCost;
				}
				else
				{
					decimal num3 = Math.Truncate((marketCost / matrixStep - basisCost / matrixStep).GetValueOrDefault(0.0m));
					num2 = num3;
					this.Floor = (num2 + basisCost / matrixStep) * matrixStep;
					num2 = 1m + num3;
					this.Ceiling = (num2 + basisCost / matrixStep) * matrixStep;
					decimal? floor = this.Floor;
					num = this.Ceiling;
					this.PreciousMetalAvrSilverMarketCostPerTOZ = ((floor != null & num != null) ? new decimal?((floor.GetValueOrDefault() + num.GetValueOrDefault()) / 2.000000m) : null);
					result = this.PreciousMetalAvrSilverMarketCostPerTOZ;
				}
			}
			return result;
		}

		// Token: 0x06000413 RID: 1043 RVA: 0x00010290 File Offset: 0x0000E490
		private decimal? GetPresiousMetalAvrCost()
		{
			INItemCost initemCost = INItemCost.PK.Find(this._graph, this.ItemCostSpecification.InventoryID, this.Currency, PKFindOptions.None);
			bool flag;
			if (initemCost != null)
			{
				decimal? qtyOnHand = initemCost.QtyOnHand;
				decimal d = 0.0m;
				flag = (qtyOnHand.GetValueOrDefault() == d & qtyOnHand != null);
			}
			else
			{
				flag = true;
			}
			bool flag2 = flag;
			decimal? result;
			if (flag2)
			{
				result = new decimal?(0.0m);
			}
			else
			{
				result = initemCost.TotalCost / initemCost.QtyOnHand;
			}
			return result;
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x0001034C File Offset: 0x0000E54C
		public decimal? GetPurchaseUnitCost(string costingType)
		{
			decimal? preciousMetalMarketCostPerTOZ = this.PreciousMetalMarketCostPerTOZ;
			decimal d = 0m;
			bool flag = preciousMetalMarketCostPerTOZ.GetValueOrDefault() == d & preciousMetalMarketCostPerTOZ != null;
			decimal? result;
			if (flag)
			{
				result = new decimal?(0m);
			}
			else
			{
				this.ItemCostSpecification.UsrPreciousMetalCost = this.CalculatePreciousMetalCost(costingType);
				result = new decimal?(this.ItemCostSpecification.UsrPreciousMetalCost.GetValueOrDefault() + this.ItemCostSpecification.UsrOtherMaterialsCost.GetValueOrDefault() + this.ItemCostSpecification.UsrFabricationCost.GetValueOrDefault() + this.ItemCostSpecification.UsrPackagingCost.GetValueOrDefault() + this.ItemCostSpecification.UsrPackagingLaborCost.GetValueOrDefault());
			}
			return result;
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x00010421 File Offset: 0x0000E621
		private ASCJSMINJewelryItem GetASCIStarINJewelryItem(int? inventoryID)
		{
			return PXSelectBase<ASCJSMINJewelryItem, PXSelect<ASCJSMINJewelryItem, Where<ASCJSMINJewelryItem.inventoryID, Equal<Required<ASCJSMINJewelryItem.inventoryID>>>>.Config>.Select(this._graph, new object[]
			{
				inventoryID
			});
		}

		// Token: 0x06000416 RID: 1046 RVA: 0x00010444 File Offset: 0x0000E644
		public static APVendorPrice GetAPVendorPrice(PXGraph graph, int? vendorID, int? inventoryID, string UOM, DateTime effectiveDate)
		{
			PXResultset<APVendorPrice> pxresultset = PXSelectBase<APVendorPrice, PXViewOf<APVendorPrice>.BasedOn<SelectFromBase<APVendorPrice, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<APVendorPrice.vendorID, Equal<P.AsInt>>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<APVendorPrice.inventoryID, Equal<P.AsInt>>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<APVendorPrice.uOM, Equal<P.AsString>>>>>.And<Brackets<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<APVendorPrice.effectiveDate, LessEqual<P.AsDateTime>>>>>.And<Brackets<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<APVendorPrice.expirationDate, GreaterEqual<P.AsDateTime>>>>>.Or<BqlOperand<APVendorPrice.expirationDate, IBqlDateTime>.IsNull>>>>>>>>.Order<By<BqlField<APVendorPrice.effectiveDate, IBqlDateTime>.Desc>>>.Config>.Select(graph, new object[]
			{
				vendorID,
				inventoryID,
				UOM,
				effectiveDate,
				effectiveDate
			});
			return (pxresultset != null) ? pxresultset.TopFirst : null;
		}

		// Token: 0x04000178 RID: 376
		private PXGraph _graph;
	}
}
