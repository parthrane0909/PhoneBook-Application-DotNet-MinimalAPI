using Microsoft.EntityFrameworkCore;
using Phonebook.Models;

namespace Phonebook.Data;

/// <summary>
/// Entity Framework Core context for the phonebook database.
/// The column names and constraints mirror the original JPA entities
/// (contacts / tags / contact_tags).
/// </summary>
public class PhonebookDbContext : DbContext
{
    public PhonebookDbContext(DbContextOptions<PhonebookDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(c => c.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
            entity.Property(c => c.PhoneNumber).HasColumnName("phone_number").IsRequired().HasMaxLength(50);
            entity.Property(c => c.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(c => c.Address).HasColumnName("address").HasColumnType("text");
            entity.Property(c => c.IsFavorite).HasColumnName("is_favorite").IsRequired();
            entity.Property(c => c.LastViewedAt).HasColumnName("last_viewed_at");
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // Same unique constraints the Java entities declared:
            // phone_number is unique, email is unique when provided.
            entity.HasIndex(c => c.PhoneNumber).IsUnique().HasDatabaseName("uk_contacts_phone_number");
            entity.HasIndex(c => c.Email).IsUnique().HasDatabaseName("uk_contacts_email");

            // Many-to-many join table: contact_tags(contact_id, tag_id).
            entity.HasMany(c => c.Tags)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "contact_tags",
                    join =>
                    {
                        // Create the join-table shadow keys explicitly (with types) so
                        // HasKey works regardless of which delegate EF runs first.
                        var relationship = join.HasOne<Tag>().WithMany().HasForeignKey("tag_id");
                        join.Property<long>("contact_id");
                        join.HasKey("contact_id", "tag_id");
                        return relationship;
                    },
                    join => join.HasOne<Contact>().WithMany().HasForeignKey("contact_id"))
                .ToTable("contact_tags");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(t => t.Name).HasColumnName("name").IsRequired().HasMaxLength(80);
            entity.HasIndex(t => t.Name).IsUnique().HasDatabaseName("uk_tags_name");
        });
    }

    // Replaces the JPA @PrePersist / @PreUpdate callbacks:
    // new contacts get created_at/updated_at, and every update refreshes updated_at.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Contact>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
