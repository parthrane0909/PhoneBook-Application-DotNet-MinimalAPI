namespace Phonebook.Middleware;

/// <summary>Maps to the Java NoSuchElementException → HTTP 404 {"detail": ...}.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
