using System.ComponentModel.DataAnnotations;

namespace Phonebook.Dtos;

/// <summary>Body of POST /contacts/ and each row of an import request.</summary>
public class ContactCreateRequest
{
    [Required(ErrorMessage = "must not be blank")]
    [StringLength(255, ErrorMessage = "size must be between 0 and 255")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "must not be blank")]
    [StringLength(50, ErrorMessage = "size must be between 0 and 50")]
    public string PhoneNumber { get; set; } = "";

    [RegularExpression(
        @"^\s*$|^[^@\s]+@[^@\s]+$",
        ErrorMessage = "must be a well-formed email address")]
    [StringLength(255, ErrorMessage = "size must be between 0 and 255")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    // Nullable so an explicit null (like the Java DTO) is accepted and
    // treated as "no tags" instead of failing validation.
    [MaxLength(10, ErrorMessage = "size must be between 0 and 10")]
    public List<string>? Tags { get; set; } = new();
}
