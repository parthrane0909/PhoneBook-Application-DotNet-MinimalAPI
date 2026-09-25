namespace Phonebook.Models;

/// <summary>A tag that can be shared by many contacts. Maps to the "tags" table.</summary>
public class Tag
{
    public long Id { get; set; }

    public string Name { get; set; } = "";
}
