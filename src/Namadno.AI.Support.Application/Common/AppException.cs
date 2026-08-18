namespace Namadno.AI.Support.Application.Common;

public sealed class AppException : Exception
{
    public AppException(string code, int statusCode, string message)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}
