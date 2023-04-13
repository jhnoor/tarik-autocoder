namespace Tarik.Application.Common;

public class ShellCommandException : Exception
{
    public ShellCommandException()
        : base()
    {
    }

    public ShellCommandException(string message)
        : base(message)
    {
    }

    public ShellCommandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
