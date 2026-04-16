using System.Net;

namespace CapFinLoan.Application.Application.Exceptions;

public sealed class ConflictException : ApplicationExceptionBase
{
    public ConflictException(string message)
        : base(message, HttpStatusCode.Conflict)
    {
    }
}
