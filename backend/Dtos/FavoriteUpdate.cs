using System.ComponentModel.DataAnnotations;

namespace Phonebook.Dtos;

/// <summary>Body of PATCH /contacts/{id}/favorite.</summary>
public class FavoriteUpdate
{
    /// <summary>Nullable so a missing field produces the same 400 as the Java @NotNull.</summary>
    [Required(ErrorMessage = "must not be null")]
    public bool? IsFavorite { get; set; }
}
