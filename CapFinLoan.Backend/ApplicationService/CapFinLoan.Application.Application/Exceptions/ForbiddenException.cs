using System.Net;

namespace CapFinLoan.Application.Application.Exceptions;

public sealed class ForbiddenException : ApplicationExceptionBase
{
    public ForbiddenException(string message)
        : base(message, HttpStatusCode.Forbidden)
    {
    }
}
