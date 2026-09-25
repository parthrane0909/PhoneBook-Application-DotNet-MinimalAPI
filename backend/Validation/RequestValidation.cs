using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Phonebook.Middleware;

namespace Phonebook.Validation;

/// <summary>
/// Runs the DataAnnotations validation ASP.NET Core MVC used to apply automatically
/// to [ApiController] request bodies — including nested import rows — and turns the
/// collected errors into the contract's {"detail": "..."} 400 responses.
/// </summary>
public static class RequestValidation
{
    /// <summary>
    /// Validates a whole request DTO and throws BadRequestException with the joined
    /// detail when anything is wrong.
    /// </summary>
    public static void Validate(object instance)
    {
        var errors = new List<RequestError>();
        Collect(instance, prefix: string.Empty, errors);

        if (errors.Count > 0)
        {
            throw new BadRequestException(ErrorResponses.Build(errors));
        }
    }

    /// <summary>
    /// Appends the validation errors of a single property — used where errors must be
    /// reported in parameter order (the GET /contacts query binder), on top of the
    /// shared walk above.
    /// </summary>
    public static void ValidateProperty(object instance, string propertyName, List<RequestError> errors)
    {
        var property = instance.GetType().GetProperty(propertyName)
            ?? throw new ArgumentException($"Unknown property '{propertyName}'.", nameof(propertyName));

        CollectProperty(instance, property, key: propertyName, errors);
    }

    private static void Collect(object instance, string prefix, List<RequestError> errors)
    {
        // MetadataToken keeps the declaration order MVC reported errors in
        // (reflection order alone is not guaranteed).
        var properties = instance.GetType().GetProperties()
            .OrderBy(p => p.MetadataToken);

        foreach (var property in properties)
        {
            var key = prefix + property.Name;
            CollectProperty(instance, property, key, errors);

            // Nested complex values (import rows) repeat with MVC's "Rows[0].Name"
            // keys. Strings are collections too, but their elements carry no
            // attributes of their own — only the property itself was validated above.
            if (property.GetValue(instance) is IEnumerable items and not string)
            {
                var index = 0;
                foreach (var item in items)
                {
                    if (item is not null and not string && item.GetType().IsClass)
                    {
                        Collect(item, $"{key}[{index}].", errors);
                    }

                    index++;
                }
            }
        }
    }

    private static void CollectProperty(
        object instance,
        PropertyInfo property,
        string key,
        List<RequestError> errors)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance) { MemberName = property.Name };

        if (Validator.TryValidateProperty(property.GetValue(instance), context, results))
        {
            return;
        }

        foreach (var result in results)
        {
            errors.Add(new RequestError(key, result.ErrorMessage ?? "is invalid"));
        }
    }
}
