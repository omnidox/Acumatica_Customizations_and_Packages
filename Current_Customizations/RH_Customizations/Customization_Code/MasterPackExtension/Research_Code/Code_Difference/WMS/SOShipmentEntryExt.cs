using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using WMS.AR.GraphExt;

namespace WMS
{
	// Token: 0x02000018 RID: 24
	public class SOShipmentEntryExt : PXGraphExtension<CarrierRatesExt, SOShipmentEntry>
	{
		// Token: 0x060000D8 RID: 216 RVA: 0x00002082 File Offset: 0x00000282
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00004558 File Offset: 0x00002758
		protected void _(Events.FieldVerifying<SOPackageDetailExt.usrIsParentBox> e)
		{
			bool flag = e == null;
			if (!flag)
			{
				SOPackageDetailEx args = (SOPackageDetailEx)e.Args.Row;
				CSBox box = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					args.BoxID
				});
				bool? canUseLikeParent = PXCacheEx.GetExtension<CSBoxExt>(box).UsrUseAsParentBox;
				bool flag2 = canUseLikeParent == null || !canUseLikeParent.Value;
				if (flag2)
				{
					throw new PXSetPropertyException("This box cannot be used as Master Pack Carton. Please move to the Boxes screen and mark “Can Be Used as Master Pack Carton” as True.", 4);
				}
			}
		}

		// Token: 0x060000DA RID: 218 RVA: 0x000045DC File Offset: 0x000027DC
		protected void _(Events.RowSelected<SOPackageDetail> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				SOPackageDetailExt extension = PXCacheEx.GetExtension<SOPackageDetailExt>(e.Row);
				bool isParentBox = extension != null && extension.UsrIsParentBox.GetValueOrDefault();
				base.Base.PackageDetailExt.PackageDetailSplit.AllowInsert = !isParentBox;
				base.Base.PackageDetailExt.PackageDetailSplit.AllowUpdate = !isParentBox;
				this.SelectedPackageContentsView.AllowInsert = !isParentBox;
				this.SelectedPackageContentsView.AllowUpdate = !isParentBox;
				PXUIFieldAttribute.SetEnabled<SOPackageDetailExt.usrSelectedParentBox>(base.Base.Packages.Cache, e.Row, !isParentBox);
			}
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00004694 File Offset: 0x00002894
		protected void _(Events.FieldUpdated<SOPackageDetailExt.usrIsParentBox> e)
		{
			bool flag = e == null;
			if (!flag)
			{
				SOPackageDetailExt package = PXCacheEx.GetExtension<SOPackageDetailExt>(base.Base.Packages.Current);
				package.UsrSOBoxNbrStr = null;
				bool flag2 = (bool)e.NewValue;
				if (flag2)
				{
					base.Base.PackageDetailExt.PackageDetailSplit.AllowInsert = false;
					base.Base.PackageDetailExt.PackageDetailSplit.AllowUpdate = false;
					this.SelectedPackageContentsView.AllowInsert = false;
					this.SelectedPackageContentsView.AllowUpdate = false;
					foreach (PXResult<SOShipLineSplitPackage> item in base.Base.PackageDetailExt.PackageDetailSplit.Select(Array.Empty<object>()))
					{
						base.Base.PackageDetailExt.PackageDetailSplit.Delete(item);
					}
					foreach (PXResult<SelectedPackageContents> item2 in this.SelectedPackageContentsView.Select(Array.Empty<object>()))
					{
						this.SelectedPackageContentsView.Delete(item2);
					}
					PXUIFieldAttribute.SetReadOnly<SOPackageDetailExt.usrSelectedParentBox>(base.Base.Packages.Cache, e.Row, true);
				}
				else
				{
					base.Base.PackageDetailExt.PackageDetailSplit.AllowInsert = true;
					base.Base.PackageDetailExt.PackageDetailSplit.AllowUpdate = true;
					this.SelectedPackageContentsView.AllowInsert = true;
					this.SelectedPackageContentsView.AllowUpdate = true;
					PXUIFieldAttribute.SetReadOnly<SOPackageDetailExt.usrSelectedParentBox>(base.Base.Packages.Cache, e.Row, false);
					List<SOPackageDetailEx> freshPackages = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>();
					this.SetBoxNbrStr(freshPackages);
				}
			}
		}

		// Token: 0x060000DC RID: 220 RVA: 0x000048A8 File Offset: 0x00002AA8
		protected void _(Events.FieldVerifying<SOPackageDetailEx.boxID> e)
		{
			bool flag = e == null || !e.ExternalCall;
			if (!flag)
			{
				SOPackageDetailEx args = (SOPackageDetailEx)e.Args.Row;
				int? customer = base.Base.Document.Current.CustomerID;
				object boxId = e.NewValue;
				Customer originalCustomer = PXSelectBase<Customer, PXViewOf<Customer>.BasedOn<SelectFromBase<Customer, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<Customer.bAccountID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					customer
				});
				CustomerPackaging packaging = PXSelectBase<CustomerPackaging, PXViewOf<CustomerPackaging>.BasedOn<SelectFromBase<CustomerPackaging, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CustomerPackaging.customer, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					customer
				});
				CustomerBoxesDAC customerBoxes = PXSelectBase<CustomerBoxesDAC, PXViewOf<CustomerBoxesDAC>.BasedOn<SelectFromBase<CustomerBoxesDAC, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<CustomerBoxesDAC.boxID, Equal<P.AsString>>>>>.And<BqlOperand<CustomerBoxesDAC.customerID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
				{
					boxId,
					originalCustomer.BAccountID
				});
				bool flag2 = packaging == null || customerBoxes == null;
				if (!flag2)
				{
					bool flag4;
					if (packaging.UseOnlyCustomerBoxes.GetValueOrDefault())
					{
						bool? active = customerBoxes.Active;
						bool flag3 = false;
						flag4 = (active.GetValueOrDefault() == flag3 & active != null);
					}
					else
					{
						flag4 = false;
					}
					bool flag5 = flag4;
					if (flag5)
					{
						throw new PXSetPropertyException("This box is not used for that particular customer. Please, make sure that you selected the correct one or make box active on the Customers screen -> Boxes tab");
					}
				}
			}
		}

		// Token: 0x060000DD RID: 221 RVA: 0x000049C8 File Offset: 0x00002BC8
		protected void _(Events.RowSelected<SOPackageDetailEx> e)
		{
			SOPackageDetailEx row = e.Row;
			bool flag = row == null;
			if (!flag)
			{
				this.UpdateOrderAndStoreInfo(row);
				this.UpdatePackageQuantities(row);
				this.SetBoxNbrStr(base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>());
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00004A20 File Offset: 0x00002C20
		protected void _(Events.RowUpdated<SOPackageDetailEx> e)
		{
			bool flag = e == null;
			if (!flag)
			{
				string oldParentBoxID = PXCacheEx.GetExtension<SOPackageDetailExt>(e.OldRow).UsrSelectedParentBox;
				string currentParentBox = PXCacheEx.GetExtension<SOPackageDetailExt>(e.Row).UsrSelectedParentBox;
				string parentPackageID = currentParentBox;
				bool flag2 = parentPackageID == null && oldParentBoxID == null;
				if (!flag2)
				{
					bool flag3 = parentPackageID != null;
					if (flag3)
					{
						IEnumerable<SOPackageDetailEx> view = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
						SOPackageDetailEx parentPackage = view.FirstOrDefault((SOPackageDetailEx x) => PXCache<SOPackageDetail>.GetExtension<SOPackageDetailExt>(x).UsrCartonNbr == parentPackageID);
						IEnumerable<SOPackageDetailEx> childrenBoxes = from x in view
						where PXCacheEx.GetExtension<SOPackageDetailExt>(x).UsrSelectedParentBox == parentPackageID
						select x;
						parentPackage.Weight = new decimal?(0m);
						parentPackage.COD = new decimal?(0m);
						parentPackage.DeclaredValue = new decimal?(0m);
						foreach (SOPackageDetailEx child in childrenBoxes)
						{
							parentPackage.Weight += child.Weight;
							parentPackage.COD += child.COD;
							parentPackage.DeclaredValue += child.DeclaredValue;
						}
						e.Cache.SetValueExt<SOPackageDetail.weight>(parentPackage, parentPackage.Weight);
						e.Cache.SetValueExt<SOPackageDetail.cOD>(parentPackage, parentPackage.COD);
						e.Cache.SetValueExt<SOPackageDetail.declaredValue>(parentPackage, parentPackage.DeclaredValue);
						base.Base.Packages.Update(parentPackage);
						base.Base.Packages.View.RequestRefresh();
					}
					bool flag4 = oldParentBoxID != null;
					if (flag4)
					{
						IEnumerable<SOPackageDetailEx> view2 = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
						SOPackageDetailEx parentPackage2 = view2.First((SOPackageDetailEx x) => PXCache<SOPackageDetail>.GetExtension<SOPackageDetailExt>(x).UsrCartonNbr == oldParentBoxID);
						parentPackage2.Weight -= e.Row.Weight;
						parentPackage2.COD -= e.Row.COD;
						parentPackage2.DeclaredValue -= e.Row.DeclaredValue;
						e.Cache.SetValueExt<SOPackageDetail.weight>(parentPackage2, parentPackage2.Weight);
						e.Cache.SetValueExt<SOPackageDetail.cOD>(parentPackage2, parentPackage2.COD);
						e.Cache.SetValueExt<SOPackageDetail.declaredValue>(parentPackage2, parentPackage2.DeclaredValue);
						base.Base.Packages.Update(parentPackage2);
						base.Base.Packages.View.RequestRefresh();
					}
				}
			}
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00004E94 File Offset: 0x00003094
		[PXOverride]
		public void CreateShipment(CreateShipmentArgs args, SOShipmentEntryExt.CreateShipmentDelegate baseMethod)
		{
			SOOrder order = args.Order;
			SOOrderType soOrderType = PXSelectBase<SOOrderType, PXViewOf<SOOrderType>.BasedOn<SelectFromBase<SOOrderType, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOOrderType.orderType, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
			{
				order.OrderType
			}).TopFirst;
			bool flag = soOrderType.Template != "SO";
			if (flag)
			{
				baseMethod(args);
			}
			else
			{
				IEnumerable<SelectedPackageContents> test = this.SelectedPackageContentsView.Select(Array.Empty<object>()).FirstTableItems;
				Customer originalCustomer = PXSelectBase<Customer, PXViewOf<Customer>.BasedOn<SelectFromBase<Customer, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<Customer.bAccountID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					order.CustomerID
				});
				CustomerPackaging customerPackages = PXSelectBase<CustomerPackaging, PXViewOf<CustomerPackaging>.BasedOn<SelectFromBase<CustomerPackaging, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CustomerPackaging.customer, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					order.CustomerID
				}).TopFirst;
				List<CustomerBoxesDAC> boxes = GraphHelper.RowCast<CustomerBoxesDAC>(PXSelectBase<CustomerBoxesDAC, PXViewOf<CustomerBoxesDAC>.BasedOn<SelectFromBase<CustomerBoxesDAC, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<CustomerBoxesDAC.customerID, Equal<P.AsInt>>>>>.And<BqlOperand<CustomerBoxesDAC.active, IBqlBool>.IsEqual<True>>>>.Config>.Select(base.Base, new object[]
				{
					originalCustomer.BAccountID
				})).ToList<CustomerBoxesDAC>();
				List<CustomerBoxesDAC> customerBoxes = new List<CustomerBoxesDAC>();
				bool flag2 = customerPackages != null && customerPackages.UseOnlyCustomerBoxes.GetValueOrDefault() && boxes.Count <= 0;
				if (flag2)
				{
					throw new PXException("Use Customer Boxes feature is enabled for this customer. However boxes are not defined or not activated on the Boxes tab on the Customers screen. Please specify and activate boxes for this customer.");
				}
				bool flag3 = customerPackages != null && customerPackages.UseOnlyCustomerBoxes.GetValueOrDefault();
				if (flag3)
				{
					customerBoxes = GraphHelper.RowCast<CustomerBoxesDAC>(PXSelectBase<CustomerBoxesDAC, PXViewOf<CustomerBoxesDAC>.BasedOn<SelectFromBase<CustomerBoxesDAC, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<CustomerBoxesDAC.customerID, Equal<P.AsInt>>>>>.And<BqlOperand<CustomerBoxesDAC.active, IBqlBool>.IsEqual<True>>>>.Config>.Select(base.Base, new object[]
					{
						originalCustomer.BAccountID
					})).ToList<CustomerBoxesDAC>();
				}
				baseMethod(args);
				test = this.SelectedPackageContentsView.Select(Array.Empty<object>()).FirstTableItems;
				foreach (SelectedPackageContents record in test)
				{
					this.SelectedPackageContentsView.Delete(record);
				}
				List<SOPackageDetailEx> packages = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>();
				List<SOPackageDetailEx> orderPackages = null;
				foreach (SOPackageDetailEx package in packages)
				{
					bool flag4 = package.ShipmentNbr == base.Base.Document.Current.ShipmentNbr && package.PackageType == "A";
					if (flag4)
					{
						base.Base.Packages.Delete(package);
						base.Base.Actions.PressSave();
					}
				}
				test = this.SelectedPackageContentsView.Select(Array.Empty<object>()).FirstTableItems;
				bool valueOrDefault = PXCacheEx.GetExtension<SOOrderExt>(order).UsrPackOrderSeparately.GetValueOrDefault();
				if (valueOrDefault)
				{
					orderPackages = this.CreateOrderPackages(order, customerBoxes);
				}
				List<SOShipLineSplit> shipmentsSplit = GraphHelper.RowCast<SOShipLineSplit>(PXSelectBase<SOShipLineSplit, PXViewOf<SOShipLineSplit>.BasedOn<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplit.shipmentNbr, Equal<BqlField<SOShipment.shipmentNbr, IBqlString>.FromCurrent>>>>>.And<BqlOperand<SOShipLineSplit.origOrderNbr, IBqlString>.IsEqual<P.AsString>>>>.Config>.Select(base.Base, new object[]
				{
					order.OrderNbr
				})).ToList<SOShipLineSplit>();
				foreach (SOShipLineSplit shipLine in shipmentsSplit)
				{
					InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
					{
						shipLine.InventoryID
					}).TopFirst;
					bool valueOrDefault2 = PXCacheEx.GetExtension<SOOrderExt>(order).UsrPackOrderSeparately.GetValueOrDefault();
					if (valueOrDefault2)
					{
						string packageOption = item.PackageOption;
						string a = packageOption;
						if (!(a == "W"))
						{
							if (a == "V")
							{
								this.CreatePackagesByVolumeAndWeightSeparately(base.Base, shipLine, order.OrderNbr, orderPackages);
							}
						}
						else
						{
							this.CreatePackagesByWeightSeparately(base.Base, shipLine, order.OrderNbr, orderPackages);
						}
					}
					bool? usrPackOrderSeparately = PXCacheEx.GetExtension<SOOrderExt>(order).UsrPackOrderSeparately;
					bool flag5 = false;
					bool flag6 = usrPackOrderSeparately.GetValueOrDefault() == flag5 & usrPackOrderSeparately != null;
					if (flag6)
					{
						string packageOption2 = item.PackageOption;
						string a2 = packageOption2;
						if (!(a2 == "W"))
						{
							if (a2 == "V")
							{
								this.CreatePackagesByVolumeAndWeight(base.Base, shipLine, customerBoxes);
							}
						}
						else
						{
							this.CreatePackagesByWeight(base.Base, shipLine, customerBoxes);
						}
					}
				}
				this.DeleteEmptyPackages(base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems);
			}
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000533C File Offset: 0x0000353C
		protected List<SOPackageDetailEx> CreateOrderPackages(SOOrder order, List<CustomerBoxesDAC> customerBoxes)
		{
			List<SOLine> orderLines = PXSelectBase<SOLine, PXViewOf<SOLine>.BasedOn<SelectFromBase<SOLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOLine.orderNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
			{
				order.OrderNbr
			}).FirstTableItems.ToList<SOLine>();
			List<CSBox> boxes = new List<CSBox>();
			foreach (CustomerBoxesDAC customerBox in customerBoxes)
			{
				CSBox box8 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					customerBox.BoxID
				}).TopFirst;
				bool flag = box8 != null;
				if (flag)
				{
					boxes.Add(box8);
				}
			}
			int orderQty = this.GetOrderQty(orderLines);
			decimal volume = 0m;
			List<SOPackageDetailEx> orderPackages = new List<SOPackageDetailEx>();
			foreach (SOLine orderLine in orderLines)
			{
				decimal itemWeight = this.GetItemWeight(orderLine.InventoryID);
				decimal itemVolume = this.GetItemVolume(orderLine.InventoryID);
				List<INItemBoxEx> InItemBoxs = GraphHelper.RowCast<INItemBoxEx>(PXSelectBase<INItemBoxEx, PXViewOf<INItemBoxEx>.BasedOn<SelectFromBase<INItemBoxEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemBoxEx.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					orderLine.InventoryID
				})).ToList<INItemBoxEx>();
				bool flag2 = customerBoxes == null || customerBoxes.Count <= 0;
				if (flag2)
				{
					foreach (INItemBoxEx InItemBox in InItemBoxs)
					{
						CSBox box2 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
						{
							InItemBox.BoxID
						}).TopFirst;
						bool flag3 = box2 != null;
						if (flag3)
						{
							boxes.Add(box2);
						}
					}
				}
				InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
				{
					orderLine.InventoryID
				}).TopFirst;
				CSBox selectedBox = null;
				string packageOption = item.PackageOption;
				string a = packageOption;
				if (!(a == "W"))
				{
					if (a == "V")
					{
						selectedBox = (from box in boxes
						orderby box.MaxWeight descending, box.MaxVolume descending
						select box).FirstOrDefault<CSBox>();
						foreach (CSBox box3 in from box in boxes
						orderby box.MaxWeight, box.MaxVolume
						select box)
						{
							int maxQtyByWeight = (int)(box3.MaxWeight / itemWeight).Value;
							int maxQtyByVolume = (int)(box3.MaxVolume / itemVolume).Value;
							int maxQtyFit = Math.Min(maxQtyByWeight, maxQtyByVolume);
							bool flag4 = maxQtyFit >= orderQty;
							if (flag4)
							{
								selectedBox = box3;
								break;
							}
						}
					}
				}
				else
				{
					selectedBox = (from box in boxes
					orderby box.MaxWeight descending
					select box).FirstOrDefault<CSBox>();
					foreach (CSBox box4 in from box in boxes
					orderby box.MaxWeight
					select box)
					{
						decimal? num = box4.MaxWeight / itemWeight;
						decimal d = orderQty;
						bool flag5 = num.GetValueOrDefault() >= d & num != null;
						if (flag5)
						{
							selectedBox = box4;
							break;
						}
					}
				}
				INItemBoxEx optimalBoxWeight = (from box in InItemBoxs
				orderby box.MaxWeight descending
				select box).FirstOrDefault<INItemBoxEx>();
				INItemBoxEx optimalBoxVolumeAndWeight = (from box in InItemBoxs
				orderby box.MaxWeight descending, box.MaxVolume descending
				select box).FirstOrDefault<INItemBoxEx>();
				bool flag6 = optimalBoxWeight == null || optimalBoxVolumeAndWeight == null;
				if (!flag6)
				{
					for (;;)
					{
						decimal? num2 = orderLine.Qty;
						decimal d = 0m;
						if (!(num2.GetValueOrDefault() > d & num2 != null))
						{
							break;
						}
						SOPackageDetailEx currentPackage = orderPackages.LastOrDefault<SOPackageDetailEx>();
						int itemsToPack = 0;
						bool flag7 = currentPackage == null;
						if (flag7)
						{
							currentPackage = new SOPackageDetailEx
							{
								Confirmed = new bool?(false),
								BoxID = selectedBox.BoxID,
								Weight = new decimal?(0m)
							};
							currentPackage = base.Base.Packages.Insert(currentPackage);
							base.Base.Actions.PressSave();
							orderPackages.Add(currentPackage);
							volume = 0m;
						}
						bool flag8 = item.PackageOption == "W";
						decimal? num;
						if (flag8)
						{
							int maxQtyByWeight2 = (int)((currentPackage.MaxWeight.Value - currentPackage.Weight) / itemWeight).Value;
							decimal remainingCapacity = 0m;
							remainingCapacity = (currentPackage.MaxWeight - currentPackage.Weight).Value;
							bool flag9 = remainingCapacity < itemWeight;
							if (flag9)
							{
								selectedBox = (from box in boxes
								orderby box.MaxWeight descending
								select box).FirstOrDefault<CSBox>();
								foreach (CSBox box5 in from box in boxes
								orderby box.MaxWeight
								select box)
								{
									num = box5.MaxWeight / itemWeight;
									d = orderQty;
									bool flag10 = num.GetValueOrDefault() >= d & num != null;
									if (flag10)
									{
										selectedBox = box5;
										break;
									}
								}
								currentPackage = new SOPackageDetailEx
								{
									Confirmed = new bool?(false),
									BoxID = selectedBox.BoxID,
									Weight = new decimal?(0m)
								};
								currentPackage = base.Base.Packages.Insert(currentPackage);
								base.Base.Actions.PressSave();
								orderPackages.Add(currentPackage);
								volume = 0m;
								remainingCapacity = (currentPackage.MaxWeight - currentPackage.Weight).Value;
							}
							itemsToPack = (int)Math.Floor(Math.Min(orderLine.Qty.Value, (currentPackage.MaxWeight / itemWeight).Value));
							currentPackage.Weight += itemsToPack * itemWeight;
							volume += itemsToPack * itemVolume;
						}
						bool flag11 = item.PackageOption == "V";
						if (flag11)
						{
							CSBox itemBox = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
							{
								currentPackage.BoxID
							}).TopFirst;
							decimal remainingVolume = itemBox.MaxVolume.Value - volume;
							decimal? remainingWeight = currentPackage.MaxWeight.Value - currentPackage.Weight;
							int maxQtyByVolume2 = (int)(remainingVolume / itemVolume);
							int maxQtyByWeight3 = (int)(remainingWeight / itemWeight).Value;
							decimal remainingCapacity2 = 0m;
							bool flag12 = maxQtyByVolume2 < maxQtyByWeight3 && maxQtyByVolume2 > 0;
							if (flag12)
							{
								itemBox = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
								{
									currentPackage.BoxID
								}).TopFirst;
								remainingCapacity2 = itemBox.MaxVolume.Value - volume;
								bool flag13 = remainingCapacity2 < itemVolume;
								if (flag13)
								{
									selectedBox = (from box in boxes
									orderby box.MaxWeight descending, box.MaxVolume descending
									select box).FirstOrDefault<CSBox>();
									foreach (CSBox box6 in from box in boxes
									orderby box.MaxWeight, box.MaxVolume
									select box)
									{
										int maxQtyByWeightNew = (int)(box6.MaxWeight / itemWeight).Value;
										int maxQtyByVolumeNew = (int)(box6.MaxVolume / itemVolume).Value;
										int maxQtyFit2 = Math.Min(maxQtyByWeightNew, maxQtyByVolumeNew);
										bool flag14 = maxQtyFit2 >= orderQty;
										if (flag14)
										{
											selectedBox = box6;
											break;
										}
									}
									currentPackage = new SOPackageDetailEx
									{
										Confirmed = new bool?(false),
										BoxID = selectedBox.BoxID,
										Weight = new decimal?(0m)
									};
									currentPackage = base.Base.Packages.Insert(currentPackage);
									base.Base.Actions.PressSave();
									orderPackages.Add(currentPackage);
									volume = 0m;
									itemBox = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
									{
										currentPackage.BoxID
									}).TopFirst;
									remainingCapacity2 = itemBox.MaxVolume.Value - volume;
								}
								itemsToPack = Math.Min((int)orderLine.Qty.Value, (int)(remainingCapacity2 / itemVolume));
							}
							bool flag15 = maxQtyByVolume2 >= maxQtyByWeight3 && maxQtyByWeight3 > 0;
							if (flag15)
							{
								remainingCapacity2 = (currentPackage.MaxWeight - currentPackage.Weight).Value;
								bool flag16 = remainingCapacity2 < itemWeight;
								if (flag16)
								{
									selectedBox = (from box in boxes
									orderby box.MaxWeight descending
									select box).FirstOrDefault<CSBox>();
									foreach (CSBox box7 in from box in boxes
									orderby box.MaxWeight
									select box)
									{
										num = box7.MaxWeight / itemWeight;
										d = orderQty;
										bool flag17 = num.GetValueOrDefault() >= d & num != null;
										if (flag17)
										{
											selectedBox = box7;
											break;
										}
									}
									currentPackage = new SOPackageDetailEx
									{
										Confirmed = new bool?(false),
										BoxID = selectedBox.BoxID,
										Weight = new decimal?(0m)
									};
									currentPackage = base.Base.Packages.Insert(currentPackage);
									base.Base.Actions.PressSave();
									orderPackages.Add(currentPackage);
									volume = 0m;
									remainingCapacity2 = (currentPackage.MaxWeight - currentPackage.Weight).Value;
								}
								itemsToPack = (int)Math.Floor(Math.Min(orderLine.Qty.Value, ((currentPackage.MaxWeight - currentPackage.Weight) / itemWeight).Value));
							}
							bool flag18 = maxQtyByVolume2 <= 0 || maxQtyByWeight3 <= 0;
							if (flag18)
							{
								currentPackage = new SOPackageDetailEx
								{
									Confirmed = new bool?(false),
									BoxID = selectedBox.BoxID,
									Weight = new decimal?(0m)
								};
								currentPackage = base.Base.Packages.Insert(currentPackage);
								base.Base.Actions.PressSave();
								orderPackages.Add(currentPackage);
								volume = 0m;
							}
							currentPackage.Weight += itemsToPack * itemWeight;
							volume += itemsToPack * itemVolume;
						}
						orderLine.Qty -= itemsToPack;
						orderQty -= itemsToPack;
						num = currentPackage.Weight;
						num2 = optimalBoxWeight.MaxWeight;
						bool flag19;
						if (!(num.GetValueOrDefault() >= num2.GetValueOrDefault() & (num != null & num2 != null)))
						{
							decimal d2 = volume;
							num2 = optimalBoxVolumeAndWeight.MaxVolume;
							flag19 = (d2 >= num2.GetValueOrDefault() & num2 != null);
						}
						else
						{
							flag19 = true;
						}
						bool flag20 = flag19;
						if (flag20)
						{
						}
					}
				}
			}
			return orderPackages;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000652C File Offset: 0x0000472C
		protected decimal GetOrderWeight(List<SOLine> orderLines)
		{
			return orderLines.Sum((SOLine orderLine) => this.GetItemWeight(orderLine.InventoryID) * orderLine.Qty).Value;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00006558 File Offset: 0x00004758
		protected decimal GetOrderVolume(List<SOLine> orderLines)
		{
			return orderLines.Sum((SOLine orderLine) => this.GetItemVolume(orderLine.InventoryID) * orderLine.Qty).Value;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00006584 File Offset: 0x00004784
		protected int GetOrderQty(List<SOLine> orderLines)
		{
			return (int)orderLines.Sum((SOLine orderLine) => orderLine.Qty).Value;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x000065C8 File Offset: 0x000047C8
		protected decimal GetItemWeight(int? inventoryID)
		{
			InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				inventoryID
			}).TopFirst;
			return ((item != null) ? item.BaseItemWeight : null).GetValueOrDefault();
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00006618 File Offset: 0x00004818
		protected decimal GetItemVolume(int? inventoryID)
		{
			InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				inventoryID
			}).TopFirst;
			return ((item != null) ? item.BaseItemVolume : null).GetValueOrDefault();
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00006668 File Offset: 0x00004868
		protected decimal GetTotalPackageWeight(SOPackageDetailEx package)
		{
			decimal totalWeight = 0m;
			List<SelectedPackageContents> packageContents = PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			}).FirstTableItems.ToList<SelectedPackageContents>();
			foreach (SelectedPackageContents packageContent in packageContents)
			{
				totalWeight += this.GetItemWeight(packageContent.InventoryID) * packageContent.PackedQty.GetValueOrDefault();
			}
			return totalWeight;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00006720 File Offset: 0x00004920
		protected decimal GetTotalPackageVolume(SOPackageDetailEx package)
		{
			decimal totalVolume = 0m;
			List<SelectedPackageContents> packageContents = PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			}).FirstTableItems.ToList<SelectedPackageContents>();
			foreach (SelectedPackageContents packageContent in packageContents)
			{
				totalVolume += this.GetItemVolume(packageContent.InventoryID) * packageContent.PackedQty.GetValueOrDefault();
			}
			return totalVolume;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x000067D8 File Offset: 0x000049D8
		protected bool HasSelectedPackageContents(SOPackageDetailEx package)
		{
			bool flag = package == null || package.ShipmentNbr == null || package.LineNbr == null;
			return !flag && PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			}).TopFirst != null;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00006844 File Offset: 0x00004A44
		protected void DeleteEmptyPackages(IEnumerable<SOPackageDetailEx> packages)
		{
			bool deletedAny = false;
			foreach (SOPackageDetailEx package in (((packages != null) ? packages.ToList<SOPackageDetailEx>() : null) ?? new List<SOPackageDetailEx>()))
			{
				string a = (package != null) ? package.ShipmentNbr : null;
				SOShipment soshipment = base.Base.Document.Current;
				bool flag = a != ((soshipment != null) ? soshipment.ShipmentNbr : null);
				if (!flag)
				{
					bool flag2 = this.HasSelectedPackageContents(package);
					if (!flag2)
					{
						base.Base.Packages.Delete(package);
						deletedAny = true;
					}
				}
			}
			bool flag3 = deletedAny;
			if (flag3)
			{
				base.Base.Actions.PressSave();
			}
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00006918 File Offset: 0x00004B18
		protected void CreatePackagesByWeightSeparately(SOShipmentEntry sOShipmentEntry, SOShipLineSplit soShipLine, string orderNbr, List<SOPackageDetailEx> packages)
		{
			decimal itemWeight = this.GetItemWeight(soShipLine.InventoryID);
			decimal qty = soShipLine.Qty.GetValueOrDefault();
			SOShipLine shipLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.origOrderNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLine.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				orderNbr,
				soShipLine.InventoryID
			}).TopFirst;
			SOOrder order = PXSelectBase<SOOrder, PXViewOf<SOOrder>.BasedOn<SelectFromBase<SOOrder, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOOrder.orderNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				orderNbr
			}).TopFirst;
			SOShipment shipment = base.Base.CurrentDocument.Current;
			SOOrderExt extension = PXCacheEx.GetExtension<SOOrderExt>(order);
			string storeNbr = (extension != null) ? extension.UsrTCStoreNumber : null;
			InventoryItem itemWarehouseDetails = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				shipLine.InventoryID
			}).TopFirst;
			foreach (SOPackageDetailEx package in packages)
			{
				PXCacheEx.GetExtension<SOPackageDetailExt>(package).UsrSepareteOrderNbr = orderNbr;
				decimal remainingCapacity = package.MaxWeight.Value - this.GetTotalPackageWeight(package);
				bool flag = remainingCapacity < itemWeight;
				if (!flag)
				{
					int itemsToPack = (int)Math.Min(qty, Math.Floor(remainingCapacity / itemWeight));
					package.Weight += itemsToPack * itemWeight;
					SelectedPackageContents newPackageContent = new SelectedPackageContents
					{
						ShipmentNbr = package.ShipmentNbr,
						PackageLineNbr = package.LineNbr,
						OrderNbr = shipLine.OrigOrderNbr,
						StoreNbr = storeNbr,
						InventoryID = soShipLine.InventoryID,
						PackedQty = new decimal?(itemsToPack),
						ShipmentSplitLineNbr = soShipLine.SplitLineNbr,
						DefaultIssueFrom = itemWarehouseDetails.DfltShipLocationID
					};
					this.SelectedPackageContentsView.Insert(newPackageContent);
					base.Base.Actions.PressSave();
					qty -= itemsToPack;
					bool flag2 = qty == 0m;
					if (flag2)
					{
						break;
					}
				}
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00006B74 File Offset: 0x00004D74
		protected void CreatePackagesByVolumeAndWeightSeparately(SOShipmentEntry sOShipmentEntry, SOShipLineSplit soShipLine, string orderNbr, List<SOPackageDetailEx> packages)
		{
			decimal itemWeight = this.GetItemWeight(soShipLine.InventoryID);
			decimal itemVolume = this.GetItemVolume(soShipLine.InventoryID);
			decimal qty = soShipLine.Qty.GetValueOrDefault();
			SOShipLine shipLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.origOrderNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLine.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				orderNbr,
				soShipLine.InventoryID
			}).TopFirst;
			SOOrder order = PXSelectBase<SOOrder, PXViewOf<SOOrder>.BasedOn<SelectFromBase<SOOrder, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOOrder.orderNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				orderNbr
			}).TopFirst;
			SOShipment shipment = base.Base.CurrentDocument.Current;
			SOOrderExt extension = PXCacheEx.GetExtension<SOOrderExt>(order);
			string storeNbr = (extension != null) ? extension.UsrTCStoreNumber : null;
			InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				soShipLine.InventoryID
			}).TopFirst;
			InventoryItem itemWarehouseDetails = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				shipLine.InventoryID
			}).TopFirst;
			foreach (SOPackageDetailEx package in packages)
			{
				PXCacheEx.GetExtension<SOPackageDetailExt>(package).UsrSepareteOrderNbr = orderNbr;
				CSBox itemBox = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					package.BoxID
				}).TopFirst;
				decimal remainingCapacityWeight = package.MaxWeight.Value - this.GetTotalPackageWeight(package);
				decimal remainingCapacityVolume = itemBox.MaxVolume.Value - this.GetTotalPackageVolume(package);
				bool flag = remainingCapacityWeight < itemWeight || remainingCapacityVolume < itemVolume;
				if (!flag)
				{
					int maxItemsByWeight = (int)Math.Floor(remainingCapacityWeight / itemWeight);
					int maxItemsByVolume = (int)Math.Floor(remainingCapacityVolume / itemVolume);
					int itemsToPack = (int)Math.Min(qty, Math.Min(maxItemsByWeight, maxItemsByVolume));
					package.Weight += itemsToPack * itemWeight;
					SelectedPackageContents newPackageContent = new SelectedPackageContents
					{
						ShipmentNbr = package.ShipmentNbr,
						PackageLineNbr = package.LineNbr,
						OrderNbr = shipLine.OrigOrderNbr,
						StoreNbr = storeNbr,
						InventoryID = soShipLine.InventoryID,
						PackedQty = new decimal?(itemsToPack),
						ShipmentSplitLineNbr = soShipLine.SplitLineNbr,
						DefaultIssueFrom = itemWarehouseDetails.DfltShipLocationID
					};
					this.SelectedPackageContentsView.Insert(newPackageContent);
					IEnumerable<SelectedPackageContents> test = this.SelectedPackageContentsView.Select(Array.Empty<object>()).FirstTableItems;
					base.Base.Actions.PressSave();
					qty -= itemsToPack;
					bool flag2 = qty == 0m;
					if (flag2)
					{
						break;
					}
				}
			}
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00006E94 File Offset: 0x00005094
		protected void CreatePackagesByWeight(SOShipmentEntry sOShipmentEntry, SOShipLineSplit order, List<CustomerBoxesDAC> customerBoxes)
		{
			List<CSBox> boxes = new List<CSBox>();
			SOShipLine shipLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.origOrderNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLine.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				order.OrigOrderNbr,
				order.InventoryID
			}).TopFirst;
			foreach (CustomerBoxesDAC customerBox in customerBoxes)
			{
				CSBox box4 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					customerBox.BoxID
				}).TopFirst;
				bool flag = box4 != null;
				if (flag)
				{
					boxes.Add(box4);
				}
			}
			IEnumerable<SOPackageDetailEx> firstTableItems = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
			SOPackageDetailEx lastPackage = (firstTableItems != null) ? firstTableItems.LastOrDefault<SOPackageDetailEx>() : null;
			List<INItemBoxEx> InItemBoxs = GraphHelper.RowCast<INItemBoxEx>(PXSelectBase<INItemBoxEx, PXViewOf<INItemBoxEx>.BasedOn<SelectFromBase<INItemBoxEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemBoxEx.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				order.InventoryID
			})).ToList<INItemBoxEx>();
			decimal itemWeight = this.GetItemWeight(order.InventoryID);
			decimal? qty = order.Qty;
			bool flag2 = customerBoxes == null || customerBoxes.Count <= 0;
			if (flag2)
			{
				foreach (INItemBoxEx inItemBox in InItemBoxs)
				{
					CSBox box2 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						inItemBox.BoxID
					}).TopFirst;
					boxes.Add(box2);
				}
			}
			CSBox currentBox = (from b in boxes
			orderby b.MaxWeight descending
			select b).FirstOrDefault<CSBox>();
			List<SOShipLineSplit> shipmentsSplit = PXSelectBase<SOShipLineSplit, PXViewOf<SOShipLineSplit>.BasedOn<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOShipLineSplit.shipmentNbr, IBqlString>.IsEqual<BqlField<SOShipment.shipmentNbr, IBqlString>.FromCurrent>>>.Config>.Select(base.Base, Array.Empty<object>()).FirstTableItems.ToList<SOShipLineSplit>();
			SOShipment shipment = base.Base.CurrentDocument.Current;
			SOOrder currentOrder = PXSelectBase<SOOrder, PXViewOf<SOOrder>.BasedOn<SelectFromBase<SOOrder, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOOrder.orderNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				order.OrigOrderNbr
			}).TopFirst;
			InventoryItem itemWarehouseDetails = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				shipLine.InventoryID
			}).TopFirst;
			SOOrderExt extension = PXCacheEx.GetExtension<SOOrderExt>(currentOrder);
			string storeNbr = (extension != null) ? extension.UsrTCStoreNumber : null;
			SOPackageDetailEx package;
			for (;;)
			{
				decimal? num = qty;
				decimal d = 0m;
				if (!(num.GetValueOrDefault() > d & num != null))
				{
					return;
				}
				foreach (CSBox box3 in from box in boxes
				orderby box.MaxWeight
				select box)
				{
					decimal? num2 = box3.MaxWeight / itemWeight;
					num = qty;
					bool flag3 = num2.GetValueOrDefault() >= num.GetValueOrDefault() & (num2 != null & num != null);
					if (flag3)
					{
						currentBox = box3;
						break;
					}
				}
				bool flag4 = lastPackage != null && PXCacheEx.GetExtension<SOPackageDetailExt>(lastPackage).UsrSepareteOrderNbr == null;
				if (flag4)
				{
					package = lastPackage;
				}
				else
				{
					package = new SOPackageDetailEx
					{
						Confirmed = new bool?(false),
						BoxID = currentBox.BoxID
					};
					package = base.Base.Packages.Insert(package);
				}
				decimal remainingWeightCapacity = (package.MaxWeight - this.GetTotalPackageWeight(package)).Value;
				bool flag5 = remainingWeightCapacity < itemWeight;
				if (flag5)
				{
					package = new SOPackageDetailEx
					{
						Confirmed = new bool?(false),
						BoxID = currentBox.BoxID
					};
					package = base.Base.Packages.Insert(package);
					remainingWeightCapacity = (package.MaxWeight - this.GetTotalPackageWeight(package)).Value;
				}
				base.Base.Actions.PressSave();
				int itemsToPack = (int)Math.Floor(Math.Min(qty.Value, remainingWeightCapacity / itemWeight));
				bool flag6 = itemsToPack <= 0;
				if (flag6)
				{
					break;
				}
				SelectedPackageContents newPackageContent = new SelectedPackageContents
				{
					ShipmentNbr = package.ShipmentNbr,
					PackageLineNbr = package.LineNbr,
					OrderNbr = shipLine.OrigOrderNbr,
					StoreNbr = storeNbr,
					InventoryID = order.InventoryID,
					PackedQty = new decimal?(itemsToPack),
					ShipmentSplitLineNbr = order.SplitLineNbr,
					DefaultIssueFrom = itemWarehouseDetails.DfltShipLocationID
				};
				this.SelectedPackageContentsView.Insert(newPackageContent);
				base.Base.Actions.PressSave();
				qty -= itemsToPack;
				num = qty;
				d = 0m;
				bool flag7 = num.GetValueOrDefault() == d & num != null;
				if (flag7)
				{
					return;
				}
			}
			bool flag8 = !this.HasSelectedPackageContents(package);
			if (flag8)
			{
				base.Base.Packages.Delete(package);
				base.Base.Actions.PressSave();
			}
			throw new PXException("Package generation could not assign any quantity for item {0}. Please verify the available box capacity for this shipment line.", new object[]
			{
				itemWarehouseDetails.InventoryCD
			});
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00007494 File Offset: 0x00005694
		protected void CreatePackagesByVolumeAndWeight(SOShipmentEntry sOShipmentEntry, SOShipLineSplit order, List<CustomerBoxesDAC> customerBoxes)
		{
			SOShipLine shipLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.origOrderNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLine.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				order.OrigOrderNbr,
				order.InventoryID
			}).TopFirst;
			SOShipment shipment = base.Base.CurrentDocument.Current;
			SOOrder currentOrder = PXSelectBase<SOOrder, PXViewOf<SOOrder>.BasedOn<SelectFromBase<SOOrder, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOOrder.orderNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				order.OrigOrderNbr
			}).TopFirst;
			SOOrderExt extension = PXCacheEx.GetExtension<SOOrderExt>(currentOrder);
			string storeNbr = (extension != null) ? extension.UsrTCStoreNumber : null;
			InventoryItem itemWarehouseDetails = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				shipLine.InventoryID
			}).TopFirst;
			List<CSBox> boxes = new List<CSBox>();
			foreach (CustomerBoxesDAC customerBox in customerBoxes)
			{
				CSBox box4 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					customerBox.BoxID
				}).TopFirst;
				bool flag = box4 != null;
				if (flag)
				{
					boxes.Add(box4);
				}
			}
			IEnumerable<SOPackageDetailEx> firstTableItems = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
			SOPackageDetailEx lastPackage = (firstTableItems != null) ? firstTableItems.LastOrDefault<SOPackageDetailEx>() : null;
			decimal itemVolume = this.GetItemVolume(order.InventoryID);
			decimal itemWeight = this.GetItemWeight(order.InventoryID);
			List<INItemBoxEx> InItemBoxs = GraphHelper.RowCast<INItemBoxEx>(PXSelectBase<INItemBoxEx, PXViewOf<INItemBoxEx>.BasedOn<SelectFromBase<INItemBoxEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemBoxEx.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				order.InventoryID
			})).ToList<INItemBoxEx>();
			decimal qty = order.Qty.GetValueOrDefault();
			bool flag2 = customerBoxes == null || customerBoxes.Count <= 0;
			if (flag2)
			{
				foreach (INItemBoxEx inItemBox in InItemBoxs)
				{
					CSBox box2 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						inItemBox.BoxID
					}).TopFirst;
					boxes.Add(box2);
				}
			}
			CSBox currentBox = (from b in boxes
			orderby b.MaxWeight descending, b.MaxVolume
			select b).FirstOrDefault<CSBox>();
			while (qty > 0m)
			{
				int maxQtyByWeight = 0;
				int maxQtyByVolume = 0;
				foreach (CSBox box3 in from box in boxes
				orderby box.MaxWeight, box.MaxVolume
				select box)
				{
					maxQtyByWeight = (int)(box3.MaxWeight / itemWeight).Value;
					maxQtyByVolume = (int)(box3.MaxVolume / itemVolume).Value;
					int maxQtyFit = Math.Min(maxQtyByWeight, maxQtyByVolume);
					bool flag3 = maxQtyFit >= qty;
					if (flag3)
					{
						currentBox = box3;
						break;
					}
				}
				SOPackageDetailEx package = (lastPackage != null && PXCacheEx.GetExtension<SOPackageDetailExt>(lastPackage).UsrSepareteOrderNbr == null) ? lastPackage : base.Base.Packages.Insert(new SOPackageDetailEx
				{
					BoxID = currentBox.BoxID
				});
				bool flag4 = maxQtyByVolume <= maxQtyByWeight;
				decimal remainingCapacity;
				if (flag4)
				{
					CSBox CrBox = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
					{
						package.BoxID
					}).TopFirst;
					remainingCapacity = CrBox.MaxVolume.Value - this.GetTotalPackageVolume(package);
					bool flag5 = remainingCapacity < itemVolume;
					if (flag5)
					{
						package = base.Base.Packages.Insert(new SOPackageDetailEx
						{
							BoxID = currentBox.BoxID
						});
						remainingCapacity = CrBox.MaxVolume.Value - this.GetTotalPackageVolume(package);
					}
				}
				else
				{
					remainingCapacity = package.MaxWeight.Value - this.GetTotalPackageWeight(package);
					bool flag6 = remainingCapacity < itemWeight;
					if (flag6)
					{
						package = base.Base.Packages.Insert(new SOPackageDetailEx
						{
							BoxID = currentBox.BoxID
						});
						remainingCapacity = package.MaxWeight.Value - this.GetTotalPackageWeight(package);
					}
				}
				int itemsToPack = (int)Math.Floor(Math.Min(qty, remainingCapacity / ((maxQtyByVolume <= maxQtyByWeight) ? itemVolume : itemWeight)));
				bool flag7 = itemsToPack <= 0;
				if (flag7)
				{
					bool flag8 = !this.HasSelectedPackageContents(package);
					if (flag8)
					{
						base.Base.Packages.Delete(package);
						base.Base.Actions.PressSave();
					}
					throw new PXException("Package generation could not assign any quantity for item {0}. Please verify the available box capacity for this shipment line.", new object[]
					{
						itemWarehouseDetails.InventoryCD
					});
				}
				SelectedPackageContents newPackageContent = new SelectedPackageContents
				{
					ShipmentNbr = package.ShipmentNbr,
					PackageLineNbr = package.LineNbr,
					OrderNbr = shipLine.OrigOrderNbr,
					StoreNbr = storeNbr,
					InventoryID = order.InventoryID,
					PackedQty = new decimal?(itemsToPack),
					ShipmentSplitLineNbr = order.SplitLineNbr,
					DefaultIssueFrom = itemWarehouseDetails.DfltShipLocationID
				};
				this.SelectedPackageContentsView.Insert(newPackageContent);
				base.Base.Actions.PressSave();
				qty -= itemsToPack;
				bool flag9 = qty == 0m;
				if (flag9)
				{
					break;
				}
			}
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00007AFC File Offset: 0x00005CFC
		protected void SetBoxNbrStr(List<SOPackageDetailEx> freshPackages)
		{
			IEnumerable<IGrouping<string, SOPackageDetailEx>> orderGroups = from p in freshPackages
			where PXCacheEx.GetExtension<SOPackageDetailExt>(p).UsrOrderNbr != null && PXCacheEx.GetExtension<SOPackageDetailExt>(p).UsrOrderNbr != "<SPLIT>" && PXCacheEx.GetExtension<SOPackageDetailExt>(p).UsrOrderNbr != string.Empty
			group p by PXCacheEx.GetExtension<SOPackageDetailExt>(p).UsrOrderNbr;
			foreach (IGrouping<string, SOPackageDetailEx> orderGroup in orderGroups)
			{
				int totalBoxes = orderGroup.Count<SOPackageDetailEx>();
				int boxNumber = 1;
				foreach (SOPackageDetailEx currentPackage in orderGroup)
				{
					SOPackageDetail packageRec = (SOPackageDetail)base.Base.Packages.Cache.Locate(currentPackage);
					string formattedValue = string.Format("{0} of {1}", boxNumber, totalBoxes);
					base.Base.Packages.Cache.SetValue<SOPackageDetailExt.usrSOBoxNbrstr>(packageRec, formattedValue);
					GraphHelper.MarkUpdated(base.Base.Packages.Cache, packageRec);
					boxNumber++;
				}
			}
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00007C48 File Offset: 0x00005E48
		private void UpdateOrderAndStoreInfo(SOPackageDetailEx row)
		{
			SOPackageDetailExt packageExt = PXCacheEx.GetExtension<SOPackageDetailExt>(row);
			List<SelectedPackageContents> packageContent = this.GetPackageContents(row).ToList<SelectedPackageContents>();
			SelectedPackageContents firstContent = packageContent.FirstOrDefault<SelectedPackageContents>();
			string firstOrderNbr = (firstContent != null) ? firstContent.OrderNbr : null;
			string firstStoreNbr = (firstContent != null) ? firstContent.StoreNbr : null;
			bool splitOrders = (from x in packageContent
			select x.OrderNbr).Distinct<string>().Count<string>() > 1;
			bool splitStores = (from x in packageContent
			select x.StoreNbr).Distinct<string>().Count<string>() > 1;
			packageExt.UsrOrderNbr = (splitOrders ? "<SPLIT>" : firstOrderNbr);
			packageExt.UsrStoreNbr = (splitStores ? "<SPLIT>" : firstStoreNbr);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00007D1C File Offset: 0x00005F1C
		private void UpdatePackageQuantities(SOPackageDetailEx row)
		{
			SOPackageDetailExExt packageExExt = PXCacheEx.GetExtension<SOPackageDetailExExt>(row);
			decimal estimatedQty = 0m;
			decimal contentQty = 0m;
			foreach (SelectedPackageContents item in this.GetPackageContents(row))
			{
				estimatedQty += item.PackedQty.GetValueOrDefault();
			}
			IEnumerable contents = this.GetPackageInventoryContents(row);
			foreach (object obj in contents)
			{
				PXResult<SOShipLineSplitPackage, InventoryItem> record = (PXResult<SOShipLineSplitPackage, InventoryItem>)obj;
				SOShipLineSplitPackage split = record;
				contentQty += split.PackedQty.GetValueOrDefault();
			}
			packageExExt.UsrEstPackageQuantity = new decimal?(estimatedQty);
			packageExExt.UsrContentPackageQuantity = new decimal?(contentQty);
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00007E28 File Offset: 0x00006028
		private void UpdatePackageWeight(PXCache cache, SOPackageDetailEx row)
		{
			SOPackageDetailExt packageExt = PXCacheEx.GetExtension<SOPackageDetailExt>(row);
			decimal weight = (packageExt != null && packageExt.UsrIsParentBox.GetValueOrDefault()) ? this.CalculateMasterPackageWeight(row) : this.CalculatePackageWeight(row);
			cache.SetValue<SOPackageDetail.weight>(row, weight);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00007E70 File Offset: 0x00006070
		private decimal CalculatePackageWeight(SOPackageDetailEx package)
		{
			decimal weight = 0m;
			IEnumerable contents = this.GetPackageInventoryContents(package);
			foreach (object obj in contents)
			{
				PXResult<SOShipLineSplitPackage, InventoryItem> record = (PXResult<SOShipLineSplitPackage, InventoryItem>)obj;
				SOShipLineSplitPackage split = record;
				InventoryItem item = record;
				weight += split.PackedQty.GetValueOrDefault() * item.BaseItemWeight.GetValueOrDefault();
			}
			return weight;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00007F18 File Offset: 0x00006118
		private decimal CalculateMasterPackageWeight(SOPackageDetailEx masterPackage)
		{
			decimal weight = 0m;
			SOPackageDetailExt masterExt = PXCacheEx.GetExtension<SOPackageDetailExt>(masterPackage);
			foreach (SOPackageDetailEx package in base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems)
			{
				SOPackageDetailExt childExt = PXCacheEx.GetExtension<SOPackageDetailExt>(package);
				bool flag = ((childExt != null) ? childExt.UsrSelectedParentBox : null) != ((masterExt != null) ? masterExt.UsrCartonNbr : null);
				if (!flag)
				{
					weight += this.CalculatePackageWeight(package);
				}
			}
			return weight;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00007FCC File Offset: 0x000061CC
		private IEnumerable<SelectedPackageContents> GetPackageContents(SOPackageDetailEx package)
		{
			return PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			}).FirstTableItems;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000800C File Offset: 0x0000620C
		private IEnumerable GetPackageInventoryContents(SOPackageDetailEx package)
		{
			return PXSelectBase<SOShipLineSplitPackage, PXViewOf<SOShipLineSplitPackage>.BasedOn<SelectFromBase<SOShipLineSplitPackage, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<InventoryItem>.On<BqlOperand<SOShipLineSplitPackage.inventoryID, IBqlInt>.IsEqual<InventoryItem.inventoryID>>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplitPackage.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLineSplitPackage.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			});
		}

		// Token: 0x04000046 RID: 70
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			1,
			0,
			0,
			1,
			1,
			0,
			1,
			1,
			0,
			1,
			1,
			0,
			0,
			1,
			1,
			0
		})]
		public FbqlSelect<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<BqlField<SOPackageDetailEx.shipmentNbr, IBqlString>.FromCurrent>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<BqlField<SOPackageDetailEx.lineNbr, IBqlInt>.FromCurrent>>>.Order<By<BqlField<SelectedPackageContents.defaultIssueFrom, IBqlInt>.Asc>>, SelectedPackageContents>.View SelectedPackageContentsView;

		// Token: 0x0200006E RID: 110
		// (Invoke) Token: 0x060001B8 RID: 440
		public delegate void CreateShipmentDelegate(CreateShipmentArgs args);
	}
}
