namespace Phonebook.Dtos;

/// <summary>Body of GET /contacts/metrics.</summary>
public record ContactMetricsResponse(
    long Total,
    long Favorites,
    long RecentlyAdded,
    long Unlabeled);
