using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.Now;

    public (DateTime fromDate, DateTime toDate) GetFromDateAndToDate(DateTime? fromDate, DateTime? toDate)
    {
        DateTime fromDateToUse = fromDate ?? Now.AddDays(-7);
        DateTime toDateToUse = toDate ?? Now;

        if (fromDateToUse > toDateToUse)
        {
            throw new ArgumentException("The 'from' date cannot be greater than the 'to' date.");
        }

        if (fromDateToUse == toDateToUse)
        {
            throw new ArgumentException("The 'from' date cannot be the same as the 'to' date.");
        }

        if (fromDateToUse > Now)
        {
            throw new ArgumentException("The 'from' date cannot be greater than the current date.");
        }

        // if delta between fromDate and toDate is less than 1 hour, set fromDate to 1 hour before toDate
        if (toDateToUse.Subtract(fromDateToUse).TotalHours < 1)
        {
            fromDateToUse = toDateToUse.AddHours(-1);
        }

        return (fromDateToUse, toDateToUse);
    }
}
