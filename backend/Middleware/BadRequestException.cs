namespace Phonebook.Middleware;

/// <summary>Maps to the Java IllegalArgumentException → HTTP 400 {"detail": ...}.</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}
