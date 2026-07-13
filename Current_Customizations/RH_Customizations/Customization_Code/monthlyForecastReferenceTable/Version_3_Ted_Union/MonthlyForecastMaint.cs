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

        /// <summary>
        /// Synchronizes ForecastDate, ForecastYear, and ForecastMonth
        /// before a new row is inserted into the cache.
        ///
        /// Supported input formats:
        ///
        /// 1. ForecastDate only:
        ///    ForecastDate = 10/15/2026
        ///    Result:
        ///    ForecastDate = 10/01/2026
        ///    ForecastYear = 2026
        ///    ForecastMonth = 10
        ///
        /// 2. ForecastYear and ForecastMonth only:
        ///    ForecastYear = 2026
        ///    ForecastMonth = 10
        ///    Result:
        ///    ForecastDate = 10/01/2026
        ///
        /// 3. All three fields:
        ///    The values must represent the same forecast period.
        /// </summary>
        protected virtual void _(
            Events.RowInserting<UsrMonthlyForecast> e)
        {
            if (e.Row == null)
                return;

            NormalizeNewForecastPeriod(e.Row);
        }

        /// <summary>
        /// Allows updates to non-key fields, such as ForecastQty,
        /// while preventing an existing forecast record from being
        /// moved to a different forecast period.
        ///
        /// ForecastYear and ForecastMonth are key fields. Changing them
        /// on an existing cached record could corrupt the cache identity.
        /// </summary>
        protected virtual void _(
            Events.RowUpdating<UsrMonthlyForecast> e)
        {
            if (e.Row == null || e.NewRow == null)
                return;

            ValidateAndNormalizeExistingForecastPeriod(
                e.Row,
                e.NewRow);
        }

        /// <summary>
        /// Performs final validation before SQL persistence.
        ///
        /// This method validates only. It deliberately does not change
        /// ForecastYear or ForecastMonth because they are key fields.
        /// </summary>
        protected virtual void _(
            Events.RowPersisting<UsrMonthlyForecast> e)
        {
            if (e.Row == null)
                return;

            ValidateForecastPeriod(e.Cache, e.Row);
        }

        /// <summary>
        /// Forecast period fields are editable only while a row is new.
        ///
        /// Once a forecast has been saved, its period is part of the
        /// record identity. To move a forecast to another month, the
        /// existing row should be deleted and a new row created.
        /// </summary>
        protected virtual void _(
            Events.RowSelected<UsrMonthlyForecast> e)
        {
            if (e.Row == null)
                return;

            bool isNewRow =
                e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted;

            PXUIFieldAttribute.SetEnabled<
                UsrMonthlyForecast.forecastDate>(
                e.Cache,
                e.Row,
                isNewRow);

            PXUIFieldAttribute.SetEnabled<
                UsrMonthlyForecast.forecastYear>(
                e.Cache,
                e.Row,
                isNewRow);

            PXUIFieldAttribute.SetEnabled<
                UsrMonthlyForecast.forecastMonth>(
                e.Cache,
                e.Row,
                isNewRow);
        }

        /// <summary>
        /// Normalizes and synchronizes the forecast period for a new row.
        ///
        /// ForecastDate is authoritative when it is supplied. Any supplied
        /// ForecastYear or ForecastMonth values must agree with it.
        ///
        /// If ForecastDate is not supplied, both ForecastYear and
        /// ForecastMonth must be supplied.
        /// </summary>
        private static void NormalizeNewForecastPeriod(
            UsrMonthlyForecast row)
        {
            bool hasDate = row.ForecastDate.HasValue;

            bool hasYear =
                !string.IsNullOrWhiteSpace(row.ForecastYear);

            bool hasMonth =
                !string.IsNullOrWhiteSpace(row.ForecastMonth);

            if (hasDate)
            {
                DateTime normalizedDate =
                    NormalizeForecastDate(
                        row.ForecastDate.Value);

                if (hasYear)
                {
                    int enteredYear =
                        ParseForecastYear(row.ForecastYear);

                    if (enteredYear != normalizedDate.Year)
                    {
                        throw new PXException(
                            "Forecast Date conflicts with Forecast Year. " +
                            $"The date represents year {normalizedDate.Year}, " +
                            $"but Forecast Year is {row.ForecastYear?.Trim()}.");
                    }
                }

                if (hasMonth)
                {
                    int enteredMonth =
                        ParseForecastMonth(row.ForecastMonth);

                    if (enteredMonth != normalizedDate.Month)
                    {
                        throw new PXException(
                            "Forecast Date conflicts with Forecast Month. " +
                            $"The date represents month {normalizedDate.Month:00}, " +
                            $"but Forecast Month is {row.ForecastMonth?.Trim()}.");
                    }
                }

                row.ForecastDate = normalizedDate;
                row.ForecastYear =
                    normalizedDate.Year.ToString("0000");
                row.ForecastMonth =
                    normalizedDate.Month.ToString("00");

                return;
            }

            if (!hasYear || !hasMonth)
            {
                throw new PXException(
                    "Enter Forecast Date, or enter both Forecast Year " +
                    "and Forecast Month.");
            }

            int year = ParseForecastYear(row.ForecastYear);
            int month = ParseForecastMonth(row.ForecastMonth);

            row.ForecastYear = year.ToString("0000");
            row.ForecastMonth = month.ToString("00");
            row.ForecastDate = new DateTime(year, month, 1);
        }

        /// <summary>
        /// Validates an update to an existing forecast record.
        ///
        /// An imported date may contain a day other than the first.
        /// It is normalized when it still represents the existing
        /// forecast month.
        ///
        /// Moving the row to another month or year is rejected because
        /// ForecastYear and ForecastMonth are key fields.
        /// </summary>
        private static void ValidateAndNormalizeExistingForecastPeriod(
            UsrMonthlyForecast existingRow,
            UsrMonthlyForecast newRow)
        {
            int existingYear =
                ParseForecastYear(existingRow.ForecastYear);

            int existingMonth =
                ParseForecastMonth(existingRow.ForecastMonth);

            int newYear =
                ParseForecastYear(newRow.ForecastYear);

            int newMonth =
                ParseForecastMonth(newRow.ForecastMonth);

            if (newYear != existingYear ||
                newMonth != existingMonth)
            {
                throw new PXException(
                    "The Forecast Year and Forecast Month of an existing " +
                    "forecast record cannot be changed. Delete the existing " +
                    "record and create a new record for the correct period.");
            }

            if (!newRow.ForecastDate.HasValue)
            {
                throw new PXException(
                    "Forecast Date is required.");
            }

            DateTime normalizedDate =
                NormalizeForecastDate(
                    newRow.ForecastDate.Value);

            if (normalizedDate.Year != existingYear ||
                normalizedDate.Month != existingMonth)
            {
                throw new PXException(
                    "Forecast Date cannot be changed to a different forecast " +
                    "period. Delete the existing record and create a new one " +
                    "for the correct period.");
            }

            // ForecastDate is not a key field, so it is safe to normalize
            // it while preserving the existing forecast period.
            newRow.ForecastDate = normalizedDate;

            // Preserve consistent fixed-length key formatting.
            newRow.ForecastYear =
                existingYear.ToString("0000");

            newRow.ForecastMonth =
                existingMonth.ToString("00");
        }

        /// <summary>
        /// Final consistency validation before persistence.
        ///
        /// No key values are changed here.
        /// </summary>
        private static void ValidateForecastPeriod(
            PXCache cache,
            UsrMonthlyForecast row)
        {
            if (!row.ForecastDate.HasValue)
            {
                RaisePersistingError(
                    cache,
                    row,
                    "Forecast Date is required.");

                return;
            }

            int year;
            int month;

            try
            {
                year = ParseForecastYear(row.ForecastYear);
                month = ParseForecastMonth(row.ForecastMonth);
            }
            catch (PXException exception)
            {
                RaisePersistingError(
                    cache,
                    row,
                    exception.Message);

                return;
            }

            DateTime normalizedDate =
                NormalizeForecastDate(
                    row.ForecastDate.Value);

            if (row.ForecastDate.Value.Date != normalizedDate)
            {
                RaisePersistingError(
                    cache,
                    row,
                    "Forecast Date must be the first day of its month.");

                return;
            }

            if (normalizedDate.Year != year ||
                normalizedDate.Month != month)
            {
                RaisePersistingError(
                    cache,
                    row,
                    "Forecast Date must match Forecast Year and Forecast Month.");
            }
        }

        /// <summary>
        /// Converts any supplied date to the first day of its month.
        /// </summary>
        private static DateTime NormalizeForecastDate(
            DateTime value)
        {
            return new DateTime(
                value.Year,
                value.Month,
                1);
        }

        /// <summary>
        /// Parses and validates the four-digit ForecastYear string.
        /// </summary>
        private static int ParseForecastYear(
            string forecastYear)
        {
            string value = forecastYear?.Trim();

            if (!int.TryParse(value, out int year) ||
                year < 1 ||
                year > 9999)
            {
                throw new PXException(
                    "Forecast Year must be a valid four-digit year.");
            }

            return year;
        }

        /// <summary>
        /// Parses and validates the two-digit ForecastMonth string.
        /// </summary>
        private static int ParseForecastMonth(
            string forecastMonth)
        {
            string value = forecastMonth?.Trim();

            if (!int.TryParse(value, out int month) ||
                month < 1 ||
                month > 12)
            {
                throw new PXException(
                    "Forecast Month must be between 01 and 12.");
            }

            return month;
        }

        /// <summary>
        /// Displays the error on ForecastDate and prevents persistence.
        /// </summary>
        private static void RaisePersistingError(
            PXCache cache,
            UsrMonthlyForecast row,
            string message)
        {
            cache.RaiseExceptionHandling<
                UsrMonthlyForecast.forecastDate>(
                row,
                row.ForecastDate,
                new PXSetPropertyException(
                    message,
                    PXErrorLevel.Error));

            throw new PXRowPersistingException(
                typeof(UsrMonthlyForecast.forecastDate).Name,
                row.ForecastDate,
                message);
        }
    }
}