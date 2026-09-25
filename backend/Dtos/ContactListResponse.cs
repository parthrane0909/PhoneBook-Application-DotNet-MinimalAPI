namespace Phonebook.Dtos;

/// <summary>Body of GET /contacts/ responses.</summary>
public record ContactListResponse(
    List<ContactResponse> Contacts,
    int Page,
    int Limit,
    long Total);
