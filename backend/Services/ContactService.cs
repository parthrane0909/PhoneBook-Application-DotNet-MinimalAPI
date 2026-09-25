using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Phonebook.Data;
using Phonebook.Dtos;
using Phonebook.Middleware;
using Phonebook.Models;

namespace Phonebook.Services;

/// <summary>
/// All phonebook business logic. Method-for-method equivalent of the Java
/// ContactService: same validation, same filters, same error messages.
/// </summary>
public class ContactService
{
    private static readonly HashSet<string> ValidSorts = new()
    {
        "name_asc",
        "name_desc",
        "recently_viewed",
        "recently_added",
        "recently_updated",
    };

    private readonly PhonebookDbContext _db;

    public ContactService(PhonebookDbContext db)
    {
        _db = db;
    }

    public ContactResponse CreateContact(ContactCreateRequest request)
    {
        ValidatePhone(request.PhoneNumber);

        var phone = request.PhoneNumber.Trim();
        var email = NormalizeNullable(request.Email);
        AssertUnique(phone, email, excludeId: null);

        var contact = new Contact
        {
            Name = request.Name,
            PhoneNumber = phone,
            Email = email,
            Address = NormalizeNullable(request.Address),
            Tags = new HashSet<Tag>(GetOrCreateTags(request.Tags)),
        };

        _db.Contacts.Add(contact);
        _db.SaveChanges();

        return ToResponse(contact);
    }

    public ContactListResponse GetContacts(
        string? search,
        bool? favorite,
        string? tag,
        bool unlabeled,
        bool recent,
        string sort,
        int page,
        int limit)
    {
        ValidateSort(sort);

        var query = _db.Contacts.Include(c => c.Tags).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = "%" + search.Trim().ToLower() + "%";
            query = query.Where(c =>
                EF.Functions.Like(c.Name.ToLower(), pattern) ||
                EF.Functions.Like(c.PhoneNumber.ToLower(), pattern) ||
                c.Tags.Any(t => EF.Functions.Like(t.Name.ToLower(), pattern)));
        }

        if (favorite.HasValue)
        {
            query = query.Where(c => c.IsFavorite == favorite.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagName = tag.Trim().ToLower();
            query = query.Where(c => c.Tags.Any(t => t.Name.ToLower() == tagName));
        }

        if (unlabeled)
        {
            query = query.Where(c => c.Tags.Count == 0);
        }

        if (recent)
        {
            query = query.Where(c => c.LastViewedAt != null);
        }

        var total = query.LongCount();

        var ordered = sort switch
        {
            "name_desc" => query.OrderByDescending(c => c.Name),
            "recently_viewed" => query
                .OrderBy(c => c.LastViewedAt == null) // non-null first == "DESC NULLS LAST"
                .ThenByDescending(c => c.LastViewedAt)
                .ThenBy(c => c.Name),
            "recently_added" => query.OrderByDescending(c => c.CreatedAt),
            "recently_updated" => query.OrderByDescending(c => c.UpdatedAt),
            _ => query.OrderBy(c => c.Name),
        };

        var contacts = ordered
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList()
            .Select(ToResponse)
            .ToList();

        return new ContactListResponse(contacts, page, limit, total);
    }

    public ContactResponse GetContact(long id)
    {
        return ToResponse(FindContact(id));
    }

    public ContactResponse UpdateContact(long id, ContactUpdateRequest request)
    {
        var contact = FindContact(id);

        // Merge semantics: only fields that are present get updated (same as Java).
        if (request.Name != null)
        {
            contact.Name = request.Name;
        }

        if (request.PhoneNumber != null)
        {
            ValidatePhone(request.PhoneNumber);
            contact.PhoneNumber = request.PhoneNumber.Trim();
        }

        if (request.Email != null)
        {
            contact.Email = NormalizeNullable(request.Email);
        }

        if (request.Address != null)
        {
            contact.Address = NormalizeNullable(request.Address);
        }

        if (request.Tags != null)
        {
            contact.Tags = new HashSet<Tag>(GetOrCreateTags(request.Tags));
        }

        AssertUnique(contact.PhoneNumber, contact.Email, excludeId: contact.Id);

        _db.SaveChanges();

        return ToResponse(contact);
    }

