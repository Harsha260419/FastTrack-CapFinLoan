using System.Net;

namespace CapFinLoan.Auth.Application.Exceptions;

public sealed class ConflictException : ApplicationExceptionBase
{
    public ConflictException(string message)
        : base(message, HttpStatusCode.Conflict)
    {
    }
}
