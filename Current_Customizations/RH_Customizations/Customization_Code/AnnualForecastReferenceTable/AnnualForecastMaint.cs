using PX.Data;
using PX.Data.BQL.Fluent;

namespace AnnualForecastReferenceTable
{
    public class AnnualForecastMaint :
        PXGraph<AnnualForecastMaint>
    {
        public PXSavePerRow<UsrAnnualForecast> Save;
        public PXCancel<UsrAnnualForecast> Cancel;

        [PXImport]
        public SelectFrom<UsrAnnualForecast>
            .OrderBy<
                Asc<UsrAnnualForecast.customerID,
                Asc<UsrAnnualForecast.inventoryID,
                Asc<UsrAnnualForecast.forecastYear,
                Asc<UsrAnnualForecast.forecastType>>>>>
            .View ForecastRecords;

        protected virtual void _(
            Events.RowSelected<UsrAnnualForecast> e)
        {
            if (e.Row == null)
                return;

            bool isNewRow =
                e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted;

            PXUIFieldAttribute.SetEnabled<
                UsrAnnualForecast.customerID>(
                    e.Cache,
                    e.Row,
                    isNewRow);

            PXUIFieldAttribute.SetEnabled<
                UsrAnnualForecast.inventoryID>(
                    e.Cache,
                    e.Row,
                    isNewRow);

            PXUIFieldAttribute.SetEnabled<
                UsrAnnualForecast.forecastYear>(
                    e.Cache,
                    e.Row,
                    isNewRow);

            PXUIFieldAttribute.SetEnabled<
                UsrAnnualForecast.forecastType>(
                    e.Cache,
                    e.Row,
                    isNewRow);
        }

        protected virtual void _(
            Events.FieldVerifying<
                UsrAnnualForecast,
                UsrAnnualForecast.forecastYear> e)
        {
            if (e.NewValue == null)
                return;

            string value = e.NewValue.ToString().Trim();

            if (value.Length != 4 ||
                !int.TryParse(value, out int year) ||
                year < 1 ||
                year > 9999)
            {
                throw new PXSetPropertyException(
                    "Forecast Year must be a valid four-digit year.");
            }

            e.NewValue = year.ToString("0000");
        }

        protected virtual void _(
            Events.FieldVerifying<
                UsrAnnualForecast,
                UsrAnnualForecast.forecastType> e)
        {
            if (e.NewValue == null)
                return;

            string value = e.NewValue
                .ToString()
                .Trim()
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new PXSetPropertyException(
                    "Forecast Type is required.");
            }

            if (value.Length > 2)
            {
                throw new PXSetPropertyException(
                    "Forecast Type cannot exceed two characters.");
            }

            e.NewValue = value;
        }
    }
}