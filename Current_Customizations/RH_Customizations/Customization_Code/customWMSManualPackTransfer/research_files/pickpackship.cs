using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.Common.Attributes;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.Extensions;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using PX.Objects.SM;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;
using PX.Objects.SO.Unassigned;
using PX.SM;

namespace PX.Objects.SO.WMS
{
	// Token: 0x020002A5 RID: 677
	public class PickPackShip : WarehouseManagementSystem<PickPackShip, PickPackShip.Host>
	{
		// Token: 0x17001149 RID: 4425
		// (get) Token: 0x06002ECA RID: 11978 RVA: 0x0009E238 File Offset: 0x0009C438
		public new decimal BaseQty
		{
			get
			{
				return INUnitAttribute.ConvertToBase(base.Graph.Transactions.Cache, base.InventoryID, base.UOM, base.Qty.GetValueOrDefault(), INPrecision.NOROUND);
			}
		}

		// Token: 0x1700114A RID: 4426
		// (get) Token: 0x06002ECB RID: 11979 RVA: 0x0009E278 File Offset: 0x0009C478
		public override bool ExplicitConfirmation
		{
			get
			{
				return this.Setup.Current.ExplicitLineConfirmation.GetValueOrDefault();
			}
		}

		// Token: 0x1700114B RID: 4427
		// (get) Token: 0x06002ECC RID: 11980 RVA: 0x0009E29D File Offset: 0x0009C49D
		public override bool DocumentIsEditable
		{
			get
			{
				return base.DocumentIsEditable && !this.DocumentIsConfirmed;
			}
		}

		// Token: 0x1700114C RID: 4428
		// (get) Token: 0x06002ECD RID: 11981 RVA: 0x0009E2B4 File Offset: 0x0009C4B4
		public virtual bool DocumentIsConfirmed
		{
			get
			{
				SOShipment shipment = this.Shipment;
				return shipment != null && shipment.Confirmed.GetValueOrDefault();
			}
		}

		// Token: 0x1700114D RID: 4429
		// (get) Token: 0x06002ECE RID: 11982 RVA: 0x0009E2DC File Offset: 0x0009C4DC
		protected override bool UseQtyCorrection
		{
			get
			{
				return !this.Setup.Current.UseDefaultQty.GetValueOrDefault();
			}
		}

		// Token: 0x1700114E RID: 4430
		// (get) Token: 0x06002ECF RID: 11983 RVA: 0x0009E304 File Offset: 0x0009C504
		protected override bool CanOverrideQty
		{
			get
			{
				return base.CanOverrideQty || (this.DocumentIsEditable && this.DefaultLotSerial && base.LotSerialTrack.IsTrackedSerial) || (this.DocumentIsEditable && this.IsTransfer && base.LotSerialTrack.IsTrackedSerial && base.LotSerialTrack.IsEnterable);
			}
		}

		// Token: 0x1700114F RID: 4431
		// (get) Token: 0x06002ED0 RID: 11984 RVA: 0x0009E36C File Offset: 0x0009C56C
		public virtual bool DefaultLocation
		{
			get
			{
				return PXSetupBase<PickPackShip.UserSetup, PickPackShip.Host, ScanHeader, SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>>.For(base.Base).DefaultLocationFromShipment.GetValueOrDefault();
			}
		}

		// Token: 0x17001150 RID: 4432
		// (get) Token: 0x06002ED1 RID: 11985 RVA: 0x0009E394 File Offset: 0x0009C594
		public virtual bool DefaultLotSerial
		{
			get
			{
				return PXSetupBase<PickPackShip.UserSetup, PickPackShip.Host, ScanHeader, SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>>.For(base.Base).DefaultLotSerialFromShipment.GetValueOrDefault();
			}
		}

		// Token: 0x17001151 RID: 4433
		// (get) Token: 0x06002ED2 RID: 11986 RVA: 0x0009E3BC File Offset: 0x0009C5BC
		public virtual bool HasPick
		{
			get
			{
				return this.Setup.Current.ShowPickTab.GetValueOrDefault();
			}
		}

		// Token: 0x17001152 RID: 4434
		// (get) Token: 0x06002ED3 RID: 11987 RVA: 0x0009E3E4 File Offset: 0x0009C5E4
		public virtual bool HasPack
		{
			get
			{
				return this.Setup.Current.ShowPackTab.GetValueOrDefault();
			}
		}

		// Token: 0x17001153 RID: 4435
		// (get) Token: 0x06002ED4 RID: 11988 RVA: 0x0009E409 File Offset: 0x0009C609
		public virtual bool CannotConfirmPartialShipments
		{
			get
			{
				return this.Setup.Current.ShortShipmentConfirmation == "F";
			}
		}

		// Token: 0x17001154 RID: 4436
		// (get) Token: 0x06002ED5 RID: 11989 RVA: 0x0009E428 File Offset: 0x0009C628
		public virtual bool PromptLocationForEveryLine
		{
			get
			{
				return this.Setup.Current.RequestLocationForEachItem.GetValueOrDefault();
			}
		}

		// Token: 0x06002ED6 RID: 11990 RVA: 0x0009E450 File Offset: 0x0009C650
		[PXButton]
		[PXUIField(DisplayName = "View Order")]
		protected virtual IEnumerable viewOrder(PXAdapter adapter)
		{
			SOShipLineSplit soshipLineSplit = (SOShipLineSplit)base.Graph.Caches<SOShipLineSplit>().Current;
			if (soshipLineSplit == null)
			{
				return adapter.Get();
			}
			PXGraph graph = base.Graph;
			object[] currents = new SOShipLineSplit[]
			{
				soshipLineSplit
			};
			SOShipLine soshipLine = PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLine.shipmentNbr, Equal<BqlField<SOShipLineSplit.shipmentNbr, IBqlString>.FromCurrent>>>>>.And<BqlOperand<SOShipLine.lineNbr, IBqlInt>.IsEqual<BqlField<SOShipLineSplit.lineNbr, IBqlInt>.FromCurrent>>>>.Config>.SelectSingleBound(graph, currents, Array.Empty<object>());
			if (soshipLine == null)
			{
				return adapter.Get();
			}
			SOOrderEntry soorderEntry = PXGraph.CreateInstance<SOOrderEntry>();
			soorderEntry.Document.Current = soorderEntry.Document.Search<SOOrder.orderType, SOOrder.orderNbr>(soshipLine.OrigOrderType, soshipLine.OrigOrderNbr, Array.Empty<object>());
			throw new PXRedirectRequiredException(soorderEntry, true, "ViewOrder")
			{
				Mode = PXBaseRedirectException.WindowMode.NewWindow
			};
		}

