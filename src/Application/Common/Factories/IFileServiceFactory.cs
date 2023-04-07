namespace Tarik.Application.Common;

public interface IFileServiceFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="IFileService"/>.
    /// FileServices are scoped to a <see cref="WorkItem"/>.
    /// </summary>
    IFileService CreateFileService(WorkItem workItem);
}