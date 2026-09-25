namespace Phonebook.Dtos;

/// <summary>JSON shape of a contact as the frontend sees it (snake_case via serializer policy).</summary>
public record ContactResponse(
    long Id,
    string Name,
    string PhoneNumber,
    string? Email,
    string? Address,
    bool IsFavorite,
    DateTime? LastViewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<TagResponse> Tags);
