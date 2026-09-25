namespace Phonebook.Dtos;

/// <summary>A row skipped during import. "Row" is the file row number (header = row 1, first data row = row 2).</summary>
public record ContactImportError(int Row, string Reason);
