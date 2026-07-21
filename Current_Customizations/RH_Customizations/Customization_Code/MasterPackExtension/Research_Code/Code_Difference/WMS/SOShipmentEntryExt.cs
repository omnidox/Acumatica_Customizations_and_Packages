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
	// Token: 0x0200001A RID: 26
	public class SOShipmentEntryExt : PXGraphExtension<CarrierRatesExt, SOShipmentEntry>
	{
		// Token: 0x060000E4 RID: 228 RVA: 0x000022B4 File Offset: 0x000004B4
		public static bool IsActive()
		{
			return true;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000478C File Offset: 0x0000298C
		[PXOverride]
		public void Persist(SOShipmentEntryExt.PersistDelegate baseMethod)
		{
			try
			{
				baseMethod();
			}
			catch (PXLockViolationException ex)
			{
				bool flag = base.Base.Document.Current != null && ex.Table == typeof(SOShipment);
				if (flag)
				{
					SOShipment current = base.Base.Document.Current;
					SOShipment fresh = PXSelectBase<SOShipment, PXViewOf<SOShipment>.BasedOn<SelectFromBase<SOShipment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOShipment.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.ReadOnly.Config>.Select(base.Base, new object[]
					{
						current.ShipmentNbr
					}).TopFirst;
					bool flag2 = fresh != null;
					if (flag2)
					{
						object newTstamp = base.Base.Document.Cache.GetValue(fresh, "tstamp");
						base.Base.Document.Cache.SetValue(current, "tstamp", newTstamp);
						baseMethod();
						return;
					}
				}
				throw;
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00004870 File Offset: 0x00002A70
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

		// Token: 0x060000E7 RID: 231 RVA: 0x000048F4 File Offset: 0x00002AF4
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

		// Token: 0x060000E8 RID: 232 RVA: 0x000049AC File Offset: 0x00002BAC
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

		// Token: 0x060000E9 RID: 233 RVA: 0x00004BC0 File Offset: 0x00002DC0
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

		// Token: 0x060000EA RID: 234 RVA: 0x00004CE0 File Offset: 0x00002EE0
		protected void _(Events.RowInserted<SOPackageDetailEx> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				List<SOPackageDetailEx> freshPackages = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>();
				this.SetBoxNbrStr(freshPackages);
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00004D28 File Offset: 0x00002F28
		protected void _(Events.RowDeleted<SOPackageDetailEx> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				List<SOPackageDetailEx> freshPackages = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>();
				this.SetBoxNbrStr(freshPackages);
			}
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00004D70 File Offset: 0x00002F70
		protected void _(Events.RowInserted<SelectedPackageContents> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				this.RecalculatePackageByLineNbr(e.Row.PackageLineNbr);
			}
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00004DA0 File Offset: 0x00002FA0
		protected void _(Events.RowUpdated<SelectedPackageContents> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				this.RecalculatePackageByLineNbr(e.Row.PackageLineNbr);
				bool flag2;
				if (e.OldRow != null)
				{
					int? packageLineNbr = e.OldRow.PackageLineNbr;
					int? packageLineNbr2 = e.Row.PackageLineNbr;
					flag2 = !(packageLineNbr.GetValueOrDefault() == packageLineNbr2.GetValueOrDefault() & packageLineNbr != null == (packageLineNbr2 != null));
				}
				else
				{
					flag2 = false;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					this.RecalculatePackageByLineNbr(e.OldRow.PackageLineNbr);
				}
			}
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00004E30 File Offset: 0x00003030
		protected void _(Events.RowDeleted<SelectedPackageContents> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				this.RecalculatePackageByLineNbr(e.Row.PackageLineNbr);
			}
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00004E60 File Offset: 0x00003060
		protected void _(Events.RowInserted<SOShipLineSplitPackage> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				this.RecalculatePackageByLineNbr(e.Row.PackageLineNbr);
			}
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00004E90 File Offset: 0x00003090
		protected void _(Events.RowUpdated<SOShipLineSplitPackage> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				this.RecalculatePackageByLineNbr(e.Row.PackageLineNbr);
				bool flag2;
				if (e.OldRow != null)
				{
					int? packageLineNbr = e.OldRow.PackageLineNbr;
					int? packageLineNbr2 = e.Row.PackageLineNbr;
					flag2 = !(packageLineNbr.GetValueOrDefault() == packageLineNbr2.GetValueOrDefault() & packageLineNbr != null == (packageLineNbr2 != null));
				}
				else
				{
					flag2 = false;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					this.RecalculatePackageByLineNbr(e.OldRow.PackageLineNbr);
				}
			}
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00004F20 File Offset: 0x00003120
		protected void _(Events.RowDeleted<SOShipLineSplitPackage> e)
		{
			bool flag = e.Row == null;
			if (!flag)
			{
				this.RecalculatePackageByLineNbr(e.Row.PackageLineNbr);
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00004F50 File Offset: 0x00003150
		private void RecalculatePackageByLineNbr(int? packageLineNbr)
		{
			bool flag = packageLineNbr == null;
			if (!flag)
			{
				SOPackageDetailEx package = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.FirstOrDefault(delegate(SOPackageDetailEx p)
				{
					int? lineNbr = p.LineNbr;
					int? packageLineNbr2 = packageLineNbr;
					return lineNbr.GetValueOrDefault() == packageLineNbr2.GetValueOrDefault() & lineNbr != null == (packageLineNbr2 != null);
				});
				bool flag2 = package != null;
				if (flag2)
				{
					this.UpdateOrderAndStoreInfo(package);
					this.UpdatePackageQuantities(package);
					base.Base.Packages.Update(package);
				}
			}
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00004FD8 File Offset: 0x000031D8
		protected void _(Events.RowUpdated<SOPackageDetailEx> e)
		{
			bool flag = e == null;
			if (!flag)
			{
				SOPackageDetailExt extension = PXCacheEx.GetExtension<SOPackageDetailExt>(e.OldRow);
				string oldOrderNbr = (extension != null) ? extension.UsrOrderNbr : null;
				SOPackageDetailExt extension2 = PXCacheEx.GetExtension<SOPackageDetailExt>(e.Row);
				string currentOrderNbr = (extension2 != null) ? extension2.UsrOrderNbr : null;
				bool flag2 = oldOrderNbr != currentOrderNbr;
				if (flag2)
				{
					List<SOPackageDetailEx> freshPackages = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>();
					this.SetBoxNbrStr(freshPackages);
				}
				string oldParentBoxID = PXCacheEx.GetExtension<SOPackageDetailExt>(e.OldRow).UsrSelectedParentBox;
				string currentParentBox = PXCacheEx.GetExtension<SOPackageDetailExt>(e.Row).UsrSelectedParentBox;
				string parentPackageID = currentParentBox;
				bool flag3 = parentPackageID == null && oldParentBoxID == null;
				if (!flag3)
				{
					bool flag4 = parentPackageID != null;
					if (flag4)
					{
						IEnumerable<SOPackageDetailEx> view = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
						SOPackageDetailEx parentPackage = view.FirstOrDefault((SOPackageDetailEx x) => PXCache<SOPackageDetail>.GetExtension<SOPackageDetailExt>(x).UsrCartonNbr == parentPackageID);
						bool flag5 = parentPackage != null;
						if (flag5)
						{
							List<SOPackageDetailEx> childrenBoxes = (from x in view
							where PXCacheEx.GetExtension<SOPackageDetailExt>(x).UsrSelectedParentBox == parentPackageID
							select x).ToList<SOPackageDetailEx>();
							decimal? calculatedWeight = new decimal?(0m);
							decimal? calculatedCOD = new decimal?(0m);
							decimal? calculatedDeclaredValue = new decimal?(0m);
							foreach (SOPackageDetailEx child in childrenBoxes)
							{
								calculatedWeight += child.Weight.GetValueOrDefault();
								calculatedCOD += child.COD.GetValueOrDefault();
								calculatedDeclaredValue += child.DeclaredValue.GetValueOrDefault();
							}
							decimal? num = parentPackage.Weight;
							decimal? num2 = calculatedWeight;
							bool flag6;
							if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
							{
								num2 = parentPackage.COD;
								num = calculatedCOD;
								if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
								{
									num = parentPackage.DeclaredValue;
									num2 = calculatedDeclaredValue;
									flag6 = !(num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null));
									goto IL_302;
								}
							}
							flag6 = true;
							IL_302:
							bool flag7 = flag6;
							if (flag7)
							{
								e.Cache.SetValueExt<SOPackageDetail.weight>(parentPackage, calculatedWeight);
								e.Cache.SetValueExt<SOPackageDetail.cOD>(parentPackage, calculatedCOD);
								e.Cache.SetValueExt<SOPackageDetail.declaredValue>(parentPackage, calculatedDeclaredValue);
								base.Base.Packages.Update(parentPackage);
								base.Base.Packages.View.RequestRefresh();
							}
						}
					}
					bool flag8 = oldParentBoxID != null;
					if (flag8)
					{
						IEnumerable<SOPackageDetailEx> view2 = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
						SOPackageDetailEx parentPackage2 = view2.FirstOrDefault((SOPackageDetailEx x) => PXCache<SOPackageDetail>.GetExtension<SOPackageDetailExt>(x).UsrCartonNbr == oldParentBoxID);
						bool flag9 = parentPackage2 != null;
						if (flag9)
						{
							List<SOPackageDetailEx> childrenBoxes2 = (from x in view2
							where PXCacheEx.GetExtension<SOPackageDetailExt>(x).UsrSelectedParentBox == oldParentBoxID
							select x).ToList<SOPackageDetailEx>();
							decimal? calculatedWeight2 = new decimal?(0m);
							decimal? calculatedCOD2 = new decimal?(0m);
							decimal? calculatedDeclaredValue2 = new decimal?(0m);
							foreach (SOPackageDetailEx child2 in childrenBoxes2)
							{
								calculatedWeight2 += child2.Weight.GetValueOrDefault();
								calculatedCOD2 += child2.COD.GetValueOrDefault();
								calculatedDeclaredValue2 += child2.DeclaredValue.GetValueOrDefault();
							}
							decimal? num2 = parentPackage2.Weight;
							decimal? num = calculatedWeight2;
							bool flag10;
							if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
							{
								num = parentPackage2.COD;
								num2 = calculatedCOD2;
								if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
								{
									num2 = parentPackage2.DeclaredValue;
									num = calculatedDeclaredValue2;
									flag10 = !(num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null));
									goto IL_5A8;
								}
							}
							flag10 = true;
							IL_5A8:
							bool flag11 = flag10;
							if (flag11)
							{
								e.Cache.SetValueExt<SOPackageDetail.weight>(parentPackage2, calculatedWeight2);
								e.Cache.SetValueExt<SOPackageDetail.cOD>(parentPackage2, calculatedCOD2);
								e.Cache.SetValueExt<SOPackageDetail.declaredValue>(parentPackage2, calculatedDeclaredValue2);
								base.Base.Packages.Update(parentPackage2);
								base.Base.Packages.View.RequestRefresh();
							}
						}
					}
				}
			}
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000561C File Offset: 0x0000381C
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
							if (!(a == "V"))
							{
								if (a == "Q")
								{
									this.CreatePackagesByQtySeparately(base.Base, shipLine, order.OrderNbr, orderPackages);
								}
							}
							else
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
							if (!(a2 == "V"))
							{
								if (a2 == "Q")
								{
									this.CreatePackagesByQty(base.Base, shipLine, customerBoxes);
								}
							}
							else
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
				this.SetBoxNbrStr(base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems.ToList<SOPackageDetailEx>());
			}
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00005B34 File Offset: 0x00003D34
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
					if (!(a == "V"))
					{
						if (a == "Q")
						{
							var boxesWithQty = (from x in boxes.Select(delegate(CSBox box)
							{
								CSBox box9 = box;
								INItemBoxEx initemBoxEx = InItemBoxs.FirstOrDefault((INItemBoxEx ib) => ib.BoxID == box.BoxID);
								return new
								{
									Box = box9,
									Qty = ((initemBoxEx != null) ? initemBoxEx.Qty : null).GetValueOrDefault()
								};
							})
							where x.Qty > 0m
							select x).ToList();
							var <>f__AnonymousType = (from x in boxesWithQty
							orderby x.Qty descending
							select x).FirstOrDefault();
							selectedBox = ((<>f__AnonymousType != null) ? <>f__AnonymousType.Box : null);
							foreach (var b in from x in boxesWithQty
							orderby x.Qty
							select x)
							{
								bool flag4 = b.Qty >= orderQty;
								if (flag4)
								{
									selectedBox = b.Box;
									break;
								}
							}
						}
					}
					else
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
							bool flag5 = maxQtyFit >= orderQty;
							if (flag5)
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
						bool flag6 = num.GetValueOrDefault() >= d & num != null;
						if (flag6)
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
				bool flag7 = optimalBoxWeight == null || optimalBoxVolumeAndWeight == null;
				if (!flag7)
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
						bool flag8 = currentPackage == null;
						if (flag8)
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
						bool flag9 = item.PackageOption == "W";
						decimal? num;
						if (flag9)
						{
							int maxQtyByWeight2 = (int)((currentPackage.MaxWeight.Value - currentPackage.Weight) / itemWeight).Value;
							decimal remainingCapacity = 0m;
							remainingCapacity = (currentPackage.MaxWeight - currentPackage.Weight).Value;
							bool flag10 = remainingCapacity < itemWeight;
							if (flag10)
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
									bool flag11 = num.GetValueOrDefault() >= d & num != null;
									if (flag11)
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
						bool flag12 = item.PackageOption == "V";
						if (flag12)
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
							bool flag13 = maxQtyByVolume2 < maxQtyByWeight3 && maxQtyByVolume2 > 0;
							if (flag13)
							{
								itemBox = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
								{
									currentPackage.BoxID
								}).TopFirst;
								remainingCapacity2 = itemBox.MaxVolume.Value - volume;
								bool flag14 = remainingCapacity2 < itemVolume;
								if (flag14)
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
										bool flag15 = maxQtyFit2 >= orderQty;
										if (flag15)
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
							bool flag16 = maxQtyByVolume2 >= maxQtyByWeight3 && maxQtyByWeight3 > 0;
							if (flag16)
							{
								remainingCapacity2 = (currentPackage.MaxWeight - currentPackage.Weight).Value;
								bool flag17 = remainingCapacity2 < itemWeight;
								if (flag17)
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
										bool flag18 = num.GetValueOrDefault() >= d & num != null;
										if (flag18)
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
							bool flag19 = maxQtyByVolume2 <= 0 || maxQtyByWeight3 <= 0;
							if (flag19)
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
						bool flag20 = item.PackageOption == "Q";
						if (flag20)
						{
							INItemBoxEx itemBoxDef = InItemBoxs.FirstOrDefault((INItemBoxEx ib) => ib.BoxID == currentPackage.BoxID);
							decimal maxQtyForBox = (itemBoxDef != null) ? itemBoxDef.Qty.GetValueOrDefault() : 0m;
							int currentQtyInBox = (itemWeight > 0m) ? ((int)(currentPackage.Weight / itemWeight).Value) : 0;
							int remainingQtyCapacity = (int)maxQtyForBox - currentQtyInBox;
							bool flag21 = remainingQtyCapacity <= 0;
							if (flag21)
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
								remainingQtyCapacity = (int)maxQtyForBox;
								bool flag22 = remainingQtyCapacity <= 0;
								if (flag22)
								{
									remainingQtyCapacity = (int)orderLine.Qty.Value;
								}
							}
							itemsToPack = (int)Math.Min(orderLine.Qty.Value, remainingQtyCapacity);
							currentPackage.Weight += itemsToPack * itemWeight;
							volume += itemsToPack * itemVolume;
						}
						orderLine.Qty -= itemsToPack;
						orderQty -= itemsToPack;
						num = currentPackage.Weight;
						num2 = optimalBoxWeight.MaxWeight;
						bool flag23;
						if (!(num.GetValueOrDefault() >= num2.GetValueOrDefault() & (num != null & num2 != null)))
						{
							decimal d2 = volume;
							num2 = optimalBoxVolumeAndWeight.MaxVolume;
							flag23 = (d2 >= num2.GetValueOrDefault() & num2 != null);
						}
						else
						{
							flag23 = true;
						}
						bool flag24 = flag23;
						if (flag24)
						{
							currentPackage = null;
						}
					}
				}
			}
			return orderPackages;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000714C File Offset: 0x0000534C
		protected decimal GetOrderWeight(List<SOLine> orderLines)
		{
			return orderLines.Sum((SOLine orderLine) => this.GetItemWeight(orderLine.InventoryID) * orderLine.Qty).Value;
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00007178 File Offset: 0x00005378
		protected decimal GetOrderVolume(List<SOLine> orderLines)
		{
			return orderLines.Sum((SOLine orderLine) => this.GetItemVolume(orderLine.InventoryID) * orderLine.Qty).Value;
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x000071A4 File Offset: 0x000053A4
		protected int GetOrderQty(List<SOLine> orderLines)
		{
			return (int)orderLines.Sum((SOLine orderLine) => orderLine.Qty).Value;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x000071E8 File Offset: 0x000053E8
		protected decimal GetItemWeight(int? inventoryID)
		{
			InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				inventoryID
			}).TopFirst;
			return ((item != null) ? item.BaseItemWeight : null).GetValueOrDefault();
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00007238 File Offset: 0x00005438
		protected decimal GetItemVolume(int? inventoryID)
		{
			InventoryItem item = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				inventoryID
			}).TopFirst;
			return ((item != null) ? item.BaseItemVolume : null).GetValueOrDefault();
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00007288 File Offset: 0x00005488
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

		// Token: 0x060000FC RID: 252 RVA: 0x00007340 File Offset: 0x00005540
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

		// Token: 0x060000FD RID: 253 RVA: 0x000073F8 File Offset: 0x000055F8
		protected bool HasSelectedPackageContents(SOPackageDetailEx package)
		{
			bool flag = package == null || package.ShipmentNbr == null || package.LineNbr == null;
			return !flag && PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			}).TopFirst != null;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00007464 File Offset: 0x00005664
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

		// Token: 0x060000FF RID: 255 RVA: 0x00007538 File Offset: 0x00005738
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

		// Token: 0x06000100 RID: 256 RVA: 0x00007794 File Offset: 0x00005994
		protected void CreatePackagesByQtySeparately(SOShipmentEntry sOShipmentEntry, SOShipLineSplit soShipLine, string orderNbr, List<SOPackageDetailEx> packages)
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
			SOOrderExt extension = PXCacheEx.GetExtension<SOOrderExt>(order);
			string storeNbr = (extension != null) ? extension.UsrTCStoreNumber : null;
			InventoryItem itemWarehouseDetails = PXSelectBase<InventoryItem, PXViewOf<InventoryItem>.BasedOn<SelectFromBase<InventoryItem, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<InventoryItem.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				shipLine.InventoryID
			}).TopFirst;
			foreach (SOPackageDetailEx package in packages)
			{
				PXCacheEx.GetExtension<SOPackageDetailExt>(package).UsrSepareteOrderNbr = orderNbr;
				INItemBoxEx itemBox = PXSelectBase<INItemBoxEx, PXViewOf<INItemBoxEx>.BasedOn<SelectFromBase<INItemBoxEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<INItemBoxEx.inventoryID, Equal<P.AsInt>>>>>.And<BqlOperand<INItemBoxEx.boxID, IBqlString>.IsEqual<P.AsString>>>>.Config>.Select(sOShipmentEntry, new object[]
				{
					soShipLine.InventoryID,
					package.BoxID
				}).TopFirst;
				decimal maxQtyForBox = ((itemBox != null) ? itemBox.Qty : null).GetValueOrDefault();
				bool flag = maxQtyForBox <= 0m;
				if (!flag)
				{
					int currentQtyInBox = (itemWeight > 0m) ? ((int)(this.GetTotalPackageWeight(package) / itemWeight)) : 0;
					int remainingCapacity = (int)maxQtyForBox - currentQtyInBox;
					bool flag2 = remainingCapacity <= 0;
					if (!flag2)
					{
						int itemsToPack = (int)Math.Min(qty, remainingCapacity);
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
						sOShipmentEntry.Actions.PressSave();
						qty -= itemsToPack;
						bool flag3 = qty <= 0m;
						if (flag3)
						{
							break;
						}
					}
				}
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00007A44 File Offset: 0x00005C44
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

		// Token: 0x06000102 RID: 258 RVA: 0x00007D64 File Offset: 0x00005F64
		protected void CreatePackagesByQty(SOShipmentEntry sOShipmentEntry, SOShipLineSplit order, List<CustomerBoxesDAC> customerBoxes)
		{
			List<CSBox> boxes = new List<CSBox>();
			SOShipLine shipLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.origOrderNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLine.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(sOShipmentEntry, new object[]
			{
				order.OrigOrderNbr,
				order.InventoryID
			}).TopFirst;
			foreach (CustomerBoxesDAC customerBox in customerBoxes)
			{
				CSBox box3 = PXSelectBase<CSBox, PXViewOf<CSBox>.BasedOn<SelectFromBase<CSBox, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<CSBox.boxID, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					customerBox.BoxID
				}).TopFirst;
				bool flag = box3 != null;
				if (flag)
				{
					boxes.Add(box3);
				}
			}
			IEnumerable<SOPackageDetailEx> firstTableItems = base.Base.Packages.Select(Array.Empty<object>()).FirstTableItems;
			SOPackageDetailEx lastPackage = (firstTableItems != null) ? firstTableItems.LastOrDefault<SOPackageDetailEx>() : null;
			List<INItemBoxEx> InItemBoxs = GraphHelper.RowCast<INItemBoxEx>(PXSelectBase<INItemBoxEx, PXViewOf<INItemBoxEx>.BasedOn<SelectFromBase<INItemBoxEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<INItemBoxEx.inventoryID, IBqlInt>.IsEqual<P.AsInt>>>.Config>.Select(base.Base, new object[]
			{
				order.InventoryID
			})).ToList<INItemBoxEx>();
			decimal itemWeight = this.GetItemWeight(order.InventoryID);
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
					bool flag3 = box2 != null;
					if (flag3)
					{
						boxes.Add(box2);
					}
				}
			}
			var boxesWithQty = (from x in boxes.Select(delegate(CSBox box)
			{
				CSBox box4 = box;
				INItemBoxEx initemBoxEx2 = InItemBoxs.FirstOrDefault((INItemBoxEx ib) => ib.BoxID == box.BoxID);
				return new
				{
					Box = box4,
					MaxQty = ((initemBoxEx2 != null) ? initemBoxEx2.Qty : null).GetValueOrDefault()
				};
			})
			where x.MaxQty > 0m
			select x).ToList();
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
			bool flag4 = boxesWithQty.Count == 0;
			if (flag4)
			{
				throw new PXException("No box quantity configuration found for item {0}. Please define boxes in Item Warehouse Details.", new object[]
				{
					itemWarehouseDetails.InventoryCD
				});
			}
			var currentBoxObj = (from b in boxesWithQty
			orderby b.MaxQty descending
			select b).FirstOrDefault();
			while (qty > 0m)
			{
				foreach (var b2 in from x in boxesWithQty
				orderby x.MaxQty
				select x)
				{
					bool flag5 = b2.MaxQty >= qty;
					if (flag5)
					{
						currentBoxObj = b2;
						break;
					}
				}
				SOPackageDetailEx package = null;
				bool flag6 = lastPackage != null && PXCacheEx.GetExtension<SOPackageDetailExt>(lastPackage).UsrSepareteOrderNbr == null;
				if (flag6)
				{
					package = lastPackage;
				}
				else
				{
					package = new SOPackageDetailEx
					{
						Confirmed = new bool?(false),
						BoxID = currentBoxObj.Box.BoxID
					};
					package = base.Base.Packages.Insert(package);
				}
				INItemBoxEx initemBoxEx = InItemBoxs.FirstOrDefault((INItemBoxEx ib) => ib.BoxID == package.BoxID);
				decimal maxQtyForPackageBox = ((initemBoxEx != null) ? initemBoxEx.Qty : null).GetValueOrDefault();
				bool flag7 = maxQtyForPackageBox <= 0m;
				if (flag7)
				{
					maxQtyForPackageBox = currentBoxObj.MaxQty;
				}
				int currentQtyInBox = (itemWeight > 0m) ? ((int)Math.Floor(this.GetTotalPackageWeight(package) / itemWeight)) : 0;
				int remainingQtyCapacity = (int)maxQtyForPackageBox - currentQtyInBox;
				bool flag8 = remainingQtyCapacity <= 0;
				if (flag8)
				{
					package = new SOPackageDetailEx
					{
						Confirmed = new bool?(false),
						BoxID = currentBoxObj.Box.BoxID
					};
					package = base.Base.Packages.Insert(package);
					remainingQtyCapacity = (int)currentBoxObj.MaxQty;
				}
				base.Base.Actions.PressSave();
				int itemsToPack = (int)Math.Min(qty, remainingQtyCapacity);
				bool flag9 = itemsToPack <= 0;
				if (flag9)
				{
					bool flag10 = !this.HasSelectedPackageContents(package);
					if (flag10)
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
				lastPackage = package;
				bool flag11 = qty <= 0m;
				if (flag11)
				{
					break;
				}
			}
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00008380 File Offset: 0x00006580
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

		// Token: 0x06000104 RID: 260 RVA: 0x00008980 File Offset: 0x00006B80
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

		// Token: 0x06000105 RID: 261 RVA: 0x00008FE8 File Offset: 0x000071E8
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
					SOPackageDetailExt ext = PXCacheEx.GetExtension<SOPackageDetailExt>(packageRec);
					bool flag = ext.UsrSOBoxNbrStr != formattedValue;
					if (flag)
					{
						base.Base.Packages.Cache.SetValue<SOPackageDetailExt.usrSOBoxNbrstr>(packageRec, formattedValue);
						GraphHelper.MarkUpdated(base.Base.Packages.Cache, packageRec);
					}
					boxNumber++;
				}
			}
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00009158 File Offset: 0x00007358
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

		// Token: 0x06000107 RID: 263 RVA: 0x0000922C File Offset: 0x0000742C
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

		// Token: 0x06000108 RID: 264 RVA: 0x00009338 File Offset: 0x00007538
		private void UpdatePackageWeight(PXCache cache, SOPackageDetailEx row)
		{
			SOPackageDetailExt packageExt = PXCacheEx.GetExtension<SOPackageDetailExt>(row);
			decimal weight = (packageExt != null && packageExt.UsrIsParentBox.GetValueOrDefault()) ? this.CalculateMasterPackageWeight(row) : this.CalculatePackageWeight(row);
			cache.SetValue<SOPackageDetail.weight>(row, weight);
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00009380 File Offset: 0x00007580
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

		// Token: 0x0600010A RID: 266 RVA: 0x00009428 File Offset: 0x00007628
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

		// Token: 0x0600010B RID: 267 RVA: 0x000094DC File Offset: 0x000076DC
		private IEnumerable<SelectedPackageContents> GetPackageContents(SOPackageDetailEx package)
		{
			return PXSelectBase<SelectedPackageContents, PXViewOf<SelectedPackageContents>.BasedOn<SelectFromBase<SelectedPackageContents, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SelectedPackageContents.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SelectedPackageContents.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			}).FirstTableItems;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000951C File Offset: 0x0000771C
		private IEnumerable GetPackageInventoryContents(SOPackageDetailEx package)
		{
			return PXSelectBase<SOShipLineSplitPackage, PXViewOf<SOShipLineSplitPackage>.BasedOn<SelectFromBase<SOShipLineSplitPackage, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<InventoryItem>.On<BqlOperand<SOShipLineSplitPackage.inventoryID, IBqlInt>.IsEqual<InventoryItem.inventoryID>>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplitPackage.shipmentNbr, Equal<P.AsString>>>>>.And<BqlOperand<SOShipLineSplitPackage.packageLineNbr, IBqlInt>.IsEqual<P.AsInt>>>>.Config>.Select(base.Base, new object[]
			{
				package.ShipmentNbr,
				package.LineNbr
			});
		}

		// Token: 0x0400004A RID: 74
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

		// Token: 0x02000070 RID: 112
		// (Invoke) Token: 0x060001CF RID: 463
		public delegate void PersistDelegate();

		// Token: 0x02000071 RID: 113
		// (Invoke) Token: 0x060001D3 RID: 467
		public delegate void CreateShipmentDelegate(CreateShipmentArgs args);
	}
}