    public void DeleteContact(long id)
    {
        var contact = FindContact(id);
        _db.Contacts.Remove(contact);
        _db.SaveChanges();
    }

    public ContactResponse UpdateFavorite(long id, bool favorite)
    {
        var contact = FindContact(id);
        contact.IsFavorite = favorite;
        _db.SaveChanges();
        return ToResponse(contact);
    }

    public ContactResponse MarkViewed(long id)
    {
        var contact = FindContact(id);
        contact.LastViewedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return ToResponse(contact);
    }

    public List<TagResponse> GetTags()
    {
        return _db.Tags
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse(t.Id, t.Name))
            .ToList();
    }

    public ContactMetricsResponse GetMetrics()
    {
        var total = _db.Contacts.LongCount();
        var favorites = _db.Contacts.Count(c => c.IsFavorite);
        var unlabeled = _db.Contacts.Count(c => c.Tags.Count == 0);

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var recentlyAdded = _db.Contacts.Count(c => c.CreatedAt >= weekAgo);

        return new ContactMetricsResponse(total, favorites, recentlyAdded, unlabeled);
    }

    public ContactImportResponse ImportContacts(ContactImportRequest request)
    {
        if (request.Rows is null)
        {
            throw new BadRequestException("rows: must not be null");
        }

        if (request.Rows.Count > 5000)
        {
            throw new BadRequestException("rows: size must be between 0 and 5000");
        }

        var imported = 0;
        var skipped = 0;
        var errors = new List<ContactImportError>();

        // Row numbers start at 2 because row 1 of an import file is the header row.
        var rowNumber = 2;

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];

            try
            {
                ImportRow(row, i);
                imported++;
            }
            catch (BadRequestException ex)
            {
                skipped++;
                errors.Add(new ContactImportError(rowNumber, ex.Message));

                // Same effect as the Java per-row transaction rollback:
                // the failed row leaves no partial data behind.
                _db.ChangeTracker.Clear();
            }
            catch (DbUpdateException)
            {
                skipped++;
                errors.Add(new ContactImportError(rowNumber, "Duplicate phone number or email"));
                _db.ChangeTracker.Clear();
            }

            rowNumber++;
        }

        return new ContactImportResponse(imported, skipped, errors);
    }

    private void ImportRow(ContactCreateRequest row, int index)
    {
        ValidateRow(row, index);

        ValidatePhone(row.PhoneNumber);

        var phone = row.PhoneNumber.Trim();
        var email = NormalizeNullable(row.Email);
        AssertUnique(phone, email, excludeId: null);

        var contact = new Contact
        {
            Name = row.Name,
            PhoneNumber = phone,
            Email = email,
            Address = NormalizeNullable(row.Address),
            Tags = new HashSet<Tag>(GetOrCreateTags(row.Tags)),
        };

        _db.Contacts.Add(contact);
        _db.SaveChanges();
    }

    /// <summary>
    /// Fallback for validation of nested import rows, used only if automatic
    /// model validation did not already reject the request. Produces the same
    /// per-row 400 the Java API produced, with the Java field names
    /// ("rows[0].name: must not be blank; rows[0].phoneNumber: ...").
    /// </summary>
    private static void ValidateRow(ContactCreateRequest row, int index)
    {
        var prefix = $"rows[{index}].";
        var rowErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            rowErrors.Add(prefix + "name: must not be blank");
        }
        else if (row.Name.Length > 255)
        {
            rowErrors.Add(prefix + "name: size must be between 0 and 255");
        }

        if (string.IsNullOrWhiteSpace(row.PhoneNumber))
        {
            rowErrors.Add(prefix + "phoneNumber: must not be blank");
        }
        else if (row.PhoneNumber.Length > 50)
        {
            rowErrors.Add(prefix + "phoneNumber: size must be between 0 and 50");
        }

        if (row.Email != null)
        {
            // Same pattern as the [RegularExpression] on the DTOs, so the
            // fallback produces identical results to automatic validation.
            if (!System.Text.RegularExpressions.Regex.IsMatch(row.Email, @"^\s*$|^[^@\s]+@[^@\s]+$"))
            {
                rowErrors.Add(prefix + "email: must be a well-formed email address");
            }
            else if (row.Email.Length > 255)
            {
                rowErrors.Add(prefix + "email: size must be between 0 and 255");
            }
        }

        if (row.Tags is { Count: > 10 })
        {
            rowErrors.Add(prefix + "tags: size must be between 0 and 10");
        }

        if (rowErrors.Count > 0)
        {
            throw new BadRequestException(string.Join("; ", rowErrors));
        }
    }

    private static void ValidateSort(string sort)
    {
        if (!ValidSorts.Contains(sort))
        {
            throw new BadRequestException("Invalid sort value");
        }
    }

    private static void ValidatePhone(string? value)
    {
        if (value is null)
        {
            throw new BadRequestException("Enter a valid phone number with 7–15 digits.");
        }

        var phone = value.Trim();
        var digits = phone.Count(char.IsDigit);

        if (phone.Length == 0 ||
            digits < 7 ||
            digits > 15 ||
            !Regex.IsMatch(phone, @"^\+?[0-9\s()\-]+$"))
        {
            throw new BadRequestException("Enter a valid phone number with 7–15 digits.");
        }
    }

    private void AssertUnique(string phone, string? email, long? excludeId)
    {
        var duplicatePhone = excludeId is null
            ? _db.Contacts.Any(c => c.PhoneNumber == phone)
            : _db.Contacts.Any(c => c.PhoneNumber == phone && c.Id != excludeId);

        var duplicateEmail = email != null && (excludeId is null
            ? _db.Contacts.Any(c => c.Email == email)
            : _db.Contacts.Any(c => c.Email == email && c.Id != excludeId));

        if (duplicatePhone || duplicateEmail)
        {
            throw new BadRequestException("Phone number or email already exists");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private List<Tag> GetOrCreateTags(List<string>? names)
    {
        if (names is null || names.Count == 0)
        {
            return new List<Tag>();
        }

        // Deduplicate case-insensitively, keeping the first spelling (same as Java).
        var cleaned = new Dictionary<string, string>();

        foreach (var raw in names)
        {
            if (raw is null)
            {
                continue;
            }

            var name = raw.Trim();

            if (name.Length == 0)
            {
                continue;
            }

            if (name.Length > 80)
            {
                throw new BadRequestException("Each tag must be 80 characters or fewer.");
            }

            var key = name.ToLowerInvariant();
            if (!cleaned.ContainsKey(key))
            {
                cleaned[key] = name;
            }
        }

        if (cleaned.Count > 10)
        {
            throw new BadRequestException("A contact can have at most 10 tags.");
        }

        var tags = new List<Tag>();

        foreach (var name in cleaned.Values)
        {
            var tag = _db.Tags
                .FirstOrDefault(t => t.Name.ToLower() == name.ToLower());

            if (tag is null)
            {
                tag = new Tag { Name = name };
                _db.Tags.Add(tag);
            }

            tags.Add(tag);
        }

        return tags;
    }

    private Contact FindContact(long id)
    {
        var contact = _db.Contacts
            .Include(c => c.Tags)
            .FirstOrDefault(c => c.Id == id);

        if (contact is null)
        {
            throw new NotFoundException("Contact not found");
        }

        return contact;
    }

    private static ContactResponse ToResponse(Contact contact)
    {
        var tags = contact.Tags
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new TagResponse(t.Id, t.Name))
            .ToList();

        return new ContactResponse(
            contact.Id,
            contact.Name,
            contact.PhoneNumber,
            contact.Email,
            contact.Address,
            contact.IsFavorite,
            contact.LastViewedAt,
            contact.CreatedAt,
            contact.UpdatedAt,
            tags);
    }
}