		// Token: 0x06002ED7 RID: 11991 RVA: 0x0009E4F0 File Offset: 0x0009C6F0
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);
			if (e.Row == null)
			{
				return;
			}
			if (this.DocumentIsConfirmed)
			{
				PXCache<SOShipLineSplit> cache = base.Graph.Caches<SOShipLineSplit>();
				cache.SetAllEditPermissions(false);
				cache.AdjustUI(null).ForAllFields(delegate(PXUIFieldAttribute a)
				{
					a.Enabled = false;
				});
			}
			if (string.IsNullOrEmpty(base.RefNbr))
			{
				base.Graph.Document.Current = null;
				return;
			}
			base.Graph.Document.Current = base.Base.Document.Search<SOShipment.shipmentNbr>(base.RefNbr, Array.Empty<object>());
		}

		// Token: 0x06002ED8 RID: 11992 RVA: 0x0009E5A4 File Offset: 0x0009C7A4
		protected virtual void _(Events.RowUpdated<SOPickPackShipUserSetup> e)
		{
			e.Row.IsOverridden = new bool?(!e.Row.SameAs(this.Setup.Current));
		}

		// Token: 0x06002ED9 RID: 11993 RVA: 0x0009E5CF File Offset: 0x0009C7CF
		protected virtual void _(Events.RowInserted<SOPickPackShipUserSetup> e)
		{
			e.Row.IsOverridden = new bool?(!e.Row.SameAs(this.Setup.Current));
		}

		// Token: 0x06002EDA RID: 11994 RVA: 0x0009E5FC File Offset: 0x0009C7FC
		protected virtual void _(Events.FieldSelecting<SOShipLineSplit, SOShipLineSplit.lotSerialNbr> e)
		{
			if (e.Row != null && e.Row.IsUnassigned.GetValueOrDefault())
			{
				e.ReturnValue = "<UNASSIGNED>";
			}
		}

		// Token: 0x06002EDB RID: 11995 RVA: 0x0009E634 File Offset: 0x0009C834
		protected virtual void _(Events.RowSelected<SOShipLineSplit> e)
		{
			if (e.Row != null && e.Row.IsUnassigned.GetValueOrDefault())
			{
				e.Cache.Adjust(e.Row).ForAllFields(delegate(PXUIFieldAttribute a)
				{
					a.Enabled = false;
				});
			}
		}

		// Token: 0x06002EDC RID: 11996 RVA: 0x0009E697 File Offset: 0x0009C897
		[BorrowedNote(typeof(SOShipment), typeof(SOShipmentEntry))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e)
		{
		}

		// Token: 0x06002EDD RID: 11997 RVA: 0x0009E699 File Offset: 0x0009C899
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Shipment Nbr.", Enabled = false)]
		[PXSelector(typeof(SOShipment.shipmentNbr))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.refNbr> e)
		{
		}

		// Token: 0x06002EDE RID: 11998 RVA: 0x0009E69B File Offset: 0x0009C89B
		[PXMergeAttributes]
		[PXFormula(typeof(BqlOperand<InventoryMultiplicator.increase, IBqlShort>.When<BqlOperand<ScanHeader.mode, IBqlString>.IsEqual<PickPackShip.ReturnMode.value>>.Else<InventoryMultiplicator.decrease>))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.inventoryMultiplicator> e)
		{
		}

		// Token: 0x06002EDF RID: 11999 RVA: 0x0009E69D File Offset: 0x0009C89D
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), "Visible", true)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.lineNbr> e)
		{
		}

		// Token: 0x06002EE0 RID: 12000 RVA: 0x0009E69F File Offset: 0x0009C89F
		[PXCustomizeBaseAttribute(typeof(SOShipLotSerialNbrAttribute), "ForceDisable", true)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.lotSerialNbr> e)
		{
		}

		// Token: 0x06002EE1 RID: 12001 RVA: 0x0009E6A1 File Offset: 0x0009C8A1
		[PXCustomizeBaseAttribute(typeof(SiteAttribute), "Enabled", false)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.siteID> e)
		{
		}

		// Token: 0x06002EE2 RID: 12002 RVA: 0x0009E6A3 File Offset: 0x0009C8A3
		[PXCustomizeBaseAttribute(typeof(SOLocationAvailAttribute), "Enabled", false)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.locationID> e)
		{
		}

		// Token: 0x06002EE3 RID: 12003 RVA: 0x0009E6A5 File Offset: 0x0009C8A5
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), "Enabled", false)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.qty> e)
		{
		}

		// Token: 0x06002EE4 RID: 12004 RVA: 0x0009E6A7 File Offset: 0x0009C8A7
		[PXMergeAttributes]
		[PXSelector(typeof(SearchFor<SOOrder.orderNbr>.Where<BqlOperand<SOOrder.orderNbr, IBqlString>.IsEqual<BqlField<SOShipLine.origOrderType, IBqlString>.FromCurrent>>))]
		protected virtual void _(Events.CacheAttached<SOShipLine.origOrderNbr> e)
		{
		}

		// Token: 0x06002EE5 RID: 12005 RVA: 0x0009E6AC File Offset: 0x0009C8AC
		protected override ScanMode<PickPackShip> GetDefaultMode()
		{
			UserPreferences userPreferences = PXSelectBase<UserPreferences, PXViewOf<UserPreferences>.BasedOn<SelectFromBase<UserPreferences, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<UserPreferences.userID, IBqlGuid>.IsEqual<BqlField<AccessInfo.userID, IBqlGuid>.FromCurrent>>>.Config>.Select(base.Base, Array.Empty<object>());
			DefaultPickPackShipModeByUser defaultPickPackShipModeByUser = (userPreferences != null) ? userPreferences.GetExtension<DefaultPickPackShipModeByUser>() : null;
			PickPackShip.PickMode result = base.ScanModes.OfType<PickPackShip.PickMode>().FirstOrDefault<PickPackShip.PickMode>();
			PickPackShip.PackMode result2 = base.ScanModes.OfType<PickPackShip.PackMode>().FirstOrDefault<PickPackShip.PackMode>();
			PickPackShip.ShipMode result3 = base.ScanModes.OfType<PickPackShip.ShipMode>().FirstOrDefault<PickPackShip.ShipMode>();
			PickPackShip.ReturnMode result4 = base.ScanModes.OfType<PickPackShip.ReturnMode>().FirstOrDefault<PickPackShip.ReturnMode>();
			if (((defaultPickPackShipModeByUser != null) ? defaultPickPackShipModeByUser.PPSMode : null) == "PICK" && this.Setup.Current.ShowPickTab.GetValueOrDefault())
			{
				return result;
			}
			if (((defaultPickPackShipModeByUser != null) ? defaultPickPackShipModeByUser.PPSMode : null) == "PACK" && this.Setup.Current.ShowPackTab.GetValueOrDefault())
			{
				return result2;
			}
			if (((defaultPickPackShipModeByUser != null) ? defaultPickPackShipModeByUser.PPSMode : null) == "SHIP" && this.Setup.Current.ShowShipTab.GetValueOrDefault())
			{
				return result3;
			}
			if (((defaultPickPackShipModeByUser != null) ? defaultPickPackShipModeByUser.PPSMode : null) == "CRTN" && this.Setup.Current.ShowReturningTab.GetValueOrDefault())
			{
				return result4;
			}
			if (this.Setup.Current.ShowPickTab.GetValueOrDefault())
			{
				return result;
			}
			if (this.Setup.Current.ShowPackTab.GetValueOrDefault())
			{
				return result2;
			}
			if (this.Setup.Current.ShowShipTab.GetValueOrDefault())
			{
				return result3;
			}
			if (!this.Setup.Current.ShowReturningTab.GetValueOrDefault())
			{
				return base.GetDefaultMode();
			}
			return result4;
		}

		// Token: 0x06002EE6 RID: 12006 RVA: 0x0009E879 File Offset: 0x0009CA79
		protected override IEnumerable<ScanMode<PickPackShip>> CreateScanModes()
		{
			yield return new PickPackShip.PickMode();
			yield return new PickPackShip.PackMode();
			yield return new PickPackShip.ShipMode();
			yield return new PickPackShip.ReturnMode();
			yield break;
		}

		// Token: 0x17001155 RID: 4437
		// (get) Token: 0x06002EE7 RID: 12007 RVA: 0x0009E882 File Offset: 0x0009CA82
		public virtual SOShipment Shipment
		{
			get
			{
				return PrimaryKeyOf<SOShipment>.By<SOShipment.shipmentNbr>.Find(base.Base, base.Base.Document.Current, PKFindOptions.None);
			}
		}

		// Token: 0x17001156 RID: 4438
		// (get) Token: 0x06002EE8 RID: 12008 RVA: 0x0009E8A0 File Offset: 0x0009CAA0
		public virtual bool IsTransfer
		{
			get
			{
				SOShipment shipment = this.Shipment;
				return ((shipment != null) ? shipment.ShipmentType : null) == "T";
			}
		}

		// Token: 0x06002EE9 RID: 12009 RVA: 0x0009E8C0 File Offset: 0x0009CAC0
		public virtual IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> GetSplits(string shipmentNbr, bool includeUnassigned = false, Func<SOShipLineSplit, bool> processedSeparator = null)
		{
			IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> enumerable = PXSelectBase<SOShipLineSplit, PXViewOf<SOShipLineSplit>.BasedOn<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>>, FbqlJoins.Inner<INLocation>.On<BqlOperand<SOShipLineSplit.locationID, IBqlInt>.IsEqual<INLocation.locationID>>>>.Where<BqlOperand<SOShipLineSplit.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
			{
				shipmentNbr
			}).AsEnumerable<PXResult<SOShipLineSplit>>().Cast<PXResult<SOShipLineSplit, SOShipLine, INLocation>>();
			IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> enumerable2;
			if (includeUnassigned)
			{
				IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> second = from PXResult<SOShipLineSplit, SOShipLine, INLocation> r in PXSelectBase<SOShipLineSplit, PXViewOf<SOShipLineSplit>.BasedOn<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>>, FbqlJoins.Inner<INLocation>.On<BqlOperand<SOShipLineSplit.locationID, IBqlInt>.IsEqual<INLocation.locationID>>>>.Where<BqlOperand<SOShipLineSplit.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
				{
					shipmentNbr
				}).AsEnumerable<PXResult<SOShipLineSplit>>()
				select new PXResult<SOShipLineSplit, SOShipLine, INLocation>(PickPackShip.<GetSplits>g__MakeAssigned|55_7(r), r, r);
				enumerable2 = enumerable.Concat(second);
			}
			else
			{
				enumerable2 = enumerable;
			}
			IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> source;
			IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> source2;
			if (processedSeparator == null)
			{
				IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> enumerable3 = Array.Empty<PXResult<SOShipLineSplit, SOShipLine, INLocation>>();
				IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> enumerable4 = enumerable2;
				source = enumerable4;
				source2 = enumerable3;
			}
			else
			{
				ValueTuple<IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>>, IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>>> valueTuple = enumerable2.DisuniteBy((PXResult<SOShipLineSplit, SOShipLine, INLocation> s) => processedSeparator(s.GetItem<SOShipLineSplit>()));
				source2 = valueTuple.Item1;
				source = valueTuple.Item2;
			}
			List<PXResult<SOShipLineSplit, SOShipLine, INLocation>> list = new List<PXResult<SOShipLineSplit, SOShipLine, INLocation>>();
			list.AddRange(source.OrderBy(delegate(PXResult<SOShipLineSplit, SOShipLine, INLocation> r)
			{
				if (!(this.Setup.Current.ShipmentLocationOrdering == "PICK"))
				{
					return r.GetItem<INLocation>().PathPriority;
				}
				short? pickPriority = r.GetItem<INLocation>().PickPriority;
				if (pickPriority == null)
				{
					return null;
				}
				return new int?((int)pickPriority.GetValueOrDefault());
			}).ThenBy(delegate(PXResult<SOShipLineSplit, SOShipLine, INLocation> r)
			{
				bool? isUnassigned = r.GetItem<SOShipLineSplit>().IsUnassigned;
				bool flag = false;
				return isUnassigned.GetValueOrDefault() == flag & isUnassigned != null;
			}).ThenBy((PXResult<SOShipLineSplit, SOShipLine, INLocation> r) => r.GetItem<SOShipLineSplit>().InventoryID).ThenBy((PXResult<SOShipLineSplit, SOShipLine, INLocation> r) => r.GetItem<SOShipLineSplit>().LotSerialNbr));
			list.AddRange(source2.OrderByDescending(delegate(PXResult<SOShipLineSplit, SOShipLine, INLocation> r)
			{
				if (!(this.Setup.Current.ShipmentLocationOrdering == "PICK"))
				{
					return r.GetItem<INLocation>().PathPriority;
				}
				short? pickPriority = r.GetItem<INLocation>().PickPriority;
				if (pickPriority == null)
				{
					return null;
				}
				return new int?((int)pickPriority.GetValueOrDefault());
			}).ThenByDescending((PXResult<SOShipLineSplit, SOShipLine, INLocation> r) => r.GetItem<SOShipLineSplit>().InventoryID).ThenByDescending((PXResult<SOShipLineSplit, SOShipLine, INLocation> r) => r.GetItem<SOShipLineSplit>().LotSerialNbr));
			return list;
		}

		// Token: 0x06002EEA RID: 12010 RVA: 0x0009EA80 File Offset: 0x0009CC80
		public virtual bool IsLocationMissing(PXSelectBase<SOShipLineSplit> splitView, INLocation location, out Validation error)
		{
			if (splitView.SelectMain(Array.Empty<object>()).All(delegate(SOShipLineSplit t)
			{
				int? locationID = t.LocationID;
				int? locationID2 = location.LocationID;
				return !(locationID.GetValueOrDefault() == locationID2.GetValueOrDefault() & locationID != null == (locationID2 != null));
			}))
			{
				error = Validation.Fail("{0} location not listed in shipment.", new object[]
				{
					location.LocationCD
				});
				return true;
			}
			error = Validation.Ok;
			return false;
		}

		// Token: 0x06002EEB RID: 12011 RVA: 0x0009EAEC File Offset: 0x0009CCEC
		public virtual bool IsItemMissing(PXSelectBase<SOShipLineSplit> splitView, PXResult<INItemXRef, InventoryItem> item, out Validation error)
		{
			INItemXRef initemXRef;
			InventoryItem inventoryItem2;
			item.Deconstruct(out initemXRef, out inventoryItem2);
			InventoryItem inventoryItem = inventoryItem2;
			if (splitView.SelectMain(Array.Empty<object>()).All(delegate(SOShipLineSplit t)
			{
				int? inventoryID = t.InventoryID;
				int? inventoryID2 = inventoryItem.InventoryID;
				return !(inventoryID.GetValueOrDefault() == inventoryID2.GetValueOrDefault() & inventoryID != null == (inventoryID2 != null));
			}))
			{
				error = Validation.Fail("{0} item not listed in shipment.", new object[]
				{
					inventoryItem.InventoryCD
				});
				return true;
			}
			error = Validation.Ok;
			return false;
		}

		// Token: 0x06002EEC RID: 12012 RVA: 0x0009EB60 File Offset: 0x0009CD60
		public virtual bool IsLotSerialMissing(PXSelectBase<SOShipLineSplit> splitView, string lotSerialNbr, out Validation error)
		{
			if (!base.LotSerialTrack.IsEnterable && splitView.SelectMain(Array.Empty<object>()).All((SOShipLineSplit t) => !string.Equals(t.LotSerialNbr, lotSerialNbr, StringComparison.OrdinalIgnoreCase)))
			{
				error = Validation.Fail("{0} lot or serial number not listed in shipment.", new object[]
				{
					lotSerialNbr
				});
				return true;
			}
			error = Validation.Ok;
			return false;
		}

		// Token: 0x06002EED RID: 12013 RVA: 0x0009EBD8 File Offset: 0x0009CDD8
		public void EnsureAssignedSplitEditing(SOShipLineSplit split)
		{
			if (split.IsUnassigned.GetValueOrDefault())
			{
				throw new InvalidOperationException("Unassigned splits should not be edited directly by WMS screen");
			}
		}

		// Token: 0x06002EEE RID: 12014 RVA: 0x0009EC00 File Offset: 0x0009CE00
		[Obsolete]
		public virtual string GetCommandOrShipmentOnlyPrompt()
		{
			return base.Get<PickPackShip.CommandOrShipmentOnlyState.Logic>().GetPromptForCommandOrShipmentOnly();
		}

		// Token: 0x06002EEF RID: 12015 RVA: 0x0009EC10 File Offset: 0x0009CE10
		public virtual bool HasNonStockLinesWithEmptyLocation(SOShipment shipment, out Validation error)
		{
			if (PXSelectBase<SOShipLine, PXViewOf<SOShipLine>.BasedOn<SelectFromBase<SOShipLine, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<InventoryItem>.On<SOShipLine.FK.InventoryItem>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<InventoryItem.stkItem, Equal<False>>>>, And<BqlOperand<InventoryItem.kitItem, IBqlBool>.IsEqual<False>>>, And<BqlOperand<SOShipLine.locationID, IBqlInt>.IsNull>>>.And<BqlOperand<SOShipLine.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>>.ReadOnly.Config>.Select(this, new object[]
			{
				shipment.ShipmentNbr
			}) != null)
			{
				error = Validation.Fail("The {0} shipment cannot be processed on the Pick, Pack, and Ship (SO302020) form because it contains a non-stock item with an empty location.", new object[]
				{
					shipment.ShipmentNbr
				});
				return true;
			}
			error = Validation.Ok;
			return false;
		}

		// Token: 0x06002EF0 RID: 12016 RVA: 0x0009EC6C File Offset: 0x0009CE6C
		public virtual bool HasIncompleteLinesBy<TQtyField>() where TQtyField : class, IBqlField, IImplement<IBqlDecimal>
		{
			PXGraph graph = this;
			object[] currents = new SOShipment[]
			{
				this.Shipment
			};
			return PXSelectBase<SOLine, PXViewOf<SOLine>.BasedOn<SelectFromBase<SOLine, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOOrder>.On<SOLine.FK.Order>>, FbqlJoins.Inner<SOShipLine>.On<SOShipLine.FK.OrderLine>>, FbqlJoins.Inner<SOShipLineSplit>.On<SOShipLineSplit.FK.ShipmentLine>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<KeysRelation<CompositeKey<Field<SOShipLine.shipmentType>.IsRelatedTo<SOShipment.shipmentType>, Field<SOShipLine.shipmentNbr>.IsRelatedTo<SOShipment.shipmentNbr>>.WithTablesOf<SOShipment, SOShipLine>, SOShipment, SOShipLine>.SameAsCurrent>, And<BqlOperand<Mult<SOShipLineSplit.qty, BqlOperand<SOLine.completeQtyMin, IBqlDecimal>.Divide<decimal100>>, IBqlDecimal>.IsGreater<TQtyField>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOOrder.shipComplete, Equal<SOShipComplete.shipComplete>>>>>.Or<BqlOperand<SOLine.shipComplete, IBqlString>.IsEqual<SOShipComplete.shipComplete>>>>>.Config>.SelectMultiBound(graph, currents, Array.Empty<object>()).Any<PXResult<SOLine>>();
		}

		// Token: 0x06002EF1 RID: 12017 RVA: 0x0009ECA0 File Offset: 0x0009CEA0
		protected override void LogScan(ScanHeader headerBefore, ScanHeader headerAfter)
		{
			base.LogScan(headerBefore, headerAfter);
			if (!headerBefore.Barcode.StartsWith("@"))
			{
				this.UpdateWorkLogOnLogScan(base.Graph.WorkLogExt, this.Info.Current.MessageType == "ERR");
				if (base.Graph.Caches<SOShipmentProcessedByUser>().IsDirty)
				{
					base.Graph.WorkLogExt.PersistWorkLog();
				}
			}
		}

		// Token: 0x06002EF2 RID: 12018 RVA: 0x0009ED14 File Offset: 0x0009CF14
		protected virtual void UpdateWorkLogOnLogScan(SOShipmentEntry.WorkLog workLogger, bool isError)
		{
			if (this.Shipment == null)
			{
				return;
			}
			string jobType;
			if (base.CurrentMode is PickPackShip.PackMode)
			{
				jobType = (this.HasPick ? "PACK" : "PPCK");
			}
			else
			{
				if (!(base.CurrentMode is PickPackShip.PickMode) && !(base.CurrentMode is PickPackShip.ReturnMode))
				{
					return;
				}
				jobType = "PICK";
			}
			workLogger.LogScanFor(this.Shipment.ShipmentNbr, new Guid?(base.Graph.Accessinfo.UserID), jobType, isError);
		}

		// Token: 0x06002EF3 RID: 12019 RVA: 0x0009ED9C File Offset: 0x0009CF9C
		public virtual void InjectLocationDeactivationOnDefaultLocationOption(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState locationState)
		{
			locationState.Intercept.IsStateActive.ByConjoin((PickPackShip basis) => !basis.DefaultLocation, false, null);
		}

		// Token: 0x06002EF4 RID: 12020 RVA: 0x0009EDE4 File Offset: 0x0009CFE4
		public virtual void InjectLotSerialDeactivationOnDefaultLotSerialOption(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState lsState, bool isEntranceAllowed)
		{
			lsState.Intercept.IsStateActive.ByConjoin((PickPackShip basis) => !basis.DefaultLotSerial || basis.Remove.GetValueOrDefault() || (isEntranceAllowed && basis.LotSerialTrack.IsEnterable), false, null);
			lsState.Intercept.IsStateActive.ByConjoin((PickPackShip basis) => basis.SelectedLotSerialClass.With((INLotSerClass it) => it.LotSerAssign == "U").Implies(!basis.IsTransfer), false, null);
		}

		// Token: 0x06002EF5 RID: 12021 RVA: 0x0009EE60 File Offset: 0x0009D060
		public virtual void InjectLocationSkippingOnPromptLocationForEveryLineOption(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState locationState)
		{
			locationState.Intercept.IsStateSkippable.ByDisjoin((PickPackShip basis) => !basis.PromptLocationForEveryLine && basis.LocationID != null, false, null);
		}

		// Token: 0x06002EF6 RID: 12022 RVA: 0x0009EEA8 File Offset: 0x0009D0A8
		public virtual void InjectItemAbsenceHandlingByLocation(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState inventoryState)
		{
			inventoryState.Intercept.HandleAbsence.ByAppend((PickPackShip basis, string barcode) => basis.TryProcessBy<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication) ? AbsenceHandling.Done : AbsenceHandling.Skipped, null);
		}

		// Token: 0x06002EF7 RID: 12023 RVA: 0x0009EEF0 File Offset: 0x0009D0F0
		public virtual void InjectLocationPresenceValidation(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState locationState, Func<PickPackShip, PXSelectBase<SOShipLineSplit>> viewSelector)
		{
			locationState.Intercept.Validate.ByAppend(delegate(PickPackShip basis, INLocation location)
			{
				Validation result;
				if (!basis.IsLocationMissing(viewSelector(basis), location, out result))
				{
					return Validation.Ok;
				}
				return result;
			}, null);
		}

		// Token: 0x06002EF8 RID: 12024 RVA: 0x0009EF30 File Offset: 0x0009D130
		public virtual void InjectItemPresenceValidation(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState itemState, Func<PickPackShip, PXSelectBase<SOShipLineSplit>> viewSelector)
		{
			itemState.Intercept.Validate.ByAppend(delegate(PickPackShip basis, PXResult<INItemXRef, InventoryItem> item)
			{
				Validation result;
				if (!basis.IsItemMissing(viewSelector(basis), item, out result))
				{
					return Validation.Ok;
				}
				return result;
			}, null);
		}

		// Token: 0x06002EF9 RID: 12025 RVA: 0x0009EF70 File Offset: 0x0009D170
		public virtual void InjectLotSerialPresenceValidation(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState lotSerailState, Func<PickPackShip, PXSelectBase<SOShipLineSplit>> viewSelector)
		{
			lotSerailState.Intercept.Validate.ByAppend(delegate(PickPackShip basis, string lotSerialNbr)
			{
				Validation result;
				if (!basis.IsLotSerialMissing(viewSelector(basis), lotSerialNbr, out result))
				{
					return Validation.Ok;
				}
				return result;
			}, null);
		}

		// Token: 0x06002EFB RID: 12027 RVA: 0x0009EFB8 File Offset: 0x0009D1B8
		[CompilerGenerated]
		internal static SOShipLineSplit <GetSplits>g__MakeAssigned|55_7(SOShipLineSplit unassignedSplit)
		{
			return PropertyTransfer.Transfer<SOShipLineSplit, SOShipLineSplit>(unassignedSplit, new SOShipLineSplit(), null);
		}

		// Token: 0x04001859 RID: 6233
		[Nullable(new byte[]
		{
			0,
			0,
			0,
			0,
			1,
			1,
			0,
			1,
			1
		})]
		public PXSetupOptional<SOPickPackShipSetup, Where<BqlOperand<SOPickPackShipSetup.branchID, IBqlInt>.IsEqual<BqlField<AccessInfo.branchID, IBqlInt>.FromCurrent>>> Setup;

		// Token: 0x0400185A RID: 6234
		public PXAction<ScanHeader> ViewOrder;

		// Token: 0x02002F4E RID: 12110
		public sealed class PackMode : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanMode
		{
			// Token: 0x1700979C RID: 38812
			// (get) Token: 0x0601EA29 RID: 125481 RVA: 0x006F55A0 File Offset: 0x006F37A0
			public override string Code
			{
				get
				{
					return "PACK";
				}
			}

			// Token: 0x1700979D RID: 38813
			// (get) Token: 0x0601EA2A RID: 125482 RVA: 0x006F55A7 File Offset: 0x006F37A7
			public override string Description
			{
				get
				{
					return "Pack";
				}
			}

			// Token: 0x0601EA2B RID: 125483 RVA: 0x006F55B0 File Offset: 0x006F37B0
			protected override bool IsModeActive()
			{
				return base.Basis.Setup.Current.ShowPackTab.GetValueOrDefault();
			}

			// Token: 0x0601EA2C RID: 125484 RVA: 0x006F55DA File Offset: 0x006F37DA
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickPackShip.PackMode.ShipmentState();
				yield return new PickPackShip.PackMode.BoxState();
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState
				{
					AlternateType = new INPrimaryAlternateType?(INPrimaryAlternateType.CPN),
					IsForIssue = true,
					IsForTransfer = base.Basis.IsTransfer,
					SuppressModuleItemStatusCheck = true
				};
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState();
				yield return new PickPackShip.PackMode.ConfirmState();
				yield return new PickPackShip.CommandOrShipmentOnlyState();
				yield return new PickPackShip.PackMode.BoxConfirming.StartState();
				yield return new PickPackShip.PackMode.BoxConfirming.WeightState();
				yield return new PickPackShip.PackMode.BoxConfirming.DimensionsState();
				yield return new PickPackShip.PackMode.BoxConfirming.CompleteState();
				if (!base.Basis.HasPick)
				{
					yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState();
					yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState
					{
						IsForIssue = true,
						IsForTransfer = base.Basis.IsTransfer
					};
				}
				yield break;
			}

			// Token: 0x0601EA2D RID: 125485 RVA: 0x006F55EC File Offset: 0x006F37EC
			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				IEnumerable<ScanTransition<PickPackShip>> first = base.StateFlow((ScanStateFlow<PickPackShip>.IFrom flow) => flow.ForkBy((PickPackShip basis) => basis.HasPick).PositiveBranch((ScanStateFlow<PickPackShip>.IFrom separatePicking) => separatePicking.From<PickPackShip.PackMode.ShipmentState>().NextTo<PickPackShip.PackMode.BoxState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(null)).NegativeBranch((ScanStateFlow<PickPackShip>.IFrom packOnly) => packOnly.From<PickPackShip.PackMode.ShipmentState>().NextTo<PickPackShip.PackMode.BoxState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState>(null)));
				IEnumerable<ScanTransition<PickPackShip>> second = base.StateFlow((ScanStateFlow<PickPackShip>.IFrom flow) => flow.From<PickPackShip.PackMode.BoxConfirming.StartState>().NextTo<PickPackShip.PackMode.BoxConfirming.WeightState>(null).NextTo<PickPackShip.PackMode.BoxConfirming.DimensionsState>(null).NextTo<PickPackShip.PackMode.BoxConfirming.CompleteState>(null));
				return first.Concat(second);
			}

			// Token: 0x0601EA2E RID: 125486 RVA: 0x006F564A File Offset: 0x006F384A
			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new PickPackShip.PackMode.RemoveCommand();
				yield return new BarcodeQtySupport<PickPackShip, PickPackShip.Host>.SetQtyCommand();
				yield return new PickPackShip.PackMode.ConfirmPackageCommand();
				yield return new PickPackShip.ConfirmShipmentCommand();
				yield break;
			}

			// Token: 0x0601EA2F RID: 125487 RVA: 0x006F5653 File Offset: 0x006F3853
			protected override IEnumerable<ScanQuestion<PickPackShip>> CreateQuestions()
			{
				yield return new PickPackShip.PackMode.BoxConfirming.WeightState.SkipQuestion();
				yield return new PickPackShip.PackMode.BoxConfirming.WeightState.SkipScalesQuestion();
				yield return new PickPackShip.PackMode.BoxConfirming.DimensionsState.SkipQuestion();
				yield break;
			}

			// Token: 0x0601EA30 RID: 125488 RVA: 0x006F565C File Offset: 0x006F385C
			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects()
			{
				return AllWMSRedirects.CreateFor<PickPackShip>();
			}

			// Token: 0x0601EA31 RID: 125489 RVA: 0x006F5664 File Offset: 0x006F3864
			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				base.Clear<PickPackShip.PackMode.ShipmentState>(fullReset && !base.Basis.IsWithinReset);
				base.Clear<PickPackShip.PackMode.BoxState>(fullReset);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(fullReset);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(true);
				base.Clear<PickPackShip.PackMode.BoxConfirming.WeightState>(true);
				base.Clear<PickPackShip.PackMode.BoxConfirming.DimensionsState>(true);
				if (fullReset)
				{
					base.Get<PickPackShip.PackMode.Logic>().PackageLineNbrUI = null;
				}
				if (!base.Basis.HasPick)
				{
					base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(fullReset || base.Basis.PromptLocationForEveryLine);
					base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState>(true);
				}
			}

			// Token: 0x0400EDC5 RID: 60869
			public const string Value = "PACK";

			// Token: 0x0200D2FB RID: 54011
			[Nullable(new byte[]
			{
				0,
				1,
				1,
				0
			})]
			public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PackMode.value>
			{
				// Token: 0x0602FD65 RID: 195941 RVA: 0x008E53F6 File Offset: 0x008E35F6
				public value() : base("PACK")
				{
				}
			}

			// Token: 0x0200D2FC RID: 54012
			public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x1700AF0F RID: 44815
				// (get) Token: 0x0602FD66 RID: 195942 RVA: 0x008E5403 File Offset: 0x008E3603
				public PackScanHeader PackHeader
				{
					get
					{
						return base.Basis.Header.Get<PackScanHeader>() ?? new PackScanHeader();
					}
				}

				// Token: 0x1700AF10 RID: 44816
				// (get) Token: 0x0602FD67 RID: 195943 RVA: 0x008E5420 File Offset: 0x008E3620
				public ValueSetter<ScanHeader>.Ext<PackScanHeader> PackSetter
				{
					get
					{
						return base.Basis.HeaderSetter.With<PackScanHeader>();
					}
				}

				// Token: 0x1700AF11 RID: 44817
				// (get) Token: 0x0602FD68 RID: 195944 RVA: 0x008E5440 File Offset: 0x008E3640
				// (set) Token: 0x0602FD69 RID: 195945 RVA: 0x008E5450 File Offset: 0x008E3650
				public int? PackageLineNbr
				{
					get
					{
						return this.PackHeader.PackageLineNbr;
					}
					set
					{
						this.PackSetter.Set<int?>((PackScanHeader h) => h.PackageLineNbr, value);
					}
				}

				// Token: 0x1700AF12 RID: 44818
				// (get) Token: 0x0602FD6A RID: 195946 RVA: 0x008E54A5 File Offset: 0x008E36A5
				// (set) Token: 0x0602FD6B RID: 195947 RVA: 0x008E54B4 File Offset: 0x008E36B4
				public int? PackageLineNbrUI
				{
					get
					{
						return this.PackHeader.PackageLineNbrUI;
					}
					set
					{
						this.PackSetter.Set<int?>((PackScanHeader h) => h.PackageLineNbrUI, value);
					}
				}

				// Token: 0x0602FD6C RID: 195948 RVA: 0x008E550C File Offset: 0x008E370C
				protected virtual IEnumerable pickedForPack()
				{
					PXDelegateResult pxdelegateResult = new PXDelegateResult();
					pxdelegateResult.IsResultSorted = true;
					pxdelegateResult.AddRange(base.Basis.GetSplits(base.Basis.RefNbr, true, delegate(SOShipLineSplit s)
					{
						decimal? packedQty = s.PackedQty;
						decimal? qty = s.Qty;
						return packedQty.GetValueOrDefault() >= qty.GetValueOrDefault() & (packedQty != null & qty != null);
					}));
					return pxdelegateResult;
				}

				// Token: 0x0602FD6D RID: 195949 RVA: 0x008E5564 File Offset: 0x008E3764
				protected virtual IEnumerable packed()
				{
					if (base.Basis.Header != null)
					{
						return from <>h__TransparentIdentifier0 in (from link in base.Graph.PackageDetailExt.PackageDetailSplit.SelectMain(new object[]
						{
							base.Basis.RefNbr,
							this.PackageLineNbrUI
						})
						from split in this.PickedForPack.Select(Array.Empty<object>()).Cast<PXResult<SOShipLineSplit, SOShipLine>>()
						select new
						{
							link,
							split
						}).Where(delegate(<>h__TransparentIdentifier0)
						{
							if (<>h__TransparentIdentifier0.split.ShipmentNbr == <>h__TransparentIdentifier0.link.ShipmentNbr)
							{
								int? num = <>h__TransparentIdentifier0.split.LineNbr;
								int? num2 = <>h__TransparentIdentifier0.link.ShipmentLineNbr;
								if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
								{
									num2 = <>h__TransparentIdentifier0.split.SplitLineNbr;
									num = <>h__TransparentIdentifier0.link.ShipmentSplitLineNbr;
									return num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null);
								}
							}
							return false;
						})
						select <>h__TransparentIdentifier0.split;
					}
					return Enumerable.Empty<PXResult<SOShipLineSplit, SOShipLine>>();
				}

				// Token: 0x0602FD6E RID: 195950 RVA: 0x008E5638 File Offset: 0x008E3838
				[PXButton]
				[PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewPack(PXAdapter adapter)
				{
					this.PackageLineNbrUI = null;
					return adapter.Get();
				}

				// Token: 0x0602FD6F RID: 195951 RVA: 0x008E565C File Offset: 0x008E385C
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					if (e.Row == null)
					{
						return;
					}
					new PXCache[]
					{
						base.Base.Packages.Cache,
						base.Base.PackageDetailExt.PackageDetailSplit.Cache
					}.Modify(delegate(PXCache c)
					{
						c.AllowInsert = (c.AllowUpdate = (c.AllowDelete = !base.Basis.DocumentIsConfirmed));
					}).Consume<PXCache>();
					this.ReviewPack.SetVisible(base.Base.IsMobile && e.Row.Mode == "PACK");
				}

				// Token: 0x0602FD70 RID: 195952 RVA: 0x008E56EC File Offset: 0x008E38EC
				public virtual void InjectExpireDateForPackDeactivationOnAlreadyEnteredLot(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState expireDateState)
				{
					expireDateState.Intercept.IsStateActive.ByConjoin(delegate(PickPackShip basis)
					{
						INLotSerClass selectedLotSerialClass = basis.SelectedLotSerialClass;
						return ((selectedLotSerialClass != null) ? selectedLotSerialClass.LotSerAssign : null) == "U" && basis.Get<PickPackShip.PackMode.Logic>().PickedForPack.SelectMain(Array.Empty<object>()).Any(delegate(SOShipLineSplit t)
						{
							if (t.IsUnassigned.GetValueOrDefault())
							{
								return true;
							}
							if (t.LotSerialNbr == basis.LotSerialNbr)
							{
								decimal? packedQty = t.PackedQty;
								decimal d = 0m;
								return packedQty.GetValueOrDefault() == d & packedQty != null;
							}
							return false;
						});
					}, false, null);
				}

				// Token: 0x0602FD71 RID: 195953 RVA: 0x008E5734 File Offset: 0x008E3934
				public virtual void InjectItemAbsenceHandlingByBox(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState itemState)
				{
					itemState.Intercept.HandleAbsence.ByAppend(delegate(PickPackShip basis, string barcode)
					{
						bool? flag = basis.Get<PickPackShip.PackMode.Logic>().TryAutoConfirmCurrentPackageAndLoadNext(barcode);
						bool flag2 = false;
						return (flag.GetValueOrDefault() == flag2 & flag != null) ? AbsenceHandling.Skipped : AbsenceHandling.Done;
					}, null);
				}

				// Token: 0x0602FD72 RID: 195954 RVA: 0x008E577C File Offset: 0x008E397C
				public virtual void InjectItemPromptForPackageConfirm(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState itemState)
				{
					itemState.Intercept.StatePrompt.ByOverride((PickPackShip basis, Func<string> base_StatePrompt) => basis.Get<PickPackShip.PackMode.Logic>().With(delegate(PickPackShip.PackMode.Logic mode)
					{
						if (basis.Remove.GetValueOrDefault() || !mode.CanConfirmPackage)
						{
							return null;
						}
						if (!basis.HasActive<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>())
						{
							return "Confirm the package, or scan the next item.";
						}
						return "Confirm the package, or scan the next item or the next location.";
					}) ?? base_StatePrompt(), null);
				}

				/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.DecorateScanState(PX.BarcodeProcessing.ScanState{`0})" />
				// Token: 0x0602FD73 RID: 195955 RVA: 0x008E57C4 File Offset: 0x008E39C4
				[PXOverride]
				public ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
				{
					ScanState<PickPackShip> scanState = base_DecorateScanState(original);
					if (scanState.ModeCode == "PACK")
					{
						WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState locationState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState;
						if (locationState != null)
						{
							base.Basis.InjectLocationDeactivationOnDefaultLocationOption(locationState);
							base.Basis.InjectLocationSkippingOnPromptLocationForEveryLineOption(locationState);
							base.Basis.InjectLocationPresenceValidation(locationState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.PackMode.Logic.<DecorateScanState>g__viewSelector|21_0));
						}
						else
						{
							WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState inventoryItemState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState;
							if (inventoryItemState != null)
							{
								if (!base.Basis.HasPick)
								{
									base.Basis.InjectItemAbsenceHandlingByLocation(inventoryItemState);
								}
								this.InjectItemPromptForPackageConfirm(inventoryItemState);
								this.InjectItemAbsenceHandlingByBox(inventoryItemState);
								base.Basis.InjectItemPresenceValidation(inventoryItemState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.PackMode.Logic.<DecorateScanState>g__viewSelector|21_0));
							}
							else
							{
								WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState lotSerialState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState;
								if (lotSerialState != null)
								{
									base.Basis.InjectLotSerialPresenceValidation(lotSerialState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.PackMode.Logic.<DecorateScanState>g__viewSelector|21_0));
									base.Basis.InjectLotSerialDeactivationOnDefaultLotSerialOption(lotSerialState, !base.Basis.HasPick);
								}
								else
								{
									WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState expireDateState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState;
									if (expireDateState != null)
									{
										this.InjectExpireDateForPackDeactivationOnAlreadyEnteredLot(expireDateState);
									}
								}
							}
						}
					}
					return scanState;
				}

				/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.OnBeforeFullClear" />
				// Token: 0x0602FD74 RID: 195956 RVA: 0x008E58CC File Offset: 0x008E3ACC
				[PXOverride]
				public void OnBeforeFullClear(Action base_OnBeforeFullClear)
				{
					base_OnBeforeFullClear();
					if (base.Basis.CurrentMode is PickPackShip.PackMode && base.Basis.RefNbr != null && base.Graph.WorkLogExt.SuspendFor(base.Basis.RefNbr, new Guid?(base.Graph.Accessinfo.UserID), base.Basis.HasPick ? "PACK" : "PPCK"))
					{
						base.Graph.WorkLogExt.PersistWorkLog();
					}
				}

				// Token: 0x0602FD75 RID: 195957 RVA: 0x008E5959 File Offset: 0x008E3B59
				[Obsolete]
				public virtual string GetCommandOrShipmentOnlyPrompt(Func<string> base_GetCommandOrShipmentOnlyPrompt)
				{
					return base_GetCommandOrShipmentOnlyPrompt();
				}

				// Token: 0x0602FD76 RID: 195958 RVA: 0x008E5961 File Offset: 0x008E3B61
				public virtual bool ShowPackTab(ScanHeader row)
				{
					return base.Basis.HasPack && row.Mode == "PACK";
				}

				// Token: 0x1700AF13 RID: 44819
				// (get) Token: 0x0602FD77 RID: 195959 RVA: 0x008E5984 File Offset: 0x008E3B84
				public virtual bool CanPack
				{
					get
					{
						if (!base.Basis.HasPick)
						{
							return this.PickedForPack.SelectMain(Array.Empty<object>()).Any(delegate(SOShipLineSplit s)
							{
								decimal? packedQty = s.PackedQty;
								decimal? qty = s.Qty;
								return packedQty.GetValueOrDefault() < qty.GetValueOrDefault() & (packedQty != null & qty != null);
							});
						}
						return this.PickedForPack.SelectMain(Array.Empty<object>()).Any(delegate(SOShipLineSplit s)
						{
							decimal? packedQty = s.PackedQty;
							decimal? pickedQty = s.PickedQty;
							return packedQty.GetValueOrDefault() < pickedQty.GetValueOrDefault() & (packedQty != null & pickedQty != null);
						});
					}
				}

				// Token: 0x1700AF14 RID: 44820
				// (get) Token: 0x0602FD78 RID: 195960 RVA: 0x008E5A08 File Offset: 0x008E3C08
				public virtual bool CanConfirmPackage
				{
					get
					{
						SOPackageDetailEx sopackageDetailEx;
						return base.Basis.RefNbr != null && this.HasConfirmableBoxes && !this.HasSingleAutoPackage(base.Basis.RefNbr, out sopackageDetailEx) && this.PackageLineNbr != null && this.SelectedPackage != null && !this.IsPackageEmpty(this.SelectedPackage);
					}
				}

				// Token: 0x0602FD79 RID: 195961 RVA: 0x008E5A68 File Offset: 0x008E3C68
				public virtual bool IsPackageEmpty(SOPackageDetailEx package)
				{
					return base.Graph.PackageDetailExt.PackageDetailSplit.Select(new object[]
					{
						package.ShipmentNbr,
						package.LineNbr
					}).Count == 0;
				}

				// Token: 0x1700AF15 RID: 44821
				// (get) Token: 0x0602FD7A RID: 195962 RVA: 0x008E5AA4 File Offset: 0x008E3CA4
				public SOPackageDetailEx SelectedPackage
				{
					get
					{
						return base.Graph.Packages.Search<SOPackageDetailEx.lineNbr>(this.PackageLineNbr, Array.Empty<object>());
					}
				}

				// Token: 0x1700AF16 RID: 44822
				// (get) Token: 0x0602FD7B RID: 195963 RVA: 0x008E5ACB File Offset: 0x008E3CCB
				public virtual bool HasConfirmableBoxes
				{
					get
					{
						return base.Graph.Packages.SelectMain(Array.Empty<object>()).Any(delegate(SOPackageDetailEx p)
						{
							bool? confirmed = p.Confirmed;
							bool flag = false;
							return (confirmed.GetValueOrDefault() == flag & confirmed != null) && !this.IsPackageEmpty(p);
						});
					}
				}

				// Token: 0x0602FD7C RID: 195964 RVA: 0x008E5AF4 File Offset: 0x008E3CF4
				public virtual bool HasSingleAutoPackage(string shipmentNbr, out SOPackageDetailEx package)
				{
					if (PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>())
					{
						SOPackageDetailEx[] array = PXSelectBase<SOPackageDetailEx, PXViewOf<SOPackageDetailEx>.BasedOn<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOPackageDetailEx.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Basis, new object[]
						{
							shipmentNbr
						}).RowCast<SOPackageDetailEx>().ToArray<SOPackageDetailEx>();
						if (array.Length == 1 && array[0].PackageType == "A")
						{
							package = array[0];
							return true;
						}
						if (array.Any((SOPackageDetailEx p) => p.PackageType == "A"))
						{
							throw new PXInvalidOperationException("The {0} shipment cannot be processed in Pack mode because it has two or more packages assigned.", new object[]
							{
								shipmentNbr
							});
						}
					}
					package = null;
					return false;
				}

				// Token: 0x0602FD7D RID: 195965 RVA: 0x008E5B94 File Offset: 0x008E3D94
				public virtual bool? TryAutoConfirmCurrentPackageAndLoadNext(string boxBarcode)
				{
					bool? remove = base.Basis.Remove;
					bool flag = false;
					if ((remove.GetValueOrDefault() == flag & remove != null) && CSBox.PK.Find(base.Basis, boxBarcode, PKFindOptions.None) != null)
					{
						if (!base.Basis.Get<PickPackShip.PackMode.BoxConfirming.CompleteState.Logic>().TryAutoConfirm())
						{
							return null;
						}
						if (base.Basis.TryProcessBy<PickPackShip.PackMode.BoxState>(boxBarcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepStateChange | StateSubstitutionRule.KeepApplication))
						{
							return new bool?(true);
						}
					}
					return new bool?(false);
				}

				// Token: 0x0602FD81 RID: 195969 RVA: 0x008E5C63 File Offset: 0x008E3E63
				[CompilerGenerated]
				internal static PXSelectBase<SOShipLineSplit> <DecorateScanState>g__viewSelector|21_0(PickPackShip basis)
				{
					return basis.Get<PickPackShip.PackMode.Logic>().PickedForPack;
				}

				// Token: 0x04015078 RID: 86136
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
					0
				})]
				public FbqlSelect<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>>>.Order<By<BqlField<SOShipLineSplit.shipmentNbr, IBqlString>.Asc, BqlField<SOShipLineSplit.isUnassigned, IBqlBool>.Desc, BqlField<SOShipLineSplit.lineNbr, IBqlInt>.Asc>>, SOShipLineSplit>.View PickedForPack;

				// Token: 0x04015079 RID: 86137
				public FbqlSelect<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Empty>, SOShipLineSplit>.View Packed;

				// Token: 0x0401507A RID: 86138
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
					0
				})]
				public FbqlSelect<SelectFromBase<SOPackageDetailEx, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOPackageDetailEx.shipmentNbr, Equal<BqlField<WMSScanHeader.refNbr, IBqlString>.FromCurrent>>>>>.And<BqlOperand<SOPackageDetailEx.lineNbr, IBqlInt>.IsEqual<BqlField<PackScanHeader.packageLineNbrUI, IBqlInt>.FromCurrent.NoDefault>>>, SOPackageDetailEx>.View ShownPackage;

				// Token: 0x0401507B RID: 86139
				public PXAction<ScanHeader> ReviewPack;

				// Token: 0x0200F0B0 RID: 61616
				public class AlterCommandOrShipmentOnlyStatePrompt : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.CommandOrShipmentOnlyState.Logic>
				{
					/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.CommandOrShipmentOnlyState.Logic.GetPromptForCommandOrShipmentOnly" />
					// Token: 0x0603242A RID: 205866 RVA: 0x00915314 File Offset: 0x00913514
					[PXOverride]
					public string GetPromptForCommandOrShipmentOnly(Func<string> base_GetPromptForCommandOrShipmentOnly)
					{
						if (base.Basis.CurrentMode is PickPackShip.PackMode)
						{
							PickPackShip.PackMode.Logic logic = base.Basis.Get<PickPackShip.PackMode.Logic>();
							if (logic != null && logic.CanConfirmPackage)
							{
								return "Confirm the package.";
							}
						}
						return base_GetPromptForCommandOrShipmentOnly();
					}
				}
			}

			// Token: 0x0200D2FD RID: 54013
			public sealed class ShipmentState : PickPackShip.ShipmentState
			{
				// Token: 0x0602FD83 RID: 195971 RVA: 0x008E5CA8 File Offset: 0x008E3EA8
				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != "I")
					{
						return Validation.Fail("The {0} shipment cannot be packed because it has the {1} operation.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.operation>(shipment)
						});
					}
					if (shipment.Status != "N")
					{
						return Validation.Fail("The {0} shipment cannot be packed because it has the {1} status.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.status>(shipment)
						});
					}
					IEnumerable<SOShipLineSplit> source = base.Basis.GetSplits(shipment.ShipmentNbr, true, null).RowCast<SOShipLineSplit>().AsEnumerable<SOShipLineSplit>();
					if (base.Basis.HasPick)
					{
						if (source.All(delegate(SOShipLineSplit s)
						{
							decimal? pickedQty = s.PickedQty;
							decimal d = 0m;
							return pickedQty.GetValueOrDefault() == d & pickedQty != null;
						}))
						{
							return Validation.Fail("The {0} shipment cannot be packed because the items have not been picked.", new object[]
							{
								shipment.ShipmentNbr
							});
						}
					}
					Validation result;
					if (base.Basis.HasNonStockLinesWithEmptyLocation(shipment, out result))
					{
						return result;
					}
					SOPackageDetailEx sopackageDetailEx;
					base.Get<PickPackShip.PackMode.Logic>().HasSingleAutoPackage(shipment.ShipmentNbr, out sopackageDetailEx);
					return Validation.Ok;
				}

				// Token: 0x0602FD84 RID: 195972 RVA: 0x008E5DC2 File Offset: 0x008E3FC2
				protected override void ReportSuccess(SOShipment shipment)
				{
					base.Basis.ReportInfo("{0} shipment loaded and ready to be packed.", new object[]
					{
						shipment.ShipmentNbr
					});
				}

				// Token: 0x0602FD85 RID: 195973 RVA: 0x008E5DE4 File Offset: 0x008E3FE4
				protected override void SetNextState()
				{
					PickPackShip.PackMode.Logic logic = base.Basis.Get<PickPackShip.PackMode.Logic>();
					if (base.Basis.Remove.GetValueOrDefault() || logic.CanPack || logic.HasConfirmableBoxes)
					{
						base.SetNextState();
						return;
					}
					base.Basis.ReportInfo("{0} {1}", new object[]
					{
						base.Basis.Info.Current.Message,
						base.Basis.Localize("{0} shipment packed.", new object[]
						{
							base.Basis.RefNbr
						})
					});
					base.Basis.SetScanState("NONE", null, Array.Empty<object>());
				}

				// Token: 0x0200F0B4 RID: 61620
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					// Token: 0x04015786 RID: 87942
					public new const string Ready = "{0} shipment loaded and ready to be packed.";

					// Token: 0x04015787 RID: 87943
					public const string InvalidStatus = "The {0} shipment cannot be packed because it has the {1} status.";

					// Token: 0x04015788 RID: 87944
					public const string InvalidOperation = "The {0} shipment cannot be packed because it has the {1} operation.";

					// Token: 0x04015789 RID: 87945
					public const string ShouldBePickedFirst = "The {0} shipment cannot be packed because the items have not been picked.";
				}
			}

			// Token: 0x0200D2FE RID: 54014
			public sealed class BoxState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.EntityState<CSBox>
			{
				// Token: 0x1700AF17 RID: 44823
				// (get) Token: 0x0602FD87 RID: 195975 RVA: 0x008E5E9C File Offset: 0x008E409C
				public PickPackShip.PackMode.Logic Mode
				{
					get
					{
						return base.Get<PickPackShip.PackMode.Logic>();
					}
				}

				// Token: 0x1700AF18 RID: 44824
				// (get) Token: 0x0602FD88 RID: 195976 RVA: 0x008E5EA4 File Offset: 0x008E40A4
				public override string Code
				{
					get
					{
						return "BOX";
					}
				}

				// Token: 0x1700AF19 RID: 44825
				// (get) Token: 0x0602FD89 RID: 195977 RVA: 0x008E5EAB File Offset: 0x008E40AB
				protected override string StatePrompt
				{
					get
					{
						return "Scan the box.";
					}
				}

				// Token: 0x0602FD8A RID: 195978 RVA: 0x008E5EB4 File Offset: 0x008E40B4
				protected override bool IsStateSkippable()
				{
					return base.IsStateSkippable() || this.Mode.PackageLineNbr != null;
				}

				// Token: 0x0602FD8B RID: 195979 RVA: 0x008E5EE0 File Offset: 0x008E40E0
				protected override void OnTakingOver()
				{
					SOPackageDetailEx sopackageDetailEx;
					if (this.Mode.HasSingleAutoPackage(base.Basis.RefNbr, out sopackageDetailEx))
					{
						this.Mode.PackageLineNbr = sopackageDetailEx.LineNbr;
						this.Mode.PackageLineNbrUI = sopackageDetailEx.LineNbr;
						base.Basis.Graph.Packages.Current = sopackageDetailEx;
						this.MoveToNextState();
					}
				}

				// Token: 0x0602FD8C RID: 195980 RVA: 0x008E5F45 File Offset: 0x008E4145
				protected override CSBox GetByBarcode(string barcode)
				{
					return CSBox.PK.Find(base.Basis, barcode, PKFindOptions.None);
				}

				// Token: 0x0602FD8D RID: 195981 RVA: 0x008E5F5C File Offset: 0x008E415C
				protected override void Apply(CSBox box)
				{
					SOPackageDetailEx sopackageDetailEx = base.Basis.Graph.Packages.SelectMain(Array.Empty<object>()).FirstOrDefault(delegate(SOPackageDetailEx p)
					{
						if (string.Equals(p.BoxID.Trim(), box.BoxID.Trim(), StringComparison.OrdinalIgnoreCase))
						{
							bool? confirmed = p.Confirmed;
							bool flag = false;
							return confirmed.GetValueOrDefault() == flag & confirmed != null;
						}
						return false;
					});
					if (sopackageDetailEx == null)
					{
						sopackageDetailEx = (SOPackageDetailEx)base.Basis.Graph.Packages.Cache.CreateInstance();
						sopackageDetailEx.BoxID = box.BoxID;
						sopackageDetailEx.ShipmentNbr = base.Basis.RefNbr;
						sopackageDetailEx = base.Basis.Graph.Packages.Insert(sopackageDetailEx);
						base.Basis.Save.Press();
					}
					this.Mode.PackageLineNbr = sopackageDetailEx.LineNbr;
					this.Mode.PackageLineNbrUI = sopackageDetailEx.LineNbr;
					base.Basis.Graph.Packages.Current = sopackageDetailEx;
				}

				// Token: 0x0602FD8E RID: 195982 RVA: 0x008E6048 File Offset: 0x008E4248
				protected override void ClearState()
				{
					this.Mode.PackageLineNbr = null;
					base.Basis.Graph.Packages.Current = null;
				}

				// Token: 0x0602FD8F RID: 195983 RVA: 0x008E607F File Offset: 0x008E427F
				protected override void ReportSuccess(CSBox entity)
				{
					base.Basis.ReportInfo("{0} box selected.", new object[]
					{
						entity.BoxID
					});
				}

				// Token: 0x0602FD90 RID: 195984 RVA: 0x008E60A0 File Offset: 0x008E42A0
				protected override void ReportMissing(string barcode)
				{
					base.Basis.ReportError("{0} box not found.", new object[]
					{
						barcode
					});
				}

				// Token: 0x0602FD91 RID: 195985 RVA: 0x008E60BC File Offset: 0x008E42BC
				protected override void SetNextState()
				{
					if (!base.Basis.Remove.GetValueOrDefault() && !this.Mode.CanPack)
					{
						base.Basis.SetScanState("NONE", null, Array.Empty<object>());
						if (!this.Mode.CanConfirmPackage)
						{
							base.Basis.ReportInfo("{0} shipment packed.", new object[]
							{
								base.Basis.RefNbr
							});
							return;
						}
					}
					else
					{
						base.SetNextState();
					}
				}

				// Token: 0x0401507C RID: 86140
				public const string Value = "BOX";

				// Token: 0x0200F0B6 RID: 61622
				[Nullable(new byte[]
				{
					0,
					1,
					1,
					0
				})]
				public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PackMode.BoxState.value>
				{
					// Token: 0x06032440 RID: 205888 RVA: 0x009156F5 File Offset: 0x009138F5
					public value() : base("BOX")
					{
					}
				}

				// Token: 0x0200F0B7 RID: 61623
				[PXLocalizable]
				public abstract class Msg
				{
					// Token: 0x0401578C RID: 87948
					public const string Prompt = "Scan the box.";

					// Token: 0x0401578D RID: 87949
					public const string Ready = "{0} box selected.";

					// Token: 0x0401578E RID: 87950
					public const string Missing = "{0} box not found.";
				}
			}

			// Token: 0x0200D2FF RID: 54015
			public static class BoxConfirming
			{
				// Token: 0x0200F0B9 RID: 61625
				public sealed class StartState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.MediatorState
				{
					// Token: 0x1700B0D7 RID: 45271
					// (get) Token: 0x06032444 RID: 205892 RVA: 0x00915761 File Offset: 0x00913961
					public override string Code
					{
						get
						{
							return "BCS";
						}
					}

					// Token: 0x06032445 RID: 205893 RVA: 0x00915768 File Offset: 0x00913968
					protected override bool IsStateActive()
					{
						return base.Get<PickPackShip.PackMode.Logic>().CanConfirmPackage;
					}

					// Token: 0x06032446 RID: 205894 RVA: 0x00915775 File Offset: 0x00913975
					protected override void Apply()
					{
						base.Basis.Clear<PickPackShip.PackMode.BoxConfirming.WeightState>(true);
						base.Basis.Clear<PickPackShip.PackMode.BoxConfirming.DimensionsState>(true);
					}

					// Token: 0x06032447 RID: 205895 RVA: 0x00915790 File Offset: 0x00913990
					protected override void SetNextState()
					{
						SOPackageDetailEx sopackageDetailEx;
						if (base.Get<PickPackShip.PackMode.Logic>().HasSingleAutoPackage(base.Basis.RefNbr, out sopackageDetailEx))
						{
							base.Basis.ReportInfo("{0} {1}", new object[]
							{
								base.Basis.Info.Current.Message,
								base.Basis.Localize("{0} shipment packed.", new object[]
								{
									base.Basis.RefNbr
								})
							});
							base.Basis.SetScanState("NONE", null, Array.Empty<object>());
							return;
						}
						base.SetNextState();
					}

					// Token: 0x04015790 RID: 87952
					public const string Value = "BCS";

					// Token: 0x0200F21D RID: 61981
					[Nullable(new byte[]
					{
						0,
						1,
						1,
						0
					})]
					public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PackMode.BoxConfirming.StartState.value>
					{
						// Token: 0x06032858 RID: 206936 RVA: 0x00925D7B File Offset: 0x00923F7B
						public value() : base("BCS")
						{
						}
					}
				}

				// Token: 0x0200F0BA RID: 61626
				public sealed class WeightState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.EntityState<decimal?>
				{
					// Token: 0x1700B0D8 RID: 45272
					// (get) Token: 0x06032449 RID: 205897 RVA: 0x00915831 File Offset: 0x00913A31
					public PickPackShip.PackMode.BoxConfirming.WeightState.Logic This
					{
						get
						{
							return base.Get<PickPackShip.PackMode.BoxConfirming.WeightState.Logic>();
						}
					}

					// Token: 0x1700B0D9 RID: 45273
					// (get) Token: 0x0603244A RID: 205898 RVA: 0x00915839 File Offset: 0x00913A39
					public PickPackShip.PackMode.Logic Mode
					{
						get
						{
							return base.Get<PickPackShip.PackMode.Logic>();
						}
					}

					// Token: 0x1700B0DA RID: 45274
					// (get) Token: 0x0603244B RID: 205899 RVA: 0x00915841 File Offset: 0x00913A41
					public override string Code
					{
						get
						{
							return "BWGT";
						}
					}

					// Token: 0x1700B0DB RID: 45275
					// (get) Token: 0x0603244C RID: 205900 RVA: 0x00915848 File Offset: 0x00913A48
					protected override string StatePrompt
					{
						get
						{
							return "Enter the actual total weight of the package.";
						}
					}

					// Token: 0x0603244D RID: 205901 RVA: 0x0091584F File Offset: 0x00913A4F
					protected override void OnTakingOver()
					{
						if (this.This.TryPrepareWeightAndSkipInputFor(this.Mode.SelectedPackage))
						{
							this.MoveToNextState();
						}
					}

					// Token: 0x0603244E RID: 205902 RVA: 0x0091586F File Offset: 0x00913A6F
					protected override void OnDismissing()
					{
						base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.WeightState.SkipQuestion>();
					}

					// Token: 0x0603244F RID: 205903 RVA: 0x0091587C File Offset: 0x00913A7C
					protected override decimal? GetByBarcode(string barcode)
					{
						decimal value;
						if (!decimal.TryParse(barcode, out value))
						{
							return null;
						}
						return new decimal?(value);
					}

					// Token: 0x06032450 RID: 205904 RVA: 0x009158A3 File Offset: 0x00913AA3
					protected override void ReportMissing(string barcode)
					{
						base.Basis.ReportError("The quantity format does not fit the locale settings.", Array.Empty<object>());
					}

					// Token: 0x06032451 RID: 205905 RVA: 0x009158BC File Offset: 0x00913ABC
					protected override Validation Validate(decimal? value)
					{
						Validation result;
						if (base.Basis.HasFault<decimal?>(value, new Func<decimal?, Validation>(base.Validate), out result))
						{
							return result;
						}
						SOPackageDetailEx selectedPackage = this.Mode.SelectedPackage;
						string errorMsg;
						if (!base.Basis.IsValid<SOPackageDetail.weight, SOPackageDetailEx>(selectedPackage, value.Value, out errorMsg))
						{
							return Validation.Fail(errorMsg, Array.Empty<object>());
						}
						return Validation.Ok;
					}

					// Token: 0x06032452 RID: 205906 RVA: 0x00915920 File Offset: 0x00913B20
					protected override void Apply(decimal? value)
					{
						this.This.Weight = new decimal?(value.Value);
					}

					// Token: 0x06032453 RID: 205907 RVA: 0x0091593C File Offset: 0x00913B3C
					protected override void ClearState()
					{
						PickPackShip.PackMode.BoxConfirming.WeightState.Logic this2 = this.This;
						PickPackShip.PackMode.BoxConfirming.WeightState.Logic this3 = this.This;
						this2.Weight = null;
						this3.LastWeighingTime = null;
					}

					// Token: 0x06032454 RID: 205908 RVA: 0x00915973 File Offset: 0x00913B73
					protected override void ReportSuccess(decimal? value)
					{
						base.Basis.ReportInfo("Once the package is confirmed, it will have the following weight: {0} {1}.", new object[]
						{
							value.Value,
							this.Mode.SelectedPackage.WeightUOM
						});
					}

					// Token: 0x04015791 RID: 87953
					public const string Value = "BWGT";

					// Token: 0x0200F21E RID: 61982
					[Nullable(new byte[]
					{
						0,
						1,
						1,
						0
					})]
					public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PackMode.BoxConfirming.WeightState.value>
					{
						// Token: 0x06032859 RID: 206937 RVA: 0x00925D88 File Offset: 0x00923F88
						public value() : base("BWGT")
						{
						}
					}

					// Token: 0x0200F21F RID: 61983
					public sealed class SkipQuestion : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanQuestion
					{
						// Token: 0x1700B15E RID: 45406
						// (get) Token: 0x0603285A RID: 206938 RVA: 0x00925D95 File Offset: 0x00923F95
						public PickPackShip.PackMode.BoxConfirming.WeightState.Logic State
						{
							get
							{
								return base.Get<PickPackShip.PackMode.BoxConfirming.WeightState.Logic>();
							}
						}

						// Token: 0x1700B15F RID: 45407
						// (get) Token: 0x0603285B RID: 206939 RVA: 0x00925D9D File Offset: 0x00923F9D
						public override string Code
						{
							get
							{
								return "SKIPWEIGHT";
							}
						}

						// Token: 0x0603285C RID: 206940 RVA: 0x00925DA4 File Offset: 0x00923FA4
						protected override string GetPrompt()
						{
							return "To skip the weighing, click OK.";
						}

						// Token: 0x0603285D RID: 206941 RVA: 0x00925DAC File Offset: 0x00923FAC
						protected override void Confirm()
						{
							if (this.State.TryUsePreparedWeightFor(this.State.Target.SelectedPackage, true) && base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.WeightState)
							{
								base.Basis.DispatchNext(null, Array.Empty<object>());
							}
						}

						// Token: 0x0603285E RID: 206942 RVA: 0x00925DFA File Offset: 0x00923FFA
						protected override void Reject()
						{
						}

						// Token: 0x0200F26A RID: 62058
						[PXLocalizable]
						public abstract class Msg
						{
							// Token: 0x04015AAF RID: 88751
							public const string Prompt = "To skip the weighing, click OK.";
						}
					}

					// Token: 0x0200F220 RID: 61984
					public sealed class SkipScalesQuestion : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanQuestion
					{
						// Token: 0x1700B160 RID: 45408
						// (get) Token: 0x06032860 RID: 206944 RVA: 0x00925E04 File Offset: 0x00924004
						public PickPackShip.PackMode.BoxConfirming.WeightState.Logic State
						{
							get
							{
								return base.Get<PickPackShip.PackMode.BoxConfirming.WeightState.Logic>();
							}
						}

						// Token: 0x1700B161 RID: 45409
						// (get) Token: 0x06032861 RID: 206945 RVA: 0x00925E0C File Offset: 0x0092400C
						public override string Code
						{
							get
							{
								return "SKIPSCALES";
							}
						}

						// Token: 0x06032862 RID: 206946 RVA: 0x00925E13 File Offset: 0x00924013
						protected override string GetPrompt()
						{
							return "Put the package on the scale and click OK. To skip the weighing, click OK without using the scale.";
						}

						// Token: 0x06032863 RID: 206947 RVA: 0x00925E1C File Offset: 0x0092401C
						protected override void Confirm()
						{
							SOPackageDetailEx selectedPackage = this.State.Target.SelectedPackage;
							if (this.State.SelectedScales.LastModifiedDateTime == this.State.LastWeighingTime)
							{
								if (this.State.TryUsePreparedWeightFor(selectedPackage, false) && base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.WeightState)
								{
									base.Basis.DispatchNext(null, Array.Empty<object>());
									return;
								}
							}
							else if (this.State.ProcessScales(selectedPackage) && base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.WeightState)
							{
								base.Basis.DispatchNext(null, Array.Empty<object>());
							}
						}

						// Token: 0x06032864 RID: 206948 RVA: 0x00925EEF File Offset: 0x009240EF
						protected override void Reject()
						{
						}

						// Token: 0x0200F26B RID: 62059
						[PXLocalizable]
						public abstract class Msg
						{
							// Token: 0x04015AB0 RID: 88752
							public const string Prompt = "Put the package on the scale and click OK. To skip the weighing, click OK without using the scale.";
						}
					}

					// Token: 0x0200F221 RID: 61985
					public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.Logic>
					{
						// Token: 0x1700B162 RID: 45410
						// (get) Token: 0x06032866 RID: 206950 RVA: 0x00925EF9 File Offset: 0x009240F9
						public virtual double ScaleWeightValiditySeconds
						{
							get
							{
								return 30.0;
							}
						}

						// Token: 0x1700B163 RID: 45411
						// (get) Token: 0x06032867 RID: 206951 RVA: 0x00925F04 File Offset: 0x00924104
						public SMScale SelectedScales
						{
							get
							{
								Guid? scaleDeviceID = PXSetupBase<PickPackShip.UserSetup, PickPackShip.Host, ScanHeader, SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>>.For(base.Graph).ScaleDeviceID;
								base.Graph.Caches<SMScale>().ClearQueryCache();
								return SMScale.PK.Find(base.Basis, scaleDeviceID, PKFindOptions.None);
							}
						}

						// Token: 0x1700B164 RID: 45412
						// (get) Token: 0x06032868 RID: 206952 RVA: 0x00925F44 File Offset: 0x00924144
						// (set) Token: 0x06032869 RID: 206953 RVA: 0x00925F58 File Offset: 0x00924158
						public decimal? Weight
						{
							get
							{
								return base.Target.PackHeader.Weight;
							}
							set
							{
								base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Weight, value);
							}
						}

						// Token: 0x1700B165 RID: 45413
						// (get) Token: 0x0603286A RID: 206954 RVA: 0x00925FB2 File Offset: 0x009241B2
						// (set) Token: 0x0603286B RID: 206955 RVA: 0x00925FC4 File Offset: 0x009241C4
						public DateTime? LastWeighingTime
						{
							get
							{
								return base.Target.PackHeader.LastWeighingTime;
							}
							set
							{
								base.Target.PackSetter.Set<DateTime?>((PackScanHeader h) => h.LastWeighingTime, value);
							}
						}

						// Token: 0x0603286C RID: 206956 RVA: 0x00926020 File Offset: 0x00924220
						public virtual bool TryUsePreparedWeightFor(SOPackageDetailEx package, bool explicitConfirmation = false)
						{
							if (!explicitConfirmation && base.Basis.Setup.Current.ConfirmEachPackageWeight.GetValueOrDefault())
							{
								return false;
							}
							if (!this.CanSkipInputFor(package))
							{
								base.Basis.ReportError("The package does not have a predefined weight.", Array.Empty<object>());
								base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.WeightState.SkipQuestion>();
								base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.WeightState.SkipScalesQuestion>();
								return false;
							}
							Validation validation;
							if (this.Weight != null && base.Basis.HasFault<decimal?>(this.Weight, (decimal? w) => base.Basis.TryValidate<decimal?>(w).By<PickPackShip.PackMode.BoxConfirming.WeightState>(), out validation))
							{
								base.Basis.ReportError(validation.Message, validation.MessageArgs);
								base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.WeightState.SkipQuestion>();
								base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.WeightState.SkipScalesQuestion>();
								return false;
							}
							return true;
						}

						// Token: 0x0603286D RID: 206957 RVA: 0x009260EC File Offset: 0x009242EC
						public virtual bool TryPrepareWeightAndSkipInputFor(SOPackageDetailEx package)
						{
							decimal? weight = package.Weight;
							decimal d = 0m;
							this.Weight = new decimal?((weight.GetValueOrDefault() == d & weight != null) ? this.AutoCalculateBoxWeightBasedOnItems(package) : package.Weight.Value);
							if (PXSetupBase<PickPackShip.UserSetup, PickPackShip.Host, ScanHeader, SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>>.For(base.Basis).UseScale.GetValueOrDefault() && !this.ProcessScales(package))
							{
								return false;
							}
							base.Basis.ReportInfo("The {0} package is ready to be confirmed. The calculated weight is {1} {2}.", new object[]
							{
								package.BoxID,
								this.Weight,
								package.WeightUOM
							});
							if (base.Basis.Setup.Current.ConfirmEachPackageWeight.GetValueOrDefault())
							{
								if (this.CanSkipInputFor(package))
								{
									this.AskToSkipFor(package);
								}
								return false;
							}
							return this.CanSkipInputFor(package);
						}

						// Token: 0x0603286E RID: 206958 RVA: 0x009261DC File Offset: 0x009243DC
						protected virtual bool CanSkipInputFor(SOPackageDetailEx package)
						{
							return this.Weight.IsNotIn(null, new decimal?(0m)) || package.Weight.IsNotIn(null, new decimal?(0m));
						}

						// Token: 0x0603286F RID: 206959 RVA: 0x00926234 File Offset: 0x00924434
						protected virtual void AskToSkipFor(SOPackageDetailEx package)
						{
							Validation validation;
							if (base.Basis.HasFault<decimal?>(this.Weight, (decimal? w) => base.Basis.TryValidate<decimal?>(w).By<PickPackShip.PackMode.BoxConfirming.WeightState>(), out validation))
							{
								base.Basis.Warn<PickPackShip.PackMode.BoxConfirming.WeightState.SkipQuestion>("The {0} package is ready to be confirmed. The calculated weight is {1} {2}.", new object[]
								{
									package.BoxID,
									this.Weight,
									package.WeightUOM
								});
								return;
							}
							base.Basis.Ask<PickPackShip.PackMode.BoxConfirming.WeightState.SkipQuestion>("The {0} package is ready to be confirmed. The calculated weight is {1} {2}.", new object[]
							{
								package.BoxID,
								this.Weight,
								package.WeightUOM
							});
						}

						// Token: 0x06032870 RID: 206960 RVA: 0x009262D0 File Offset: 0x009244D0
						protected virtual decimal AutoCalculateBoxWeightBasedOnItems(SOPackageDetailEx package)
						{
							CSBox csbox = CSBox.PK.Find(base.Basis, package.BoxID, PKFindOptions.None);
							decimal num = ((csbox != null) ? csbox.BoxWeight : null).GetValueOrDefault();
							foreach (SOShipLineSplitPackage soshipLineSplitPackage in base.Graph.PackageDetailExt.PackageDetailSplit.SelectMain(new object[]
							{
								package.ShipmentNbr,
								package.LineNbr
							}))
							{
								InventoryItem inventoryItem = InventoryItem.PK.Find(base.Basis, soshipLineSplitPackage.InventoryID, PKFindOptions.None);
								num += inventoryItem.BaseWeight.GetValueOrDefault() * soshipLineSplitPackage.BasePackedQty.GetValueOrDefault();
							}
							return Math.Round(num, 4);
						}

						// Token: 0x06032871 RID: 206961 RVA: 0x009263A4 File Offset: 0x009245A4
						public virtual bool ProcessScales(SOPackageDetailEx package)
						{
							SMScale selectedScales = this.SelectedScales;
							if (selectedScales == null)
							{
								base.Basis.ReportError("{0} scale not found.", new object[]
								{
									""
								});
								return false;
							}
							DateTime serverTime = this.GetServerTime();
							this.LastWeighingTime = new DateTime?(selectedScales.LastModifiedDateTime.Value);
							if (selectedScales.LastModifiedDateTime.Value.AddHours(1.0) < serverTime)
							{
								base.Basis.ReportError("The system could not retrieve the weighing result from the {0} scale. Check if the scale is connected to the working station with DeviceHub.", new object[]
								{
									selectedScales.ScaleID
								});
								return false;
							}
							if (selectedScales.LastWeight.GetValueOrDefault() == 0m)
							{
								base.Basis.Warn<PickPackShip.PackMode.BoxConfirming.WeightState.SkipScalesQuestion>("The system could not retrieve the weighing result from the {0} scale. Ensure that items are placed on the scale.", new object[]
								{
									selectedScales.ScaleID
								});
								return false;
							}
							if (selectedScales.LastModifiedDateTime.Value.AddSeconds(this.ScaleWeightValiditySeconds) < serverTime)
							{
								base.Basis.ReportError("The weighing result on the {0} scale is more than {1} seconds old. Remove the package from the scale and weigh it again.", new object[]
								{
									selectedScales.ScaleID,
									this.ScaleWeightValiditySeconds
								});
								return false;
							}
							SMScaleWeightConversion extension = selectedScales.GetExtension<SMScaleWeightConversion>();
							if (extension == null || extension.CompanyUOM == null)
							{
								base.Basis.ReportError("Default values for weight UOM and volume UOM are not specified on the Companies (CS101500) form.", Array.Empty<object>());
								return false;
							}
							if (extension == null || extension.CompanyLastWeight == null)
							{
								base.Basis.ReportError("No rule for converting the {0} unit of measure to the {1} unit of measure has been set up on the Units of Measure (CS203500) form.", new object[]
								{
									selectedScales.UOM,
									extension.CompanyUOM
								});
								return false;
							}
							this.Weight = new decimal?(extension.CompanyLastWeight.GetValueOrDefault());
							return true;
						}

						// Token: 0x06032872 RID: 206962 RVA: 0x0092655C File Offset: 0x0092475C
						protected virtual DateTime GetServerTime()
						{
							DateTime dateTime;
							DateTime dateTime2;
							PXDatabase.SelectDate(out dateTime, out dateTime2);
							dateTime2 = PXTimeZoneInfo.ConvertTimeFromUtc(dateTime2, LocaleInfo.GetTimeZone(), true);
							return dateTime2;
						}

						/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.DecorateScanCommand(PX.BarcodeProcessing.ScanCommand{`0})" />
						// Token: 0x06032873 RID: 206963 RVA: 0x00926580 File Offset: 0x00924780
						[PXOverride]
						public ScanCommand<PickPackShip> DecorateScanCommand(ScanCommand<PickPackShip> original, Func<ScanCommand<PickPackShip>, ScanCommand<PickPackShip>> base_DecorateScanCommand)
						{
							ScanCommand<PickPackShip> scanCommand = base_DecorateScanCommand(original);
							PickPackShip.PackMode.ConfirmPackageCommand confirmPackageCommand = scanCommand as PickPackShip.PackMode.ConfirmPackageCommand;
							if (confirmPackageCommand != null)
							{
								confirmPackageCommand.Intercept.IsEnabled.ByConjoin((PickPackShip basis) => !(basis.CurrentState is PickPackShip.PackMode.BoxConfirming.WeightState), false, null);
							}
							return scanCommand;
						}

						// Token: 0x040159F4 RID: 88564
						public PXSetupOptional<CommonSetup> CommonSetupUOM;
					}

					// Token: 0x0200F222 RID: 61986
					public class AlterComplete : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.BoxConfirming.CompleteState.Logic>
					{
						/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.PackMode.BoxConfirming.CompleteState.Logic.ApplyChanges(PX.Objects.SO.SOPackageDetailEx)" />
						// Token: 0x06032877 RID: 206967 RVA: 0x00926628 File Offset: 0x00924828
						[PXOverride]
						public void ApplyChanges(SOPackageDetailEx package, Action<SOPackageDetailEx> base_ApplyChanges)
						{
							base_ApplyChanges(package);
							PickPackShip.PackMode.BoxConfirming.WeightState.Logic logic = base.Basis.Get<PickPackShip.PackMode.BoxConfirming.WeightState.Logic>();
							if (logic.Weight.IsNotIn(null, new decimal?(0m)))
							{
								package.Weight = new decimal?(Math.Round(logic.Weight.Value, 4));
							}
						}

						/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.PackMode.BoxConfirming.CompleteState.Logic.TryForwardProcessing" />
						// Token: 0x06032878 RID: 206968 RVA: 0x00926688 File Offset: 0x00924888
						[PXOverride]
						public bool TryForwardProcessing(Func<bool> base_TryForwardProcessing)
						{
							return (!(base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.WeightState) || this.TryForward()) && base_TryForwardProcessing() && (!(base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.WeightState) || this.TryForward());
						}

						/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.PackMode.BoxConfirming.CompleteState.Logic.ClearStates" />
						// Token: 0x06032879 RID: 206969 RVA: 0x009266D8 File Offset: 0x009248D8
						[PXOverride]
						public void ClearStates(Action base_ClearStates)
						{
							base_ClearStates();
							base.Basis.Clear<PickPackShip.PackMode.BoxConfirming.WeightState>(true);
						}

						// Token: 0x0603287A RID: 206970 RVA: 0x009266EC File Offset: 0x009248EC
						protected virtual bool TryForward()
						{
							PickPackShip.PackMode.BoxConfirming.WeightState.Logic logic = base.Basis.Get<PickPackShip.PackMode.BoxConfirming.WeightState.Logic>();
							if (!logic.TryUsePreparedWeightFor(logic.Target.SelectedPackage, false))
							{
								return false;
							}
							base.Basis.DispatchNext(null, Array.Empty<object>());
							return true;
						}
					}

					// Token: 0x0200F223 RID: 61987
					[PXLocalizable]
					public abstract class Msg
					{
						// Token: 0x040159F5 RID: 88565
						public const string Prompt = "Enter the actual total weight of the package.";

						// Token: 0x040159F6 RID: 88566
						public const string BadFormat = "The quantity format does not fit the locale settings.";

						// Token: 0x040159F7 RID: 88567
						public const string Success = "Once the package is confirmed, it will have the following weight: {0} {1}.";

						// Token: 0x040159F8 RID: 88568
						public const string CalculatedWeight = "The {0} package is ready to be confirmed. The calculated weight is {1} {2}.";

						// Token: 0x040159F9 RID: 88569
						public const string NoSkip = "The package does not have a predefined weight.";

						// Token: 0x040159FA RID: 88570
						public const string ScaleMissing = "{0} scale not found.";

						// Token: 0x040159FB RID: 88571
						public const string ScaleDisconnected = "The system could not retrieve the weighing result from the {0} scale. Check if the scale is connected to the working station with DeviceHub.";

						// Token: 0x040159FC RID: 88572
						public const string ScaleTimeout = "The weighing result on the {0} scale is more than {1} seconds old. Remove the package from the scale and weigh it again.";

						// Token: 0x040159FD RID: 88573
						public const string ScaleNoBox = "The system could not retrieve the weighing result from the {0} scale. Ensure that items are placed on the scale.";
					}
				}

				// Token: 0x0200F0BB RID: 61627
				[TupleElementNames(new string[]
				{
					"L",
					"W",
					"H"
				})]
				public sealed class DimensionsState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.EntityState<ValueTuple<decimal, decimal, decimal>?>
				{
					// Token: 0x1700B0DC RID: 45276
					// (get) Token: 0x06032456 RID: 205910 RVA: 0x009159B5 File Offset: 0x00913BB5
					public PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic This
					{
						get
						{
							return base.Get<PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic>();
						}
					}

					// Token: 0x1700B0DD RID: 45277
					// (get) Token: 0x06032457 RID: 205911 RVA: 0x009159BD File Offset: 0x00913BBD
					public PickPackShip.PackMode.Logic Mode
					{
						get
						{
							return base.Get<PickPackShip.PackMode.Logic>();
						}
					}

					// Token: 0x1700B0DE RID: 45278
					// (get) Token: 0x06032458 RID: 205912 RVA: 0x009159C5 File Offset: 0x00913BC5
					public override string Code
					{
						get
						{
							return "BDIM";
						}
					}

					// Token: 0x1700B0DF RID: 45279
					// (get) Token: 0x06032459 RID: 205913 RVA: 0x009159CC File Offset: 0x00913BCC
					protected override string StatePrompt
					{
						get
						{
							return "Enter the actual length, width, and height of the package. Use a space as a separator.";
						}
					}

					// Token: 0x0603245A RID: 205914 RVA: 0x009159D4 File Offset: 0x00913BD4
					protected override bool IsStateSkippable()
					{
						bool? confirmEachPackageDimensions = base.Basis.Setup.Current.ConfirmEachPackageDimensions;
						bool flag = false;
						if (!(confirmEachPackageDimensions.GetValueOrDefault() == flag & confirmEachPackageDimensions != null) && this.Mode.SelectedPackage != null)
						{
							CSBox csbox = CSBox.PK.Find(base.Basis, this.Mode.SelectedPackage.BoxID, PKFindOptions.None);
							return csbox == null || !csbox.AllowOverrideDimension.GetValueOrDefault();
						}
						return true;
					}

					// Token: 0x0603245B RID: 205915 RVA: 0x00915A53 File Offset: 0x00913C53
					protected override void OnTakingOver()
					{
						if (this.This.TryPrepareDimensionsAndSkipInputFor(this.Mode.SelectedPackage))
						{
							this.MoveToNextState();
						}
					}

					// Token: 0x0603245C RID: 205916 RVA: 0x00915A73 File Offset: 0x00913C73
					protected override void OnDismissing()
					{
						base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.DimensionsState.SkipQuestion>();
					}

					// Token: 0x0603245D RID: 205917 RVA: 0x00915A80 File Offset: 0x00913C80
					[return: TupleElementNames(new string[]
					{
						"L",
						"W",
						"H"
					})]
					protected override ValueTuple<decimal, decimal, decimal>? GetByBarcode(string barcode)
					{
						string[] array = barcode.Trim().Split(new char[]
						{
							' '
						});
						if (array.Length < 3)
						{
							return null;
						}
						string text;
						string text2;
						string text3;
						array.Deconstruct(out text, out text2, out text3);
						string s = text;
						string s2 = text2;
						string s3 = text3;
						decimal item;
						decimal item2;
						decimal item3;
						if (decimal.TryParse(s, out item) && decimal.TryParse(s2, out item2) && decimal.TryParse(s3, out item3))
						{
							return new ValueTuple<decimal, decimal, decimal>?(new ValueTuple<decimal, decimal, decimal>(item, item2, item3));
						}
						return null;
					}

					// Token: 0x0603245E RID: 205918 RVA: 0x00915B00 File Offset: 0x00913D00
					protected override void ReportMissing(string barcode)
					{
						base.Basis.ReportError("The format of the entered string is incorrect. The string should contain three numeric dimensions separated by a space. Example: 31.2 20 13.5", Array.Empty<object>());
					}

					// Token: 0x0603245F RID: 205919 RVA: 0x00915B18 File Offset: 0x00913D18
					protected override Validation Validate([TupleElementNames(new string[]
					{
						"L",
						"W",
						"H"
					})] ValueTuple<decimal, decimal, decimal>? value)
					{
						Validation result;
						if (base.Basis.HasFault<ValueTuple<decimal, decimal, decimal>?>(value, new Func<ValueTuple<decimal, decimal, decimal>?, Validation>(base.Validate), out result))
						{
							return result;
						}
						SOPackageDetailEx selectedPackage = this.Mode.SelectedPackage;
						string errorMsg;
						if (!base.Basis.IsValid<SOPackageDetail.length, SOPackageDetailEx>(selectedPackage, value.Value.Item1, out errorMsg))
						{
							return Validation.Fail(errorMsg, Array.Empty<object>());
						}
						string errorMsg2;
						if (!base.Basis.IsValid<SOPackageDetail.width, SOPackageDetailEx>(selectedPackage, value.Value.Item2, out errorMsg2))
						{
							return Validation.Fail(errorMsg2, Array.Empty<object>());
						}
						string errorMsg3;
						if (!base.Basis.IsValid<SOPackageDetail.height, SOPackageDetailEx>(selectedPackage, value.Value.Item3, out errorMsg3))
						{
							return Validation.Fail(errorMsg3, Array.Empty<object>());
						}
						return Validation.Ok;
					}

					// Token: 0x06032460 RID: 205920 RVA: 0x00915BDC File Offset: 0x00913DDC
					protected override void Apply([TupleElementNames(new string[]
					{
						"L",
						"W",
						"H"
					})] ValueTuple<decimal, decimal, decimal>? value)
					{
						this.This.Dimensions = new ValueTuple<decimal, decimal, decimal>?(value.Value);
					}

					// Token: 0x06032461 RID: 205921 RVA: 0x00915BF8 File Offset: 0x00913DF8
					protected override void ClearState()
					{
						this.This.Dimensions = null;
					}

					// Token: 0x06032462 RID: 205922 RVA: 0x00915C1C File Offset: 0x00913E1C
					protected override void ReportSuccess([TupleElementNames(new string[]
					{
						"L",
						"W",
						"H"
					})] ValueTuple<decimal, decimal, decimal>? value)
					{
						base.Basis.ReportInfo("Once the package is confirmed, it will have the following dimensions: {0} x {1} x {2} {3}.", new object[]
						{
							value.Value.Item1,
							value.Value.Item2,
							value.Value.Item3,
							this.Mode.SelectedPackage.LinearUOM
						});
					}

					// Token: 0x04015792 RID: 87954
					public const string Value = "BDIM";

					// Token: 0x0200F224 RID: 61988
					[Nullable(new byte[]
					{
						0,
						1,
						1,
						0
					})]
					public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PackMode.BoxConfirming.DimensionsState.value>
					{
						// Token: 0x0603287D RID: 206973 RVA: 0x00926730 File Offset: 0x00924930
						public value() : base("BDIM")
						{
						}
					}

					// Token: 0x0200F225 RID: 61989
					public sealed class SkipQuestion : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanQuestion
					{
						// Token: 0x1700B166 RID: 45414
						// (get) Token: 0x0603287E RID: 206974 RVA: 0x0092673D File Offset: 0x0092493D
						public PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic State
						{
							get
							{
								return base.Get<PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic>();
							}
						}

						// Token: 0x1700B167 RID: 45415
						// (get) Token: 0x0603287F RID: 206975 RVA: 0x00926745 File Offset: 0x00924945
						public override string Code
						{
							get
							{
								return "SKIPDIMENSIONS";
							}
						}

						// Token: 0x06032880 RID: 206976 RVA: 0x0092674C File Offset: 0x0092494C
						protected override string GetPrompt()
						{
							return "To use the default dimensions, click OK.";
						}

						// Token: 0x06032881 RID: 206977 RVA: 0x00926754 File Offset: 0x00924954
						protected override void Confirm()
						{
							if (this.State.TryUsePreparedDimensionsFor(this.State.Target.SelectedPackage, true) && base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.DimensionsState)
							{
								base.Basis.DispatchNext(null, Array.Empty<object>());
							}
						}

						// Token: 0x06032882 RID: 206978 RVA: 0x009267A2 File Offset: 0x009249A2
						protected override void Reject()
						{
						}

						// Token: 0x0200F26D RID: 62061
						[PXLocalizable]
						public abstract class Msg
						{
							// Token: 0x04015AB3 RID: 88755
							public const string Prompt = "To use the default dimensions, click OK.";
						}
					}

					// Token: 0x0200F226 RID: 61990
					public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.Logic>
					{
						// Token: 0x1700B168 RID: 45416
						// (get) Token: 0x06032884 RID: 206980 RVA: 0x009267AC File Offset: 0x009249AC
						// (set) Token: 0x06032885 RID: 206981 RVA: 0x00926858 File Offset: 0x00924A58
						[TupleElementNames(new string[]
						{
							"L",
							"W",
							"H"
						})]
						public ValueTuple<decimal, decimal, decimal>? Dimensions
						{
							[return: TupleElementNames(new string[]
							{
								"L",
								"W",
								"H"
							})]
							get
							{
								if (PX.Common.EnumerableExtensions.IsIn((T)null, base.Target.PackHeader.Length, base.Target.PackHeader.Width, base.Target.PackHeader.Height))
								{
									return null;
								}
								return new ValueTuple<decimal, decimal, decimal>?(new ValueTuple<decimal, decimal, decimal>(base.Target.PackHeader.Length.Value, base.Target.PackHeader.Width.Value, base.Target.PackHeader.Height.Value));
							}
							[param: TupleElementNames(new string[]
							{
								"L",
								"W",
								"H"
							})]
							set
							{
								if (value == null)
								{
									base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Length, null);
									base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Width, null);
									base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Height, null);
									return;
								}
								base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Length, new decimal?(value.Value.Item1));
								base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Width, new decimal?(value.Value.Item2));
								base.Target.PackSetter.Set<decimal?>((PackScanHeader h) => h.Height, new decimal?(value.Value.Item3));
							}
						}

						// Token: 0x06032886 RID: 206982 RVA: 0x00926A88 File Offset: 0x00924C88
						public virtual bool TryUsePreparedDimensionsFor(SOPackageDetailEx package, bool explicitConfirmation = false)
						{
							if (!explicitConfirmation && base.Basis.Setup.Current.ConfirmEachPackageDimensions.GetValueOrDefault())
							{
								return false;
							}
							if (!this.CanSkipInputFor(package))
							{
								base.Basis.ReportError("The package does not have predefined dimensions.", Array.Empty<object>());
								base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.DimensionsState.SkipQuestion>();
								return false;
							}
							Validation validation;
							if (this.Dimensions != null && base.Basis.HasFault<ValueTuple<decimal, decimal, decimal>?>(this.Dimensions, ([TupleElementNames(new string[]
							{
								"L",
								"W",
								"H"
							})] ValueTuple<decimal, decimal, decimal>? dims) => base.Basis.TryValidate<ValueTuple<decimal, decimal, decimal>?>(dims).By<PickPackShip.PackMode.BoxConfirming.DimensionsState>(), out validation))
							{
								base.Basis.ReportError(validation.Message, validation.MessageArgs);
								base.Basis.RevokeQuestion<PickPackShip.PackMode.BoxConfirming.DimensionsState.SkipQuestion>();
								return false;
							}
							return true;
						}

						// Token: 0x06032887 RID: 206983 RVA: 0x00926B3C File Offset: 0x00924D3C
						public virtual bool TryPrepareDimensionsAndSkipInputFor(SOPackageDetailEx package)
						{
							this.Dimensions = new ValueTuple<decimal, decimal, decimal>?(new ValueTuple<decimal, decimal, decimal>(package.Length.GetValueOrDefault(), package.Width.GetValueOrDefault(), package.Height.GetValueOrDefault()));
							base.Basis.ReportInfo("The {0} package is ready to be confirmed. It has the following default dimensions: {1} x {2} x {3} {4}.", new object[]
							{
								package.BoxID,
								this.Dimensions.Value.Item1,
								this.Dimensions.Value.Item2,
								this.Dimensions.Value.Item3,
								package.LinearUOM
							});
							if (base.Basis.Setup.Current.ConfirmEachPackageDimensions.GetValueOrDefault())
							{
								if (this.CanSkipInputFor(package))
								{
									this.AskToSkipFor(package);
								}
								return false;
							}
							return this.CanSkipInputFor(package);
						}

						// Token: 0x06032888 RID: 206984 RVA: 0x00926C34 File Offset: 0x00924E34
						protected virtual bool CanSkipInputFor(SOPackageDetailEx package)
						{
							return this.Dimensions.IsNotIn(null, new ValueTuple<decimal, decimal, decimal>?(new ValueTuple<decimal, decimal, decimal>(0m, 0m, 0m))) || (package.Length.IsNotIn(null, new decimal?(0m)) && package.Width.IsNotIn(null, new decimal?(0m)) && package.Height.IsNotIn(null, new decimal?(0m)));
						}

						// Token: 0x06032889 RID: 206985 RVA: 0x00926CE0 File Offset: 0x00924EE0
						protected virtual void AskToSkipFor(SOPackageDetailEx package)
						{
							Validation validation;
							if (base.Basis.HasFault<ValueTuple<decimal, decimal, decimal>?>(this.Dimensions, ([TupleElementNames(new string[]
							{
								"L",
								"W",
								"H"
							})] ValueTuple<decimal, decimal, decimal>? dims) => base.Basis.TryValidate<ValueTuple<decimal, decimal, decimal>?>(dims).By<PickPackShip.PackMode.BoxConfirming.DimensionsState>(), out validation))
							{
								base.Basis.Warn<PickPackShip.PackMode.BoxConfirming.DimensionsState.SkipQuestion>("The {0} package is ready to be confirmed. It has the following default dimensions: {1} x {2} x {3} {4}.", new object[]
								{
									package.BoxID,
									this.Dimensions.Value.Item1,
									this.Dimensions.Value.Item2,
									this.Dimensions.Value.Item3,
									package.LinearUOM
								});
								return;
							}
							base.Basis.Ask<PickPackShip.PackMode.BoxConfirming.DimensionsState.SkipQuestion>("The {0} package is ready to be confirmed. It has the following default dimensions: {1} x {2} x {3} {4}.", new object[]
							{
								package.BoxID,
								this.Dimensions.Value.Item1,
								this.Dimensions.Value.Item2,
								this.Dimensions.Value.Item3,
								package.LinearUOM
							});
						}

						/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.DecorateScanCommand(PX.BarcodeProcessing.ScanCommand{`0})" />
						// Token: 0x0603288A RID: 206986 RVA: 0x00926E04 File Offset: 0x00925004
						[PXOverride]
						public ScanCommand<PickPackShip> DecorateScanCommand(ScanCommand<PickPackShip> original, Func<ScanCommand<PickPackShip>, ScanCommand<PickPackShip>> base_DecorateScanCommand)
						{
							ScanCommand<PickPackShip> scanCommand = base_DecorateScanCommand(original);
							PickPackShip.PackMode.ConfirmPackageCommand confirmPackageCommand = scanCommand as PickPackShip.PackMode.ConfirmPackageCommand;
							if (confirmPackageCommand != null)
							{
								confirmPackageCommand.Intercept.IsEnabled.ByConjoin((PickPackShip basis) => !(basis.CurrentState is PickPackShip.PackMode.BoxConfirming.DimensionsState), false, null);
							}
							return scanCommand;
						}
					}

					// Token: 0x0200F227 RID: 61991
					[PXUIField(DisplayName = "Dimensions (L x W x H)")]
					public class PackageDimensionsCombined : PXFieldAttachedTo<SOPackageDetailEx>.By<PickPackShip.Host>.AsString.Named<PickPackShip.PackMode.BoxConfirming.DimensionsState.PackageDimensionsCombined>
					{
						// Token: 0x0603288E RID: 206990 RVA: 0x00926EAC File Offset: 0x009250AC
						public override string GetValue(SOPackageDetailEx Row)
						{
							return string.Format("{0} x {1} x {2} {3}", new object[]
							{
								Row.Length,
								Row.Width,
								Row.Height,
								Row.LinearUOM
							});
						}
					}

					// Token: 0x0200F228 RID: 61992
					public class AlterComplete : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.BoxConfirming.CompleteState.Logic>
					{
						/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.PackMode.BoxConfirming.CompleteState.Logic.ApplyChanges(PX.Objects.SO.SOPackageDetailEx)" />
						// Token: 0x06032890 RID: 206992 RVA: 0x00926F04 File Offset: 0x00925104
						[PXOverride]
						public void ApplyChanges(SOPackageDetailEx package, Action<SOPackageDetailEx> base_ApplyChanges)
						{
							base_ApplyChanges(package);
							PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic logic = base.Basis.Get<PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic>();
							if (logic.Dimensions.IsNotIn(null, new ValueTuple<decimal, decimal, decimal>?(new ValueTuple<decimal, decimal, decimal>(0m, 0m, 0m))))
							{
								package.Length = new decimal?(Math.Round(logic.Dimensions.Value.Item1, 4));
								package.Width = new decimal?(Math.Round(logic.Dimensions.Value.Item2, 4));
								package.Height = new decimal?(Math.Round(logic.Dimensions.Value.Item3, 4));
							}
						}

						/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.PackMode.BoxConfirming.CompleteState.Logic.TryForwardProcessing" />
						// Token: 0x06032891 RID: 206993 RVA: 0x00926FC0 File Offset: 0x009251C0
						[PXOverride]
						public bool TryForwardProcessing(Func<bool> base_TryForwardProcessing)
						{
							return (!(base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.DimensionsState) || this.TryForward()) && base_TryForwardProcessing() && (!(base.Basis.CurrentState is PickPackShip.PackMode.BoxConfirming.DimensionsState) || this.TryForward());
						}

						/// Overrides <seealso cref="M:PX.Objects.SO.WMS.PickPackShip.PackMode.BoxConfirming.CompleteState.Logic.ClearStates" />
						// Token: 0x06032892 RID: 206994 RVA: 0x00927010 File Offset: 0x00925210
						[PXOverride]
						public void ClearStates(Action base_ClearStates)
						{
							base_ClearStates();
							base.Basis.Clear<PickPackShip.PackMode.BoxConfirming.DimensionsState>(true);
						}

						// Token: 0x06032893 RID: 206995 RVA: 0x00927024 File Offset: 0x00925224
						protected virtual bool TryForward()
						{
							PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic logic = base.Basis.Get<PickPackShip.PackMode.BoxConfirming.DimensionsState.Logic>();
							if (!logic.TryUsePreparedDimensionsFor(logic.Target.SelectedPackage, false))
							{
								return false;
							}
							base.Basis.DispatchNext(null, Array.Empty<object>());
							return true;
						}
					}

					// Token: 0x0200F229 RID: 61993
					[PXLocalizable]
					public abstract class Msg
					{
						// Token: 0x040159FE RID: 88574
						public const string Prompt = "Enter the actual length, width, and height of the package. Use a space as a separator.";

						// Token: 0x040159FF RID: 88575
						public const string BadFormat = "The format of the entered string is incorrect. The string should contain three numeric dimensions separated by a space. Example: 31.2 20 13.5";

						// Token: 0x04015A00 RID: 88576
						public const string Success = "Once the package is confirmed, it will have the following dimensions: {0} x {1} x {2} {3}.";

						// Token: 0x04015A01 RID: 88577
						public const string NoSkip = "The package does not have predefined dimensions.";

						// Token: 0x04015A02 RID: 88578
						public const string CalculatedDimensions = "The {0} package is ready to be confirmed. It has the following default dimensions: {1} x {2} x {3} {4}.";

						// Token: 0x04015A03 RID: 88579
						public const string PackageDimensionsCombined = "Dimensions (L x W x H)";
					}
				}

				// Token: 0x0200F0BC RID: 61628
				public sealed class CompleteState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.MediatorState
				{
					// Token: 0x1700B0E0 RID: 45280
					// (get) Token: 0x06032464 RID: 205924 RVA: 0x00915C96 File Offset: 0x00913E96
					public override string Code
					{
						get
						{
							return "BCC";
						}
					}

					// Token: 0x06032465 RID: 205925 RVA: 0x00915C9D File Offset: 0x00913E9D
					protected override void Apply()
					{
						base.Get<PickPackShip.PackMode.BoxConfirming.CompleteState.Logic>().Call(delegate(PickPackShip.PackMode.BoxConfirming.CompleteState.Logic state)
						{
							state.SettleAndConfirmPackage(state.Target.SelectedPackage);
						});
					}

					// Token: 0x06032466 RID: 205926 RVA: 0x00915CC9 File Offset: 0x00913EC9
					protected override void SetNextState()
					{
						base.Basis.SetDefaultState(null, Array.Empty<object>());
					}

					// Token: 0x04015793 RID: 87955
					public const string Value = "BCC";

					// Token: 0x0200F22A RID: 61994
					[Nullable(new byte[]
					{
						0,
						1,
						1,
						0
					})]
					public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PackMode.BoxConfirming.CompleteState.value>
					{
						// Token: 0x06032896 RID: 206998 RVA: 0x00927068 File Offset: 0x00925268
						public value() : base("BCC")
						{
						}
					}

					// Token: 0x0200F22B RID: 61995
					public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension<PickPackShip.PackMode.Logic>
					{
						// Token: 0x06032897 RID: 206999 RVA: 0x00927078 File Offset: 0x00925278
						public virtual bool TryAutoConfirm()
						{
							if (!base.Basis.HasActive<PickPackShip.PackMode.BoxConfirming.StartState>())
							{
								return true;
							}
							base.Basis.SetScanState<PickPackShip.PackMode.BoxConfirming.StartState>(null, Array.Empty<object>());
							if (base.Target.SelectedPackage.Confirmed.GetValueOrDefault())
							{
								return true;
							}
							if (!this.TryForwardProcessing())
							{
								return false;
							}
							this.SettleAndConfirmPackage(base.Target.SelectedPackage);
							return true;
						}

						// Token: 0x06032898 RID: 207000 RVA: 0x009270E0 File Offset: 0x009252E0
						public virtual void SettleAndConfirmPackage(SOPackageDetailEx package)
						{
							this.ApplyChanges(package);
							package.Confirmed = new bool?(true);
							base.Graph.Packages.Update(package);
							base.Basis.Save.Press();
							this.ClearStates();
							base.Basis.Reset(false);
							base.Basis.ReportInfo("Package confirmed.", Array.Empty<object>());
						}

						// Token: 0x06032899 RID: 207001 RVA: 0x00927149 File Offset: 0x00925349
						protected virtual bool TryForwardProcessing()
						{
							return true;
						}

						// Token: 0x0603289A RID: 207002 RVA: 0x0092714C File Offset: 0x0092534C
						protected virtual void ApplyChanges(SOPackageDetailEx package)
						{
						}

						// Token: 0x0603289B RID: 207003 RVA: 0x0092714E File Offset: 0x0092534E
						protected virtual void ClearStates()
						{
							base.Basis.Clear<PickPackShip.PackMode.BoxState>(true);
						}
					}

					// Token: 0x0200F22C RID: 61996
					[PXLocalizable]
					public abstract class Msg
					{
						// Token: 0x04015A04 RID: 88580
						public const string Success = "Package confirmed.";
					}
				}
			}

			// Token: 0x0200D300 RID: 54016
			public sealed class ConfirmState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ConfirmationState
			{
				// Token: 0x1700AF1A RID: 44826
				// (get) Token: 0x0602FD93 RID: 195987 RVA: 0x008E6144 File Offset: 0x008E4344
				public sealed override string Prompt
				{
					get
					{
						return base.Basis.Localize("Confirm packing {0} x {1} {2}.", new object[]
						{
							base.Basis.SightOf<WMSScanHeader.inventoryID>(),
							base.Basis.Qty,
							base.Basis.UOM
						});
					}
				}

				// Token: 0x0602FD94 RID: 195988 RVA: 0x008E6196 File Offset: 0x008E4396
				protected sealed override FlowStatus PerformConfirmation()
				{
					return base.Get<PickPackShip.PackMode.ConfirmState.Logic>().Confirm();
				}

				// Token: 0x0200F0BD RID: 61629
				public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
				{
					// Token: 0x1700B0E1 RID: 45281
					// (get) Token: 0x06032468 RID: 205928 RVA: 0x00915CE4 File Offset: 0x00913EE4
					// (set) Token: 0x06032469 RID: 205929 RVA: 0x00915CEC File Offset: 0x00913EEC
					private protected PickPackShip.PackMode.Logic Mode { protected get; private set; }

					// Token: 0x0603246A RID: 205930 RVA: 0x00915CF5 File Offset: 0x00913EF5
					public override void Initialize()
					{
						this.Mode = base.Basis.Get<PickPackShip.PackMode.Logic>();
					}

					// Token: 0x0603246B RID: 205931 RVA: 0x00915D08 File Offset: 0x00913F08
					public virtual FlowStatus Confirm()
					{
						PickPackShip.PackMode.ConfirmState.Logic.<>c__DisplayClass5_0 CS$<>8__locals1 = new PickPackShip.PackMode.ConfirmState.Logic.<>c__DisplayClass5_0();
						CS$<>8__locals1.<>4__this = this;
						FlowStatus result = FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "No items to remove from shipment." : "No items to pack.", Array.Empty<object>());
						if (this.Mode.PackageLineNbr == null)
						{
							return result;
						}
						CS$<>8__locals1.packageDetail = this.Mode.SelectedPackage;
						if (base.Basis.InventoryID != null)
						{
							decimal? num = base.Basis.Qty;
							decimal d = 0m;
							if (!(num.GetValueOrDefault() == d & num != null))
							{
								IEnumerable<SOShipLineSplit> splitsToPack = this.GetSplitsToPack();
								if (!splitsToPack.Any<SOShipLineSplit>())
								{
									return result.WithModeReset.WithPostAction(new Action(CS$<>8__locals1.<Confirm>g__KeepPackageSelection|0));
								}
								decimal baseQty = base.Basis.BaseQty;
								string text = base.Basis.SightOf<WMSScanHeader.inventoryID>();
								bool flag;
								if (!base.Basis.Remove.GetValueOrDefault())
								{
									num = splitsToPack.Sum(delegate(SOShipLineSplit s)
									{
										decimal? num4 = CS$<>8__locals1.<>4__this.TargetQty(s);
										decimal? packedQty = s.PackedQty;
										if (!(num4 != null & packedQty != null))
										{
											return null;
										}
										return new decimal?(num4.GetValueOrDefault() - packedQty.GetValueOrDefault());
									});
									d = baseQty;
									flag = (num.GetValueOrDefault() < d & num != null);
								}
								else
								{
									num = splitsToPack.Sum((SOShipLineSplit s) => s.PackedQty) - baseQty;
									d = 0m;
									flag = (num.GetValueOrDefault() < d & num != null);
								}
								if (flag)
								{
									return FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "The packed quantity cannot be negative." : "The packed quantity cannot be greater than the quantity in the shipment lines with this item.", new object[]
									{
										text,
										base.Basis.Qty,
										base.Basis.UOM
									});
								}
								decimal num2 = Sign.MinusIf(base.Basis.Remove.GetValueOrDefault()) * baseQty;
								foreach (SOShipLineSplit soshipLineSplit in splitsToPack)
								{
									decimal num3 = base.Basis.Remove.GetValueOrDefault() ? (-Math.Min(soshipLineSplit.PackedQty.Value, -num2)) : Math.Min(this.TargetQty(soshipLineSplit).Value - soshipLineSplit.PackedQty.Value, num2);
									if (!this.PackSplit(soshipLineSplit, CS$<>8__locals1.packageDetail, num3))
									{
										return FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "The packed quantity cannot be negative." : "The packed quantity cannot be greater than the quantity in the shipment lines with this item.", new object[]
										{
											text,
											base.Basis.Qty,
											base.Basis.UOM
										});
									}
									num2 -= num3;
									if (num2 == 0m)
									{
										break;
									}
								}
								if (this.Mode.IsPackageEmpty(CS$<>8__locals1.packageDetail))
								{
									base.Basis.Graph.Packages.Delete(CS$<>8__locals1.packageDetail);
									base.Basis.Clear<PickPackShip.PackMode.BoxState>(true);
									this.Mode.PackageLineNbrUI = null;
								}
								else
								{
									this.Mode.PackageLineNbrUI = this.Mode.PackageLineNbr;
								}
								this.EnsureShipmentUserLinkForPack();
								base.Basis.ReportInfo(base.Basis.Remove.GetValueOrDefault() ? "{0} x {1} {2} removed from shipment." : "{0} x {1} {2} added to shipment.", new object[]
								{
									base.Basis.SightOf<WMSScanHeader.inventoryID>(),
									base.Basis.Qty,
									base.Basis.UOM
								});
								return FlowStatus.Ok.WithDispatchNext;
							}
						}
						return result;
					}

					// Token: 0x0603246C RID: 205932 RVA: 0x00916148 File Offset: 0x00914348
					public virtual decimal? TargetQty(SOShipLineSplit split)
					{
						if (base.Basis.HasPick)
						{
							return split.PickedQty;
						}
						decimal? qty = split.Qty;
						decimal qtyThreshold = base.Basis.Graph.GetQtyThreshold(split);
						if (qty == null)
						{
							return null;
						}
						return new decimal?(qty.GetValueOrDefault() * qtyThreshold);
					}

					// Token: 0x0603246D RID: 205933 RVA: 0x009161A8 File Offset: 0x009143A8
					protected virtual bool IsSelectedSplit(SOShipLineSplit split)
					{
						int? num = split.InventoryID;
						int? num2 = base.Basis.InventoryID;
						if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
						{
							num2 = split.SubItemID;
							num = base.Basis.SubItemID;
							if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
							{
								num = split.SiteID;
								num2 = base.Basis.SiteID;
								if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
								{
									num2 = split.LocationID;
									int? locationID = base.Basis.LocationID;
									num = ((locationID != null) ? locationID : split.LocationID);
									if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
									{
										return string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase);
									}
								}
							}
						}
						return false;
					}

					// Token: 0x0603246E RID: 205934 RVA: 0x009162CC File Offset: 0x009144CC
					public virtual IEnumerable<SOShipLineSplit> GetSplitsToPack()
					{
						bool locationIsRequired = base.Basis.HasActive<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>();
						return this.Mode.PickedForPack.SelectMain(Array.Empty<object>()).Where(delegate(SOShipLineSplit r)
						{
							int? num = r.InventoryID;
							int? num2 = this.Basis.InventoryID;
							if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
							{
								num2 = r.SubItemID;
								num = this.Basis.SubItemID;
								if ((num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null)) && (r.IsUnassigned.GetValueOrDefault() || r.HasGeneratedLotSerialNbr.GetValueOrDefault() || string.Equals(r.LotSerialNbr, this.Basis.LotSerialNbr ?? r.LotSerialNbr, StringComparison.OrdinalIgnoreCase)))
								{
									bool locationIsRequired = locationIsRequired;
									num = r.LocationID;
									int? locationID = this.Basis.LocationID;
									num2 = ((locationID != null) ? locationID : r.LocationID);
									if (locationIsRequired.Implies(num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null)))
									{
										decimal? packedQty;
										if (!this.Basis.Remove.GetValueOrDefault())
										{
											decimal? num3 = this.TargetQty(r);
											packedQty = r.PackedQty;
											return num3.GetValueOrDefault() > packedQty.GetValueOrDefault() & (num3 != null & packedQty != null);
										}
										packedQty = r.PackedQty;
										decimal d = 0m;
										return packedQty.GetValueOrDefault() > d & packedQty != null;
									}
								}
							}
							return false;
						}).With(new Func<IEnumerable<SOShipLineSplit>, IOrderedEnumerable<SOShipLineSplit>>(this.PrioritizeSplits));
					}

					// Token: 0x0603246F RID: 205935 RVA: 0x00916330 File Offset: 0x00914530
					public virtual IOrderedEnumerable<SOShipLineSplit> PrioritizeSplits(IEnumerable<SOShipLineSplit> splits)
					{
						if (!base.Basis.HasPick)
						{
							SOShipment shipment = base.Basis.Shipment;
							bool flag;
							if (shipment == null)
							{
								flag = false;
							}
							else
							{
								bool? pickedViaWorksheet = shipment.PickedViaWorksheet;
								bool flag2 = false;
								flag = (pickedViaWorksheet.GetValueOrDefault() == flag2 & pickedViaWorksheet != null);
							}
							if (flag)
							{
								return splits.OrderByAccordanceTo(delegate(SOShipLineSplit split)
								{
									bool? flag3 = split.IsUnassigned;
									bool flag4 = false;
									if (flag3.GetValueOrDefault() == flag4 & flag3 != null)
									{
										flag3 = split.HasGeneratedLotSerialNbr;
										flag4 = false;
										return flag3.GetValueOrDefault() == flag4 & flag3 != null;
									}
									return false;
								}).ThenByAccordanceTo(delegate(SOShipLineSplit split)
								{
									decimal? pickedQty;
									if (!base.Basis.Remove.GetValueOrDefault())
									{
										decimal? qty = split.Qty;
										pickedQty = split.PickedQty;
										return qty.GetValueOrDefault() > pickedQty.GetValueOrDefault() & (qty != null & pickedQty != null);
									}
									pickedQty = split.PickedQty;
									decimal d = 0m;
									return pickedQty.GetValueOrDefault() > d & pickedQty != null;
								}).ThenByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenByAccordanceTo((SOShipLineSplit split) => string.IsNullOrEmpty(split.LotSerialNbr)).ThenByAccordanceTo(delegate(SOShipLineSplit split)
								{
									decimal? qty = split.Qty;
									decimal? pickedQty = split.PickedQty;
									if ((qty.GetValueOrDefault() > pickedQty.GetValueOrDefault() & (qty != null & pickedQty != null)) || base.Basis.Remove.GetValueOrDefault())
									{
										pickedQty = split.PickedQty;
										decimal d = 0m;
										return pickedQty.GetValueOrDefault() > d & pickedQty != null;
									}
									return false;
								}).ThenByDescending(delegate(SOShipLineSplit split)
								{
									Sign sign = Sign.MinusIf(base.Basis.Remove.GetValueOrDefault());
									decimal? num = split.Qty - split.PickedQty;
									if (num == null)
									{
										return null;
									}
									return new decimal?(sign * num.GetValueOrDefault());
								});
							}
						}
						return from split in splits
						orderby 0
						select split;
					}

					// Token: 0x06032470 RID: 205936 RVA: 0x00916434 File Offset: 0x00914634
					public virtual bool PackSplit(SOShipLineSplit split, SOPackageDetailEx package, decimal qty)
					{
						if (base.Basis.HasPick)
						{
							base.Basis.EnsureAssignedSplitEditing(split);
						}
						else if (split.IsUnassigned.GetValueOrDefault())
						{
							SOShipLineSplit soshipLineSplit = this.Mode.PickedForPack.SelectMain(Array.Empty<object>()).FirstOrDefault(delegate(SOShipLineSplit s)
							{
								int? lineNbr = s.LineNbr;
								int? lineNbr2 = split.LineNbr;
								if (lineNbr.GetValueOrDefault() == lineNbr2.GetValueOrDefault() & lineNbr != null == (lineNbr2 != null))
								{
									bool? isUnassigned = s.IsUnassigned;
									bool flag = false;
									if ((isUnassigned.GetValueOrDefault() == flag & isUnassigned != null) && string.Equals(s.LotSerialNbr, this.Basis.LotSerialNbr ?? s.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
									{
										return this.IsSelectedSplit(s);
									}
								}
								return false;
							});
							if (soshipLineSplit == null)
							{
								SOShipLineSplit soshipLineSplit2 = PXCache<SOShipLineSplit>.CreateCopy(split);
								soshipLineSplit2.SplitLineNbr = null;
								soshipLineSplit2.LotSerialNbr = base.Basis.LotSerialNbr;
								soshipLineSplit2.ExpireDate = base.Basis.ExpireDate;
								soshipLineSplit2.Qty = new decimal?(qty);
								soshipLineSplit2.PickedQty = new decimal?(qty);
								soshipLineSplit2.PackedQty = new decimal?(0m);
								soshipLineSplit2.IsUnassigned = new bool?(false);
								soshipLineSplit2.PlanID = null;
								split = this.Mode.PickedForPack.Insert(soshipLineSplit2);
							}
							else
							{
								soshipLineSplit.Qty += qty;
								soshipLineSplit.ExpireDate = base.Basis.ExpireDate;
								split = this.Mode.PickedForPack.Update(soshipLineSplit);
							}
						}
						else if (split.HasGeneratedLotSerialNbr.GetValueOrDefault())
						{
							SOShipLineSplit soshipLineSplit3 = PXCache<SOShipLineSplit>.CreateCopy(split);
							decimal? num = soshipLineSplit3.Qty;
							if (num.GetValueOrDefault() == qty & num != null)
							{
								this.Mode.PickedForPack.Delete(soshipLineSplit3);
							}
							else
							{
								soshipLineSplit3.Qty -= qty;
								soshipLineSplit3.PickedQty -= Math.Min(qty, soshipLineSplit3.PickedQty.Value);
								this.Mode.PickedForPack.Update(soshipLineSplit3);
							}
							SOShipLineSplit soshipLineSplit4 = this.Mode.PickedForPack.SelectMain(Array.Empty<object>()).FirstOrDefault(delegate(SOShipLineSplit s)
							{
								int? lineNbr = s.LineNbr;
								int? lineNbr2 = split.LineNbr;
								if (lineNbr.GetValueOrDefault() == lineNbr2.GetValueOrDefault() & lineNbr != null == (lineNbr2 != null))
								{
									bool? hasGeneratedLotSerialNbr = s.HasGeneratedLotSerialNbr;
									bool flag = false;
									if ((hasGeneratedLotSerialNbr.GetValueOrDefault() == flag & hasGeneratedLotSerialNbr != null) && string.Equals(s.LotSerialNbr, this.Basis.LotSerialNbr ?? s.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
									{
										return this.IsSelectedSplit(s);
									}
								}
								return false;
							});
							if (soshipLineSplit4 == null)
							{
								SOShipLineSplit soshipLineSplit5 = PXCache<SOShipLineSplit>.CreateCopy(split);
								soshipLineSplit5.SplitLineNbr = null;
								soshipLineSplit5.LotSerialNbr = base.Basis.LotSerialNbr;
								if (base.Basis.ExpireDate != null)
								{
									soshipLineSplit5.ExpireDate = base.Basis.ExpireDate;
								}
								soshipLineSplit5.Qty = new decimal?(qty);
								soshipLineSplit5.PickedQty = new decimal?(qty);
								soshipLineSplit5.PackedQty = new decimal?(0m);
								soshipLineSplit5.PlanID = null;
								split = this.Mode.PickedForPack.Insert(soshipLineSplit5);
								split.HasGeneratedLotSerialNbr = new bool?(false);
								split = this.Mode.PickedForPack.Update(split);
							}
							else
							{
								soshipLineSplit4.Qty += qty;
								soshipLineSplit4.PickedQty += qty;
								if (base.Basis.ExpireDate != null)
								{
									soshipLineSplit4.ExpireDate = base.Basis.ExpireDate;
								}
								split = this.Mode.PickedForPack.Update(soshipLineSplit4);
							}
						}
						SOShipLineSplitPackage soshipLineSplitPackage = base.Graph.PackageDetailExt.PackageDetailSplit.SelectMain(new object[]
						{
							package.ShipmentNbr,
							package.LineNbr
						}).FirstOrDefault(delegate(SOShipLineSplitPackage t)
						{
							if (t.ShipmentNbr == split.ShipmentNbr)
							{
								int? num2 = t.ShipmentLineNbr;
								int? num3 = split.LineNbr;
								if (num2.GetValueOrDefault() == num3.GetValueOrDefault() & num2 != null == (num3 != null))
								{
									num3 = t.ShipmentSplitLineNbr;
									num2 = split.SplitLineNbr;
									return num3.GetValueOrDefault() == num2.GetValueOrDefault() & num3 != null == (num2 != null);
								}
							}
							return false;
						});
						if (qty < 0m)
						{
							if (soshipLineSplitPackage != null)
							{
								decimal? num = soshipLineSplitPackage.PackedQty + qty;
								decimal d = 0m;
								if (!(num.GetValueOrDefault() < d & num != null))
								{
									if (!base.Basis.HasPick && base.Basis.LotSerialTrack.IsEnterable)
									{
										if (base.Basis.SelectedLotSerialClass.AutoNextNbr.GetValueOrDefault())
										{
											num = split.PackedQty + qty;
											d = 0m;
											if (num.GetValueOrDefault() == d & num != null)
											{
												split.HasGeneratedLotSerialNbr = new bool?(true);
												split = this.Mode.PickedForPack.Update(split);
											}
										}
										else
										{
											split.Qty += qty;
											num = split.Qty;
											d = 0m;
											if (num.GetValueOrDefault() == d & num != null)
											{
												split = this.Mode.PickedForPack.Delete(split);
											}
											else
											{
												split = this.Mode.PickedForPack.Update(split);
											}
										}
									}
									num = soshipLineSplitPackage.PackedQty + qty;
									d = 0m;
									if (num.GetValueOrDefault() > d & num != null)
									{
										soshipLineSplitPackage.PackedQty += qty;
										base.Graph.PackageDetailExt.PackageDetailSplit.Update(soshipLineSplitPackage);
									}
									else
									{
										num = soshipLineSplitPackage.PackedQty + qty;
										d = 0m;
										if (num.GetValueOrDefault() == d & num != null)
										{
											base.Graph.PackageDetailExt.PackageDetailSplit.Delete(soshipLineSplitPackage);
										}
									}
									package.Confirmed = new bool?(false);
									base.Graph.Packages.Update(package);
									return true;
								}
							}
							return false;
						}
						if (soshipLineSplitPackage == null)
						{
							soshipLineSplitPackage = (SOShipLineSplitPackage)base.Base.PackageDetailExt.PackageDetailSplit.Cache.CreateInstance();
							PXFieldVerifying handler = delegate(PXCache c, PXFieldVerifyingEventArgs a)
							{
								a.Cancel = true;
							};
							base.Graph.FieldVerifying.AddHandler<SOShipLineSplitPackage.shipmentSplitLineNbr>(handler);
							soshipLineSplitPackage.ShipmentSplitLineNbr = split.SplitLineNbr;
							soshipLineSplitPackage.PackedQty = new decimal?(qty);
							soshipLineSplitPackage = base.Graph.PackageDetailExt.PackageDetailSplit.Insert(soshipLineSplitPackage);
							base.Graph.FieldVerifying.RemoveHandler<SOShipLineSplitPackage.shipmentSplitLineNbr>(handler);
							soshipLineSplitPackage.ShipmentNbr = split.ShipmentNbr;
							soshipLineSplitPackage.ShipmentLineNbr = split.LineNbr;
							soshipLineSplitPackage.PackageLineNbr = package.LineNbr;
							soshipLineSplitPackage.InventoryID = split.InventoryID;
							soshipLineSplitPackage.SubItemID = split.SubItemID;
							soshipLineSplitPackage.LotSerialNbr = split.LotSerialNbr;
							soshipLineSplitPackage.UOM = split.UOM;
							soshipLineSplitPackage = base.Graph.PackageDetailExt.PackageDetailSplit.Update(soshipLineSplitPackage);
						}
						else
						{
							soshipLineSplitPackage.PackedQty += qty;
							base.Graph.PackageDetailExt.PackageDetailSplit.Update(soshipLineSplitPackage);
						}
						return true;
					}

					// Token: 0x06032471 RID: 205937 RVA: 0x00916D6C File Offset: 0x00914F6C
					public virtual void EnsureShipmentUserLinkForPack()
					{
						base.Graph.WorkLogExt.EnsureFor(base.Basis.RefNbr, new Guid?(base.Graph.Accessinfo.UserID), base.Basis.HasPick ? "PACK" : "PPCK");
					}
				}

				// Token: 0x0200F0BE RID: 61630
				[PXLocalizable]
				public new abstract class Msg
				{
					// Token: 0x04015795 RID: 87957
					public const string Prompt = "Confirm packing {0} x {1} {2}.";

					// Token: 0x04015796 RID: 87958
					public const string NothingToPack = "No items to pack.";

					// Token: 0x04015797 RID: 87959
					public const string NothingToRemove = "No items to remove from shipment.";

					// Token: 0x04015798 RID: 87960
					public const string InventoryAdded = "{0} x {1} {2} added to shipment.";

					// Token: 0x04015799 RID: 87961
					public const string InventoryRemoved = "{0} x {1} {2} removed from shipment.";

					// Token: 0x0401579A RID: 87962
					public const string BoxCanNotPack = "The packed quantity cannot be greater than the quantity in the shipment lines with this item.";

					// Token: 0x0401579B RID: 87963
					public const string BoxCanNotUnpack = "The packed quantity cannot be negative.";
				}
			}

			// Token: 0x0200D301 RID: 54017
			public sealed class ConfirmPackageCommand : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanCommand
			{
				// Token: 0x1700AF1B RID: 44827
				// (get) Token: 0x0602FD96 RID: 195990 RVA: 0x008E61AB File Offset: 0x008E43AB
				public PickPackShip.PackMode.Logic Mode
				{
					get
					{
						return base.Get<PickPackShip.PackMode.Logic>();
					}
				}

				// Token: 0x1700AF1C RID: 44828
				// (get) Token: 0x0602FD97 RID: 195991 RVA: 0x008E61B3 File Offset: 0x008E43B3
				public override string Code
				{
					get
					{
						return "PACKAGE*CONFIRM";
					}
				}

				// Token: 0x1700AF1D RID: 44829
				// (get) Token: 0x0602FD98 RID: 195992 RVA: 0x008E61BA File Offset: 0x008E43BA
				public override string ButtonName
				{
					get
					{
						return "scanConfirmPackage";
					}
				}

				// Token: 0x1700AF1E RID: 44830
				// (get) Token: 0x0602FD99 RID: 195993 RVA: 0x008E61C1 File Offset: 0x008E43C1
				public override string DisplayName
				{
					get
					{
						return "Confirm Package";
					}
				}

				// Token: 0x1700AF1F RID: 44831
				// (get) Token: 0x0602FD9A RID: 195994 RVA: 0x008E61C8 File Offset: 0x008E43C8
				protected override bool IsEnabled
				{
					get
					{
						return this.Mode.CanConfirmPackage;
					}
				}

				// Token: 0x0602FD9B RID: 195995 RVA: 0x008E61D5 File Offset: 0x008E43D5
				protected override bool Process()
				{
					base.Basis.SetScanState<PickPackShip.PackMode.BoxConfirming.StartState>(null, Array.Empty<object>());
					return true;
				}

				// Token: 0x0200F0BF RID: 61631
				[PXLocalizable]
				public abstract class Msg
				{
					// Token: 0x0401579C RID: 87964
					public const string DisplayName = "Confirm Package";
				}
			}

			// Token: 0x0200D302 RID: 54018
			public sealed class RemoveCommand : WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.RemoveCommand
			{
				// Token: 0x1700AF20 RID: 44832
				// (get) Token: 0x0602FD9D RID: 195997 RVA: 0x008E61F1 File Offset: 0x008E43F1
				protected override bool IsEnabled
				{
					get
					{
						return base.IsEnabled && base.Get<PickPackShip.PackMode.Logic>().HasConfirmableBoxes;
					}
				}
			}

			// Token: 0x0200D303 RID: 54019
			public sealed class RedirectFrom<TForeignBasis> : PX.BarcodeProcessing.RedirectFrom<TForeignBasis>.To<PickPackShip>.SetMode<PickPackShip.PackMode> where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				// Token: 0x1700AF21 RID: 44833
				// (get) Token: 0x0602FD9F RID: 195999 RVA: 0x008E6210 File Offset: 0x008E4410
				public override string Code
				{
					get
					{
						return "PACK";
					}
				}

				// Token: 0x1700AF22 RID: 44834
				// (get) Token: 0x0602FDA0 RID: 196000 RVA: 0x008E6217 File Offset: 0x008E4417
				public override string DisplayName
				{
					get
					{
						return "Pack";
					}
				}

				// Token: 0x1700AF23 RID: 44835
				// (get) Token: 0x0602FDA1 RID: 196001 RVA: 0x008E621E File Offset: 0x008E441E
				// (set) Token: 0x0602FDA2 RID: 196002 RVA: 0x008E6226 File Offset: 0x008E4426
				private string RefNbr { get; set; }

				// Token: 0x1700AF24 RID: 44836
				// (get) Token: 0x0602FDA3 RID: 196003 RVA: 0x008E6230 File Offset: 0x008E4430
				public override bool IsPossible
				{
					get
					{
						bool flag = PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
						SOPickPackShipSetup sopickPackShipSetup = SOPickPackShipSetup.PK.Find(base.Basis.Graph, base.Basis.Graph.Accessinfo.BranchID, PKFindOptions.None);
						return flag && sopickPackShipSetup != null && sopickPackShipSetup.ShowPackTab.GetValueOrDefault();
					}
				}

				// Token: 0x0602FDA4 RID: 196004 RVA: 0x008E628C File Offset: 0x008E448C
				protected override bool PrepareRedirect()
				{
					PickPackShip pickPackShip = base.Basis as PickPackShip;
					if (pickPackShip != null && pickPackShip.RefNbr != null && !pickPackShip.DocumentIsConfirmed)
					{
						Validation? validation = pickPackShip.FindMode<PickPackShip.PackMode>().TryValidate<SOShipment>(pickPackShip.Shipment).By<PickPackShip.PackMode.ShipmentState>();
						if (validation != null)
						{
							Validation valueOrDefault = validation.GetValueOrDefault();
							if (valueOrDefault.IsError.GetValueOrDefault())
							{
								pickPackShip.ReportError(valueOrDefault.Message, valueOrDefault.MessageArgs);
								return false;
							}
						}
						this.RefNbr = pickPackShip.RefNbr;
					}
					return true;
				}

				// Token: 0x0602FDA5 RID: 196005 RVA: 0x008E6320 File Offset: 0x008E4520
				protected override void CompleteRedirect()
				{
					PickPackShip pickPackShip = base.Basis as PickPackShip;
					if (pickPackShip != null && pickPackShip.CurrentMode.Code != "CRTN" && this.RefNbr != null && pickPackShip.TryProcessBy("RNBR", this.RefNbr, (StateSubstitutionRule)253))
					{
						pickPackShip.SetDefaultState(null, Array.Empty<object>());
						this.RefNbr = null;
					}
				}
			}

			// Token: 0x0200D304 RID: 54020
			[PXLocalizable]
			public new abstract class Msg : ScanMode<PickPackShip>.Msg
			{
				// Token: 0x0401507E RID: 86142
				public const string Description = "Pack";

				// Token: 0x0401507F RID: 86143
				public const string PackedQtyPerBox = "Packed Qty.";

				// Token: 0x04015080 RID: 86144
				public const string Completed = "{0} shipment packed.";

				// Token: 0x04015081 RID: 86145
				public const string CannotBePacked = "The {0} shipment cannot be processed in Pack mode because it has two or more packages assigned.";

				// Token: 0x04015082 RID: 86146
				public const string BoxConfirmPrompt = "Confirm the package.";

				// Token: 0x04015083 RID: 86147
				public const string BoxConfirmOrContinuePrompt = "Confirm the package, or scan the next item.";

				// Token: 0x04015084 RID: 86148
				public const string BoxConfirmOrContinuePromptNoPick = "Confirm the package, or scan the next item or the next location.";
			}

			// Token: 0x0200D305 RID: 54021
			[PXUIField(Visible = false)]
			public class ShowPack : PXFieldAttachedTo<ScanHeader>.By<PickPackShip.Host>.AsBool.Named<PickPackShip.PackMode.ShowPack>
			{
				// Token: 0x0602FDA8 RID: 196008 RVA: 0x008E639B File Offset: 0x008E459B
				public override bool? GetValue(ScanHeader row)
				{
					return new bool?(base.Base.WMS.Get<PickPackShip.PackMode.Logic>().ShowPackTab(row));
				}
			}

			// Token: 0x0200D306 RID: 54022
			[PXUIField(DisplayName = "Packed Qty.")]
			public class PackedQtyPerBox : PXFieldAttachedTo<SOShipLineSplit>.By<PickPackShip.Host>.AsDecimal.Named<PickPackShip.PackMode.PackedQtyPerBox>
			{
				// Token: 0x0602FDAA RID: 196010 RVA: 0x008E63C0 File Offset: 0x008E45C0
				public override decimal? GetValue(SOShipLineSplit row)
				{
					SOShipLineSplitPackage soshipLineSplitPackage = base.Base.PackageDetailExt.PackageDetailSplit.SelectMain(new object[]
					{
						base.Base.WMS.RefNbr,
						base.Base.WMS.Get<PickPackShip.PackMode.Logic>().PackageLineNbrUI
					}).FirstOrDefault(delegate(SOShipLineSplitPackage t)
					{
						int? shipmentSplitLineNbr = t.ShipmentSplitLineNbr;
						int? splitLineNbr = row.SplitLineNbr;
						return shipmentSplitLineNbr.GetValueOrDefault() == splitLineNbr.GetValueOrDefault() & shipmentSplitLineNbr != null == (splitLineNbr != null);
					});
					return new decimal?(((soshipLineSplitPackage != null) ? soshipLineSplitPackage.PackedQty : null).GetValueOrDefault());
				}
			}
		}

		// Token: 0x02002F4F RID: 12111
		public sealed class PickMode : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanMode
		{
			// Token: 0x1700979E RID: 38814
			// (get) Token: 0x0601EA33 RID: 125491 RVA: 0x006F56FF File Offset: 0x006F38FF
			public override string Code
			{
				get
				{
					return "PICK";
				}
			}

			// Token: 0x1700979F RID: 38815
			// (get) Token: 0x0601EA34 RID: 125492 RVA: 0x006F5706 File Offset: 0x006F3906
			public override string Description
			{
				get
				{
					return "Pick";
				}
			}

			// Token: 0x0601EA35 RID: 125493 RVA: 0x006F5710 File Offset: 0x006F3910
			protected override bool IsModeActive()
			{
				return base.Basis.Setup.Current.ShowPickTab.GetValueOrDefault();
			}

			// Token: 0x0601EA36 RID: 125494 RVA: 0x006F573A File Offset: 0x006F393A
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickPackShip.PickMode.ShipmentState();
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState();
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState
				{
					AlternateType = new INPrimaryAlternateType?(INPrimaryAlternateType.CPN),
					IsForIssue = true,
					IsForTransfer = base.Basis.IsTransfer,
					SuppressModuleItemStatusCheck = true
				};
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState();
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState
				{
					IsForIssue = true,
					IsForTransfer = base.Basis.IsTransfer
				};
				yield return new PickPackShip.PickMode.ConfirmState();
				yield return new PickPackShip.CommandOrShipmentOnlyState();
				yield break;
			}

			// Token: 0x0601EA37 RID: 125495 RVA: 0x006F574A File Offset: 0x006F394A
			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return base.StateFlow((ScanStateFlow<PickPackShip>.IFrom flow) => flow.From<PickPackShip.PickMode.ShipmentState>().NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState>(null));
			}

			// Token: 0x0601EA38 RID: 125496 RVA: 0x006F5771 File Offset: 0x006F3971
			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.RemoveCommand();
				yield return new BarcodeQtySupport<PickPackShip, PickPackShip.Host>.SetQtyCommand();
				yield return new PickPackShip.PickMode.ConfirmShipmentPickingCommand();
				yield return new PickPackShip.ConfirmShipmentCommand();
				yield break;
			}

			// Token: 0x0601EA39 RID: 125497 RVA: 0x006F577A File Offset: 0x006F397A
			protected override IEnumerable<ScanQuestion<PickPackShip>> CreateQuestions()
			{
				yield return new PickPackShip.PickMode.ReopenPickingQuestion();
				yield break;
			}

			// Token: 0x0601EA3A RID: 125498 RVA: 0x006F5783 File Offset: 0x006F3983
			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects()
			{
				return AllWMSRedirects.CreateFor<PickPackShip>();
			}

			// Token: 0x0601EA3B RID: 125499 RVA: 0x006F578C File Offset: 0x006F398C
			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				bool when;
				if (fullReset)
				{
					if (base.Basis.IsWithinReset)
					{
						SOShipment shipment = base.Basis.Shipment;
						when = (shipment != null && shipment.Picked.GetValueOrDefault());
					}
					else
					{
						when = true;
					}
				}
				else
				{
					when = false;
				}
				base.Clear<PickPackShip.PickMode.ShipmentState>(when);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(fullReset || base.Basis.PromptLocationForEveryLine);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(fullReset);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(true);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState>(true);
			}

			// Token: 0x0400EDC6 RID: 60870
			public const string Value = "PICK";

			// Token: 0x0200D30B RID: 54027
			[Nullable(new byte[]
			{
				0,
				1,
				1,
				0
			})]
			public class value : BqlType<IBqlString, string>.Constant<PickPackShip.PickMode.value>
			{
				// Token: 0x0602FDCB RID: 196043 RVA: 0x008E69D7 File Offset: 0x008E4BD7
				public value() : base("PICK")
				{
				}
			}

			// Token: 0x0200D30C RID: 54028
			public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x0602FDCC RID: 196044 RVA: 0x008E69E4 File Offset: 0x008E4BE4
				protected virtual IEnumerable picked()
				{
					PXDelegateResult pxdelegateResult = new PXDelegateResult();
					pxdelegateResult.IsResultSorted = true;
					pxdelegateResult.AddRange(base.Basis.GetSplits(base.Basis.RefNbr, true, delegate(SOShipLineSplit s)
					{
						decimal? pickedQty = s.PickedQty;
						decimal? qty = s.Qty;
						return pickedQty.GetValueOrDefault() >= qty.GetValueOrDefault() & (pickedQty != null & qty != null);
					}));
					return pxdelegateResult;
				}

				// Token: 0x0602FDCD RID: 196045 RVA: 0x008E6A39 File Offset: 0x008E4C39
				[PXButton]
				[PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewPick(PXAdapter adapter)
				{
					return adapter.Get();
				}

				// Token: 0x0602FDCE RID: 196046 RVA: 0x008E6A41 File Offset: 0x008E4C41
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					PXAction reviewPick = this.ReviewPick;
					bool visible;
					if (base.Base.IsMobile)
					{
						ScanHeader row = e.Row;
						visible = (((row != null) ? row.Mode : null) == "PICK");
					}
					else
					{
						visible = false;
					}
					reviewPick.SetVisible(visible);
				}

				// Token: 0x0602FDCF RID: 196047 RVA: 0x008E6A7C File Offset: 0x008E4C7C
				public virtual void InjectExpireDateForPickDeactivationOnAlreadyEnteredLot(WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState expireDateState)
				{
					expireDateState.Intercept.IsStateActive.ByConjoin(delegate(PickPackShip basis)
					{
						INLotSerClass selectedLotSerialClass = basis.SelectedLotSerialClass;
						return ((selectedLotSerialClass != null) ? selectedLotSerialClass.LotSerIssueMethod : null) != "U" && basis.Get<PickPackShip.PickMode.Logic>().Picked.SelectMain(Array.Empty<object>()).Any(delegate(SOShipLineSplit t)
						{
							if (t.IsUnassigned.GetValueOrDefault())
							{
								return true;
							}
							if (string.Equals(t.LotSerialNbr, basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
							{
								decimal? pickedQty = t.PickedQty;
								decimal d = 0m;
								return pickedQty.GetValueOrDefault() == d & pickedQty != null;
							}
							return false;
						});
					}, false, null);
				}

				/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.DecorateScanState(PX.BarcodeProcessing.ScanState{`0})" />
				// Token: 0x0602FDD0 RID: 196048 RVA: 0x008E6AC4 File Offset: 0x008E4CC4
				[PXOverride]
				public ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
				{
					ScanState<PickPackShip> scanState = base_DecorateScanState(original);
					if (scanState.ModeCode == "PICK")
					{
						WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState locationState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState;
						if (locationState != null)
						{
							base.Basis.InjectLocationDeactivationOnDefaultLocationOption(locationState);
							base.Basis.InjectLocationSkippingOnPromptLocationForEveryLineOption(locationState);
							base.Basis.InjectLocationPresenceValidation(locationState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.PickMode.Logic.<DecorateScanState>g__viewSelector|6_0));
						}
						else
						{
							WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState inventoryItemState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState;
							if (inventoryItemState != null)
							{
								base.Basis.InjectItemAbsenceHandlingByLocation(inventoryItemState);
								base.Basis.InjectItemPresenceValidation(inventoryItemState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.PickMode.Logic.<DecorateScanState>g__viewSelector|6_0));
							}
							else
							{
								WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState lotSerialState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState;
								if (lotSerialState != null)
								{
									base.Basis.InjectLotSerialPresenceValidation(lotSerialState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.PickMode.Logic.<DecorateScanState>g__viewSelector|6_0));
									base.Basis.InjectLotSerialDeactivationOnDefaultLotSerialOption(lotSerialState, true);
								}
								else
								{
									WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState expireDateState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.ExpireDateState;
									if (expireDateState != null)
									{
										this.InjectExpireDateForPickDeactivationOnAlreadyEnteredLot(expireDateState);
									}
								}
							}
						}
					}
					return scanState;
				}

				/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.OnBeforeFullClear" />
				// Token: 0x0602FDD1 RID: 196049 RVA: 0x008E6BA0 File Offset: 0x008E4DA0
				[PXOverride]
				public void OnBeforeFullClear(Action base_OnBeforeFullClear)
				{
					base_OnBeforeFullClear();
					if (base.Basis.CurrentMode is PickPackShip.PickMode && base.Basis.RefNbr != null && base.Graph.WorkLogExt.SuspendFor(base.Basis.RefNbr, new Guid?(base.Graph.Accessinfo.UserID), "PICK"))
					{
						base.Graph.WorkLogExt.PersistWorkLog();
					}
				}

				/// Overrides <seealso cref="P:PX.Objects.SO.WMS.PickPackShip.DocumentIsEditable" />
				// Token: 0x0602FDD2 RID: 196050 RVA: 0x008E6C1C File Offset: 0x008E4E1C
				[PXOverride]
				public bool get_DocumentIsEditable(Func<bool> base_DocumentIsEditable)
				{
					if (base_DocumentIsEditable())
					{
						bool antecedent = base.Basis.CurrentMode is PickPackShip.PickMode;
						SOShipment shipment = base.Basis.Shipment;
						return antecedent.Implies(shipment == null || !shipment.Picked.GetValueOrDefault());
					}
					return false;
				}

				// Token: 0x0602FDD3 RID: 196051 RVA: 0x008E6C6D File Offset: 0x008E4E6D
				public virtual bool ShowPickTab(ScanHeader row)
				{
					return base.Basis.HasPick && row.Mode == "PICK";
				}

				// Token: 0x1700AF2B RID: 44843
				// (get) Token: 0x0602FDD4 RID: 196052 RVA: 0x008E6C90 File Offset: 0x008E4E90
				public virtual bool CanPick
				{
					get
					{
						if (this.Picked.SelectMain(Array.Empty<object>()).Any(delegate(SOShipLineSplit s)
						{
							decimal? pickedQty = s.PickedQty;
							decimal? qty = s.Qty;
							return pickedQty.GetValueOrDefault() < qty.GetValueOrDefault() & (pickedQty != null & qty != null);
						}))
						{
							SOShipment shipment = base.Basis.Shipment;
							return shipment == null || !shipment.Picked.GetValueOrDefault();
						}
						return false;
					}
				}

				// Token: 0x0602FDD6 RID: 196054 RVA: 0x008E6CFE File Offset: 0x008E4EFE
				[CompilerGenerated]
				internal static PXSelectBase<SOShipLineSplit> <DecorateScanState>g__viewSelector|6_0(PickPackShip basis)
				{
					return basis.Get<PickPackShip.PickMode.Logic>().Picked;
				}

				// Token: 0x04015095 RID: 86165
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
					0
				})]
				public FbqlSelect<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>>>.Order<By<BqlField<SOShipLineSplit.shipmentNbr, IBqlString>.Asc, BqlField<SOShipLineSplit.isUnassigned, IBqlBool>.Desc, BqlField<SOShipLineSplit.lineNbr, IBqlInt>.Asc>>, SOShipLineSplit>.View Picked;

				// Token: 0x04015096 RID: 86166
				public PXAction<ScanHeader> ReviewPick;
			}

			// Token: 0x0200D30D RID: 54029
			public sealed class ShipmentState : PickPackShip.ShipmentState
			{
				// Token: 0x0602FDD7 RID: 196055 RVA: 0x008E6D0C File Offset: 0x008E4F0C
				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != "I")
					{
						return Validation.Fail("The {0} shipment cannot be picked because it has the {1} operation.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.operation>(shipment)
						});
					}
					if (shipment.Status != "N")
					{
						return Validation.Fail("The {0} shipment cannot be picked because it has the {1} status.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.status>(shipment)
						});
					}
					Validation result;
					if (base.Basis.HasNonStockLinesWithEmptyLocation(shipment, out result))
					{
						return result;
					}
					return Validation.Ok;
				}

				// Token: 0x0602FDD8 RID: 196056 RVA: 0x008E6DA6 File Offset: 0x008E4FA6
				protected override void ReportSuccess(SOShipment shipment)
				{
					base.Basis.ReportInfo("{0} shipment loaded and ready to be picked.", new object[]
					{
						shipment.ShipmentNbr
					});
				}

				// Token: 0x0602FDD9 RID: 196057 RVA: 0x008E6DC8 File Offset: 0x008E4FC8
				protected override void SetNextState()
				{
					bool? remove = base.Basis.Remove;
					bool flag = false;
					if ((remove.GetValueOrDefault() == flag & remove != null) && !base.Basis.Get<PickPackShip.PickMode.Logic>().CanPick)
					{
						base.Basis.ReportInfo("{0} {1}", new object[]
						{
							base.Basis.Info.Current.Message,
							base.Basis.Localize("{0} shipment picked.", new object[]
							{
								base.Basis.RefNbr
							})
						});
						base.Basis.SetScanState("NONE", null, Array.Empty<object>());
						SOShipment shipment = base.Basis.Shipment;
						if (shipment != null && shipment.Picked.GetValueOrDefault())
						{
							base.Basis.Warn<PickPackShip.PickMode.ReopenPickingQuestion>("The {0} shipment has already been picked.", new object[]
							{
								base.Basis.RefNbr
							});
							return;
						}
					}
					else
					{
						base.SetNextState();
					}
				}

				// Token: 0x0200F0C3 RID: 61635
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					// Token: 0x040157A3 RID: 87971
					public new const string Ready = "{0} shipment loaded and ready to be picked.";

					// Token: 0x040157A4 RID: 87972
					public const string InvalidStatus = "The {0} shipment cannot be picked because it has the {1} status.";

					// Token: 0x040157A5 RID: 87973
					public const string InvalidOperation = "The {0} shipment cannot be picked because it has the {1} operation.";

					// Token: 0x040157A6 RID: 87974
					public const string AlreadyPicked = "The {0} shipment has already been picked.";
				}
			}

			// Token: 0x0200D30E RID: 54030
			public sealed class ConfirmState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ConfirmationState
			{
				// Token: 0x1700AF2C RID: 44844
				// (get) Token: 0x0602FDDB RID: 196059 RVA: 0x008E6ED0 File Offset: 0x008E50D0
				public override string Prompt
				{
					get
					{
						return base.Basis.Localize("Confirm picking {0} x {1} {2}.", new object[]
						{
							base.Basis.SightOf<WMSScanHeader.inventoryID>(),
							base.Basis.Qty,
							base.Basis.UOM
						});
					}
				}

				// Token: 0x0602FDDC RID: 196060 RVA: 0x008E6F22 File Offset: 0x008E5122
				protected override FlowStatus PerformConfirmation()
				{
					return base.Get<PickPackShip.PickMode.ConfirmState.Logic>().ConfirmPicked();
				}

				// Token: 0x0200F0C4 RID: 61636
				public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
				{
					// Token: 0x1700B0E2 RID: 45282
					// (get) Token: 0x06032483 RID: 205955 RVA: 0x00917142 File Offset: 0x00915342
					// (set) Token: 0x06032484 RID: 205956 RVA: 0x0091714A File Offset: 0x0091534A
					private protected PickPackShip.PickMode.Logic Mode { protected get; private set; }

					// Token: 0x06032485 RID: 205957 RVA: 0x00917153 File Offset: 0x00915353
					public override void Initialize()
					{
						this.Mode = base.Basis.Get<PickPackShip.PickMode.Logic>();
					}

					// Token: 0x06032486 RID: 205958 RVA: 0x00917168 File Offset: 0x00915368
					public virtual FlowStatus ConfirmPicked()
					{
						decimal num = Sign.MinusIf(base.Basis.Remove.GetValueOrDefault()) * base.Basis.BaseQty;
						bool hasSuitableSplits = false;
						if (num == 0m)
						{
							return FlowStatus.Ok.WithDispatchNext;
						}
						if (base.Basis.LotSerialTrack.IsTrackedSerial || base.Basis.SelectedInventoryItem.WeightItem.GetValueOrDefault())
						{
							SOShipLineSplit soshipLineSplit = this.GetSplitsToPick().FirstOrDefault<SOShipLineSplit>();
							if (soshipLineSplit != null)
							{
								hasSuitableSplits = true;
								bool? flag = base.Basis.Remove;
								bool flag2 = false;
								decimal num2 = ((flag.GetValueOrDefault() == flag2 & flag != null) && !base.Basis.LotSerialTrack.IsTrackedSerial) ? base.Graph.GetQtyThreshold(soshipLineSplit) : 1m;
								decimal num3 = base.Basis.Remove.GetValueOrDefault() ? (-Math.Min(soshipLineSplit.PickedQty.Value, -num)) : Math.Min(soshipLineSplit.Qty.Value * num2 - soshipLineSplit.PickedQty.Value, num);
								if (base.Basis.LotSerialTrack.IsTrackedSerial && num3.IsNotIn(1m, -1m))
								{
									return FlowStatus.Fail("Serialized items can be processed only with the base UOM and the 1.00 quantity.", Array.Empty<object>());
								}
								FlowStatus result = this.PickSplit(soshipLineSplit, num, num2);
								flag = result.IsError;
								flag2 = false;
								if (!(flag.GetValueOrDefault() == flag2 & flag != null))
								{
									return result;
								}
								num -= num3;
							}
						}
						else
						{
							IEnumerable<SOShipLineSplit> splitsToPick = this.GetSplitsToPick().Select(delegate(SOShipLineSplit s)
							{
								hasSuitableSplits = true;
								return s;
							});
							FlowStatus result2 = this.PickAllSplits(splitsToPick, ref num, false);
							bool? flag = result2.IsError;
							bool flag2 = false;
							if (!(flag.GetValueOrDefault() == flag2 & flag != null))
							{
								return result2;
							}
							if (num != 0m)
							{
								flag = base.Basis.Remove;
								flag2 = false;
								if ((flag.GetValueOrDefault() == flag2 & flag != null) && base.Basis.SelectedInventoryItem.DecimalBaseUnit.GetValueOrDefault())
								{
									FlowStatus result3 = this.PickAllSplits(splitsToPick, ref num, true);
									flag = result3.IsError;
									flag2 = false;
									if (!(flag.GetValueOrDefault() == flag2 & flag != null))
									{
										return result3;
									}
								}
							}
						}
						if (!hasSuitableSplits)
						{
							return FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "No items to remove from shipment." : "No items to pick.", Array.Empty<object>()).WithModeReset;
						}
						if (Math.Abs(num) > 0m)
						{
							return FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "The picked quantity cannot be negative." : "The picked quantity cannot be greater than the quantity in the shipment line.", Array.Empty<object>()).WithModeReset.WithChangesDiscard;
						}
						this.EnsureShipmentUserLinkForPick();
						base.Basis.ReportInfo(base.Basis.Remove.GetValueOrDefault() ? "{0} x {1} {2} removed from shipment." : "{0} x {1} {2} added to shipment.", new object[]
						{
							base.Basis.SightOf<WMSScanHeader.inventoryID>(),
							base.Basis.Qty,
							base.Basis.UOM
						});
						return FlowStatus.Ok.WithDispatchNext;
					}

					// Token: 0x06032487 RID: 205959 RVA: 0x00917504 File Offset: 0x00915704
					public virtual FlowStatus PickAllSplits(IEnumerable<SOShipLineSplit> splitsToPick, ref decimal restDeltaQty, bool withThresholds)
					{
						foreach (SOShipLineSplit soshipLineSplit in splitsToPick)
						{
							if (!withThresholds)
							{
								goto IL_3B;
							}
							bool? flag = base.Basis.Remove;
							bool flag2 = false;
							if (!(flag.GetValueOrDefault() == flag2 & flag != null))
							{
								goto IL_3B;
							}
							decimal num = base.Graph.GetQtyThreshold(soshipLineSplit);
							IL_4E:
							decimal num2 = num;
							decimal num3 = base.Basis.Remove.GetValueOrDefault() ? (-Math.Min(soshipLineSplit.PickedQty.Value, -restDeltaQty)) : Math.Min(soshipLineSplit.Qty.Value * num2 - soshipLineSplit.PickedQty.Value, restDeltaQty);
							FlowStatus flowStatus = this.PickSplit(soshipLineSplit, num3, num2);
							flag = flowStatus.IsError;
							flag2 = false;
							if (!(flag.GetValueOrDefault() == flag2 & flag != null))
							{
								return flowStatus.WithChangesDiscard;
							}
							restDeltaQty -= num3;
							if (restDeltaQty == 0m)
							{
								break;
							}
							continue;
							IL_3B:
							num = 1m;
							goto IL_4E;
						}
						return FlowStatus.Ok;
					}

					// Token: 0x06032488 RID: 205960 RVA: 0x0091766C File Offset: 0x0091586C
					public virtual FlowStatus PickSplit(SOShipLineSplit pickedSplit, decimal deltaQty, decimal threshold)
					{
						bool flag = false;
						if (deltaQty < 0m)
						{
							decimal? num = pickedSplit.PickedQty + deltaQty;
							decimal d = 0m;
							if (num.GetValueOrDefault() < d & num != null)
							{
								return FlowStatus.Fail("The picked quantity cannot be negative.", Array.Empty<object>());
							}
							num = pickedSplit.PickedQty + deltaQty;
							decimal? num2 = pickedSplit.PackedQty;
							if (num.GetValueOrDefault() < num2.GetValueOrDefault() & (num != null & num2 != null))
							{
								return FlowStatus.Fail("The picked quantity cannot be less than the already packed quantity.", Array.Empty<object>());
							}
						}
						else
						{
							decimal? num2 = pickedSplit.PickedQty + deltaQty;
							decimal? num = pickedSplit.Qty * threshold;
							if (num2.GetValueOrDefault() > num.GetValueOrDefault() & (num2 != null & num != null))
							{
								return FlowStatus.Fail("The picked quantity cannot be greater than the quantity in the shipment line.", Array.Empty<object>());
							}
							if (!string.Equals(pickedSplit.LotSerialNbr, base.Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase) && base.Basis.LotSerialTrack.IsEnterable)
							{
								if (!this.SetLotSerialNbrAndQty(pickedSplit, deltaQty))
								{
									return FlowStatus.Fail("The picked quantity cannot be greater than the quantity in the shipment line.", Array.Empty<object>());
								}
								flag = true;
							}
						}
						if (!flag)
						{
							base.Basis.EnsureAssignedSplitEditing(pickedSplit);
							pickedSplit.PickedQty += deltaQty;
							if (deltaQty < 0m && base.Basis.LotSerialTrack.IsEnterable)
							{
								decimal? num = pickedSplit.PickedQty - deltaQty;
								decimal? num2 = pickedSplit.Qty;
								if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
								{
									pickedSplit.Qty = pickedSplit.PickedQty;
								}
								num2 = pickedSplit.Qty;
								decimal d = 0m;
								if (num2.GetValueOrDefault() == d & num2 != null)
								{
									this.Mode.Picked.Delete(pickedSplit);
								}
								else
								{
									this.Mode.Picked.Update(pickedSplit);
								}
							}
							else
							{
								this.Mode.Picked.Update(pickedSplit);
							}
						}
						return FlowStatus.Ok;
					}

					// Token: 0x06032489 RID: 205961 RVA: 0x00917984 File Offset: 0x00915B84
					public virtual bool IsSelectedSplit(SOShipLineSplit split)
					{
						int? num = split.InventoryID;
						int? num2 = base.Basis.InventoryID;
						if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
						{
							num2 = split.SubItemID;
							num = base.Basis.SubItemID;
							if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
							{
								num = split.SiteID;
								num2 = base.Basis.SiteID;
								if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
								{
									num2 = split.LocationID;
									int? locationID = base.Basis.LocationID;
									num = ((locationID != null) ? locationID : split.LocationID);
									if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
									{
										if (string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
										{
											return true;
										}
										bool? remove = base.Basis.Remove;
										bool flag = false;
										if (!(remove.GetValueOrDefault() == flag & remove != null))
										{
											return false;
										}
										if (base.Basis.SelectedLotSerialClass.LotSerAssign == "U")
										{
											return true;
										}
										if (base.Basis.SelectedLotSerialClass.LotSerIssueMethod == "U")
										{
											decimal? packedQty = split.PackedQty;
											decimal d = 0m;
											return packedQty.GetValueOrDefault() == d & packedQty != null;
										}
										return false;
									}
								}
							}
						}
						return false;
					}

					// Token: 0x0603248A RID: 205962 RVA: 0x00917B3C File Offset: 0x00915D3C
					public virtual bool SetLotSerialNbrAndQty(SOShipLineSplit pickedSplit, decimal qty)
					{
						PXSelectBase<SOShipLineSplit> picked = this.Mode.Picked;
						decimal? pickedQty = pickedSplit.PickedQty;
						decimal d = 0m;
						bool? isUnassigned;
						bool flag;
						if (pickedQty.GetValueOrDefault() == d & pickedQty != null)
						{
							isUnassigned = pickedSplit.IsUnassigned;
							flag = false;
							if (isUnassigned.GetValueOrDefault() == flag & isUnassigned != null)
							{
								if (!base.Basis.LotSerialTrack.IsTrackedSerial || !(base.Basis.SelectedLotSerialClass.LotSerIssueMethod == "U"))
								{
									pickedSplit.LotSerialNbr = base.Basis.LotSerialNbr;
									if (base.Basis.LotSerialTrack.HasExpiration && base.Basis.ExpireDate != null)
									{
										pickedSplit.ExpireDate = base.Basis.ExpireDate;
									}
									pickedSplit.PickedQty += qty;
									pickedSplit = picked.Update(pickedSplit);
									return true;
								}
								SOShipLineSplit soshipLineSplit = PXSelectBase<SOShipLineSplit, PXViewOf<SOShipLineSplit>.BasedOn<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOLineSplit>.On<SOShipLineSplit.FK.OriginalLineSplit>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplit.shipmentNbr, Equal<BqlField<WMSScanHeader.refNbr, IBqlString>.FromCurrent>>>>>.And<BqlOperand<SOLineSplit.lotSerialNbr, IBqlString>.IsEqual<P.AsString>>>>.Config>.Select(base.Basis, new object[]
								{
									base.Basis.LotSerialNbr
								});
								if (soshipLineSplit == null)
								{
									pickedSplit.LotSerialNbr = base.Basis.LotSerialNbr;
									pickedSplit.PickedQty += qty;
									pickedSplit = picked.Update(pickedSplit);
									return true;
								}
								if (string.Equals(soshipLineSplit.LotSerialNbr, base.Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
								{
									return false;
								}
								SOShipLineSplit soshipLineSplit2 = PXCache<SOShipLineSplit>.CreateCopy(soshipLineSplit);
								SOShipLineSplit soshipLineSplit3 = PXCache<SOShipLineSplit>.CreateCopy(pickedSplit);
								soshipLineSplit.Qty = new decimal?(0m);
								soshipLineSplit.LotSerialNbr = base.Basis.LotSerialNbr;
								soshipLineSplit = picked.Update(soshipLineSplit);
								soshipLineSplit.Qty = soshipLineSplit2.Qty;
								soshipLineSplit.PickedQty = soshipLineSplit3.PickedQty + qty;
								soshipLineSplit.ExpireDate = soshipLineSplit3.ExpireDate;
								soshipLineSplit = picked.Update(soshipLineSplit);
								pickedSplit.Qty = new decimal?(0m);
								pickedSplit.LotSerialNbr = soshipLineSplit2.LotSerialNbr;
								pickedSplit = picked.Update(pickedSplit);
								pickedSplit.Qty = soshipLineSplit3.Qty;
								pickedSplit.PickedQty = soshipLineSplit2.PickedQty;
								pickedSplit.ExpireDate = soshipLineSplit2.ExpireDate;
								pickedSplit = picked.Update(pickedSplit);
								return true;
							}
						}
						SOShipLineSplit soshipLineSplit4 = (pickedSplit.IsUnassigned.GetValueOrDefault() || base.Basis.LotSerialTrack.IsTrackedLot) ? picked.SelectMain(Array.Empty<object>()).FirstOrDefault(delegate(SOShipLineSplit s)
						{
							int? num2 = s.LineNbr;
							int? num3 = pickedSplit.LineNbr;
							if (num2.GetValueOrDefault() == num3.GetValueOrDefault() & num2 != null == (num3 != null))
							{
								bool? isUnassigned2 = s.IsUnassigned;
								bool flag2 = false;
								if (isUnassigned2.GetValueOrDefault() == flag2 & isUnassigned2 != null)
								{
									num3 = s.LocationID;
									int? locationID = this.Basis.LocationID;
									num2 = ((locationID != null) ? locationID : pickedSplit.LocationID);
									if ((num3.GetValueOrDefault() == num2.GetValueOrDefault() & num3 != null == (num2 != null)) && string.Equals(s.LotSerialNbr, this.Basis.LotSerialNbr ?? s.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
									{
										return this.IsSelectedSplit(s);
									}
								}
							}
							return false;
						}) : null;
						isUnassigned = pickedSplit.IsUnassigned;
						flag = false;
						bool suppress = (isUnassigned.GetValueOrDefault() == flag & isUnassigned != null) && base.Basis.LotSerialTrack.IsTrackedLot;
						decimal? num;
						if (soshipLineSplit4 != null)
						{
							soshipLineSplit4.PickedQty += qty;
							pickedQty = soshipLineSplit4.PickedQty;
							num = soshipLineSplit4.Qty;
							if (pickedQty.GetValueOrDefault() > num.GetValueOrDefault() & (pickedQty != null & num != null))
							{
								soshipLineSplit4.Qty = soshipLineSplit4.PickedQty;
							}
							using (base.Graph.LineSplittingExt.SuppressedModeScope(suppress))
							{
								soshipLineSplit4 = picked.Update(soshipLineSplit4);
								goto IL_5C5;
							}
						}
						SOShipLineSplit soshipLineSplit5 = PXCache<SOShipLineSplit>.CreateCopy(pickedSplit);
						soshipLineSplit5.SplitLineNbr = null;
						soshipLineSplit5.PlanID = null;
						soshipLineSplit5.LotSerialNbr = base.Basis.LotSerialNbr;
						num = pickedSplit.Qty - qty;
						d = 0m;
						if ((num.GetValueOrDefault() > d & num != null) || pickedSplit.IsUnassigned.GetValueOrDefault())
						{
							soshipLineSplit5.Qty = new decimal?(qty);
							soshipLineSplit5.PickedQty = new decimal?(qty);
							soshipLineSplit5.PackedQty = new decimal?(0m);
							soshipLineSplit5.IsUnassigned = new bool?(false);
						}
						else
						{
							soshipLineSplit5.Qty = pickedSplit.Qty;
							soshipLineSplit5.PickedQty = pickedSplit.PickedQty;
							soshipLineSplit5.PackedQty = pickedSplit.PackedQty;
						}
						using (base.Graph.LineSplittingExt.SuppressedModeScope(suppress))
						{
							soshipLineSplit5 = picked.Insert(soshipLineSplit5);
						}
						IL_5C5:
						isUnassigned = pickedSplit.IsUnassigned;
						flag = false;
						if (isUnassigned.GetValueOrDefault() == flag & isUnassigned != null)
						{
							num = pickedSplit.Qty;
							d = 0m;
							if (num.GetValueOrDefault() <= d & num != null)
							{
								pickedSplit = picked.Delete(pickedSplit);
							}
							else
							{
								pickedSplit.Qty -= qty;
								pickedSplit = picked.Update(pickedSplit);
							}
						}
						return true;
					}

					// Token: 0x0603248B RID: 205963 RVA: 0x009181E0 File Offset: 0x009163E0
					[Obsolete("Use the GetSplitsToPick method instead.")]
					public virtual SOShipLineSplit GetPickedSplit()
					{
						return this.GetSplitsToPick().FirstOrDefault<SOShipLineSplit>();
					}

					// Token: 0x0603248C RID: 205964 RVA: 0x009181ED File Offset: 0x009163ED
					public virtual IEnumerable<SOShipLineSplit> GetSplitsToPick()
					{
						return this.Mode.Picked.SelectMain(Array.Empty<object>()).Where(new Func<SOShipLineSplit, bool>(this.IsSelectedSplit)).With(new Func<IEnumerable<SOShipLineSplit>, IOrderedEnumerable<SOShipLineSplit>>(this.PrioritizeSplits));
					}

					// Token: 0x0603248D RID: 205965 RVA: 0x00918228 File Offset: 0x00916428
					public virtual IOrderedEnumerable<SOShipLineSplit> PrioritizeSplits(IEnumerable<SOShipLineSplit> splits)
					{
						if (base.Basis.Remove.GetValueOrDefault())
						{
							return splits.OrderByAccordanceTo(delegate(SOShipLineSplit split)
							{
								decimal? pickedQty = split.PickedQty;
								decimal d = 0m;
								return pickedQty.GetValueOrDefault() > d & pickedQty != null;
							}).ThenByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenBy(delegate(SOShipLineSplit split)
							{
								decimal? qty = split.Qty;
								decimal? pickedQty = split.PickedQty;
								if (!(qty != null & pickedQty != null))
								{
									return null;
								}
								return new decimal?(qty.GetValueOrDefault() - pickedQty.GetValueOrDefault());
							});
						}
						if (base.Basis.LotSerialTrack.IsTrackedSerial)
						{
							return splits.OrderByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenByAccordanceTo(delegate(SOShipLineSplit split)
							{
								decimal? pickedQty = split.PickedQty;
								decimal d = 0m;
								return pickedQty.GetValueOrDefault() == d & pickedQty != null;
							}).ThenByAccordanceTo((SOShipLineSplit split) => split.IsUnassigned.GetValueOrDefault());
						}
						return splits.OrderByAccordanceTo(delegate(SOShipLineSplit split)
						{
							decimal? qty = split.Qty;
							decimal? pickedQty = split.PickedQty;
							return qty.GetValueOrDefault() > pickedQty.GetValueOrDefault() & (qty != null & pickedQty != null);
						}).ThenByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenByAccordanceTo((SOShipLineSplit split) => string.IsNullOrEmpty(split.LotSerialNbr)).ThenByAccordanceTo(delegate(SOShipLineSplit split)
						{
							decimal? pickedQty = split.PickedQty;
							decimal d = 0m;
							return pickedQty.GetValueOrDefault() > d & pickedQty != null;
						}).ThenByDescending(delegate(SOShipLineSplit split)
						{
							decimal? qty = split.Qty;
							decimal? pickedQty = split.PickedQty;
							if (!(qty != null & pickedQty != null))
							{
								return null;
							}
							return new decimal?(qty.GetValueOrDefault() - pickedQty.GetValueOrDefault());
						});
					}

					// Token: 0x0603248E RID: 205966 RVA: 0x009183B7 File Offset: 0x009165B7
					public virtual void EnsureShipmentUserLinkForPick()
					{
						base.Graph.WorkLogExt.EnsureFor(base.Basis.RefNbr, new Guid?(base.Graph.Accessinfo.UserID), "PICK");
					}
				}

				// Token: 0x0200F0C5 RID: 61637
				[PXLocalizable]
				public new abstract class Msg
				{
					// Token: 0x040157A8 RID: 87976
					public const string Prompt = "Confirm picking {0} x {1} {2}.";

					// Token: 0x040157A9 RID: 87977
					public const string NothingToPick = "No items to pick.";

					// Token: 0x040157AA RID: 87978
					public const string NothingToRemove = "No items to remove from shipment.";

					// Token: 0x040157AB RID: 87979
					public const string Overpicking = "The picked quantity cannot be greater than the quantity in the shipment line.";

					// Token: 0x040157AC RID: 87980
					public const string Underpicking = "The picked quantity cannot be negative.";

					// Token: 0x040157AD RID: 87981
					public const string UnderpickingByPack = "The picked quantity cannot be less than the already packed quantity.";

					// Token: 0x040157AE RID: 87982
					public const string InventoryAdded = "{0} x {1} {2} added to shipment.";

					// Token: 0x040157AF RID: 87983
					public const string InventoryRemoved = "{0} x {1} {2} removed from shipment.";
				}
			}

			// Token: 0x0200D30F RID: 54031
			public sealed class ConfirmShipmentPickingCommand : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanCommand
			{
				// Token: 0x1700AF2D RID: 44845
				// (get) Token: 0x0602FDDE RID: 196062 RVA: 0x008E6F37 File Offset: 0x008E5137
				public override string Code
				{
					get
					{
						return "CONFIRM*PICK";
					}
				}

				// Token: 0x1700AF2E RID: 44846
				// (get) Token: 0x0602FDDF RID: 196063 RVA: 0x008E6F3E File Offset: 0x008E513E
				public override string ButtonName
				{
					get
					{
						return "scanConfirmShipmentPicking";
					}
				}

				// Token: 0x1700AF2F RID: 44847
				// (get) Token: 0x0602FDE0 RID: 196064 RVA: 0x008E6F45 File Offset: 0x008E5145
				public override string DisplayName
				{
					get
					{
						return "Confirm Picking";
					}
				}

				// Token: 0x1700AF30 RID: 44848
				// (get) Token: 0x0602FDE1 RID: 196065 RVA: 0x008E6F4C File Offset: 0x008E514C
				protected override bool IsEnabled
				{
					get
					{
						return base.Basis.DocumentIsEditable;
					}
				}

				// Token: 0x0602FDE2 RID: 196066 RVA: 0x008E6F59 File Offset: 0x008E5159
				protected override bool Process()
				{
					return base.Basis.Get<PickPackShip.PickMode.ConfirmShipmentPickingCommand.Logic>().ConfirmShipmentPicking();
				}

				// Token: 0x0200F0C6 RID: 61638
				public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
				{
					// Token: 0x06032494 RID: 205972 RVA: 0x00918460 File Offset: 0x00916660
					public virtual bool ConfirmShipmentPicking()
					{
						if (!this.CanConfirmPicking())
						{
							return true;
						}
						base.Basis.SaveChanges();
						base.Basis.WaitFor<SOShipment>(delegate(PickPackShip basis, SOShipment doc)
						{
							PickPackShip.PickMode.ConfirmShipmentPickingCommand.Logic.ConfirmPickingHandler(doc.ShipmentNbr);
						}).WithDescription("Marking the {0} shipment as picked.", new object[]
						{
							base.Basis.RefNbr
						}).ActualizeDataBy((PickPackShip basis, SOShipment doc) => PrimaryKeyOf<SOShipment>.By<SOShipment.shipmentNbr>.Find(basis, doc, PKFindOptions.None)).OnSuccess(delegate(ScanLongRunAwaiter<PickPackShip, SOShipment>.ISuccessProcessor x)
						{
							x.Say("The shipment has been successfully marked as picked.", Array.Empty<object>()).ChangeStateTo<PickPackShip.PickMode.ShipmentState>();
						}).OnFail(delegate(ScanLongRunAwaiter<PickPackShip, SOShipment>.IResultProcessor x)
						{
							x.Say("The shipment could not be marked as picked.", Array.Empty<object>());
						}).BeginAwait(base.Basis.Shipment);
						return true;
					}

					// Token: 0x06032495 RID: 205973 RVA: 0x00918547 File Offset: 0x00916747
					protected static void ConfirmPickingHandler(string shipmentNbr)
					{
						PXGraph.CreateInstance<SOShipmentEntry>().FindImplementation<PickPackShip.PickMode.ConfirmShipmentPickingCommand.PickingConfirmation>().ConfirmPickedQtyAndMarkShipmentPicked(shipmentNbr);
					}

					// Token: 0x06032496 RID: 205974 RVA: 0x0091855C File Offset: 0x0091675C
					protected virtual bool CanConfirmPicking()
					{
						SOShipLineSplit[] source = base.Basis.Get<PickPackShip.PickMode.Logic>().Picked.SelectMain(Array.Empty<object>());
						if (source.All(delegate(SOShipLineSplit s)
						{
							decimal? pickedQty = s.PickedQty;
							decimal d = 0m;
							return pickedQty.GetValueOrDefault() == d & pickedQty != null;
						}))
						{
							base.Basis.ReportError("The shipment cannot be marked as picked because no items have been picked.", Array.Empty<object>());
							return false;
						}
						if (base.Basis.Info.Current.MessageType != "WRN" && source.Any(delegate(SOShipLineSplit s)
						{
							decimal? pickedQty = s.PickedQty;
							decimal? num = s.Qty * base.Basis.Graph.GetMinQtyThreshold(s);
							return pickedQty.GetValueOrDefault() < num.GetValueOrDefault() & (pickedQty != null & num != null);
						}))
						{
							if (base.Basis.CannotConfirmPartialShipments)
							{
								base.Basis.ReportError("The shipment cannot be marked as picked because it is not complete.", Array.Empty<object>());
							}
							else
							{
								base.Basis.ReportWarning("The shipment is incomplete and should not be marked as picked. Do you want to finish picking the shipment?", Array.Empty<object>());
							}
							return false;
						}
						if (base.Basis.HasIncompleteLinesBy<SOShipLineSplit.pickedQty>())
						{
							base.Basis.ReportError("The shipment cannot be marked as picked because it is not complete.", Array.Empty<object>());
							return false;
						}
						return true;
					}
				}

				// Token: 0x0200F0C7 RID: 61639
				public class PickingConfirmation : PXGraphExtension<SOShipmentEntry>
				{
					// Token: 0x06032499 RID: 205977 RVA: 0x009186D8 File Offset: 0x009168D8
					public static bool IsActive()
					{
						return PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
					}

					// Token: 0x0603249A RID: 205978 RVA: 0x009186E0 File Offset: 0x009168E0
					public virtual void ConfirmPickedQtyAndMarkShipmentPicked(string shipmentNbr)
					{
						NonStockKitSpecHelper nonStockKitSpecHelper = new NonStockKitSpecHelper(base.Base);
						Func<int, bool> RequireShipping = Func.Memorize<int, bool>((int inventoryID) => InventoryItem.PK.Find(this.Base, new int?(inventoryID), PKFindOptions.None).With((InventoryItem item) => item.StkItem.GetValueOrDefault() || item.NonStockShip.GetValueOrDefault()));
						PXSelectBase<SOShipment> document = base.Base.Document;
						PXSelectBase<SOShipLine> transactions = base.Base.Transactions;
						PXSelectBase<SOShipLineSplit> splits = base.Base.splits;
						document.Current = PXSelectBase<SOShipment, PXViewOf<SOShipment>.BasedOn<SelectFromBase<SOShipment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOShipment.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
						{
							shipmentNbr
						});
						decimal num = 0m;
						Func<KeyValuePair<int, decimal>, bool> <>9__3;
						foreach (PXResult<SOShipLine> r2 in transactions.Select(Array.Empty<object>()))
						{
							SOShipLine soshipLine = r2;
							transactions.Current = soshipLine;
							decimal num2 = 0m;
							if (nonStockKitSpecHelper.IsNonStockKit(transactions.Current.InventoryID))
							{
								IEnumerable<KeyValuePair<int, decimal>> nonStockKitSpec = nonStockKitSpecHelper.GetNonStockKitSpec(transactions.Current.InventoryID.Value);
								Func<KeyValuePair<int, decimal>, bool> predicate;
								if ((predicate = <>9__3) == null)
								{
									predicate = (<>9__3 = ((KeyValuePair<int, decimal> pair) => RequireShipping(pair.Key)));
								}
								Dictionary<int, decimal> dictionary = nonStockKitSpec.Where(predicate).ToDictionary<int, decimal>();
								Dictionary<int, decimal> dictionary2 = (from r in splits.SelectMain(Array.Empty<object>())
								group r by r.InventoryID.Value).ToDictionary((IGrouping<int, SOShipLineSplit> g) => g.Key, (IGrouping<int, SOShipLineSplit> g) => g.Sum((SOShipLineSplit s) => s.PickedQty.GetValueOrDefault()));
								decimal num3;
								if (dictionary.Keys.Count<int>() != 0 && dictionary.Keys.Except(dictionary2.Keys).Count<int>() <= 0)
								{
									num3 = dictionary2.Join(dictionary, delegate(KeyValuePair<int, decimal> split)
									{
										KeyValuePair<int, decimal> keyValuePair = split;
										return keyValuePair.Key;
									}, delegate(KeyValuePair<int, decimal> spec)
									{
										KeyValuePair<int, decimal> keyValuePair = spec;
										return keyValuePair.Key;
									}, delegate(KeyValuePair<int, decimal> split, KeyValuePair<int, decimal> spec)
									{
										KeyValuePair<int, decimal> keyValuePair = split;
										decimal value = keyValuePair.Value;
										keyValuePair = spec;
										return Math.Floor(decimal.Divide(value, keyValuePair.Value));
									}).Min();
								}
								else
								{
									num3 = 0m;
								}
								num2 = num3;
							}
							else
							{
								num2 = INUnitAttribute.ConvertFromBase(transactions.Cache, transactions.Current.InventoryID, transactions.Current.UOM, splits.SelectMain(Array.Empty<object>()).Sum((SOShipLineSplit s) => s.PickedQty.GetValueOrDefault()), INPrecision.NOROUND);
							}
							transactions.Cache.MarkUpdated(soshipLine, true);
							decimal? pickedQty = transactions.Current.PickedQty;
							transactions.Current.PickedQty = new decimal?(num2);
							transactions.Cache.RaiseFieldUpdated<SOShipLine.pickedQty>(transactions.Current, pickedQty);
							num += num2;
						}
						document.Cache.MarkUpdated(document.Current, true);
						decimal? pickedQty2 = document.Current.PickedQty;
						document.Current.PickedQty = new decimal?(num);
						document.Cache.RaiseFieldUpdated<SOShipment.pickedQty>(document.Current, pickedQty2);
						bool? picked = document.Current.Picked;
						document.Current.Picked = new bool?(true);
						document.Cache.RaiseFieldUpdated<SOShipment.picked>(document.Current, picked);
						base.Base.Save.Press();
					}

					// Token: 0x0603249B RID: 205979 RVA: 0x00918A7C File Offset: 0x00916C7C
					public virtual void ReopenPickedShipment(string shipmentNbr)
					{
						PXSelectBase<SOShipment> document = base.Base.Document;
						PXSelectBase<SOShipLine> transactions = base.Base.Transactions;
						document.Current = PXSelectBase<SOShipment, PXViewOf<SOShipment>.BasedOn<SelectFromBase<SOShipment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOShipment.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(base.Base, new object[]
						{
							shipmentNbr
						});
						foreach (PXResult<SOShipLine> r in transactions.Select(Array.Empty<object>()))
						{
							SOShipLine soshipLine = r;
							transactions.Current = soshipLine;
							transactions.Cache.MarkUpdated(soshipLine, true);
							decimal? pickedQty = transactions.Current.PickedQty;
							transactions.Current.PickedQty = new decimal?(0m);
							transactions.Cache.RaiseFieldUpdated<SOShipLine.pickedQty>(transactions.Current, pickedQty);
						}
						document.Cache.MarkUpdated(document.Current, true);
						decimal? pickedQty2 = document.Current.PickedQty;
						document.Current.PickedQty = new decimal?(0m);
						document.Cache.RaiseFieldUpdated<SOShipment.pickedQty>(document.Current, pickedQty2);
						bool? picked = document.Current.Picked;
						document.Current.Picked = new bool?(false);
						document.Cache.RaiseFieldUpdated<SOShipment.picked>(document.Current, picked);
						base.Base.Save.Press();
					}
				}

				// Token: 0x0200F0C8 RID: 61640
				[PXLocalizable]
				public abstract class Msg
				{
					// Token: 0x040157B0 RID: 87984
					public const string DisplayName = "Confirm Picking";

					// Token: 0x040157B1 RID: 87985
					public const string InProcess = "Marking the {0} shipment as picked.";

					// Token: 0x040157B2 RID: 87986
					public const string Success = "The shipment has been successfully marked as picked.";

					// Token: 0x040157B3 RID: 87987
					public const string Fail = "The shipment could not be marked as picked.";

					// Token: 0x040157B4 RID: 87988
					public const string PickingCannotBeConfirmed = "The shipment cannot be marked as picked because no items have been picked.";

					// Token: 0x040157B5 RID: 87989
					public const string PickingCannotBeConfirmedInPart = "The shipment cannot be marked as picked because it is not complete.";

					// Token: 0x040157B6 RID: 87990
					public const string PickingShouldNotBeConfirmedInPart = "The shipment is incomplete and should not be marked as picked. Do you want to finish picking the shipment?";
				}
			}

			// Token: 0x0200D310 RID: 54032
			public sealed class ReopenPickingQuestion : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanQuestion
			{
				// Token: 0x1700AF31 RID: 44849
				// (get) Token: 0x0602FDE4 RID: 196068 RVA: 0x008E6F73 File Offset: 0x008E5173
				public override string Code
				{
					get
					{
						return "REOPENPICK";
					}
				}

				// Token: 0x0602FDE5 RID: 196069 RVA: 0x008E6F7A File Offset: 0x008E517A
				protected override string GetPrompt()
				{
					return "To continue picking the current shipment, click OK.";
				}

				// Token: 0x0602FDE6 RID: 196070 RVA: 0x008E6F84 File Offset: 0x008E5184
				protected override void Confirm()
				{
					string refNbr = base.Basis.RefNbr;
					PXGraph.CreateInstance<SOShipmentEntry>().FindImplementation<PickPackShip.PickMode.ConfirmShipmentPickingCommand.PickingConfirmation>().ReopenPickedShipment(base.Basis.RefNbr);
					base.Basis.Graph.Clear();
					base.Basis.TryProcessBy<PickPackShip.PickMode.ShipmentState>(refNbr, StateSubstitutionRule.KeepAll);
				}

				// Token: 0x0602FDE7 RID: 196071 RVA: 0x008E6FD9 File Offset: 0x008E51D9
				protected override void Reject()
				{
					base.Basis.Clear<PickPackShip.PickMode.ShipmentState>(true);
				}

				// Token: 0x0200F0C9 RID: 61641
				[PXLocalizable]
				public static class Msg
				{
					// Token: 0x040157B7 RID: 87991
					public const string Prompt = "To continue picking the current shipment, click OK.";
				}
			}

			// Token: 0x0200D311 RID: 54033
			public sealed class RedirectFrom<TForeignBasis> : PX.BarcodeProcessing.RedirectFrom<TForeignBasis>.To<PickPackShip>.SetMode<PickPackShip.PickMode> where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				// Token: 0x1700AF32 RID: 44850
				// (get) Token: 0x0602FDE9 RID: 196073 RVA: 0x008E6FEF File Offset: 0x008E51EF
				public override string Code
				{
					get
					{
						return "PICK";
					}
				}

				// Token: 0x1700AF33 RID: 44851
				// (get) Token: 0x0602FDEA RID: 196074 RVA: 0x008E6FF6 File Offset: 0x008E51F6
				public override string DisplayName
				{
					get
					{
						return "Pick";
					}
				}

				// Token: 0x1700AF34 RID: 44852
				// (get) Token: 0x0602FDEB RID: 196075 RVA: 0x008E6FFD File Offset: 0x008E51FD
				// (set) Token: 0x0602FDEC RID: 196076 RVA: 0x008E7005 File Offset: 0x008E5205
				private string RefNbr { get; set; }

				// Token: 0x1700AF35 RID: 44853
				// (get) Token: 0x0602FDED RID: 196077 RVA: 0x008E7010 File Offset: 0x008E5210
				public override bool IsPossible
				{
					get
					{
						bool flag = PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
						SOPickPackShipSetup sopickPackShipSetup = SOPickPackShipSetup.PK.Find(base.Basis.Graph, base.Basis.Graph.Accessinfo.BranchID, PKFindOptions.None);
						return flag && sopickPackShipSetup != null && sopickPackShipSetup.ShowPickTab.GetValueOrDefault();
					}
				}

				// Token: 0x0602FDEE RID: 196078 RVA: 0x008E706C File Offset: 0x008E526C
				protected override bool PrepareRedirect()
				{
					PickPackShip pickPackShip = base.Basis as PickPackShip;
					if (pickPackShip != null && pickPackShip.RefNbr != null && !pickPackShip.DocumentIsConfirmed)
					{
						Validation? validation = pickPackShip.FindMode<PickPackShip.PickMode>().TryValidate<SOShipment>(pickPackShip.Shipment).By<PickPackShip.PickMode.ShipmentState>();
						if (validation != null)
						{
							Validation valueOrDefault = validation.GetValueOrDefault();
							if (valueOrDefault.IsError.GetValueOrDefault())
							{
								pickPackShip.ReportError(valueOrDefault.Message, valueOrDefault.MessageArgs);
								return false;
							}
						}
						this.RefNbr = pickPackShip.RefNbr;
					}
					return true;
				}

				// Token: 0x0602FDEF RID: 196079 RVA: 0x008E7100 File Offset: 0x008E5300
				protected override void CompleteRedirect()
				{
					PickPackShip pickPackShip = base.Basis as PickPackShip;
					if (pickPackShip != null && pickPackShip.CurrentMode.Code != "CRTN" && this.RefNbr != null && pickPackShip.TryProcessBy("RNBR", this.RefNbr, (StateSubstitutionRule)253))
					{
						pickPackShip.SetDefaultState(null, Array.Empty<object>());
						this.RefNbr = null;
					}
				}
			}

			// Token: 0x0200D312 RID: 54034
			[PXLocalizable]
			public new abstract class Msg : ScanMode<PickPackShip>.Msg
			{
				// Token: 0x04015098 RID: 86168
				public const string Description = "Pick";

				// Token: 0x04015099 RID: 86169
				public const string Completed = "{0} shipment picked.";
			}

			// Token: 0x0200D313 RID: 54035
			[PXUIField(Visible = false)]
			public class ShowPick : PXFieldAttachedTo<ScanHeader>.By<PickPackShip.Host>.AsBool.Named<PickPackShip.PickMode.ShowPick>
			{
				// Token: 0x0602FDF2 RID: 196082 RVA: 0x008E717B File Offset: 0x008E537B
				public override bool? GetValue(ScanHeader row)
				{
					return new bool?(base.Base.WMS.Get<PickPackShip.PickMode.Logic>().ShowPickTab(row));
				}
			}
		}

		// Token: 0x02002F50 RID: 12112
		public sealed class ReturnMode : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanMode
		{
			// Token: 0x170097A0 RID: 38816
			// (get) Token: 0x0601EA3D RID: 125501 RVA: 0x006F580F File Offset: 0x006F3A0F
			public override string Code
			{
				get
				{
					return "CRTN";
				}
			}

			// Token: 0x170097A1 RID: 38817
			// (get) Token: 0x0601EA3E RID: 125502 RVA: 0x006F5816 File Offset: 0x006F3A16
			public override string Description
			{
				get
				{
					return "Return";
				}
			}

			// Token: 0x0601EA3F RID: 125503 RVA: 0x006F5820 File Offset: 0x006F3A20
			protected override bool IsModeActive()
			{
				return base.Basis.Setup.Current.ShowReturningTab.GetValueOrDefault();
			}

			// Token: 0x0601EA40 RID: 125504 RVA: 0x006F584A File Offset: 0x006F3A4A
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickPackShip.ReturnMode.ShipmentState();
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState();
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState
				{
					AlternateType = new INPrimaryAlternateType?(INPrimaryAlternateType.CPN),
					IsForIssue = false
				};
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState();
				yield return new PickPackShip.ReturnMode.ConfirmState();
				yield return new PickPackShip.CommandOrShipmentOnlyState();
				yield break;
			}

			// Token: 0x0601EA41 RID: 125505 RVA: 0x006F5853 File Offset: 0x006F3A53
			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return base.StateFlow((ScanStateFlow<PickPackShip>.IFrom flow) => flow.From<PickPackShip.ReturnMode.ShipmentState>().NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(null).NextTo<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(null));
			}

			// Token: 0x0601EA42 RID: 125506 RVA: 0x006F587A File Offset: 0x006F3A7A
			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.RemoveCommand();
				yield return new BarcodeQtySupport<PickPackShip, PickPackShip.Host>.SetQtyCommand();
				yield return new PickPackShip.ConfirmShipmentCommand();
				yield break;
			}

			// Token: 0x0601EA43 RID: 125507 RVA: 0x006F5883 File Offset: 0x006F3A83
			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects()
			{
				return AllWMSRedirects.CreateFor<PickPackShip>();
			}

			// Token: 0x0601EA44 RID: 125508 RVA: 0x006F588C File Offset: 0x006F3A8C
			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				base.Clear<PickPackShip.ReturnMode.ShipmentState>(fullReset && !base.Basis.IsWithinReset);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState>(fullReset || base.Basis.PromptLocationForEveryLine);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(fullReset);
				base.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState>(true);
			}

			// Token: 0x0400EDC7 RID: 60871
			public const string Value = "CRTN";

			// Token: 0x0200D318 RID: 54040
			[Nullable(new byte[]
			{
				0,
				1,
				1,
				0
			})]
			public class value : BqlType<IBqlString, string>.Constant<PickPackShip.ReturnMode.value>
			{
				// Token: 0x0602FE0F RID: 196111 RVA: 0x008E756B File Offset: 0x008E576B
				public value() : base("CRTN")
				{
				}
			}

			// Token: 0x0200D319 RID: 54041
			public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x0602FE10 RID: 196112 RVA: 0x008E7578 File Offset: 0x008E5778
				protected virtual IEnumerable returned()
				{
					PXDelegateResult pxdelegateResult = new PXDelegateResult();
					pxdelegateResult.IsResultSorted = true;
					pxdelegateResult.AddRange(base.Basis.GetSplits(base.Basis.RefNbr, true, delegate(SOShipLineSplit s)
					{
						decimal? pickedQty = s.PickedQty;
						decimal? qty = s.Qty;
						return pickedQty.GetValueOrDefault() >= qty.GetValueOrDefault() & (pickedQty != null & qty != null);
					}));
					return pxdelegateResult;
				}

				// Token: 0x0602FE11 RID: 196113 RVA: 0x008E75CD File Offset: 0x008E57CD
				[PXButton]
				[PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewReturn(PXAdapter adapter)
				{
					return adapter.Get();
				}

				// Token: 0x0602FE12 RID: 196114 RVA: 0x008E75D5 File Offset: 0x008E57D5
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					PXAction reviewReturn = this.ReviewReturn;
					bool visible;
					if (base.Base.IsMobile)
					{
						ScanHeader row = e.Row;
						visible = (((row != null) ? row.Mode : null) == "CRTN");
					}
					else
					{
						visible = false;
					}
					reviewReturn.SetVisible(visible);
				}

				/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.DecorateScanState(PX.BarcodeProcessing.ScanState{`0})" />
				// Token: 0x0602FE13 RID: 196115 RVA: 0x008E7610 File Offset: 0x008E5810
				[PXOverride]
				public ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
				{
					ScanState<PickPackShip> scanState = base_DecorateScanState(original);
					if (scanState.ModeCode == "CRTN")
					{
						WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState locationState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LocationState;
						if (locationState != null)
						{
							base.Basis.InjectLocationDeactivationOnDefaultLocationOption(locationState);
							base.Basis.InjectLocationSkippingOnPromptLocationForEveryLineOption(locationState);
						}
						else
						{
							WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState inventoryItemState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState;
							if (inventoryItemState != null)
							{
								base.Basis.InjectItemPresenceValidation(inventoryItemState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.ReturnMode.Logic.<DecorateScanState>g__viewSelector|5_0));
							}
							else
							{
								WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState lotSerialState = scanState as WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.LotSerialState;
								if (lotSerialState != null)
								{
									base.Basis.InjectLotSerialPresenceValidation(lotSerialState, new Func<PickPackShip, PXSelectBase<SOShipLineSplit>>(PickPackShip.ReturnMode.Logic.<DecorateScanState>g__viewSelector|5_0));
								}
							}
						}
					}
					return scanState;
				}

				/// Overrides <seealso cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.OnBeforeFullClear" />
				// Token: 0x0602FE14 RID: 196116 RVA: 0x008E76A4 File Offset: 0x008E58A4
				[PXOverride]
				public void OnBeforeFullClear(Action base_OnBeforeFullClear)
				{
					base_OnBeforeFullClear();
					if (base.Basis.CurrentMode is PickPackShip.ReturnMode && base.Basis.RefNbr != null && base.Graph.WorkLogExt.SuspendFor(base.Basis.RefNbr, new Guid?(base.Graph.Accessinfo.UserID), "PICK"))
					{
						base.Graph.WorkLogExt.PersistWorkLog();
					}
				}

				// Token: 0x0602FE15 RID: 196117 RVA: 0x008E7720 File Offset: 0x008E5920
				public virtual bool ShowReturnTab(ScanHeader row)
				{
					return base.Basis.Setup.Current.ShowReturningTab.GetValueOrDefault() && row.Mode == "CRTN";
				}

				// Token: 0x1700AF3C RID: 44860
				// (get) Token: 0x0602FE16 RID: 196118 RVA: 0x008E775E File Offset: 0x008E595E
				public virtual bool CanReturn
				{
					get
					{
						return this.Returned.SelectMain(Array.Empty<object>()).Any(delegate(SOShipLineSplit s)
						{
							decimal? pickedQty = s.PickedQty;
							decimal? qty = s.Qty;
							return pickedQty.GetValueOrDefault() < qty.GetValueOrDefault() & (pickedQty != null & qty != null);
						});
					}
				}

				// Token: 0x0602FE18 RID: 196120 RVA: 0x008E779C File Offset: 0x008E599C
				[CompilerGenerated]
				internal static PXSelectBase<SOShipLineSplit> <DecorateScanState>g__viewSelector|5_0(PickPackShip basis)
				{
					return basis.Get<PickPackShip.ReturnMode.Logic>().Returned;
				}

				// Token: 0x040150A6 RID: 86182
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
					0
				})]
				public FbqlSelect<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>>>.Order<By<BqlField<SOShipLineSplit.shipmentNbr, IBqlString>.Asc, BqlField<SOShipLineSplit.isUnassigned, IBqlBool>.Desc, BqlField<SOShipLineSplit.lineNbr, IBqlInt>.Asc>>, SOShipLineSplit>.View Returned;

				// Token: 0x040150A7 RID: 86183
				public PXAction<ScanHeader> ReviewReturn;
			}

			// Token: 0x0200D31A RID: 54042
			public sealed class ShipmentState : PickPackShip.ShipmentState
			{
				// Token: 0x0602FE19 RID: 196121 RVA: 0x008E77AC File Offset: 0x008E59AC
				protected override AbsenceHandling.Of<SOShipment> HandleAbsence(string barcode)
				{
					SOShipment soshipment = PXSelectBase<SOShipment, PXViewOf<SOShipment>.BasedOn<SelectFromBase<SOShipment, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<INSite>.On<SOShipment.FK.Site>>, FbqlJoins.Inner<SOOrderShipment>.On<SOOrderShipment.FK.Shipment>>, FbqlJoins.Inner<SOOrder>.On<SOOrderShipment.FK.Order>>, FbqlJoins.Left<Customer>.On<BqlOperand<SOShipment.customerID, IBqlInt>.IsEqual<Customer.bAccountID>>.SingleTableOnly>>.Where<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Match<INSite, BqlField<AccessInfo.userName, IBqlString>.FromCurrent>>, And<BqlOperand<SOShipment.status, IBqlString>.IsEqual<SOShipmentStatus.open>>>, And<BqlOperand<SOShipment.operation, IBqlString>.IsEqual<SOOperation.receipt>>>, And<BqlOperand<SOOrder.customerRefNbr, IBqlString>.IsEqual<P.AsString>>>, And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipment.siteID, Equal<BqlField<WMSScanHeader.siteID, IBqlInt>.FromCurrent>>>>>.Or<BqlOperand<Current2<WMSScanHeader.siteID>, IBqlInt>.IsNull>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<Customer.bAccountID, PX.Data.IsNull>>>>.Or<Match<Customer, BqlField<AccessInfo.userName, IBqlString>.FromCurrent>>>>>.Config>.Select(base.Basis, new object[]
					{
						barcode
					});
					if (soshipment != null)
					{
						return AbsenceHandling.ReplaceWith<SOShipment>(soshipment);
					}
					return base.HandleAbsence(barcode);
				}

				// Token: 0x0602FE1A RID: 196122 RVA: 0x008E77EC File Offset: 0x008E59EC
				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != "R")
					{
						return Validation.Fail("The {0} shipment cannot be returned because it has the {1} operation.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.operation>(shipment)
						});
					}
					if (shipment.Status != "N")
					{
						return Validation.Fail("The {0} shipment cannot be returned because it has the {1} status.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.status>(shipment)
						});
					}
					return Validation.Ok;
				}

				// Token: 0x0602FE1B RID: 196123 RVA: 0x008E7874 File Offset: 0x008E5A74
				protected override void ReportSuccess(SOShipment shipment)
				{
					base.Basis.ReportInfo("{0} shipment loaded and ready to be returned.", new object[]
					{
						shipment.ShipmentNbr
					});
				}

				// Token: 0x0602FE1C RID: 196124 RVA: 0x008E7898 File Offset: 0x008E5A98
				protected override void SetNextState()
				{
					bool? remove = base.Basis.Remove;
					bool flag = false;
					if ((remove.GetValueOrDefault() == flag & remove != null) && !base.Basis.Get<PickPackShip.ReturnMode.Logic>().CanReturn)
					{
						base.Basis.ReportInfo("{0} {1}", new object[]
						{
							base.Basis.Info.Current.Message,
							base.Basis.Localize("{0} shipment returned.", new object[]
							{
								base.Basis.RefNbr
							})
						});
						base.Basis.SetScanState("NONE", null, Array.Empty<object>());
						return;
					}
					base.SetNextState();
				}

				// Token: 0x0200F0CB RID: 61643
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					// Token: 0x040157BB RID: 87995
					public new const string Ready = "{0} shipment loaded and ready to be returned.";

					// Token: 0x040157BC RID: 87996
					public const string InvalidStatus = "The {0} shipment cannot be returned because it has the {1} status.";

					// Token: 0x040157BD RID: 87997
					public const string InvalidOperation = "The {0} shipment cannot be returned because it has the {1} operation.";
				}
			}

			// Token: 0x0200D31B RID: 54043
			public sealed class ConfirmState : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ConfirmationState
			{
				// Token: 0x1700AF3D RID: 44861
				// (get) Token: 0x0602FE1E RID: 196126 RVA: 0x008E7954 File Offset: 0x008E5B54
				public override string Prompt
				{
					get
					{
						return base.Basis.Localize("Confirm returning {0} x {1} {2}.", new object[]
						{
							base.Basis.SightOf<WMSScanHeader.inventoryID>(),
							base.Basis.Qty,
							base.Basis.UOM
						});
					}
				}

				// Token: 0x0602FE1F RID: 196127 RVA: 0x008E79A6 File Offset: 0x008E5BA6
				protected override FlowStatus PerformConfirmation()
				{
					return base.Basis.Get<PickPackShip.ReturnMode.ConfirmState.Logic>().Confirm();
				}

				// Token: 0x0200F0CC RID: 61644
				public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
				{
					// Token: 0x1700B0E3 RID: 45283
					// (get) Token: 0x060324A3 RID: 205987 RVA: 0x00918C92 File Offset: 0x00916E92
					// (set) Token: 0x060324A4 RID: 205988 RVA: 0x00918C9A File Offset: 0x00916E9A
					private protected PickPackShip.ReturnMode.Logic ModeLogic { protected get; private set; }

					// Token: 0x060324A5 RID: 205989 RVA: 0x00918CA3 File Offset: 0x00916EA3
					public override void Initialize()
					{
						this.ModeLogic = base.Basis.Get<PickPackShip.ReturnMode.Logic>();
					}

					// Token: 0x060324A6 RID: 205990 RVA: 0x00918CB8 File Offset: 0x00916EB8
					public virtual FlowStatus Confirm()
					{
						decimal num = Sign.MinusIf(base.Basis.Remove.GetValueOrDefault()) * base.Basis.BaseQty;
						bool hasSuitableSplits = false;
						if (num == 0m)
						{
							return FlowStatus.Ok.WithDispatchNext;
						}
						if (base.Basis.LotSerialTrack.IsTrackedSerial || base.Basis.SelectedInventoryItem.WeightItem.GetValueOrDefault())
						{
							SOShipLineSplit soshipLineSplit = this.GetSplitsToReturn().FirstOrDefault<SOShipLineSplit>();
							if (soshipLineSplit != null)
							{
								hasSuitableSplits = true;
								bool? flag = base.Basis.Remove;
								bool flag2 = false;
								decimal d = ((flag.GetValueOrDefault() == flag2 & flag != null) && !base.Basis.LotSerialTrack.IsTrackedSerial) ? base.Graph.GetQtyThreshold(soshipLineSplit) : 1m;
								decimal num2 = base.Basis.Remove.GetValueOrDefault() ? (-Math.Min(soshipLineSplit.PickedQty.Value, -num)) : Math.Min(soshipLineSplit.Qty.Value * d - soshipLineSplit.PickedQty.Value, num);
								if (base.Basis.LotSerialTrack.IsTrackedSerial && num2.IsNotIn(1m, -1m))
								{
									return FlowStatus.Fail("Serialized items can be processed only with the base UOM and the 1.00 quantity.", Array.Empty<object>());
								}
								FlowStatus result = this.ReturnSplit(soshipLineSplit, num, base.Graph.GetQtyThreshold(soshipLineSplit));
								flag = result.IsError;
								flag2 = false;
								if (!(flag.GetValueOrDefault() == flag2 & flag != null))
								{
									return result;
								}
								num -= num2;
							}
						}
						else
						{
							IEnumerable<SOShipLineSplit> splitsToReturn = this.GetSplitsToReturn().Select(delegate(SOShipLineSplit s)
							{
								hasSuitableSplits = true;
								return s;
							});
							FlowStatus result2 = this.ReturnAllSplits(splitsToReturn, ref num, false);
							bool? flag = result2.IsError;
							bool flag2 = false;
							if (!(flag.GetValueOrDefault() == flag2 & flag != null))
							{
								return result2;
							}
							if (num != 0m)
							{
								flag = base.Basis.Remove;
								flag2 = false;
								if ((flag.GetValueOrDefault() == flag2 & flag != null) && base.Basis.SelectedInventoryItem.DecimalBaseUnit.GetValueOrDefault())
								{
									FlowStatus result3 = this.ReturnAllSplits(splitsToReturn, ref num, true);
									flag = result3.IsError;
									flag2 = false;
									if (!(flag.GetValueOrDefault() == flag2 & flag != null))
									{
										return result3;
									}
								}
							}
						}
						if (!hasSuitableSplits)
						{
							return FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "No items to remove from shipment." : "No items to return.", Array.Empty<object>()).WithModeReset;
						}
						if (Math.Abs(num) > 0m)
						{
							return FlowStatus.Fail(base.Basis.Remove.GetValueOrDefault() ? "The returned quantity cannot be negative." : "The returned quantity cannot be greater than the quantity in the shipment line.", Array.Empty<object>()).WithModeReset.WithChangesDiscard;
						}
						this.EnsureShipmentUserLinkForReturn();
						base.Basis.ReportInfo(base.Basis.Remove.GetValueOrDefault() ? "{0} x {1} {2} removed from return." : "{0} x {1} {2} added to return.", new object[]
						{
							base.Basis.SightOf<WMSScanHeader.inventoryID>(),
							base.Basis.Qty,
							base.Basis.UOM
						});
						return FlowStatus.Ok.WithDispatchNext;
					}

					// Token: 0x060324A7 RID: 205991 RVA: 0x00919060 File Offset: 0x00917260
					public virtual FlowStatus ReturnAllSplits(IEnumerable<SOShipLineSplit> splitsToReturn, ref decimal restDeltaQty, bool withThresholds)
					{
						foreach (SOShipLineSplit soshipLineSplit in splitsToReturn)
						{
							if (!withThresholds)
							{
								goto IL_3B;
							}
							bool? flag = base.Basis.Remove;
							bool flag2 = false;
							if (!(flag.GetValueOrDefault() == flag2 & flag != null))
							{
								goto IL_3B;
							}
							decimal num = base.Graph.GetQtyThreshold(soshipLineSplit);
							IL_4E:
							decimal num2 = num;
							decimal num3 = base.Basis.Remove.GetValueOrDefault() ? (-Math.Min(soshipLineSplit.PickedQty.Value, -restDeltaQty)) : Math.Min(soshipLineSplit.Qty.Value * num2 - soshipLineSplit.PickedQty.Value, restDeltaQty);
							FlowStatus flowStatus = this.ReturnSplit(soshipLineSplit, num3, num2);
							flag = flowStatus.IsError;
							flag2 = false;
							if (!(flag.GetValueOrDefault() == flag2 & flag != null))
							{
								return flowStatus.WithChangesDiscard;
							}
							restDeltaQty -= num3;
							if (restDeltaQty == 0m)
							{
								break;
							}
							continue;
							IL_3B:
							num = 1m;
							goto IL_4E;
						}
						return FlowStatus.Ok;
					}

					// Token: 0x060324A8 RID: 205992 RVA: 0x009191C8 File Offset: 0x009173C8
					public virtual FlowStatus ReturnSplit(SOShipLineSplit returnedSplit, decimal deltaQty, decimal threshold)
					{
						bool flag = false;
						if (deltaQty < 0m)
						{
							decimal? num = returnedSplit.PickedQty + deltaQty;
							decimal d = 0m;
							if (num.GetValueOrDefault() < d & num != null)
							{
								return FlowStatus.Fail("The returned quantity cannot be negative.", Array.Empty<object>());
							}
						}
						else
						{
							decimal? num = returnedSplit.PickedQty + deltaQty;
							decimal? num2 = returnedSplit.Qty * threshold;
							if (num.GetValueOrDefault() > num2.GetValueOrDefault() & (num != null & num2 != null))
							{
								return FlowStatus.Fail("The returned quantity cannot be greater than the quantity in the shipment line.", Array.Empty<object>());
							}
							if (!string.Equals(returnedSplit.LotSerialNbr, base.Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase) && base.Basis.LotSerialTrack.IsEnterable)
							{
								if (!this.SetLotSerialNbrAndQty(returnedSplit, deltaQty))
								{
									return FlowStatus.Fail("The returned quantity cannot be greater than the quantity in the shipment line.", Array.Empty<object>());
								}
								flag = true;
							}
						}
						if (!flag)
						{
							base.Basis.EnsureAssignedSplitEditing(returnedSplit);
							if (deltaQty > 0m && base.Basis.LocationID != null)
							{
								returnedSplit.LocationID = base.Basis.LocationID;
							}
							returnedSplit.PickedQty += deltaQty;
							this.ModeLogic.Returned.Update(returnedSplit);
						}
						return FlowStatus.Ok;
					}

					// Token: 0x060324A9 RID: 205993 RVA: 0x009193C0 File Offset: 0x009175C0
					public virtual bool IsSelectedSplit(SOShipLineSplit split)
					{
						int? num = split.InventoryID;
						int? num2 = base.Basis.InventoryID;
						if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
						{
							num2 = split.SubItemID;
							num = base.Basis.SubItemID;
							if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
							{
								num = split.SiteID;
								num2 = base.Basis.SiteID;
								if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
								{
									return string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase);
								}
							}
						}
						return false;
					}

					// Token: 0x060324AA RID: 205994 RVA: 0x00919498 File Offset: 0x00917698
					public virtual bool SetLotSerialNbrAndQty(SOShipLineSplit pickedSplit, decimal qty)
					{
						PXSelectBase<SOShipLineSplit> returned = this.ModeLogic.Returned;
						decimal? pickedQty = pickedSplit.PickedQty;
						decimal d = 0m;
						bool? flag;
						bool flag2;
						if (pickedQty.GetValueOrDefault() == d & pickedQty != null)
						{
							flag = pickedSplit.IsUnassigned;
							flag2 = false;
							if (flag.GetValueOrDefault() == flag2 & flag != null)
							{
								if (!base.Basis.LotSerialTrack.IsTrackedSerial || !(base.Basis.SelectedLotSerialClass.LotSerIssueMethod == "U"))
								{
									flag = base.Basis.Remove;
									flag2 = false;
									if (flag.GetValueOrDefault() == flag2 & flag != null)
									{
										pickedSplit.LocationID = base.Basis.LocationID;
									}
									pickedSplit.LotSerialNbr = base.Basis.LotSerialNbr;
									if (base.Basis.LotSerialTrack.HasExpiration && base.Basis.ExpireDate != null)
									{
										pickedSplit.ExpireDate = base.Basis.ExpireDate;
									}
									pickedSplit.PickedQty += qty;
									pickedSplit = returned.Update(pickedSplit);
									return true;
								}
								SOShipLineSplit soshipLineSplit = PXSelectBase<SOShipLineSplit, PXViewOf<SOShipLineSplit>.BasedOn<SelectFromBase<SOShipLineSplit, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<SOLineSplit>.On<SOShipLineSplit.FK.OriginalLineSplit>>>.Where<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipLineSplit.shipmentNbr, Equal<BqlField<WMSScanHeader.refNbr, IBqlString>.FromCurrent>>>>>.And<BqlOperand<SOLineSplit.lotSerialNbr, IBqlString>.IsEqual<P.AsString>>>>.Config>.Select(base.Basis, new object[]
								{
									base.Basis.LotSerialNbr
								});
								if (soshipLineSplit == null)
								{
									flag = base.Basis.Remove;
									flag2 = false;
									if (flag.GetValueOrDefault() == flag2 & flag != null)
									{
										pickedSplit.LocationID = base.Basis.LocationID;
									}
									pickedSplit.LotSerialNbr = base.Basis.LotSerialNbr;
									pickedSplit.PickedQty += qty;
									pickedSplit = returned.Update(pickedSplit);
									return true;
								}
								if (string.Equals(soshipLineSplit.LotSerialNbr, base.Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
								{
									return false;
								}
								SOShipLineSplit soshipLineSplit2 = PXCache<SOShipLineSplit>.CreateCopy(soshipLineSplit);
								SOShipLineSplit soshipLineSplit3 = PXCache<SOShipLineSplit>.CreateCopy(pickedSplit);
								soshipLineSplit.Qty = new decimal?(0m);
								soshipLineSplit.LotSerialNbr = base.Basis.LotSerialNbr;
								soshipLineSplit = returned.Update(soshipLineSplit);
								soshipLineSplit.Qty = soshipLineSplit2.Qty;
								soshipLineSplit.PickedQty = soshipLineSplit3.PickedQty + qty;
								soshipLineSplit.ExpireDate = soshipLineSplit3.ExpireDate;
								soshipLineSplit = returned.Update(soshipLineSplit);
								pickedSplit.Qty = new decimal?(0m);
								flag = base.Basis.Remove;
								flag2 = false;
								if (flag.GetValueOrDefault() == flag2 & flag != null)
								{
									pickedSplit.LocationID = base.Basis.LocationID;
								}
								pickedSplit.LotSerialNbr = soshipLineSplit2.LotSerialNbr;
								pickedSplit = returned.Update(pickedSplit);
								pickedSplit.Qty = soshipLineSplit3.Qty;
								pickedSplit.PickedQty = soshipLineSplit2.PickedQty;
								pickedSplit.ExpireDate = soshipLineSplit2.ExpireDate;
								pickedSplit = returned.Update(pickedSplit);
								return true;
							}
						}
						SOShipLineSplit soshipLineSplit4 = (pickedSplit.IsUnassigned.GetValueOrDefault() || base.Basis.LotSerialTrack.IsTrackedLot) ? returned.SelectMain(Array.Empty<object>()).FirstOrDefault(delegate(SOShipLineSplit s)
						{
							int? num2 = s.LineNbr;
							int? num3 = pickedSplit.LineNbr;
							if (num2.GetValueOrDefault() == num3.GetValueOrDefault() & num2 != null == (num3 != null))
							{
								bool? isUnassigned = s.IsUnassigned;
								bool flag3 = false;
								if (isUnassigned.GetValueOrDefault() == flag3 & isUnassigned != null)
								{
									num3 = s.LocationID;
									int? locationID = this.Basis.LocationID;
									num2 = ((locationID != null) ? locationID : pickedSplit.LocationID);
									if ((num3.GetValueOrDefault() == num2.GetValueOrDefault() & num3 != null == (num2 != null)) && string.Equals(s.LotSerialNbr, this.Basis.LotSerialNbr ?? s.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
									{
										return this.IsSelectedSplit(s);
									}
								}
							}
							return false;
						}) : null;
						flag = pickedSplit.IsUnassigned;
						flag2 = false;
						bool suppress = (flag.GetValueOrDefault() == flag2 & flag != null) && base.Basis.LotSerialTrack.IsTrackedLot;
						decimal? num;
						if (soshipLineSplit4 != null)
						{
							soshipLineSplit4.PickedQty += qty;
							pickedQty = soshipLineSplit4.PickedQty;
							num = soshipLineSplit4.Qty;
							if (pickedQty.GetValueOrDefault() > num.GetValueOrDefault() & (pickedQty != null & num != null))
							{
								soshipLineSplit4.Qty = soshipLineSplit4.PickedQty;
							}
							using (base.Graph.LineSplittingExt.SuppressedModeScope(suppress))
							{
								soshipLineSplit4 = returned.Update(soshipLineSplit4);
								goto IL_679;
							}
						}
						SOShipLineSplit soshipLineSplit5 = PXCache<SOShipLineSplit>.CreateCopy(pickedSplit);
						soshipLineSplit5.SplitLineNbr = null;
						soshipLineSplit5.PlanID = null;
						soshipLineSplit5.LotSerialNbr = base.Basis.LotSerialNbr;
						num = pickedSplit.Qty - qty;
						d = 0m;
						if ((num.GetValueOrDefault() > d & num != null) || pickedSplit.IsUnassigned.GetValueOrDefault())
						{
							soshipLineSplit5.Qty = new decimal?(qty);
							soshipLineSplit5.PickedQty = new decimal?(qty);
							soshipLineSplit5.PackedQty = new decimal?(0m);
							soshipLineSplit5.IsUnassigned = new bool?(false);
						}
						else
						{
							soshipLineSplit5.Qty = pickedSplit.Qty;
							soshipLineSplit5.PickedQty = pickedSplit.PickedQty;
							soshipLineSplit5.PackedQty = pickedSplit.PackedQty;
						}
						using (base.Graph.LineSplittingExt.SuppressedModeScope(suppress))
						{
							soshipLineSplit5 = returned.Insert(soshipLineSplit5);
						}
						IL_679:
						flag = pickedSplit.IsUnassigned;
						flag2 = false;
						if (flag.GetValueOrDefault() == flag2 & flag != null)
						{
							num = pickedSplit.Qty;
							d = 0m;
							if (num.GetValueOrDefault() <= d & num != null)
							{
								pickedSplit = returned.Delete(pickedSplit);
							}
							else
							{
								pickedSplit.Qty -= qty;
								pickedSplit = returned.Update(pickedSplit);
							}
						}
						return true;
					}

					// Token: 0x060324AB RID: 205995 RVA: 0x00919BF0 File Offset: 0x00917DF0
					[Obsolete("Use the GetSplitsToReturn method instead.")]
					public virtual SOShipLineSplit GetSplitToReturn()
					{
						return this.GetSplitsToReturn().FirstOrDefault<SOShipLineSplit>();
					}

					// Token: 0x060324AC RID: 205996 RVA: 0x00919BFD File Offset: 0x00917DFD
					public virtual IEnumerable<SOShipLineSplit> GetSplitsToReturn()
					{
						return this.ModeLogic.Returned.SelectMain(Array.Empty<object>()).Where(new Func<SOShipLineSplit, bool>(this.IsSelectedSplit)).With(new Func<IEnumerable<SOShipLineSplit>, IOrderedEnumerable<SOShipLineSplit>>(this.PrioritizeSplits));
					}

					// Token: 0x060324AD RID: 205997 RVA: 0x00919C38 File Offset: 0x00917E38
					public virtual IOrderedEnumerable<SOShipLineSplit> PrioritizeSplits(IEnumerable<SOShipLineSplit> splits)
					{
						if (base.Basis.Remove.GetValueOrDefault())
						{
							return splits.OrderByAccordanceTo(delegate(SOShipLineSplit split)
							{
								decimal? pickedQty = split.PickedQty;
								decimal d = 0m;
								return pickedQty.GetValueOrDefault() > d & pickedQty != null;
							}).ThenByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenBy(delegate(SOShipLineSplit split)
							{
								decimal? qty = split.Qty;
								decimal? pickedQty = split.PickedQty;
								if (!(qty != null & pickedQty != null))
								{
									return null;
								}
								return new decimal?(qty.GetValueOrDefault() - pickedQty.GetValueOrDefault());
							});
						}
						if (base.Basis.LotSerialTrack.IsTrackedSerial)
						{
							return splits.OrderByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenByAccordanceTo(delegate(SOShipLineSplit split)
							{
								decimal? pickedQty = split.PickedQty;
								decimal d = 0m;
								return pickedQty.GetValueOrDefault() == d & pickedQty != null;
							}).ThenByAccordanceTo((SOShipLineSplit split) => split.IsUnassigned.GetValueOrDefault());
						}
						return splits.OrderByAccordanceTo(delegate(SOShipLineSplit split)
						{
							decimal? qty = split.Qty;
							decimal? pickedQty = split.PickedQty;
							return qty.GetValueOrDefault() > pickedQty.GetValueOrDefault() & (qty != null & pickedQty != null);
						}).ThenByAccordanceTo((SOShipLineSplit split) => string.Equals(split.LotSerialNbr, base.Basis.LotSerialNbr ?? split.LotSerialNbr, StringComparison.OrdinalIgnoreCase)).ThenByAccordanceTo((SOShipLineSplit split) => string.IsNullOrEmpty(split.LotSerialNbr)).ThenByAccordanceTo(delegate(SOShipLineSplit split)
						{
							decimal? pickedQty = split.PickedQty;
							decimal d = 0m;
							return pickedQty.GetValueOrDefault() > d & pickedQty != null;
						}).ThenByDescending(delegate(SOShipLineSplit split)
						{
							decimal? qty = split.Qty;
							decimal? pickedQty = split.PickedQty;
							if (!(qty != null & pickedQty != null))
							{
								return null;
							}
							return new decimal?(qty.GetValueOrDefault() - pickedQty.GetValueOrDefault());
						});
					}

					// Token: 0x060324AE RID: 205998 RVA: 0x00919DC7 File Offset: 0x00917FC7
					public virtual void EnsureShipmentUserLinkForReturn()
					{
						base.Graph.WorkLogExt.EnsureFor(base.Basis.RefNbr, new Guid?(base.Graph.Accessinfo.UserID), "PICK");
					}
				}

				// Token: 0x0200F0CD RID: 61645
				[PXLocalizable]
				public new abstract class Msg
				{
					// Token: 0x040157BF RID: 87999
					public const string Prompt = "Confirm returning {0} x {1} {2}.";

					// Token: 0x040157C0 RID: 88000
					public const string NothingToReturn = "No items to return.";

					// Token: 0x040157C1 RID: 88001
					public const string NothingToRemove = "No items to remove from shipment.";

					// Token: 0x040157C2 RID: 88002
					public const string Overreturning = "The returned quantity cannot be greater than the quantity in the shipment line.";

					// Token: 0x040157C3 RID: 88003
					public const string Underreturning = "The returned quantity cannot be negative.";

					// Token: 0x040157C4 RID: 88004
					public const string InventoryAdded = "{0} x {1} {2} added to return.";

					// Token: 0x040157C5 RID: 88005
					public const string InventoryRemoved = "{0} x {1} {2} removed from return.";
				}
			}

			// Token: 0x0200D31C RID: 54044
			public sealed class RedirectFrom<TForeignBasis> : PX.BarcodeProcessing.RedirectFrom<TForeignBasis>.To<PickPackShip>.SetMode<PickPackShip.ReturnMode> where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				// Token: 0x1700AF3E RID: 44862
				// (get) Token: 0x0602FE21 RID: 196129 RVA: 0x008E79C0 File Offset: 0x008E5BC0
				public override string Code
				{
					get
					{
						return "SORETURN";
					}
				}

				// Token: 0x1700AF3F RID: 44863
				// (get) Token: 0x0602FE22 RID: 196130 RVA: 0x008E79C7 File Offset: 0x008E5BC7
				public override string DisplayName
				{
					get
					{
						return "SO Return";
					}
				}

				// Token: 0x1700AF40 RID: 44864
				// (get) Token: 0x0602FE23 RID: 196131 RVA: 0x008E79D0 File Offset: 0x008E5BD0
				public override bool IsPossible
				{
					get
					{
						bool flag = PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
						SOPickPackShipSetup sopickPackShipSetup = SOPickPackShipSetup.PK.Find(base.Basis.Graph, base.Basis.Graph.Accessinfo.BranchID, PKFindOptions.None);
						return flag && sopickPackShipSetup != null && sopickPackShipSetup.ShowReturningTab.GetValueOrDefault();
					}
				}

				// Token: 0x0200F0CE RID: 61646
				[PXLocalizable]
				public abstract class Msg
				{
					// Token: 0x040157C6 RID: 88006
					public const string DisplayName = "SO Return";
				}
			}

			// Token: 0x0200D31D RID: 54045
			[PXLocalizable]
			public new abstract class Msg : ScanMode<PickPackShip>.Msg
			{
				// Token: 0x040150A8 RID: 86184
				public const string Description = "Return";

				// Token: 0x040150A9 RID: 86185
				public const string Completed = "{0} shipment returned.";
			}

			// Token: 0x0200D31E RID: 54046
			[PXUIField(Visible = false)]
			public class ShowReturn : PXFieldAttachedTo<ScanHeader>.By<PickPackShip.Host>.AsBool.Named<PickPackShip.ReturnMode.ShowReturn>
			{
				// Token: 0x0602FE26 RID: 196134 RVA: 0x008E7A3A File Offset: 0x008E5C3A
				public override bool? GetValue(ScanHeader row)
				{
					return new bool?(base.Base.WMS.Get<PickPackShip.ReturnMode.Logic>().ShowReturnTab(row));
				}
			}
		}

		// Token: 0x02002F51 RID: 12113
		public sealed class ShipMode : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanMode
		{
			// Token: 0x170097A2 RID: 38818
			// (get) Token: 0x0601EA46 RID: 125510 RVA: 0x006F58E7 File Offset: 0x006F3AE7
			public override string Code
			{
				get
				{
					return "SHIP";
				}
			}

			// Token: 0x170097A3 RID: 38819
			// (get) Token: 0x0601EA47 RID: 125511 RVA: 0x006F58EE File Offset: 0x006F3AEE
			public override string Description
			{
				get
				{
					return "Ship";
				}
			}

			// Token: 0x0601EA48 RID: 125512 RVA: 0x006F58F8 File Offset: 0x006F3AF8
			protected override bool IsModeActive()
			{
				return base.Basis.Setup.Current.ShowShipTab.GetValueOrDefault();
			}

			// Token: 0x0601EA49 RID: 125513 RVA: 0x006F5922 File Offset: 0x006F3B22
			protected override ScanState<PickPackShip> GetDefaultState()
			{
				if (base.Basis.RefNbr != null)
				{
					return base.FindState("NONE");
				}
				return base.GetDefaultState();
			}

			// Token: 0x0601EA4A RID: 125514 RVA: 0x006F5943 File Offset: 0x006F3B43
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickPackShip.ShipMode.ShipmentState();
				yield return new PickPackShip.CommandOrShipmentOnlyState();
				yield break;
			}

			// Token: 0x0601EA4B RID: 125515 RVA: 0x006F594C File Offset: 0x006F3B4C
			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new PickPackShip.ShipMode.RefreshRatesCommand();
				yield return new PickPackShip.ShipMode.GetLabelsCommand();
				yield return new PickPackShip.ConfirmShipmentCommand();
				yield break;
			}

			// Token: 0x0601EA4C RID: 125516 RVA: 0x006F5955 File Offset: 0x006F3B55
			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects()
			{
				return AllWMSRedirects.CreateFor<PickPackShip>();
			}

			// Token: 0x0601EA4D RID: 125517 RVA: 0x006F595C File Offset: 0x006F3B5C
			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				base.Clear<PickPackShip.ShipMode.ShipmentState>(fullReset && !base.Basis.IsWithinReset);
			}

			// Token: 0x0400EDC8 RID: 60872
			public const string Value = "SHIP";

			// Token: 0x0200D322 RID: 54050
			[Nullable(new byte[]
			{
				0,
				1,
				1,
				0
			})]
			public class value : BqlType<IBqlString, string>.Constant<PickPackShip.ShipMode.value>
			{
				// Token: 0x0602FE3B RID: 196155 RVA: 0x008E7CEF File Offset: 0x008E5EEF
				public value() : base("SHIP")
				{
				}
			}

			// Token: 0x0200D323 RID: 54051
			public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x0602FE3C RID: 196156 RVA: 0x008E7CFC File Offset: 0x008E5EFC
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					ScanHeader row = e.Row;
					if (((row != null) ? row.Mode : null) == "SHIP")
					{
						base.Basis.ScanConfirm.SetVisible(false);
					}
				}

				// Token: 0x0602FE3D RID: 196157 RVA: 0x008E7D30 File Offset: 0x008E5F30
				public virtual bool ShowShipTab(ScanHeader row)
				{
					return base.Basis.Setup.Current.ShowShipTab.GetValueOrDefault() && row.Mode == "SHIP";
				}
			}

			// Token: 0x0200D324 RID: 54052
			public class CarrierRatesLogic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x0602FE3F RID: 196159 RVA: 0x008E7D76 File Offset: 0x008E5F76
				protected virtual void ClearCarrierRates()
				{
					base.Graph.CarrierRatesExt.CarrierRates.Cache.Clear();
				}

				// Token: 0x0602FE40 RID: 196160 RVA: 0x008E7D92 File Offset: 0x008E5F92
				protected virtual void _(Events.RowInserted<SOPackageDetailEx> e)
				{
					this.ClearCarrierRates();
				}

				// Token: 0x0602FE41 RID: 196161 RVA: 0x008E7D9A File Offset: 0x008E5F9A
				protected virtual void _(Events.RowUpdated<SOPackageDetailEx> e)
				{
					this.ClearCarrierRates();
				}

				// Token: 0x0602FE42 RID: 196162 RVA: 0x008E7DA2 File Offset: 0x008E5FA2
				protected virtual void _(Events.RowDeleted<SOPackageDetailEx> e)
				{
					this.ClearCarrierRates();
				}
			}

			// Token: 0x0200D325 RID: 54053
			public sealed class ShipmentState : PickPackShip.ShipmentState
			{
				// Token: 0x0602FE44 RID: 196164 RVA: 0x008E7DB4 File Offset: 0x008E5FB4
				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != "I")
					{
						return Validation.Fail("The {0} shipment cannot be processed in Ship mode because it has the {1} operation.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.operation>(shipment)
						});
					}
					if (shipment.Status != "N")
					{
						return Validation.Fail("The {0} shipment cannot be processed in Ship mode because it has the {1} status.", new object[]
						{
							shipment.ShipmentNbr,
							base.Basis.SightOf<SOShipment.status>(shipment)
						});
					}
					return Validation.Ok;
				}

				// Token: 0x0602FE45 RID: 196165 RVA: 0x008E7E3C File Offset: 0x008E603C
				protected override void Apply(SOShipment shipment)
				{
					this._needToRefreshRates = false;
					string refNbr = base.Basis.RefNbr;
					base.Apply(shipment);
					if (base.Basis.RefNbr.IsNotIn(null, refNbr) && !base.Basis.Header.Barcode.StartsWith("@"))
					{
						this._needToRefreshRates = true;
					}
				}

				// Token: 0x0602FE46 RID: 196166 RVA: 0x008E7E9A File Offset: 0x008E609A
				protected override void ClearState()
				{
					base.ClearState();
					this._needToRefreshRates = false;
				}

				// Token: 0x0602FE47 RID: 196167 RVA: 0x008E7EA9 File Offset: 0x008E60A9
				protected override void ReportSuccess(SOShipment shipment)
				{
					base.Basis.ReportInfo("{0} shipment loaded and ready to be shipped.", new object[]
					{
						shipment.ShipmentNbr
					});
				}

				// Token: 0x0602FE48 RID: 196168 RVA: 0x008E7ECA File Offset: 0x008E60CA
				protected override void SetNextState()
				{
					if (this._needToRefreshRates)
					{
						base.Basis.Get<PickPackShip.ShipMode.RefreshRatesCommand.Logic>().UpdateRates();
					}
					this._needToRefreshRates = false;
				}

				// Token: 0x040150B2 RID: 86194
				private bool _needToRefreshRates;

				// Token: 0x0200F0CF RID: 61647
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					// Token: 0x040157C7 RID: 88007
					public new const string Ready = "{0} shipment loaded and ready to be shipped.";

					// Token: 0x040157C8 RID: 88008
					public const string InvalidStatus = "The {0} shipment cannot be processed in Ship mode because it has the {1} status.";

					// Token: 0x040157C9 RID: 88009
					public const string InvalidOperation = "The {0} shipment cannot be processed in Ship mode because it has the {1} operation.";
				}
			}

			// Token: 0x0200D326 RID: 54054
			public sealed class GetLabelsCommand : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanCommand
			{
				// Token: 0x1700AF45 RID: 44869
				// (get) Token: 0x0602FE4A RID: 196170 RVA: 0x008E7EF3 File Offset: 0x008E60F3
				public override string Code
				{
					get
					{
						return "GET*LABELS";
					}
				}

				// Token: 0x1700AF46 RID: 44870
				// (get) Token: 0x0602FE4B RID: 196171 RVA: 0x008E7EFA File Offset: 0x008E60FA
				public override string ButtonName
				{
					get
					{
						return "scanGetLabels";
					}
				}

				// Token: 0x1700AF47 RID: 44871
				// (get) Token: 0x0602FE4C RID: 196172 RVA: 0x008E7F01 File Offset: 0x008E6101
				public override string DisplayName
				{
					get
					{
						return "Get Return Labels";
					}
				}

				// Token: 0x1700AF48 RID: 44872
				// (get) Token: 0x0602FE4D RID: 196173 RVA: 0x008E7F08 File Offset: 0x008E6108
				protected override bool IsEnabled
				{
					get
					{
						return base.Basis.DocumentIsEditable;
					}
				}

				// Token: 0x0602FE4E RID: 196174 RVA: 0x008E7F15 File Offset: 0x008E6115
				protected override bool Process()
				{
					return base.Get<PickPackShip.ShipMode.GetLabelsCommand.Logic>().GetLabels();
				}

				// Token: 0x0200F0D0 RID: 61648
				public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
				{
					// Token: 0x060324B6 RID: 206006 RVA: 0x00919E80 File Offset: 0x00918080
					public virtual bool GetLabels()
					{
						base.Basis.Save.Press();
						PickPackShip.Host clone = base.Graph.Clone<PickPackShip.Host>();
						string refNbr = base.Basis.RefNbr;
						PXLongOperation.StartOperation(base.Basis.Graph, delegate()
						{
							PXLongOperation.SetCustomInfo(clone);
							SOShipment shiporder = PXSelectBase<SOShipment, PXViewOf<SOShipment>.BasedOn<SelectFromBase<SOShipment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOShipment.shipmentNbr, IBqlString>.IsEqual<P.AsString>>>.Config>.Select(clone, new object[]
							{
								refNbr
							});
							clone.GetExtension<LabelsPrinting>().GetReturnLabels(shiporder);
						});
						return true;
					}
				}
			}

			// Token: 0x0200D327 RID: 54055
			public sealed class RefreshRatesCommand : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanCommand
			{
				// Token: 0x1700AF49 RID: 44873
				// (get) Token: 0x0602FE50 RID: 196176 RVA: 0x008E7F2A File Offset: 0x008E612A
				public override string Code
				{
					get
					{
						return "REFRESH*RATES";
					}
				}

				// Token: 0x1700AF4A RID: 44874
				// (get) Token: 0x0602FE51 RID: 196177 RVA: 0x008E7F31 File Offset: 0x008E6131
				public override string ButtonName
				{
					get
					{
						return "scanRefreshRates";
					}
				}

				// Token: 0x1700AF4B RID: 44875
				// (get) Token: 0x0602FE52 RID: 196178 RVA: 0x008E7F38 File Offset: 0x008E6138
				public override string DisplayName
				{
					get
					{
						return "Refresh Rates";
					}
				}

				// Token: 0x1700AF4C RID: 44876
				// (get) Token: 0x0602FE53 RID: 196179 RVA: 0x008E7F3F File Offset: 0x008E613F
				protected override bool IsEnabled
				{
					get
					{
						return base.Basis.DocumentIsEditable;
					}
				}

				// Token: 0x0602FE54 RID: 196180 RVA: 0x008E7F4C File Offset: 0x008E614C
				protected override bool Process()
				{
					return base.Get<PickPackShip.ShipMode.RefreshRatesCommand.Logic>().PerformRatesRefresh();
				}

				// Token: 0x0200F0D1 RID: 61649
				public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
				{
					// Token: 0x060324B8 RID: 206008 RVA: 0x00919EEC File Offset: 0x009180EC
					public virtual bool PerformRatesRefresh()
					{
						if (!string.IsNullOrEmpty(base.Basis.RefNbr))
						{
							base.Basis.Save.Press();
							PickPackShip.Host clone = base.Graph.Clone<PickPackShip.Host>();
							PXLongOperation.StartOperation(base.Graph, delegate()
							{
								PXLongOperation.SetCustomInfo(clone);
								PickPackShip.ShipMode.RefreshRatesCommand.Logic.UpdateRates(clone);
							});
							base.Basis.Graph.RowSelected.AddHandler<SOCarrierRate>(delegate(PXCache cache, PXRowSelectedEventArgs args)
							{
								if (args.Row != null)
								{
									cache.AdjustUI(args.Row).For<SOCarrierRate.amount>(delegate(PXUIFieldAttribute a)
									{
										if (a.ErrorLevel == PXErrorLevel.Error)
										{
											((IPXInterfaceField)a).ErrorLevel = PXErrorLevel.RowError;
										}
									});
								}
							});
						}
						return true;
					}

					// Token: 0x060324B9 RID: 206009 RVA: 0x00919F80 File Offset: 0x00918180
					public static void UpdateRates(PickPackShip.Host graph)
					{
						PickPackShip.ShipMode.RefreshRatesCommand.Logic.<>c__DisplayClass1_0 CS$<>8__locals1 = new PickPackShip.ShipMode.RefreshRatesCommand.Logic.<>c__DisplayClass1_0();
						CS$<>8__locals1.carrierRateErrors = new Dictionary<SOCarrierRate, PXSetPropertyException>();
						try
						{
							graph.ExceptionHandling.AddHandler<SOCarrierRate.method>(new PXExceptionHandling(CS$<>8__locals1.<UpdateRates>g__saveCarrierRateError|0));
							graph.CarrierRatesExt.UpdateRates();
						}
						finally
						{
							graph.ExceptionHandling.RemoveHandler<SOCarrierRate.method>(new PXExceptionHandling(CS$<>8__locals1.<UpdateRates>g__saveCarrierRateError|0));
						}
						PXCache<SOCarrierRate> pxcache = graph.Caches<SOCarrierRate>();
						foreach (KeyValuePair<SOCarrierRate, PXSetPropertyException> keyValuePair in CS$<>8__locals1.carrierRateErrors)
						{
							SOCarrierRate key = keyValuePair.Key;
							PXSetPropertyException ex = keyValuePair.Value;
							ex = new PXSetPropertyException(ex.Message, PXErrorLevel.Error)
							{
								ErrorValue = key.Amount
							};
							pxcache.RaiseExceptionHandling<SOCarrierRate.amount>(key, key.Amount, ex);
						}
					}

					// Token: 0x060324BA RID: 206010 RVA: 0x0091A078 File Offset: 0x00918278
					public virtual void UpdateRates()
					{
						if (base.Basis.Graph.Packages.SelectWindowed(0, 1, Array.Empty<object>()) == null)
						{
							return;
						}
						try
						{
							base.Basis.Graph.CarrierRatesExt.UpdateRates();
						}
						catch (PXException ex)
						{
							base.Basis.ReportError(ex.MessageNoPrefix, Array.Empty<object>());
						}
					}
				}
			}

			// Token: 0x0200D328 RID: 54056
			public sealed class RedirectFrom<TForeignBasis> : PX.BarcodeProcessing.RedirectFrom<TForeignBasis>.To<PickPackShip>.SetMode<PickPackShip.ShipMode> where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				// Token: 0x1700AF4D RID: 44877
				// (get) Token: 0x0602FE56 RID: 196182 RVA: 0x008E7F61 File Offset: 0x008E6161
				public override string Code
				{
					get
					{
						return "SHIP";
					}
				}

				// Token: 0x1700AF4E RID: 44878
				// (get) Token: 0x0602FE57 RID: 196183 RVA: 0x008E7F68 File Offset: 0x008E6168
				public override string DisplayName
				{
					get
					{
						return "Ship";
					}
				}

				// Token: 0x1700AF4F RID: 44879
				// (get) Token: 0x0602FE58 RID: 196184 RVA: 0x008E7F6F File Offset: 0x008E616F
				// (set) Token: 0x0602FE59 RID: 196185 RVA: 0x008E7F77 File Offset: 0x008E6177
				private string RefNbr { get; set; }

				// Token: 0x1700AF50 RID: 44880
				// (get) Token: 0x0602FE5A RID: 196186 RVA: 0x008E7F80 File Offset: 0x008E6180
				public override bool IsPossible
				{
					get
					{
						if (base.Basis.Graph.IsMobile)
						{
							return false;
						}
						bool flag = PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
						SOPickPackShipSetup sopickPackShipSetup = SOPickPackShipSetup.PK.Find(base.Basis.Graph, base.Basis.Graph.Accessinfo.BranchID, PKFindOptions.None);
						return flag && sopickPackShipSetup != null && sopickPackShipSetup.ShowShipTab.GetValueOrDefault();
					}
				}

				// Token: 0x0602FE5B RID: 196187 RVA: 0x008E7FF4 File Offset: 0x008E61F4
				protected override bool PrepareRedirect()
				{
					PickPackShip pickPackShip = base.Basis as PickPackShip;
					if (pickPackShip != null && pickPackShip.RefNbr != null && !pickPackShip.DocumentIsConfirmed)
					{
						Validation? validation = pickPackShip.FindMode<PickPackShip.ShipMode>().TryValidate<SOShipment>(pickPackShip.Shipment).By<PickPackShip.ShipMode.ShipmentState>();
						if (validation != null)
						{
							Validation valueOrDefault = validation.GetValueOrDefault();
							if (valueOrDefault.IsError.GetValueOrDefault())
							{
								pickPackShip.ReportError(valueOrDefault.Message, valueOrDefault.MessageArgs);
								return false;
							}
						}
						this.RefNbr = pickPackShip.RefNbr;
					}
					return true;
				}

				// Token: 0x0602FE5C RID: 196188 RVA: 0x008E8088 File Offset: 0x008E6288
				protected override void CompleteRedirect()
				{
					PickPackShip pickPackShip = base.Basis as PickPackShip;
					if (pickPackShip != null && pickPackShip.CurrentMode.Code != "CRTN" && this.RefNbr != null && pickPackShip.TryProcessBy("RNBR", this.RefNbr, (StateSubstitutionRule)253))
					{
						pickPackShip.SetDefaultState(null, Array.Empty<object>());
						this.RefNbr = null;
						SOPackageDetailEx sopackageDetailEx;
						if (pickPackShip.Get<PickPackShip.PackMode.Logic>().HasSingleAutoPackage(pickPackShip.RefNbr, out sopackageDetailEx) && !sopackageDetailEx.Confirmed.GetValueOrDefault())
						{
							sopackageDetailEx.Confirmed = new bool?(true);
							pickPackShip.Graph.Packages.Update(sopackageDetailEx);
							pickPackShip.Graph.Document.Current.IsPackageValid = new bool?(true);
							pickPackShip.Graph.Document.UpdateCurrent();
							pickPackShip.Reset(false);
							pickPackShip.SaveChanges();
						}
						pickPackShip.Get<PickPackShip.ShipMode.RefreshRatesCommand.Logic>().UpdateRates();
					}
				}
			}

			// Token: 0x0200D329 RID: 54057
			[PXLocalizable]
			public new abstract class Msg : ScanMode<PickPackShip>.Msg
			{
				// Token: 0x040150B4 RID: 86196
				public const string Description = "Ship";
			}

			// Token: 0x0200D32A RID: 54058
			[PXUIField(Visible = false)]
			public class ShowShip : PXFieldAttachedTo<ScanHeader>.By<PickPackShip.Host>.AsBool.Named<PickPackShip.ShipMode.ShowShip>
			{
				// Token: 0x0602FE5F RID: 196191 RVA: 0x008E819C File Offset: 0x008E639C
				public override bool? GetValue(ScanHeader row)
				{
					return new bool?(base.Base.WMS.Get<PickPackShip.ShipMode.Logic>().ShowShipTab(row));
				}
			}
		}

		// Token: 0x02002F52 RID: 12114
		public class Host : SOShipmentEntry, ICaptionable
		{
			// Token: 0x170097A4 RID: 38820
			// (get) Token: 0x0601EA4F RID: 125519 RVA: 0x006F5987 File Offset: 0x006F3B87
			public PickPackShip WMS
			{
				get
				{
					return this.FindImplementation<PickPackShip>();
				}
			}

			// Token: 0x0601EA50 RID: 125520 RVA: 0x006F598F File Offset: 0x006F3B8F
			string ICaptionable.Caption()
			{
				return this.WMS.GetCaption();
			}
		}

		// Token: 0x02002F53 RID: 12115
		public new class QtySupport : WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.QtySupport
		{
		}

		// Token: 0x02002F54 RID: 12116
		public new class GS1Support : WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.GS1Support
		{
		}

		// Token: 0x02002F55 RID: 12117
		public class UserSetup : PXUserSetup<PickPackShip.UserSetup, PickPackShip.Host, ScanHeader, SOPickPackShipUserSetup, SOPickPackShipUserSetup.userID>
		{
		}

		// Token: 0x02002F56 RID: 12118
		public abstract class ShipmentState : WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.RefNbrState<SOShipment>
		{
			// Token: 0x170097A5 RID: 38821
			// (get) Token: 0x0601EA55 RID: 125525 RVA: 0x006F59BC File Offset: 0x006F3BBC
			protected override string StatePrompt
			{
				get
				{
					return "Scan the shipment number.";
				}
			}

			// Token: 0x0601EA56 RID: 125526 RVA: 0x006F59C3 File Offset: 0x006F3BC3
			protected override SOShipment GetByBarcode(string barcode)
			{
				return PXSelectBase<SOShipment, PXViewOf<SOShipment>.BasedOn<SelectFromBase<SOShipment, TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Append<TypeArrayOf<IFbqlJoin>.Empty, FbqlJoins.Inner<INSite>.On<SOShipment.FK.Site>>, FbqlJoins.Left<Customer>.On<BqlOperand<SOShipment.customerID, IBqlInt>.IsEqual<Customer.bAccountID>>.SingleTableOnly>>.Where<BqlChainableConditionMirror<TypeArrayOf<IBqlBinary>.Append<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<SOShipment.shipmentNbr, Equal<P.AsString>>>>, And<Match<INSite, BqlField<AccessInfo.userName, IBqlString>.FromCurrent>>>>.And<BqlChainableConditionBase<TypeArrayOf<IBqlBinary>.FilledWith<And<Compare<Customer.bAccountID, PX.Data.IsNull>>>>.Or<Match<Customer, BqlField<AccessInfo.userName, IBqlString>.FromCurrent>>>>>.ReadOnly.Config>.Select(base.Basis, new object[]
				{
					barcode
				});
			}

			// Token: 0x0601EA57 RID: 125527 RVA: 0x006F59E4 File Offset: 0x006F3BE4
			protected override void Apply(SOShipment shipment)
			{
				base.Basis.Graph.Document.Current = shipment;
				base.Basis.RefNbr = shipment.ShipmentNbr;
				base.Basis.SiteID = shipment.SiteID;
				base.Basis.TranDate = shipment.ShipDate;
				base.Basis.TranType = ((shipment.ShipmentType == "T") ? "TRX" : ((shipment.Operation == "R") ? "RET" : "III"));
				base.Basis.NoteID = shipment.NoteID;
			}

			// Token: 0x0601EA58 RID: 125528 RVA: 0x006F5A90 File Offset: 0x006F3C90
			protected override void ClearState()
			{
				base.Basis.Graph.Document.Current = null;
				base.Basis.RefNbr = null;
				base.Basis.SiteID = null;
				base.Basis.TranDate = null;
				base.Basis.TranType = null;
				base.Basis.NoteID = null;
			}

			// Token: 0x0601EA59 RID: 125529 RVA: 0x006F5B07 File Offset: 0x006F3D07
			protected override void ReportMissing(string barcode)
			{
				base.Basis.ReportError("{0} shipment not found.", new object[]
				{
					barcode
				});
			}

			// Token: 0x0601EA5A RID: 125530 RVA: 0x006F5B23 File Offset: 0x006F3D23
			protected override void ReportSuccess(SOShipment shipment)
			{
				base.Basis.ReportInfo("{0} shipment loaded and ready to be processed.", new object[]
				{
					shipment.ShipmentNbr
				});
			}

			// Token: 0x0200D32D RID: 54061
			[PXLocalizable]
			public abstract class Msg
			{
				// Token: 0x040150BB RID: 86203
				public const string Prompt = "Scan the shipment number.";

				// Token: 0x040150BC RID: 86204
				public const string Ready = "{0} shipment loaded and ready to be processed.";

				// Token: 0x040150BD RID: 86205
				public const string Missing = "{0} shipment not found.";

				// Token: 0x040150BE RID: 86206
				public const string Invalid = "The {0} shipment cannot be processed because it has the {1} status.";
			}
		}

		// Token: 0x02002F57 RID: 12119
		public sealed class CommandOrShipmentOnlyState : CommandOnlyStateBase<PickPackShip>
		{
			// Token: 0x0601EA5C RID: 125532 RVA: 0x006F5B4C File Offset: 0x006F3D4C
			public override void MoveToNextState()
			{
			}

			// Token: 0x170097A6 RID: 38822
			// (get) Token: 0x0601EA5D RID: 125533 RVA: 0x006F5B4E File Offset: 0x006F3D4E
			public override string Prompt
			{
				get
				{
					return base.Basis.Get<PickPackShip.CommandOrShipmentOnlyState.Logic>().GetPromptForCommandOrShipmentOnly();
				}
			}

			// Token: 0x0601EA5E RID: 125534 RVA: 0x006F5B60 File Offset: 0x006F3D60
			public override bool Process(string barcode)
			{
				if (base.Basis.TryProcessBy<PickPackShip.ShipmentState>(barcode, StateSubstitutionRule.KeepAbsenceHandling))
				{
					base.Basis.Clear<PickPackShip.ShipmentState>(true);
					base.Basis.Reset(false);
					base.Basis.SetScanState<PickPackShip.ShipmentState>(null, Array.Empty<object>());
					base.Basis.CurrentMode.FindState<PickPackShip.ShipmentState>(false).Process(barcode);
					return true;
				}
				base.Basis.Reporter.Error(base.Basis.Get<PickPackShip.CommandOrShipmentOnlyState.Logic>().GetErrorForCommandOrShipmentOnly(), Array.Empty<object>());
				return false;
			}

			// Token: 0x0200D32E RID: 54062
			public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x0602FE72 RID: 196210 RVA: 0x008E839F File Offset: 0x008E659F
				public virtual string GetPromptForCommandOrShipmentOnly()
				{
					return "Use any command or scan the next shipment number to continue.";
				}

				// Token: 0x0602FE73 RID: 196211 RVA: 0x008E83A6 File Offset: 0x008E65A6
				public virtual string GetErrorForCommandOrShipmentOnly()
				{
					return "Only commands or a shipment number can be used to continue.";
				}
			}

			// Token: 0x0200D32F RID: 54063
			[PXLocalizable]
			public new abstract class Msg
			{
				// Token: 0x040150BF RID: 86207
				public const string UseCommandOrShipmentToContinue = "Use any command or scan the next shipment number to continue.";

				// Token: 0x040150C0 RID: 86208
				public const string OnlyCommandsAndShipmentsAreAllowed = "Only commands or a shipment number can be used to continue.";
			}
		}

		// Token: 0x02002F58 RID: 12120
		public sealed class ConfirmShipmentCommand : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanCommand
		{
			// Token: 0x170097A7 RID: 38823
			// (get) Token: 0x0601EA60 RID: 125536 RVA: 0x006F5BED File Offset: 0x006F3DED
			public override string Code
			{
				get
				{
					return "CONFIRM*SHIPMENT";
				}
			}

			// Token: 0x170097A8 RID: 38824
			// (get) Token: 0x0601EA61 RID: 125537 RVA: 0x006F5BF4 File Offset: 0x006F3DF4
			public override string ButtonName
			{
				get
				{
					return "scanConfirmShipment";
				}
			}

			// Token: 0x170097A9 RID: 38825
			// (get) Token: 0x0601EA62 RID: 125538 RVA: 0x006F5BFB File Offset: 0x006F3DFB
			public override string DisplayName
			{
				get
				{
					return "Confirm Shipment";
				}
			}

			// Token: 0x170097AA RID: 38826
			// (get) Token: 0x0601EA63 RID: 125539 RVA: 0x006F5C02 File Offset: 0x006F3E02
			protected override bool IsEnabled
			{
				get
				{
					return base.Basis.DocumentIsEditable;
				}
			}

			// Token: 0x0601EA64 RID: 125540 RVA: 0x006F5C0F File Offset: 0x006F3E0F
			protected override bool Process()
			{
				return base.Basis.Get<PickPackShip.ConfirmShipmentCommand.Logic>().ConfirmShipment(!base.Basis.HasPick && !base.Basis.HasPack);
			}

			// Token: 0x0200D330 RID: 54064
			public class Logic : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanExtension
			{
				// Token: 0x0602FE76 RID: 196214 RVA: 0x008E83C0 File Offset: 0x008E65C0
				public virtual bool ConfirmShipment(bool confirmAsIs)
				{
					if (!this.CanConfirm(confirmAsIs))
					{
						return true;
					}
					PickPackShip.PackMode.Logic logic = base.Basis.Get<PickPackShip.PackMode.Logic>();
					SOPackageDetailEx selectedPackage = logic.SelectedPackage;
					bool flag;
					if (selectedPackage == null)
					{
						flag = false;
					}
					else
					{
						bool? confirmed = selectedPackage.Confirmed;
						bool flag2 = false;
						flag = (confirmed.GetValueOrDefault() == flag2 & confirmed != null);
					}
					if (flag && !base.Basis.Get<PickPackShip.PackMode.BoxConfirming.CompleteState.Logic>().TryAutoConfirm())
					{
						return true;
					}
					int? packageLineNbr = logic.PackageLineNbr;
					base.Basis.Reset(false);
					base.Basis.Clear<WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.InventoryItemState>(true);
					logic.PackageLineNbr = packageLineNbr;
					SOPackageDetailEx autoPackageToConfirm = null;
					if (!confirmAsIs && base.Basis.Header.Mode.IsIn("PACK", "SHIP"))
					{
						logic.HasSingleAutoPackage(base.Basis.RefNbr, out autoPackageToConfirm);
					}
					string refNbr = base.Basis.RefNbr;
					SOPickPackShipSetup setup2 = base.Basis.Setup.Current;
					SOPickPackShipUserSetup userSetup2 = PXSetupBase<PickPackShip.UserSetup, PickPackShip.Host, ScanHeader, SOPickPackShipUserSetup, Where<SOPickPackShipUserSetup.userID, Equal<Current<AccessInfo.userID>>>>.For(base.Basis);
					SOPickPackShipUserSetup userSetup = userSetup2;
					SOPickPackShipSetup setup = setup2;
					base.Basis.SaveChanges();
					base.Basis.AwaitFor<SOShipment>((PickPackShip basis, SOShipment doc, CancellationToken ct) => PickPackShip.ConfirmShipmentCommand.Logic.ConfirmShipmentHandler(doc.ShipmentNbr, confirmAsIs, setup, userSetup, autoPackageToConfirm, ct)).WithDescription("Confirmation of {0} shipment in progress.", new object[]
					{
						base.Basis.RefNbr
					}).ActualizeDataBy((PickPackShip basis, SOShipment doc) => PrimaryKeyOf<SOShipment>.By<SOShipment.shipmentNbr>.Find(basis, doc, PKFindOptions.None)).OnSuccess(delegate(ScanLongRunAwaiter<PickPackShip, SOShipment>.ISuccessProcessor x)
					{
						x.Say("Shipment successfully confirmed.", Array.Empty<object>()).ChangeStateTo<PickPackShip.ShipmentState>();
					}).OnFail(delegate(ScanLongRunAwaiter<PickPackShip, SOShipment>.IResultProcessor x)
					{
						x.Say("Shipment not confirmed.", Array.Empty<object>());
					}).BeginAwait(base.Basis.Shipment);
					return true;
				}

				// Token: 0x0602FE77 RID: 196215 RVA: 0x008E85A0 File Offset: 0x008E67A0
				protected static Task ConfirmShipmentHandler(string shipmentNbr, bool confirmAsIs, SOPickPackShipSetup setup, SOPickPackShipUserSetup userSetup, SOPackageDetailEx autoPackageToConfirm, CancellationToken cancellationToken)
				{
					return PXGraph.CreateInstance<SOShipmentEntry>().FindImplementation<PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation>().ApplyPickedQtyAndConfirmShipment(shipmentNbr, confirmAsIs, setup, userSetup, autoPackageToConfirm, cancellationToken);
				}

				// Token: 0x0602FE78 RID: 196216 RVA: 0x008E85B9 File Offset: 0x008E67B9
				protected virtual bool CanConfirm(bool confirmAsIs)
				{
					return confirmAsIs || ((!base.Basis.HasPick || this.CanConfirmPicked()) && (!base.Basis.HasPack || this.CanConfirmPacked()));
				}

				// Token: 0x0602FE79 RID: 196217 RVA: 0x008E85F0 File Offset: 0x008E67F0
				protected virtual bool CanConfirmPicked()
				{
					SOShipLineSplit[] source = base.Basis.Get<PickPackShip.PickMode.Logic>().Picked.SelectMain(Array.Empty<object>());
					if (source.All(delegate(SOShipLineSplit s)
					{
						decimal? pickedQty = s.PickedQty;
						decimal d = 0m;
						return pickedQty.GetValueOrDefault() == d & pickedQty != null;
					}))
					{
						base.Basis.ReportError("The shipment cannot be confirmed because no items have been picked.", Array.Empty<object>());
						return false;
					}
					if (base.Basis.Info.Current.MessageType != "WRN" && source.Any(delegate(SOShipLineSplit s)
					{
						decimal? pickedQty = s.PickedQty;
						decimal? num = s.Qty * base.Basis.Graph.GetMinQtyThreshold(s);
						return pickedQty.GetValueOrDefault() < num.GetValueOrDefault() & (pickedQty != null & num != null);
					}))
					{
						if (base.Basis.CannotConfirmPartialShipments)
						{
							base.Basis.ReportError("The shipment cannot be confirmed because at least one line has not been processed to completion.", Array.Empty<object>());
						}
						else
						{
							base.Basis.ReportWarning("At least one line has not been processed to completion. Do you want to confirm the shipment?", Array.Empty<object>());
						}
						return false;
					}
					if (base.Basis.HasIncompleteLinesBy<SOShipLineSplit.pickedQty>())
					{
						base.Basis.ReportError("The shipment cannot be confirmed because at least one line has not been processed to completion.", Array.Empty<object>());
						return false;
					}
					return true;
				}

				// Token: 0x0602FE7A RID: 196218 RVA: 0x008E86EC File Offset: 0x008E68EC
				protected virtual bool CanConfirmPacked()
				{
					SOShipLineSplit[] source = base.Basis.Get<PickPackShip.PackMode.Logic>().PickedForPack.SelectMain(Array.Empty<object>());
					if (source.All(delegate(SOShipLineSplit s)
					{
						decimal? packedQty = s.PackedQty;
						decimal d = 0m;
						return packedQty.GetValueOrDefault() == d & packedQty != null;
					}))
					{
						return true;
					}
					if (base.Basis.Info.Current.MessageType != "WRN" && source.Any(delegate(SOShipLineSplit s)
					{
						decimal? packedQty = s.PackedQty;
						decimal? num = s.Qty * base.Basis.Graph.GetMinQtyThreshold(s);
						return packedQty.GetValueOrDefault() < num.GetValueOrDefault() & (packedQty != null & num != null);
					}))
					{
						if (base.Basis.CannotConfirmPartialShipments)
						{
							base.Basis.ReportError("The shipment cannot be confirmed because at least one line has not been processed to completion.", Array.Empty<object>());
						}
						else
						{
							base.Basis.ReportWarning("At least one line has not been processed to completion. Do you want to confirm the shipment?", Array.Empty<object>());
						}
						return false;
					}
					if (base.Basis.HasIncompleteLinesBy<SOShipLineSplit.packedQty>())
					{
						base.Basis.ReportError("The shipment cannot be confirmed because at least one line has not been processed to completion.", Array.Empty<object>());
						return false;
					}
					return true;
				}

				// Token: 0x0602FE7B RID: 196219 RVA: 0x008E87D2 File Offset: 0x008E69D2
				[Obsolete("Use the PickPackShip.HasIncompleteLinesBy method instead.")]
				protected virtual bool HasIncompleteLinesBy<TQtyField>() where TQtyField : class, IBqlField, IImplement<IBqlDecimal>
				{
					return base.Basis.HasIncompleteLinesBy<TQtyField>();
				}
			}

			// Token: 0x0200D331 RID: 54065
			public class PickPackShipShipmentConfirmation : PXGraphExtension<SOShipmentEntry>
			{
				// Token: 0x0602FE7F RID: 196223 RVA: 0x008E88D8 File Offset: 0x008E6AD8
				public static bool IsActive()
				{
					return PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
				}

				// Token: 0x0602FE80 RID: 196224 RVA: 0x008E88E0 File Offset: 0x008E6AE0
				public virtual Task ApplyPickedQtyAndConfirmShipment(string shipmentNbr, bool confirmAsIs, SOPickPackShipSetup setup, SOPickPackShipUserSetup userSetup, SOPackageDetailEx autoPackageToConfirm, CancellationToken cancellationToken)
				{
					PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation.<ApplyPickedQtyAndConfirmShipment>d__1 <ApplyPickedQtyAndConfirmShipment>d__;
					<ApplyPickedQtyAndConfirmShipment>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
					<ApplyPickedQtyAndConfirmShipment>d__.<>4__this = this;
					<ApplyPickedQtyAndConfirmShipment>d__.shipmentNbr = shipmentNbr;
					<ApplyPickedQtyAndConfirmShipment>d__.confirmAsIs = confirmAsIs;
					<ApplyPickedQtyAndConfirmShipment>d__.setup = setup;
					<ApplyPickedQtyAndConfirmShipment>d__.userSetup = userSetup;
					<ApplyPickedQtyAndConfirmShipment>d__.autoPackageToConfirm = autoPackageToConfirm;
					<ApplyPickedQtyAndConfirmShipment>d__.cancellationToken = cancellationToken;
					<ApplyPickedQtyAndConfirmShipment>d__.<>1__state = -1;
					<ApplyPickedQtyAndConfirmShipment>d__.<>t__builder.Start<PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation.<ApplyPickedQtyAndConfirmShipment>d__1>(ref <ApplyPickedQtyAndConfirmShipment>d__);
					return <ApplyPickedQtyAndConfirmShipment>d__.<>t__builder.Task;
				}

				// Token: 0x0602FE81 RID: 196225 RVA: 0x008E8956 File Offset: 0x008E6B56
				protected virtual void CloseShipmentUserLinks(string shipmentNbr)
				{
					SOShipmentEntry.WorkLog workLogExt = base.Base.WorkLogExt;
					if (workLogExt == null)
					{
						return;
					}
					workLogExt.CloseFor(shipmentNbr);
				}

				// Token: 0x0602FE82 RID: 196226 RVA: 0x008E8970 File Offset: 0x008E6B70
				protected virtual void ApplyPickedQty(bool confirmAsIs, SOPickPackShipSetup setup)
				{
					PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation.<>c__DisplayClass3_0 CS$<>8__locals1 = new PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation.<>c__DisplayClass3_0();
					CS$<>8__locals1.<>4__this = this;
					CS$<>8__locals1.setup = setup;
					NonStockKitSpecHelper nonStockKitSpecHelper = new NonStockKitSpecHelper(base.Base);
					CS$<>8__locals1.RequireShipping = Func.Memorize<int, bool>((int inventoryID) => InventoryItem.PK.Find(CS$<>8__locals1.<>4__this.Base, new int?(inventoryID), PKFindOptions.None).With((InventoryItem item) => item.StkItem.GetValueOrDefault() || item.NonStockShip.GetValueOrDefault()));
					if (!confirmAsIs && (CS$<>8__locals1.setup.ShowPickTab.GetValueOrDefault() || CS$<>8__locals1.setup.ShowPackTab.GetValueOrDefault()))
					{
						PXSelectBase<SOShipLine> transactions = base.Base.Transactions;
						PXSelectBase<SOShipLineSplit> splits = base.Base.splits;
						foreach (PXResult<SOShipLine> r3 in transactions.Select(Array.Empty<object>()))
						{
							SOShipLine soshipLine = r3;
							transactions.Current = soshipLine;
							decimal num = 0m;
							decimal? qty;
							decimal d2;
							if (nonStockKitSpecHelper.IsNonStockKit(soshipLine.InventoryID))
							{
								IEnumerable<KeyValuePair<int, decimal>> nonStockKitSpec = nonStockKitSpecHelper.GetNonStockKitSpec(soshipLine.InventoryID.Value);
								Func<KeyValuePair<int, decimal>, bool> predicate;
								if ((predicate = CS$<>8__locals1.<>9__3) == null)
								{
									predicate = (CS$<>8__locals1.<>9__3 = ((KeyValuePair<int, decimal> pair) => CS$<>8__locals1.RequireShipping(pair.Key)));
								}
								Dictionary<int, decimal> dictionary = nonStockKitSpec.Where(predicate).ToDictionary<int, decimal>();
								IEnumerable<IGrouping<int, SOShipLineSplit>> source = from r in splits.SelectMain(Array.Empty<object>())
								group r by r.InventoryID.Value;
								Func<IGrouping<int, SOShipLineSplit>, int> keySelector = (IGrouping<int, SOShipLineSplit> g) => g.Key;
								Func<IGrouping<int, SOShipLineSplit>, decimal> elementSelector;
								if ((elementSelector = CS$<>8__locals1.<>9__6) == null)
								{
									elementSelector = (CS$<>8__locals1.<>9__6 = delegate(IGrouping<int, SOShipLineSplit> g)
									{
										Func<SOShipLineSplit, decimal> selector;
										if ((selector = CS$<>8__locals1.<>9__10) == null)
										{
											selector = (CS$<>8__locals1.<>9__10 = ((SOShipLineSplit s) => base.<ApplyPickedQty>g__GetNewQty|2(s)));
										}
										return g.Sum(selector);
									});
								}
								Dictionary<int, decimal> dictionary2 = source.ToDictionary(keySelector, elementSelector);
								decimal num2;
								if (dictionary.Keys.Count<int>() != 0 && dictionary.Keys.Except(dictionary2.Keys).Count<int>() <= 0)
								{
									num2 = dictionary2.Join(dictionary, delegate(KeyValuePair<int, decimal> split)
									{
										KeyValuePair<int, decimal> keyValuePair = split;
										return keyValuePair.Key;
									}, delegate(KeyValuePair<int, decimal> spec)
									{
										KeyValuePair<int, decimal> keyValuePair = spec;
										return keyValuePair.Key;
									}, delegate(KeyValuePair<int, decimal> split, KeyValuePair<int, decimal> spec)
									{
										KeyValuePair<int, decimal> keyValuePair = split;
										decimal value2 = keyValuePair.Value;
										keyValuePair = spec;
										return Math.Floor(decimal.Divide(value2, keyValuePair.Value));
									}).Min();
								}
								else
								{
									num2 = 0m;
								}
								num = num2;
							}
							else
							{
								using (new UpdateIfFieldsChangedScope().AppendContext(new Type[]
								{
									typeof(SOShipLine.locationID)
								}))
								{
									foreach (PXResult<SOShipLineSplit> r2 in splits.Select(Array.Empty<object>()))
									{
										SOShipLineSplit value = r2;
										splits.Current = value;
										decimal num3 = CS$<>8__locals1.<ApplyPickedQty>g__GetNewQty|2(splits.Current);
										decimal d = num3;
										qty = splits.Current.Qty;
										if (!(d == qty.GetValueOrDefault() & qty != null))
										{
											splits.Current.Qty = new decimal?(num3);
											splits.UpdateCurrent();
										}
										qty = splits.Current.Qty;
										d2 = 0m;
										if (!(qty.GetValueOrDefault() == d2 & qty != null))
										{
											num += splits.Current.Qty.GetValueOrDefault();
										}
									}
								}
								num = INUnitAttribute.ConvertFromBase(transactions.Cache, transactions.Current.InventoryID, transactions.Current.UOM, num, INPrecision.NOROUND);
							}
							transactions.Current.Qty = new decimal?(num);
							transactions.UpdateCurrent();
							PXSelectBase<SOSetup> sosetup = base.Base.sosetup;
							qty = transactions.Current.Qty;
							d2 = 0m;
							if (qty.GetValueOrDefault() == d2 & qty != null)
							{
								bool? addAllToShipment = sosetup.Current.AddAllToShipment;
								bool flag = false;
								if (addAllToShipment.GetValueOrDefault() == flag & addAllToShipment != null)
								{
									transactions.DeleteCurrent();
								}
							}
						}
					}
				}

				// Token: 0x0602FE83 RID: 196227 RVA: 0x008E8DC4 File Offset: 0x008E6FC4
				protected virtual void HandleCarts()
				{
					foreach (PXResult<SOCartShipment> r in PXSelectBase<SOCartShipment, PXViewOf<SOCartShipment>.BasedOn<SelectFromBase<SOCartShipment, TypeArrayOf<IFbqlJoin>.Empty>.Where<BqlOperand<SOCartShipment.shipmentNbr, IBqlString>.IsEqual<BqlField<SOShipment.shipmentNbr, IBqlString>.FromCurrent>>>.Config>.Select(base.Base, Array.Empty<object>()))
					{
						SOCartShipment data = r;
						base.Base.Caches<SOCartShipment>().Delete(data);
					}
				}

				// Token: 0x0602FE84 RID: 196228 RVA: 0x008E8E2C File Offset: 0x008E702C
				protected virtual void HandlePackages(bool confirmAsIs, SOPickPackShipSetup setup, SOPackageDetailEx autoPackageToConfirm)
				{
					if (!confirmAsIs && (setup.ShowPickTab.GetValueOrDefault() || setup.ShowPackTab.GetValueOrDefault()))
					{
						foreach (SOPackageDetailEx sopackageDetailEx in base.Base.Packages.SelectMain(Array.Empty<object>()))
						{
							if (sopackageDetailEx.PackageType == "M" && base.Base.PackageDetailExt.PackageDetailSplit.Select(new object[]
							{
								sopackageDetailEx.ShipmentNbr,
								sopackageDetailEx.LineNbr
							}).Count == 0)
							{
								base.Base.Packages.Delete(sopackageDetailEx);
							}
						}
					}
					if (autoPackageToConfirm != null)
					{
						bool? flag = autoPackageToConfirm.Confirmed;
						bool flag2 = false;
						if (flag.GetValueOrDefault() == flag2 & flag != null)
						{
							autoPackageToConfirm.Confirmed = new bool?(true);
							base.Base.Packages.Update(autoPackageToConfirm);
						}
					}
					if (PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>())
					{
						SOPackageDetailEx[] source = base.Base.Packages.SelectMain(Array.Empty<object>());
						if (confirmAsIs)
						{
							foreach (SOPackageDetailEx sopackageDetailEx2 in from x in source
							where !x.Confirmed.GetValueOrDefault()
							select x)
							{
								sopackageDetailEx2.Confirmed = new bool?(true);
								base.Base.Packages.Cache.Update(sopackageDetailEx2);
							}
						}
						bool? flag = base.Base.Document.Current.IsPackageValid;
						bool flag2 = false;
						if (flag.GetValueOrDefault() == flag2 & flag != null)
						{
							if (source.Any((SOPackageDetailEx p) => p.PackageType == "A"))
							{
								base.Base.Document.Current.IsPackageValid = new bool?(true);
								base.Base.Document.UpdateCurrent();
							}
						}
					}
					if (base.Base.IsDirty)
					{
						base.Base.Document.Current.IsPackageValid = new bool?(true);
						base.Base.Document.UpdateCurrent();
						base.Base.Save.Press();
					}
				}

				// Token: 0x0602FE85 RID: 196229 RVA: 0x008E909C File Offset: 0x008E729C
				protected virtual void TryUseExternalConfirmation()
				{
					Carrier carrier;
					if (this.UseExternalShippingApplication(base.Base.Document.Current, out carrier))
					{
						throw new PXRedirectToUrlException("../../Frames/ShipmentAppLauncher.html?ShipmentApplicationType=" + carrier.ShippingApplicationType + "&ShipmentNbr=" + base.Base.Document.Current.ShipmentNbr, PXBaseRedirectException.WindowMode.NewWindow, true, string.Empty);
					}
				}

				// Token: 0x0602FE86 RID: 196230 RVA: 0x008E90FC File Offset: 0x008E72FC
				public virtual Task<bool> TryPrintShipmentForms(SOPickPackShipUserSetup userSetup, CancellationToken cancellationToken)
				{
					PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation.<TryPrintShipmentForms>d__7 <TryPrintShipmentForms>d__;
					<TryPrintShipmentForms>d__.<>t__builder = AsyncTaskMethodBuilder<bool>.Create();
					<TryPrintShipmentForms>d__.<>4__this = this;
					<TryPrintShipmentForms>d__.userSetup = userSetup;
					<TryPrintShipmentForms>d__.cancellationToken = cancellationToken;
					<TryPrintShipmentForms>d__.<>1__state = -1;
					<TryPrintShipmentForms>d__.<>t__builder.Start<PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation.<TryPrintShipmentForms>d__7>(ref <TryPrintShipmentForms>d__);
					return <TryPrintShipmentForms>d__.<>t__builder.Task;
				}

				// Token: 0x0602FE87 RID: 196231 RVA: 0x008E9150 File Offset: 0x008E7350
				protected virtual bool UseExternalShippingApplication(SOShipment shipment, out Carrier carrier)
				{
					carrier = Carrier.PK.Find(base.Base, shipment.ShipVia, PKFindOptions.None);
					return !base.Base.IsMobile && carrier != null && carrier.IsExternalShippingApplication.GetValueOrDefault();
				}
			}

			// Token: 0x0200D332 RID: 54066
			[PXLocalizable]
			public abstract class Msg
			{
				// Token: 0x040150C1 RID: 86209
				public const string DisplayName = "Confirm Shipment";

				// Token: 0x040150C2 RID: 86210
				public const string InProcess = "Confirmation of {0} shipment in progress.";

				// Token: 0x040150C3 RID: 86211
				public const string Success = "Shipment successfully confirmed.";

				// Token: 0x040150C4 RID: 86212
				public const string Fail = "Shipment not confirmed.";

				// Token: 0x040150C5 RID: 86213
				public const string ShipmentCannotBeConfirmed = "The shipment cannot be confirmed because no items have been picked.";

				// Token: 0x040150C6 RID: 86214
				public const string ShipmentCannotBeConfirmedNoPacked = "The shipment cannot be confirmed because no items have been packed.";

				// Token: 0x040150C7 RID: 86215
				public const string ShipmentCannotBeConfirmedInPart = "The shipment cannot be confirmed because at least one line has not been processed to completion.";

				// Token: 0x040150C8 RID: 86216
				public const string ShipmentShouldNotBeConfirmedInPart = "At least one line has not been processed to completion. Do you want to confirm the shipment?";
			}
		}

		// Token: 0x02002F59 RID: 12121
		public sealed class ConfirmShipmentAsIsCommand : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>.ScanCommand
		{
			// Token: 0x170097AB RID: 38827
			// (get) Token: 0x0601EA66 RID: 125542 RVA: 0x006F5C47 File Offset: 0x006F3E47
			public override string Code
			{
				get
				{
					return "CONFIRM*SHIPMENT*ALL";
				}
			}

			// Token: 0x170097AC RID: 38828
			// (get) Token: 0x0601EA67 RID: 125543 RVA: 0x006F5C4E File Offset: 0x006F3E4E
			public override string ButtonName
			{
				get
				{
					return "scanConfirmShipmentAll";
				}
			}

			// Token: 0x170097AD RID: 38829
			// (get) Token: 0x0601EA68 RID: 125544 RVA: 0x006F5C55 File Offset: 0x006F3E55
			public override string DisplayName
			{
				get
				{
					return "Confirm Shipment As Is";
				}
			}

			// Token: 0x170097AE RID: 38830
			// (get) Token: 0x0601EA69 RID: 125545 RVA: 0x006F5C5C File Offset: 0x006F3E5C
			protected override bool IsEnabled
			{
				get
				{
					return base.Basis.DocumentIsEditable;
				}
			}

			// Token: 0x0601EA6A RID: 125546 RVA: 0x006F5C69 File Offset: 0x006F3E69
			protected override bool Process()
			{
				return base.Basis.Get<PickPackShip.ConfirmShipmentCommand.Logic>().ConfirmShipment(true);
			}

			// Token: 0x0200D333 RID: 54067
			[PXLocalizable]
			public abstract class Msg : PickPackShip.ConfirmShipmentCommand.Msg
			{
				// Token: 0x040150C9 RID: 86217
				public new const string DisplayName = "Confirm Shipment As Is";
			}
		}

		// Token: 0x02002F5A RID: 12122
		[PXLocalizable]
		public new abstract class Msg : WarehouseManagementSystem<PickPackShip, PickPackShip.Host>.Msg
		{
			// Token: 0x0400EDC9 RID: 60873
			public const string ShipmentIsNotEditable = "The document has become unavailable for editing. Contact your manager.";

			// Token: 0x0400EDCA RID: 60874
			public const string InventoryMissingInShipment = "{0} item not listed in shipment.";

			// Token: 0x0400EDCB RID: 60875
			public const string LocationMissingInShipment = "{0} location not listed in shipment.";

			// Token: 0x0400EDCC RID: 60876
			public const string LotSerialMissingInShipment = "{0} lot or serial number not listed in shipment.";

			// Token: 0x0400EDCD RID: 60877
			public const string ShipmentContainsNonStockItemWithEmptyLocation = "The {0} shipment cannot be processed on the Pick, Pack, and Ship (SO302020) form because it contains a non-stock item with an empty location.";
		}

		// Token: 0x02002F5B RID: 12123
		public static class FieldAttached
		{
			// Token: 0x0200D334 RID: 54068
			public abstract class To<TTable> : PXFieldAttachedTo<TTable>.By<PickPackShip.Host> where TTable : class, IBqlTable, new()
			{
			}

			// Token: 0x0200D335 RID: 54069
			[PXUIField(DisplayName = "Matched")]
			public class Fits : PXFieldAttachedTo<SOShipLineSplit>.By<PickPackShip.Host>.AsBool.Named<PickPackShip.FieldAttached.Fits>
			{
				// Token: 0x0602FE8D RID: 196237 RVA: 0x008E91C0 File Offset: 0x008E73C0
				public override bool? GetValue(SOShipLineSplit row)
				{
					bool flag = true;
					if (base.Base.WMS.LocationID != null)
					{
						bool flag2 = flag;
						int? num = base.Base.WMS.LocationID;
						int? num2 = row.LocationID;
						flag = (flag2 & (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null)));
					}
					if (base.Base.WMS.InventoryID != null)
					{
						bool flag3 = flag;
						int? num2 = base.Base.WMS.InventoryID;
						int? num = row.InventoryID;
						bool flag4;
						if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
						{
							num = base.Base.WMS.SubItemID;
							num2 = row.SubItemID;
							flag4 = (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null));
						}
						else
						{
							flag4 = false;
						}
						flag = (flag3 && flag4);
					}
					if (base.Base.WMS.LotSerialNbr != null)
					{
						bool flag5 = flag;
						bool flag6;
						if (!string.Equals(base.Base.WMS.LotSerialNbr, row.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
						{
							if (base.Base.WMS.Header.Mode == "PICK" && base.Base.WMS.LotSerialTrack.IsEnterable)
							{
								decimal? pickedQty = row.PickedQty;
								decimal d = 0m;
								flag6 = (pickedQty.GetValueOrDefault() == d & pickedQty != null);
							}
							else
							{
								flag6 = false;
							}
						}
						else
						{
							flag6 = true;
						}
						flag = (flag5 && flag6);
					}
					return new bool?(flag);
				}
			}

			// Token: 0x0200D336 RID: 54070
			[PXUIField(Visible = false)]
			public class ShowLog : PXFieldAttachedTo<ScanHeader>.By<PickPackShip.Host>.AsBool.Named<PickPackShip.FieldAttached.ShowLog>
			{
				// Token: 0x0602FE8F RID: 196239 RVA: 0x008E9364 File Offset: 0x008E7564
				public override bool? GetValue(ScanHeader row)
				{
					return new bool?(base.Base.WMS.Setup.Current.ShowScanLogTab.GetValueOrDefault());
				}
			}
		}
	}
}
