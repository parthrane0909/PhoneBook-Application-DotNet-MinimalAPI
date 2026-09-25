namespace Phonebook.Dtos;

public record ContactImportResponse(
    int Imported,
    int Skipped,
    List<ContactImportError> Errors);
