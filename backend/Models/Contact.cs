namespace Phonebook.Models;

/// <summary>A single contact. Maps to the "contacts" table.</summary>
public class Contact
{
    public long Id { get; set; }

    public string Name { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime? LastViewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public HashSet<Tag> Tags { get; set; } = new();
}
