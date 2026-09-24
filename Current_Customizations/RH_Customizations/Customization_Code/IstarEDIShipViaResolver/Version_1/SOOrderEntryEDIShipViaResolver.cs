using System;
using System.Linq;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.SO;
using TCAddon;

namespace IstarEDIShipViaResolver
{
    public class SOOrderEntryEDIShipViaResolver : PXGraphExtension<SOOrderEntry>
    {
        private bool _resolving;
        private bool _settingShipVia;

        protected virtual void _(Events.RowInserted<SOOrder> e)
        {
            Resolve(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowUpdated<SOOrder> e)
        {
            Resolve(e.Cache, e.Row);
        }

        protected virtual void _(
            Events.FieldUpdated<SOOrder,
                TCSOOrderExt.usrTCCustomField1> e)
        {
            Resolve(e.Cache, e.Row);
        }

        protected virtual void _(
            Events.FieldUpdated<SOOrder,
                TCSOOrderExt.usrTCCustomField2> e)
        {
            Resolve(e.Cache, e.Row);
        }

        protected virtual void _(
            Events.FieldUpdated<SOOrder,
                SOOrderEDIShipViaExt.usrManualShipVia> e)
        {
            if (e.Row != null && e.NewValue is bool manual && !manual)
                Resolve(e.Cache, e.Row);
        }

        protected virtual void _(
            Events.FieldUpdated<SOOrder, SOOrder.shipVia> e)
        {
            if (e.Row == null || _settingShipVia || !e.ExternalCall ||
                Base.IsContractBasedAPI || Base.IsImportFromExcel)
                return;

            // A user's direct Ship Via edit becomes an explicit override.
            e.Cache.SetValueExt<SOOrderEDIShipViaExt.usrManualShipVia>(
                e.Row, true);
        }

        private void Resolve(PXCache cache, SOOrder order)
        {
            if (_resolving || order == null || order.CustomerID == null)
                return;

            var control = cache.GetExtension<SOOrderEDIShipViaExt>(order);
            if (control?.UsrManualShipVia == true)
                return;

            var tc = cache.GetExtension<TCSOOrderExt>(order);
            string method = ShipViaMappings.Normalize(tc?.UsrTCCustomField1);
            string scac = ShipViaMappings.Normalize(tc?.UsrTCCustomField2);

            _resolving = true;
            try
            {
                Customer customer = Customer.PK.Find(Base, order.CustomerID);
                var matches = ShipViaMappings.Find(
                    customer?.AcctCD, method, scac);

                string[] shipVias = matches
                    .Select(x => x.ShipVia?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                string selected = shipVias.Length == 1
                    ? shipVias[0]
                    : null;

                Carrier carrier = selected == null
                    ? null
                    : Carrier.PK.Find(Base, selected);

                if (carrier?.IsActive == true)
                {
                    ClearWarning(cache, order);
                    if (!string.Equals(order.ShipVia, selected,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        SetShipVia(cache, order, selected);
                    }
                    cache.SetValue<SOOrderEDIShipViaExt.usrAutoShipVia>(
                        order, selected);
                    return;
                }

                // If an earlier mapping supplied the current value, let
                // Acumatica restore its normal customer/location default.
                if (!string.IsNullOrEmpty(control?.UsrAutoShipVia) &&
                    string.Equals(order.ShipVia, control.UsrAutoShipVia,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _settingShipVia = true;
                    try
                    {
                        cache.SetDefaultExt<SOOrder.shipVia>(order);
                    }
                    finally
                    {
                        _settingShipVia = false;
                    }
                }

                cache.SetValue<SOOrderEDIShipViaExt.usrAutoShipVia>(
                    order, null);

                ClearWarning(cache, order);
                if (method != null || scac != null)
                    ShowWarning(cache, order, method, scac);
            }
            finally
            {
                _resolving = false;
            }
        }

        private void SetShipVia(PXCache cache, SOOrder order,
            string shipVia)
        {
            _settingShipVia = true;
            try
            {
                cache.SetValueExt<SOOrder.shipVia>(order, shipVia);
            }
            finally
            {
                _settingShipVia = false;
            }
        }

        private static void ShowWarning(PXCache cache, SOOrder order,
            string method, string scac)
        {
            if (method != null)
            {
                cache.RaiseExceptionHandling<
                    TCSOOrderExt.usrTCCustomField1>(
                    order, method,
                    new PXSetPropertyException(
                        order,
                        "No unique active EDI Ship Via mapping was found. Acumatica's current Ship Via remains in use.",
                        PXErrorLevel.Warning));
            }
            else if (scac != null)
            {
                cache.RaiseExceptionHandling<
                    TCSOOrderExt.usrTCCustomField2>(
                    order, scac,
                    new PXSetPropertyException(
                        order,
                        "No unique active EDI Ship Via mapping was found. Acumatica's current Ship Via remains in use.",
                        PXErrorLevel.Warning));
            }
        }

        private static void ClearWarning(PXCache cache, SOOrder order)
        {
            var tc = cache.GetExtension<TCSOOrderExt>(order);
            cache.RaiseExceptionHandling<
                TCSOOrderExt.usrTCCustomField1>(
                order, tc?.UsrTCCustomField1, null);
            cache.RaiseExceptionHandling<
                TCSOOrderExt.usrTCCustomField2>(
                order, tc?.UsrTCCustomField2, null);
        }
    }
}
