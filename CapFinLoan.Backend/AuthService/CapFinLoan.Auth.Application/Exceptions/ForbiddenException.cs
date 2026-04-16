using System.Net;

namespace CapFinLoan.Auth.Application.Exceptions;

public sealed class ForbiddenException : ApplicationExceptionBase
{
    public ForbiddenException(string message)
        : base(message, HttpStatusCode.Forbidden)
    {
    }
}
