using PX.Data;
using PX.Data.BQL.Fluent;

namespace MonthlyForecastReferenceTable
{
    public class MonthlyForecastMaint : PXGraph<MonthlyForecastMaint>
    {
        public PXSave<UsrMonthlyForecast> Save;
        public PXCancel<UsrMonthlyForecast> Cancel;

        public SelectFrom<UsrMonthlyForecast>
            .OrderBy<
                Asc<UsrMonthlyForecast.inventoryID,
                Asc<UsrMonthlyForecast.finPeriodID>>>
            .View ForecastRecords;
    }
}