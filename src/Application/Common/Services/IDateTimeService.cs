namespace Tarik.Application.Common;

public interface IDateTimeService
{
    DateTime Now { get; }
    (DateTime fromDate, DateTime toDate) GetFromDateAndToDate(DateTime? fromDate, DateTime? toDate);
}
