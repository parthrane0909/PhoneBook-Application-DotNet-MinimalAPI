namespace Phonebook.Middleware;

/// <summary>
/// One problem found while binding or validating a request. The key keeps the shape
/// the old MVC model-state keys had: "Name", "Rows[0].PhoneNumber", "$" for the JSON
/// document as a whole, or "" when the body itself is missing.
/// </summary>
public sealed record RequestError(string Key, string Message);

/// <summary>
/// Builds the {"detail": "..."} body when binding or validation fails (invalid JSON
/// fields, bad query parameters). This replaces the MethodArgumentNotValid /
/// ConstraintViolation / MethodArgumentTypeMismatch / HttpMessageNotReadable handlers
/// of the Java GlobalExceptionHandler — and the MVC model-state pipeline that used to
/// feed it — using the same decision rules, so every message stays byte-identical.
/// </summary>
public static class ErrorResponses
{
    public static string Build(IReadOnlyList<RequestError> errors)
    {
        if (errors.Count == 0)
        {
            return "Invalid request";
        }

        // A problem with the body as a whole: JSON conversion failures are keyed "$"
        // (or carry a System.Text.Json conversion message such as
        // "The JSON value could not be converted to type ...").
        // (Java: HttpMessageNotReadableException → "Invalid request body".)
        if (errors.Any(e => e.Key == "$" ||
            e.Message.Contains("could not be converted", StringComparison.Ordinal)))
        {
            return "Invalid request body";
        }

        // A value could not be converted, recorded the way ASP.NET Core used to
        // report it: "The value 'x' is not valid." (Java: MethodArgumentTypeMismatch
        // → "Invalid request parameter"). Query parameters never reach this — the
        // query binder throws the constant itself, with the same precedence MVC's
        // FormatException rule had — so this only guards stray legacy messages.
        if (errors.Any(e => e.Message.Contains("is not valid", StringComparison.Ordinal)))
        {
            return "Invalid request parameter";
        }

        // A missing body produces an error with no key at all.
        // (Java: HttpMessageNotReadableException → "Invalid request body".)
        if (errors.Any(e => string.IsNullOrEmpty(e.Key)))
        {
            return "Invalid request body";
        }

        // Same joined format as Java:
        // "name: must not be blank; phone_number: must not be blank"
        return string.Join("; ", errors.Select(e => JsonField(e.Key) + ": " + e.Message));
    }

    /// <summary>
    /// Mirrors the Java GlobalExceptionHandler jsonField() mapping:
    /// "PhoneNumber" → "phone_number", "IsFavorite" → "is_favorite",
    /// everything else just lower-cases the first letter of each segment
    /// ("Rows[0].Name" → "rows[0].name", "Rows[0].PhoneNumber" → "rows[0].phoneNumber").
    /// </summary>
    private static string JsonField(string modelStateKey)
    {
        var segments = modelStateKey.Split('.');

        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length > 0)
            {
                segments[i] = char.ToLowerInvariant(segments[i][0]) + segments[i][1..];
            }
        }

        var key = string.Join(".", segments);

        return key switch
        {
            "phoneNumber" => "phone_number",
            "isFavorite" => "is_favorite",
            _ => key,
        };
    }
}
