using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace WMS
{
	// Token: 0x0200000B RID: 11
	public class PackModeLogicExt : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.Logic>
	{
		// Token: 0x0600003A RID: 58 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000026C8 File Offset: 0x000008C8
		public static string GetStyleKey(string orderNbr, int? inventoryID)
		{
			return string.Format("{0}|{1}", ((orderNbr != null) ? orderNbr.Trim().ToUpperInvariant() : null) ?? string.Empty, inventoryID.GetValueOrDefault());
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000026FC File Offset: 0x000008FC
		private int? GetCurrentPackageLineNbr()
		{
			PickPackShip.PackMode.Logic @base = base.Base2;
			int? num = (@base != null) ? @base.PackageLineNbrUI : null;
			int? result;
			if (num == null)
			{
				PickPackShip.PackMode.Logic base2 = base.Base2;
				int? num2 = (base2 != null) ? base2.PackageLineNbr : null;
				if (num2 == null)
				{
					PickPackShip.PackMode.Logic base3 = base.Base2;
					if (base3 == null)
					{
						result = null;
					}
					else
					{
						SOPackageDetailEx selectedPackage = base3.SelectedPackage;
						result = ((selectedPackage != null) ? selectedPackage.LineNbr : null);
					}
				}
				else
				{
					result = num2;
				}
			}
			else
			{
				result = num;
			}
			return result;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002788 File Offset: 0x00000988
		public Dictionary<string, decimal> GetExpectedStyleTotals(int? packageLineNbr = null)
		{
			PickPackShip basis = base.Basis;
			string a;
			if (basis == null)
			{
				a = null;
			}
			else
			{
				ScanHeader header = basis.Header;
				a = ((header != null) ? header.Mode : null);
			}
			bool flag = a != "PACK";
			Dictionary<string, decimal> result;
			if (flag)
			{
				result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			}
			else
			{
				bool flag2 = packageLineNbr == null;
				if (flag2)
				{
					packageLineNbr = this.GetCurrentPackageLineNbr();
				}
				bool flag3 = string.IsNullOrEmpty(base.Basis.RefNbr) || packageLineNbr == null;
				if (flag3)
				{
					result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
				}
				else
				{
					result = (from content in GraphHelper.RowCast<SelectedPackageContents>(PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
					{
						base.Basis.RefNbr,
						packageLineNbr
					}))
					group content by PackModeLogicExt.GetStyleKey(content.OrderNbr, content.InventoryID)).ToDictionary((IGrouping<string, SelectedPackageContents> group) => group.Key, (IGrouping<string, SelectedPackageContents> group) => group.Sum((SelectedPackageContents content) => content.PackedQty.GetValueOrDefault()), StringComparer.OrdinalIgnoreCase);
				}
			}
			return result;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000028BC File Offset: 0x00000ABC
		[return: TupleElementNames(new string[]
		{
			"LineNbr",
			"SplitLineNbr"
		})]
		public Dictionary<ValueTuple<int, int>, decimal> GetSelectedPackagePlanSplits(int? packageLineNbr = null)
		{
			PickPackShip basis = base.Basis;
			string a;
			if (basis == null)
			{
				a = null;
			}
			else
			{
				ScanHeader header = basis.Header;
				a = ((header != null) ? header.Mode : null);
			}
			bool flag = a != "PACK";
			Dictionary<ValueTuple<int, int>, decimal> result2;
			if (flag)
			{
				result2 = new Dictionary<ValueTuple<int, int>, decimal>();
			}
			else
			{
				bool flag2 = packageLineNbr == null;
				if (flag2)
				{
					packageLineNbr = this.GetCurrentPackageLineNbr();
				}
				bool flag3 = string.IsNullOrEmpty(base.Basis.RefNbr) || packageLineNbr == null;
				if (flag3)
				{
					result2 = new Dictionary<ValueTuple<int, int>, decimal>();
				}
				else
				{
					Dictionary<ValueTuple<int, int>, decimal> result = new Dictionary<ValueTuple<int, int>, decimal>();
					foreach (SelectedPackageContents content in GraphHelper.RowCast<SelectedPackageContents>(PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
					{
						base.Basis.RefNbr,
						packageLineNbr
					})))
					{
						bool flag4 = content.ShipmentLineNbr == null || content.ShipmentSplitLineNbr == null;
						if (!flag4)
						{
							ValueTuple<int, int> key = new ValueTuple<int, int>(content.ShipmentLineNbr.Value, content.ShipmentSplitLineNbr.Value);
							decimal qty = content.PackedQty.GetValueOrDefault();
							decimal current;
							result[key] = (result.TryGetValue(key, out current) ? (current + qty) : qty);
						}
					}
					result2 = result;
				}
			}
			return result2;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002A54 File Offset: 0x00000C54
		public decimal GetSelectedPackageEstimatedQtyForSplit(SOShipLineSplit split, int? packageLineNbr = null)
		{
			SOShipLineSplit split2 = split;
			bool flag = split2 == null || split2.LineNbr == null;
			decimal result;
			if (flag)
			{
				result = 0m;
			}
			else
			{
				PickPackShip basis = base.Basis;
				string a;
				if (basis == null)
				{
					a = null;
				}
				else
				{
					ScanHeader header = basis.Header;
					a = ((header != null) ? header.Mode : null);
				}
				bool flag2 = a != "PACK";
				if (flag2)
				{
					result = 0m;
				}
				else
				{
					bool flag3 = packageLineNbr == null;
					if (flag3)
					{
						packageLineNbr = this.GetCurrentPackageLineNbr();
					}
					bool flag4 = string.IsNullOrEmpty(base.Basis.RefNbr) || packageLineNbr == null;
					if (flag4)
					{
						result = 0m;
					}
					else
					{
						List<SelectedPackageContents> planRows = GraphHelper.RowCast<SelectedPackageContents>(PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
						{
							base.Basis.RefNbr,
							packageLineNbr
						})).Where(delegate(SelectedPackageContents content)
						{
							int? shipmentLineNbr = content.ShipmentLineNbr;
							int? lineNbr = split.LineNbr;
							return shipmentLineNbr.GetValueOrDefault() == lineNbr.GetValueOrDefault() & shipmentLineNbr != null == (lineNbr != null);
						}).ToList<SelectedPackageContents>();
						bool flag5 = planRows.Count == 0;
						if (flag5)
						{
							result = 0m;
						}
						else
						{
							bool flag6 = split.SplitLineNbr != null;
							if (flag6)
							{
								SelectedPackageContents exactMatch = planRows.FirstOrDefault(delegate(SelectedPackageContents p)
								{
									int? shipmentSplitLineNbr = p.ShipmentSplitLineNbr;
									int? splitLineNbr = split.SplitLineNbr;
									return shipmentSplitLineNbr.GetValueOrDefault() == splitLineNbr.GetValueOrDefault() & shipmentSplitLineNbr != null == (splitLineNbr != null);
								});
								bool flag7 = exactMatch != null;
								if (flag7)
								{
									return exactMatch.PackedQty.GetValueOrDefault();
								}
							}
							result = planRows.Where(delegate(SelectedPackageContents p)
							{
								bool result2;
								if (split.InventoryID != null && p.InventoryID != null)
								{
									int? inventoryID = p.InventoryID;
									int? inventoryID2 = split.InventoryID;
									result2 = (inventoryID.GetValueOrDefault() == inventoryID2.GetValueOrDefault() & inventoryID != null == (inventoryID2 != null));
								}
								else
								{
									result2 = true;
								}
								return result2;
							}).Sum((SelectedPackageContents p) => p.PackedQty.GetValueOrDefault());
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002C10 File Offset: 0x00000E10
		private Dictionary<string, decimal> GetActualStyleTotals(int? packageLineNbr = null)
		{
			PickPackShip basis = base.Basis;
			string a;
			if (basis == null)
			{
				a = null;
			}
			else
			{
				ScanHeader header = basis.Header;
				a = ((header != null) ? header.Mode : null);
			}
			bool flag = a != "PACK";
			Dictionary<string, decimal> result2;
			if (flag)
			{
				result2 = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			}
			else
			{
				bool flag2 = packageLineNbr == null;
				if (flag2)
				{
					packageLineNbr = this.GetCurrentPackageLineNbr();
				}
				bool flag3 = string.IsNullOrEmpty(base.Basis.RefNbr) || packageLineNbr == null;
				if (flag3)
				{
					result2 = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
				}
				else
				{
					Dictionary<string, decimal> totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
					foreach (PXResult<SOShipLineSplitPackage> pxresult in PXSelectBase<SOShipLineSplitPackage, PXViewOf<SOShipLineSplitPackage>.BasedOn<SelectFromBase<SOShipLineSplitPackage, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLineSplit>.On<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplitPackage.shipmentNbr, Equal<SOShipLineSplit.shipmentNbr>>>>, And<BqlOperand<SOShipLineSplitPackage.shipmentLineNbr, IBqlInt>.IsEqual<SOShipLineSplit.lineNbr>>>>.And<BqlOperand<SOShipLineSplitPackage.shipmentSplitLineNbr, IBqlInt>.IsEqual<SOShipLineSplit.splitLineNbr>>>>, FbqlJoins.Inner<SOShipLine>.On<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.shipmentNbr, Equal<SOShipLineSplit.shipmentNbr>>>>>.And<BqlOperand<SOShipLine.lineNbr, IBqlInt>.IsEqual<SOShipLineSplit.lineNbr>>>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplitPackage.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLineSplitPackage.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
					{
						base.Basis.RefNbr,
						packageLineNbr
					}))
					{
						PXResult<SOShipLineSplitPackage, SOShipLineSplit, SOShipLine> result = (PXResult<SOShipLineSplitPackage, SOShipLineSplit, SOShipLine>)pxresult;
						SOShipLineSplitPackage link = result;
						SOShipLineSplit split = result;
						SOShipLine line = result;
						string styleKey = PackModeLogicExt.GetStyleKey((line != null) ? line.OrigOrderNbr : null, (split != null) ? split.InventoryID : null);
						decimal packedQty = ((link != null) ? link.PackedQty : null).GetValueOrDefault();
						decimal currentQty;
						totals[styleKey] = (totals.TryGetValue(styleKey, out currentQty) ? (currentQty + packedQty) : packedQty);
					}
					result2 = totals;
				}
			}
			return result2;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002DB4 File Offset: 0x00000FB4
		private static decimal GetDictionaryQty(Dictionary<string, decimal> totals, string styleKey)
		{
			decimal qty;
			return totals.TryGetValue(styleKey, out qty) ? qty : 0m;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002DD4 File Offset: 0x00000FD4
		private static bool QuantitiesMatch(decimal leftQty, decimal rightQty)
		{
			return Math.Abs(leftQty - rightQty) <= 0.000001m;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002DF4 File Offset: 0x00000FF4
		public decimal GetSelectedPackageEstimatedQty(SOShipLineSplit split, string orderNbr, int? packageLineNbr = null)
		{
			bool flag = split == null;
			decimal result;
			if (flag)
			{
				result = 0m;
			}
			else
			{
				result = PackModeLogicExt.GetDictionaryQty(this.GetExpectedStyleTotals(packageLineNbr), PackModeLogicExt.GetStyleKey(orderNbr, split.InventoryID));
			}
			return result;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002E30 File Offset: 0x00001030
		public decimal GetSelectedPackageActualQty(SOShipLineSplit split, string orderNbr, int? packageLineNbr = null)
		{
			bool flag = split == null;
			decimal result;
			if (flag)
			{
				result = 0m;
			}
			else
			{
				result = PackModeLogicExt.GetDictionaryQty(this.GetActualStyleTotals(packageLineNbr), PackModeLogicExt.GetStyleKey(orderNbr, split.InventoryID));
			}
			return result;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002E6C File Offset: 0x0000106C
		public bool DoesSelectedPackageMatchEstimates(int? packageLineNbr = null)
		{
			Dictionary<string, decimal> expectedTotals = this.GetExpectedStyleTotals(packageLineNbr);
			bool flag = expectedTotals.Count == 0;
			bool result;
			if (flag)
			{
				result = true;
			}
			else
			{
				Dictionary<string, decimal> actualTotals = this.GetActualStyleTotals(packageLineNbr);
				IEnumerable<string> styleKeys = expectedTotals.Keys.Union(actualTotals.Keys, StringComparer.OrdinalIgnoreCase);
				result = styleKeys.All((string styleKey) => PackModeLogicExt.QuantitiesMatch(PackModeLogicExt.GetDictionaryQty(expectedTotals, styleKey), PackModeLogicExt.GetDictionaryQty(actualTotals, styleKey)));
			}
			return result;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002EE8 File Offset: 0x000010E8
		public string GetSelectedPackageMismatchMessage(int? packageLineNbr = null)
		{
			bool flag = this.DoesSelectedPackageMatchEstimates(packageLineNbr);
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				result = "The package cannot be confirmed because Total Packed Qty does not match Total Estimated Qty for one or more styles.";
			}
			return result;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00002F10 File Offset: 0x00001110
		public decimal GetSelectedPackageEstimatedTotal(SOPackageDetailEx package)
		{
			return (package == null || package.LineNbr == null) ? 0m : this.GetExpectedStyleTotals(package.LineNbr).Values.Sum();
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00002F54 File Offset: 0x00001154
		public decimal GetSelectedPackageActualTotal(SOPackageDetailEx package)
		{
			return (package == null || package.LineNbr == null) ? 0m : this.GetActualStyleTotals(package.LineNbr).Values.Sum();
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002F98 File Offset: 0x00001198
		public PXResult<CSBox> GetBoxByCartonID(string cartonID)
		{
			return PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOPackageDetailEx>.On<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<SOPackageDetailEx.boxID>>>>.Where<BqlOperand<SOPackageDetailExExt.usrCartonNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.SelectSingleBound(base.Basis, null, new object[]
			{
				cartonID
			});
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00002FCC File Offset: 0x000011CC
		[PXOverride]
		public virtual IEnumerable pickedForPack(PackModeLogicExt.PickedForPackDelegate baseMethod)
		{
			IEnumerable baseResult = baseMethod();
			bool flag = baseResult == null;
			IEnumerable result2;
			if (flag)
			{
				result2 = null;
			}
			else
			{
				Dictionary<string, decimal> expectedTotals = this.GetExpectedStyleTotals(null) ?? new Dictionary<string, decimal>();
				Dictionary<ValueTuple<int, int>, decimal> planSplits = this.GetSelectedPackagePlanSplits(null) ?? new Dictionary<ValueTuple<int, int>, decimal>();
				List<object> sortedList = baseResult.Cast<object>().OrderByDescending(delegate(object result)
				{
					SOShipLineSplit split = PXResult.Unwrap<SOShipLineSplit>(result);
					bool flag2 = split == null || split.LineNbr == null || split.SplitLineNbr == null;
					int result3;
					if (flag2)
					{
						result3 = 0;
					}
					else
					{
						result3 = (planSplits.ContainsKey(new ValueTuple<int, int>(split.LineNbr.Value, split.SplitLineNbr.Value)) ? 1 : 0);
					}
					return result3;
				}).ThenByDescending(delegate(object result)
				{
					SOShipLineSplit split = PXResult.Unwrap<SOShipLineSplit>(result);
					SOShipLine shipmentLine = PXResult.Unwrap<SOShipLine>(result);
					bool flag2 = split == null;
					int result3;
					if (flag2)
					{
						result3 = 0;
					}
					else
					{
						string styleKey = PackModeLogicExt.GetStyleKey((shipmentLine != null) ? shipmentLine.OrigOrderNbr : null, split.InventoryID);
						result3 = ((styleKey != null && expectedTotals.ContainsKey(styleKey)) ? 1 : 0);
					}
					return result3;
				}).ThenBy(delegate(object result)
				{
					SOShipLineSplit soshipLineSplit = PXResult.Unwrap<SOShipLineSplit>(result);
					return ((soshipLineSplit != null) ? soshipLineSplit.LineNbr : null).GetValueOrDefault();
				}).ThenBy(delegate(object result)
				{
					SOShipLineSplit soshipLineSplit = PXResult.Unwrap<SOShipLineSplit>(result);
					return ((soshipLineSplit != null) ? soshipLineSplit.SplitLineNbr : null).GetValueOrDefault();
				}).ToList<object>();
				PXDelegateResult delegateResult = new PXDelegateResult
				{
					IsResultSorted = true
				};
				delegateResult.AddRange(sortedList);
				result2 = delegateResult;
			}
			return result2;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000030CC File Offset: 0x000012CC
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> scanState, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			scanState = base_DecorateScanState(scanState);
			PickPackShip.PackMode.BoxState boxState = scanState as PickPackShip.PackMode.BoxState;
			bool flag = boxState != null;
			if (flag)
			{
				boxState.Intercept.StatePrompt.ByReplace(delegate()
				{
					ScanHeaderExt scanHeaderExt = ScanHeaderExt.Get<ScanHeaderExt>(base.Base1.Header);
					bool flag3 = scanHeaderExt == null || scanHeaderExt.BoxPrompt == null;
					string result;
					if (flag3)
					{
						result = "Scan the Carton #";
					}
					else
					{
						result = scanHeaderExt.BoxPrompt;
					}
					return result;
				}, null);
				boxState.Intercept.GetByBarcode.ByReplace((string x) => base.Basis.Get<PackModeLogicExt>().GetBoxByCartonID(x), null);
				boxState.Intercept.ReportMissing.ByReplace(delegate(string errorBarcode)
				{
					base.Basis.ReportError("The {0} carton is not found in the database.", new object[]
					{
						errorBarcode
					});
				}, null);
				boxState.Intercept.ReportSuccess.ByReplace(delegate(CSBox box)
				{
					SOPackageDetailEx currentBox = PXSelectBase<SOPackageDetailEx, PXViewOf<SOPackageDetailEx>.BasedOn<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOPackageDetailExt.usrCartonNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						base.Base1.Header.Barcode
					});
					bool flag3 = currentBox != null;
					if (flag3)
					{
						base.Basis.ReportInfo("The Carton #{1} ({0}) is selected.", new object[]
						{
							currentBox.BoxID,
							PXCacheEx.GetExtension<SOPackageDetailExt>(currentBox).UsrCartonNbr
						});
					}
				}, null);
				boxState.Intercept.Apply.ByOverride(delegate(CSBox box, Action<CSBox> baseApply)
				{
					string barcode = base.Basis.Header.Barcode;
					SOPackageDetailEx currentBox = PXSelectBase<SOPackageDetailEx, PXViewOf<SOPackageDetailEx>.BasedOn<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOPackageDetailExt.usrCartonNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						barcode
					}).TopFirst;
					SOPackageDetailEx selectedPackage = PXSelectBase<SOPackageDetailEx, PXViewOf<SOPackageDetailEx>.BasedOn<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOPackageDetailExExt.usrCartonNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						barcode
					});
					SOPackageDetailEx package = base.Base2.SelectedPackage;
					ScanHeaderExt scanHeaderExt = ScanHeaderExt.Get<ScanHeaderExt>(base.Base1.Header);
					bool flag3 = scanHeaderExt.UsrMasterPackCartonNbr != null;
					if (flag3)
					{
						SOPackageDetailEx innerPackage = PXSelectBase<SOPackageDetailEx, PXViewOf<SOPackageDetailEx>.BasedOn<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOPackageDetailExt.usrCartonNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
						{
							barcode
						}).TopFirst;
						PXCacheEx.GetExtension<SOPackageDetailExt>(innerPackage).UsrSelectedParentBox = scanHeaderExt.UsrMasterPackCartonNbr;
						base.Basis.Graph.Packages.Update(innerPackage);
						scanHeaderExt.CanConfirm = new bool?(true);
						scanHeaderExt.BoxPrompt = "Scan the inner carton barcode or confirm a master pack carton";
					}
					else
					{
						currentBox = PXSelectBase<SOPackageDetailEx, PXViewOf<SOPackageDetailEx>.BasedOn<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOPackageDetailExt.usrCartonNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
						{
							base.Base1.Header.Barcode
						});
						bool flag4 = PXCacheEx.GetExtension<CSBoxExt>(box).UsrUseAsParentBox.GetValueOrDefault() && PXCacheEx.GetExtension<SOPackageDetailExt>(currentBox).UsrIsParentBox.GetValueOrDefault();
						if (flag4)
						{
							scanHeaderExt.UsrMasterPackCartonNbr = PXCacheEx.GetExtension<SOPackageDetailExt>(currentBox).UsrCartonNbr;
							scanHeaderExt.BoxPrompt = "Scan the inner carton barcode";
						}
						base.Basis.Get<PickPackShip.PackMode.Logic>().PackageLineNbr = currentBox.LineNbr;
						base.Basis.Get<PickPackShip.PackMode.Logic>().PackageLineNbrUI = currentBox.LineNbr;
						base.Basis.Graph.Packages.Current = currentBox;
					}
				}, null);
				boxState.Intercept.Validate.ByAppend(delegate(CSBox x)
				{
					string barcode = base.Basis.Header.Barcode;
					ScanHeaderExt scanHeaderExt = ScanHeaderExt.Get<ScanHeaderExt>(base.Base1.Header);
					bool flag3 = scanHeaderExt.UsrMasterPackCartonNbr == null;
					Validation result;
					if (flag3)
					{
						result = Validation.Ok;
					}
					else
					{
						SOPackageDetailEx package = this.GetBoxByCartonID(barcode).GetItem<SOPackageDetailEx>();
						bool flag4 = package.Confirmed.GetValueOrDefault() && !PXCacheEx.GetExtension<SOPackageDetailExt>(package).UsrIsParentBox.GetValueOrDefault();
						if (flag4)
						{
							result = Validation.Ok;
						}
						else
						{
							result = Validation.Fail("Carton #" + PXCacheEx.GetExtension<SOPackageDetailExExt>(package).UsrCartonNbr + " is not a confirmed inner carton. Please confirm it first or scan a different confirmed inner carton.", Array.Empty<object>());
						}
					}
					return result;
				}, null);
				boxState.Intercept.SetNextState.ByOverride(delegate(Action x)
				{
					ScanHeaderExt scanHeaderExt = ScanHeaderExt.Get<ScanHeaderExt>(base.Base1.Header);
					bool flag3 = scanHeaderExt.UsrMasterPackCartonNbr != null;
					if (flag3)
					{
						base.Basis.CurrentState.Clear();
						base.Basis.SetScanState<PickPackShip.PackMode.BoxState>(null, Array.Empty<object>());
					}
					else if (x != null)
					{
						x();
					}
				}, null);
			}
			WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState itemState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState;
			bool flag2 = itemState != null;
			if (flag2)
			{
				itemState.Intercept.GetByBarcode.ByReplace(delegate(string x)
				{
					string barcode = base.Basis.Header.Barcode;
					PXResult<INItemXRef, InventoryItem> result = null;
					PickPackShip.PackMode.Logic @base = base.Base2;
					List<SOShipLineSplit> list;
					if (@base == null)
					{
						list = null;
					}
					else
					{
						FbqlSelect<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>>>.Order<By<BqlField<SOShipLineSplit.shipmentNbr, IBqlString>.Asc, BqlField<SOShipLineSplit.isUnassigned, IBqlBool>.Desc, BqlField<SOShipLineSplit.lineNbr, IBqlInt>.Asc>>, SOShipLineSplit>.View pickedForPack = @base.PickedForPack;
						if (pickedForPack == null)
						{
							list = null;
						}
						else
						{
							SOShipLineSplit[] array = pickedForPack.SelectMain(Array.Empty<object>());
							list = ((array != null) ? array.ToList<SOShipLineSplit>() : null);
						}
					}
					List<SOShipLineSplit> items = list ?? new List<SOShipLineSplit>();
					foreach (SOShipLineSplit packItem in items)
					{
						bool flag3 = packItem == null || packItem.InventoryID == null;
						if (!flag3)
						{
							PXResult<InventoryItem> queryResult = (from r in PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<INItemXRef>.On<BqlOperand<INItemXRef.inventoryID, IBqlInt>.IsEqual<InventoryItem.inventoryID>>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemXRef.inventoryID, Equal<P.AsInt>>>>>.And<BqlOperand<INItemXRef.alternateID, IBqlString>.IsEqual<P.AsString>>>>.Config>.Select(base.Base, new object[]
							{
								packItem.InventoryID,
								barcode
							}).AsEnumerable<PXResult<InventoryItem>>()
							orderby EnumerableExtensions.IsIn<string>(r.GetItem<INItemXRef>().AlternateType, "BAR", "GIN") descending
							select r).FirstOrDefault<PXResult<InventoryItem>>();
							bool flag4 = queryResult != null;
							if (flag4)
							{
								INItemXRef inItemXRef = (INItemXRef)queryResult[typeof(INItemXRef)];
								InventoryItem inventoryItem = (InventoryItem)queryResult[typeof(InventoryItem)];
								return new PXResult<INItemXRef, InventoryItem>(inItemXRef, inventoryItem);
							}
						}
					}
					InventoryItem inventoryResult = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryCD, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						barcode
					}).TopFirst;
					bool flag5 = inventoryResult != null;
					if (flag5)
					{
						INItemXRef inItemXRef2 = PXSelectBase<INItemXRef, PXViewOf<INItemXRef>.BasedOn<SelectFromBase<INItemXRef, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemXRef.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
						{
							inventoryResult.InventoryID
						}).TopFirst;
						result = new PXResult<INItemXRef, InventoryItem>(inItemXRef2, inventoryResult);
					}
					return result;
				}, null);
			}
			return scanState;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003244 File Offset: 0x00001444
		private PXResult<INItemXRef, InventoryItem> ReadItemById(string barcode, INPrimaryAlternateType? additionalAlternateType = null)
		{
			InventoryItem inventory = InventoryItem.UK.Find(base.Basis, barcode, 0);
			bool flag = inventory != null;
			PXResult<INItemXRef, InventoryItem> result;
			if (flag)
			{
				INItemXRef xref = new INItemXRef
				{
					InventoryID = inventory.InventoryID,
					AlternateType = "BAR",
					AlternateID = barcode
				};
				object defaultSubItem;
				GraphHelper.Caches<INItemXRef>(base.Basis.Graph).RaiseFieldDefaulting<INItemXRef.subItemID>(xref, ref defaultSubItem);
				xref.SubItemID = (int?)defaultSubItem;
				result = new PXResult<INItemXRef, InventoryItem>(xref, inventory);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000032D0 File Offset: 0x000014D0
		protected PXResult<INItemXRef, InventoryItem> ReadItemByBarcode(string barcode, INPrimaryAlternateType? additionalAlternateType = null)
		{
			PXViewOf<INItemXRef>.BasedOn<SelectFromBase<INItemXRef, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<InventoryItem>.On<INItemXRef.FK.InventoryItem>>>.Where<BqlOperand<INItemXRef.alternateID, IBqlString>.IsEqual<P.AsString>>.Order<By<BqlField<INItemXRef.alternateType, IBqlString>.Asc>>>.ReadOnly view = new PXViewOf<INItemXRef>.BasedOn<SelectFromBase<INItemXRef, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<InventoryItem>.On<INItemXRef.FK.InventoryItem>>>.Where<BqlOperand<INItemXRef.alternateID, IBqlString>.IsEqual<P.AsString>>.Order<By<BqlField<INItemXRef.alternateType, IBqlString>.Asc>>>.ReadOnly(base.Basis);
			bool flag = additionalAlternateType.GetValueOrDefault() == 1;
			if (flag)
			{
				view.WhereAnd<Where<BqlOperand<INItemXRef.alternateType, IBqlString>.IsIn<INAlternateType.barcode, INAlternateType.gIN, INAlternateType.cPN>>>();
			}
			else
			{
				INPrimaryAlternateType? inprimaryAlternateType = additionalAlternateType;
				INPrimaryAlternateType inprimaryAlternateType2 = 0;
				bool flag2 = inprimaryAlternateType.GetValueOrDefault() == inprimaryAlternateType2 & inprimaryAlternateType != null;
				if (flag2)
				{
					view.WhereAnd<Where<BqlOperand<INItemXRef.alternateType, IBqlString>.IsIn<INAlternateType.barcode, INAlternateType.gIN, INAlternateType.vPN>>>();
				}
				else
				{
					view.WhereAnd<Where<BqlOperand<INItemXRef.alternateType, IBqlString>.IsIn<INAlternateType.barcode, INAlternateType.gIN>>>();
				}
			}
			PXResult<INItemXRef, InventoryItem> item = (from r in view.Select(new object[]
			{
				barcode
			}).AsEnumerable<PXResult<INItemXRef>>()
			orderby EnumerableExtensions.IsIn<string>(r.GetItem<INItemXRef>().AlternateType, "BAR", "GIN") descending
			select r).Cast<PXResult<INItemXRef, InventoryItem>>().FirstOrDefault<PXResult<INItemXRef, InventoryItem>>();
			bool flag3 = item == null || item == null;
			if (flag3)
			{
				item = this.ReadItemById(barcode, additionalAlternateType);
			}
			return item;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000339C File Offset: 0x0000159C
		[PXOverride]
		public virtual ScanCommand<PickPackShip> DecorateScanCommand(ScanCommand<PickPackShip> original, Func<ScanCommand<PickPackShip>, ScanCommand<PickPackShip>> base_DecorateScanCommand)
		{
			original = base_DecorateScanCommand(original);
			bool flag = original is PickPackShip.PackMode.ConfirmPackageCommand;
			if (flag)
			{
				original.Intercept.IsEnabled.ByOverride(delegate(Func<bool> x)
				{
					ScanHeaderExt scanHeaderExt = ScanHeaderExt.Get<ScanHeaderExt>(base.Base1.Header);
					bool flag2 = scanHeaderExt.CanConfirm != null && scanHeaderExt.CanConfirm.Value;
					return flag2 || ((x != null) ? new bool?(x()) : null).Value;
				}, null);
				original.Intercept.Process.ByOverride(delegate(Func<bool> action)
				{
					ScanHeaderExt scanHeaderExt = ScanHeaderExt.Get<ScanHeaderExt>(base.Base1.Header);
					bool flag2 = scanHeaderExt.CanConfirm != null && scanHeaderExt.CanConfirm.Value;
					bool result;
					if (flag2)
					{
						SOPackageDetailEx selectedPackage = base.Base2.SelectedPackage;
						SOPackageDetailEx parentPackage = this.GetBoxByCartonID(scanHeaderExt.UsrMasterPackCartonNbr).GetItem<SOPackageDetailEx>();
						parentPackage.Confirmed = new bool?(true);
						base.Basis.Get<PickPackShip.PackMode.BoxConfirming.CompleteState.Logic>().SettleAndConfirmPackage(parentPackage);
						scanHeaderExt.CanConfirm = new bool?(false);
						scanHeaderExt.BoxPrompt = "Scan the Master Pack Carton barcode or confirm the Shipment";
						scanHeaderExt.UsrMasterPackCartonNbr = null;
						base.Basis.SetScanState<PickPackShip.PackMode.BoxState>(null, Array.Empty<object>());
						result = true;
					}
					else
					{
						string mismatchMessage = this.GetSelectedPackageMismatchMessage(null);
						bool flag3 = mismatchMessage != null;
						if (flag3)
						{
							base.Basis.ReportError(mismatchMessage, Array.Empty<object>());
							result = true;
						}
						else
						{
							result = action();
						}
					}
					return result;
				}, null);
			}
			return original;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003414 File Offset: 0x00001614
		[PXOverride]
		public bool HasSingleAutoPackage(string shipmentNbr, out SOPackageDetailEx package, PackModeLogicExt.HasSingleAutoPackageDelegate baseMethod)
		{
			package = null;
			return false;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x0000342C File Offset: 0x0000162C
		[PXOverride]
		public virtual bool IsPackageEmpty(SOPackageDetailEx package, PackModeLogicExt.IsPackageEmptyDelegate baseMethod)
		{
			if (package != null)
			{
				SOPackageDetailExt detailExt = PXCache<SOPackageDetail>.GetExtension<SOPackageDetailExt>(package);
				if (detailExt != null && detailExt.UsrIsParentBox.GetValueOrDefault())
				{
					return false;
				}
			}
			return baseMethod(package);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003460 File Offset: 0x00001660
		public bool AutoConfirmPackage(bool skipBoxWeightInput)
		{
			return true;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003474 File Offset: 0x00001674
		[PXOverride]
		public virtual bool? TryAutoConfirmCurrentPackageAndLoadNext(string boxBarcode, PackModeLogicExt.TryAutoConfirmCurrentPackageAndLoadNextDelegate besaMethod)
		{
			bool? remove = base.Basis.Remove;
			bool flag = false;
			bool flag2 = remove.GetValueOrDefault() == flag & remove != null;
			if (flag2)
			{
				CSBox box = this.GetBoxByCartonID(boxBarcode);
				bool flag3 = box != null;
				if (flag3)
				{
					bool flag4 = base.Basis.TryProcessBy<PickPackShip.PackMode.BoxState>(boxBarcode, 26);
					if (flag4)
					{
						return new bool?(true);
					}
				}
			}
			return new bool?(false);
		}

		// Token: 0x04000016 RID: 22
		private const decimal QtyComparisonTolerance = 0.000001m;

		// Token: 0x02000031 RID: 49
		// (Invoke) Token: 0x0600014D RID: 333
		public delegate IEnumerable PickedForPackDelegate();

		// Token: 0x02000032 RID: 50
		// (Invoke) Token: 0x06000151 RID: 337
		public delegate bool HasSingleAutoPackageDelegate(string shipmentNbr, out SOPackageDetailEx package);

		// Token: 0x02000033 RID: 51
		// (Invoke) Token: 0x06000155 RID: 341
		public delegate bool IsPackageEmptyDelegate(SOPackageDetailEx package);

		// Token: 0x02000034 RID: 52
		// (Invoke) Token: 0x06000159 RID: 345
		public delegate bool? TryAutoConfirmCurrentPackageAndLoadNextDelegate(string boxBarcode);
	}
}
