using System.ComponentModel.DataAnnotations;
using Phonebook.Middleware;
using Phonebook.Validation;

namespace Phonebook.Dtos;

/// <summary>
/// Query parameters of GET /contacts/ (filters, sort, pagination). The [Range]
/// attributes and their hand-set messages are the ones the old controller declared;
/// Bind() converts the raw query string with the rules the MVC model binder used.
/// </summary>
public class ContactListQuery
{
    public string? Search { get; set; }

    public bool? Favorite { get; set; }

    public string? Tag { get; set; }

    public bool Unlabeled { get; set; }

    public bool Recent { get; set; }

    public string Sort { get; set; } = "name_asc";

    [Range(1, int.MaxValue, ErrorMessage = "must be greater than or equal to 1")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "must be between 1 and 100")]
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Converts the raw query string, collecting errors in parameter order:
    /// absent values keep their defaults; an empty or whitespace-only value means
    /// "not set" for strings and nullable booleans but is an error for the others;
    /// a value that cannot be converted fails the whole request with
    /// "Invalid request parameter" — the same precedence ASP.NET Core MVC's
    /// FormatException rule had, regardless of where the bad parameter sits;
    /// remaining errors (empty values, [Range]) are joined into one detail string.
    /// </summary>
    public static ContactListQuery Bind(IQueryCollection query)
    {
        var errors = new List<RequestError>();
        var result = new ContactListQuery();

        result.Search = NullIfWhitespace(Raw(query, "search"));

        var favorite = Raw(query, "favorite");
        if (!string.IsNullOrWhiteSpace(favorite))
        {
            if (bool.TryParse(favorite, out var value))
            {
                result.Favorite = value;
            }
            else
            {
                throw new BadRequestException("Invalid request parameter");
            }
        }

        result.Tag = NullIfWhitespace(Raw(query, "tag"));

        var unlabeled = Raw(query, "unlabeled");
        if (unlabeled is not null)
        {
            if (unlabeled.Length == 0)
            {
                errors.Add(new RequestError(nameof(Unlabeled), "The value '' is invalid."));
            }
            else if (bool.TryParse(unlabeled, out var value))
            {
                result.Unlabeled = value;
            }
            else
            {
                throw new BadRequestException("Invalid request parameter");
            }
        }

        var recent = Raw(query, "recent");
        if (recent is not null)
        {
            if (recent.Length == 0)
            {
                errors.Add(new RequestError(nameof(Recent), "The value '' is invalid."));
            }
            else if (bool.TryParse(recent, out var value))
            {
                result.Recent = value;
            }
            else
            {
                throw new BadRequestException("Invalid request parameter");
            }
        }

        var sort = Raw(query, "sort");
        if (!string.IsNullOrWhiteSpace(sort))
        {
            result.Sort = sort;
        }

        var page = Raw(query, "page");
        if (page is not null)
        {
            if (page.Length == 0)
            {
                errors.Add(new RequestError(nameof(Page), "The value '' is invalid."));
            }
            else if (!int.TryParse(page, out var value))
            {
                throw new BadRequestException("Invalid request parameter");
            }
            else
            {
                result.Page = value;
                RequestValidation.ValidateProperty(result, nameof(Page), errors);
            }
        }

        var limit = Raw(query, "limit");
        if (limit is not null)
        {
            if (limit.Length == 0)
            {
                errors.Add(new RequestError(nameof(Limit), "The value '' is invalid."));
            }
            else if (!int.TryParse(limit, out var value))
            {
                throw new BadRequestException("Invalid request parameter");
            }
            else
            {
                result.Limit = value;
                RequestValidation.ValidateProperty(result, nameof(Limit), errors);
            }
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException(ErrorResponses.Build(errors));
        }

        return result;
    }

    /// <summary>First value of a repeated parameter wins; an absent parameter is null.</summary>
    private static string? Raw(IQueryCollection query, string name) =>
        query.TryGetValue(name, out var values) && values.Count > 0 ? values[0] : null;

    /// <summary>
    /// MVC converted whitespace-only query values to null for string targets (real
    /// values are never trimmed — only all-whitespace ones are dropped).
    /// </summary>
    private static string? NullIfWhitespace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
