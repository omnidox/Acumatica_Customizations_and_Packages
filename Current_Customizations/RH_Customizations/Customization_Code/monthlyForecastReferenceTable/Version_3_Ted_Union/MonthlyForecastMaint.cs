using System;
using PX.Data;
using PX.Data.BQL.Fluent;

namespace MonthlyForecastReferenceTable
{
    public class MonthlyForecastMaint : PXGraph<MonthlyForecastMaint>
    {
        public PXSavePerRow<UsrMonthlyForecast> Save;
        public PXCancel<UsrMonthlyForecast> Cancel;

        [PXImport]
        public SelectFrom<UsrMonthlyForecast>
            .OrderBy<
                Asc<UsrMonthlyForecast.customerID,
                Asc<UsrMonthlyForecast.inventoryID,
                Asc<UsrMonthlyForecast.forecastType,
                Asc<UsrMonthlyForecast.forecastYear,
                Asc<UsrMonthlyForecast.forecastMonth>>>>>>
            .View ForecastRecords;

        protected virtual void _(Events.RowInserting<UsrMonthlyForecast> e)
        {
            SetYearMonthFromForecastDate(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowUpdating<UsrMonthlyForecast> e)
        {
            SetYearMonthFromForecastDate(e.Cache, e.NewRow);
        }

        private static void SetYearMonthFromForecastDate(PXCache cache, UsrMonthlyForecast row)
        {
            if (row == null || row.ForecastDate == null)
                return;

            row.ForecastYear = row.ForecastDate.Value.Year.ToString();
            row.ForecastMonth = row.ForecastDate.Value.Month.ToString("00");
        }
    }
}