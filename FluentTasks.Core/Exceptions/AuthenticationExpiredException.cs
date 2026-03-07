namespace FluentTasks.Core.Exceptions;

/// <summary>
/// Thrown when the user's authentication token has expired and re-authentication is required.
/// </summary>
public sealed class AuthenticationExpiredException : Exception
{
    public AuthenticationExpiredException()
        : base("Authentication has expired. Please sign in again.") { }

    public AuthenticationExpiredException(string message)
        : base(message) { }

    public AuthenticationExpiredException(string message, Exception innerException)
        : base(message, innerException) { }
}
