namespace Phonebook.Middleware;

/// <summary>
/// The request body used a content type no JSON formatter accepts (or none at all).
/// Unlike the other contract errors this one is answered with a problem+json body,
/// not {"detail": "..."} — see ExceptionHandlingMiddleware.
/// </summary>
public class UnsupportedMediaTypeException : Exception
{
}
