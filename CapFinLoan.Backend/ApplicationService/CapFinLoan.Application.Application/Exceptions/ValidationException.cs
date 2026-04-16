using System.Net;

namespace CapFinLoan.Application.Application.Exceptions;

public sealed class ValidationException : ApplicationExceptionBase
{
    public ValidationException(string message)
        : base(message, HttpStatusCode.BadRequest)
    {
    }
}
