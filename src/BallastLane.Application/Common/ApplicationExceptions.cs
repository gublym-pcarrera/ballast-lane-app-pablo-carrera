namespace BallastLane.Application.Common;

public sealed class ValidationException : Exception
{
    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public Dictionary<string, string[]> Errors { get; }
}

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class AuthenticationFailedException(string message) : Exception(message);
