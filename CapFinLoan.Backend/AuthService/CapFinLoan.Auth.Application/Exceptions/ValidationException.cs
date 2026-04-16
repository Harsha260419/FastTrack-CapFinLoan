using System.Net;

namespace CapFinLoan.Auth.Application.Exceptions;

public sealed class ValidationException : ApplicationExceptionBase
{
    public ValidationException(string message)
        : base(message, HttpStatusCode.BadRequest)
    {
    }
}
