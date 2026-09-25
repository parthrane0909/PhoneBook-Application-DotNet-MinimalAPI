using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Phonebook.Dtos;
using Phonebook.Services;
using Phonebook.Validation;

namespace Phonebook.Endpoints;

/// <summary>
/// All contact endpoints — the Minimal API equivalent of the old ContactsController.
/// Handlers stay thin: bind the request, call <see cref="ContactService"/>, return the
/// result. The {id:long} constraint keeps literal routes (/contacts/tags,
/// /contacts/metrics) from being captured by /contacts/{id} and makes /contacts/abc a
/// 404, exactly as the old controller routes behaved.
/// </summary>
public static class ContactEndpoints
{
    public static void MapContactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var contacts = endpoints.MapGroup("/contacts");

        contacts.MapGet("", ListContacts);
        contacts.MapPost("", CreateContact);
        contacts.MapGet("tags", GetTags);
        contacts.MapGet("metrics", GetMetrics);
        contacts.MapPost("import", ImportContacts);
        contacts.MapGet("{id:long}", GetContact);
        contacts.MapPut("{id:long}", UpdateContact);
        contacts.MapDelete("{id:long}", DeleteContact);
        contacts.MapPatch("{id:long}/favorite", UpdateFavorite);
        contacts.MapPatch("{id:long}/viewed", MarkViewed);
    }

    private static IResult ListContacts(HttpRequest request, ContactService contactService)
    {
        var query = ContactListQuery.Bind(request.Query);

        return Results.Ok(contactService.GetContacts(
            query.Search,
            query.Favorite,
            query.Tag,
            query.Unlabeled,
            query.Recent,
            query.Sort,
            query.Page,
            query.Limit));
    }

    private static async Task<IResult> CreateContact(
        HttpRequest request,
        IOptions<JsonOptions> jsonOptions,
        ContactService contactService)
    {
        var body = await ReadBodyAsync<ContactCreateRequest>(request, jsonOptions);
        return Results.Ok(contactService.CreateContact(body));
    }

    private static IResult GetTags(ContactService contactService) =>
        Results.Ok(contactService.GetTags());

    private static IResult GetMetrics(ContactService contactService) =>
        Results.Ok(contactService.GetMetrics());

    private static async Task<IResult> ImportContacts(
        HttpRequest request,
        IOptions<JsonOptions> jsonOptions,
        ContactService contactService)
    {
        var body = await ReadBodyAsync<ContactImportRequest>(request, jsonOptions);
        return Results.Ok(contactService.ImportContacts(body));
    }

    private static IResult GetContact(long id, ContactService contactService) =>
        Results.Ok(contactService.GetContact(id));

    private static async Task<IResult> UpdateContact(
        long id,
        HttpRequest request,
        IOptions<JsonOptions> jsonOptions,
        ContactService contactService)
    {
        var body = await ReadBodyAsync<ContactUpdateRequest>(request, jsonOptions);
        return Results.Ok(contactService.UpdateContact(id, body));
    }

    private static IResult DeleteContact(long id, ContactService contactService)
    {
        contactService.DeleteContact(id);
        return Results.Ok(new { message = "Contact deleted successfully" });
    }

    private static async Task<IResult> UpdateFavorite(
        long id,
        HttpRequest request,
        IOptions<JsonOptions> jsonOptions,
        ContactService contactService)
    {
        var body = await ReadBodyAsync<FavoriteUpdate>(request, jsonOptions);
        return Results.Ok(contactService.UpdateFavorite(id, body.IsFavorite!.Value));
    }

    private static IResult MarkViewed(long id, ContactService contactService) =>
        Results.Ok(contactService.MarkViewed(id));

    private static Task<T> ReadBodyAsync<T>(HttpRequest request, IOptions<JsonOptions> jsonOptions)
        where T : class =>
        JsonBody.ReadAsync<T>(request, jsonOptions.Value.SerializerOptions);
}
