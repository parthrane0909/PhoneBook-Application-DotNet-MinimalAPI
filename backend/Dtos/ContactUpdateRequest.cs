using System.ComponentModel.DataAnnotations;

namespace Phonebook.Dtos;

/// <summary>
/// Body of PUT /contacts/{id}. All fields are optional: a null field is
/// left unchanged on the existing contact (merge semantics, same as Java).
/// </summary>
public class ContactUpdateRequest
{
    [StringLength(255, ErrorMessage = "size must be between 0 and 255")]
    public string? Name { get; set; }

    [StringLength(50, ErrorMessage = "size must be between 0 and 50")]
    public string? PhoneNumber { get; set; }

    [RegularExpression(
        @"^\s*$|^[^@\s]+@[^@\s]+$",
        ErrorMessage = "must be a well-formed email address")]
    [StringLength(255, ErrorMessage = "size must be between 0 and 255")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>null = keep current tags, empty list = remove all tags.</summary>
    public List<string>? Tags { get; set; }
}
