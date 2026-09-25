namespace Phonebook.Dtos;

/// <summary>Body of POST /contacts/import.</summary>
public class ContactImportRequest
{
    public List<ContactCreateRequest>? Rows { get; set; }
}
