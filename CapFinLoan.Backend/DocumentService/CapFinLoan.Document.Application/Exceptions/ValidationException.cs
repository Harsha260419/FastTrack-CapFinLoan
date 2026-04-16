using System.Net;

namespace CapFinLoan.Document.Application.Exceptions;

public sealed class ValidationException : ApplicationExceptionBase
{
    public ValidationException(string message)
        : base(message, HttpStatusCode.BadRequest)
    {
    }
}
