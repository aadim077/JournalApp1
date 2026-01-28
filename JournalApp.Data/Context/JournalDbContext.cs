using JournalApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Data.Context;


// Database context for the Journal application.
// Manages all entity sets and database configuration.

public class JournalDbContext : DbContext
{
    public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<Mood> Moods { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Streak> Streaks { get; set; }
    public DbSet<EntryMood> EntryMoods { get; set; }
    public DbSet<EntryTag> EntryTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Salt).IsRequired();
        });

        // JournalEntry configuration
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique(); // One entry per user per day
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Entries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Entries)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Mood configuration
        modelBuilder.Entity<Mood>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7); // Hex color
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.CustomTags)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Streak configuration
        modelBuilder.Entity<Streak>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithOne(u => u.Streak)
                .HasForeignKey<Streak>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EntryMood configuration
        modelBuilder.Entity<EntryMood>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.JournalEntry)
                .WithMany(j => j.EntryMoods)
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Mood)
                .WithMany(m => m.EntryMoods)
                .HasForeignKey(e => e.MoodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EntryTag configuration
        modelBuilder.Entity<EntryTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.JournalEntry)
                .WithMany(j => j.EntryTags)
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.EntryTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedMoods(modelBuilder);
        SeedTags(modelBuilder);
        SeedCategories(modelBuilder);
    }

    private void SeedMoods(ModelBuilder modelBuilder)
    {
        var moods = new List<Mood>
        {
            // Positive moods
            new Mood { Id = 1, Name = "Happy", Category = MoodCategory.Positive, Color = "#FFD700", Icon = "😊" },
            new Mood { Id = 2, Name = "Excited", Category = MoodCategory.Positive, Color = "#FF6B6B", Icon = "🤩" },
            new Mood { Id = 3, Name = "Relaxed", Category = MoodCategory.Positive, Color = "#4ECDC4", Icon = "😌" },
            new Mood { Id = 4, Name = "Grateful", Category = MoodCategory.Positive, Color = "#95E1D3", Icon = "🙏" },
            new Mood { Id = 5, Name = "Confident", Category = MoodCategory.Positive, Color = "#F38181", Icon = "💪" },
            
            // Neutral moods
            new Mood { Id = 6, Name = "Calm", Category = MoodCategory.Neutral, Color = "#A8E6CF", Icon = "😐" },
            new Mood { Id = 7, Name = "Thoughtful", Category = MoodCategory.Neutral, Color = "#DCEDC1", Icon = "🤔" },
            new Mood { Id = 8, Name = "Curious", Category = MoodCategory.Neutral, Color = "#FFD3B6", Icon = "🧐" },
            new Mood { Id = 9, Name = "Nostalgic", Category = MoodCategory.Neutral, Color = "#FFAAA5", Icon = "😌" },
            new Mood { Id = 10, Name = "Bored", Category = MoodCategory.Neutral, Color = "#C7CEEA", Icon = "😑" },
            
            // Negative moods
            new Mood { Id = 11, Name = "Sad", Category = MoodCategory.Negative, Color = "#6C5CE7", Icon = "😢" },
            new Mood { Id = 12, Name = "Angry", Category = MoodCategory.Negative, Color = "#E74C3C", Icon = "😠" },
            new Mood { Id = 13, Name = "Stressed", Category = MoodCategory.Negative, Color = "#E67E22", Icon = "😰" },
            new Mood { Id = 14, Name = "Lonely", Category = MoodCategory.Negative, Color = "#95A5A6", Icon = "😔" },
            new Mood { Id = 15, Name = "Anxious", Category = MoodCategory.Negative, Color = "#9B59B6", Icon = "😟" }
        };

        modelBuilder.Entity<Mood>().HasData(moods);
    }

    private void SeedTags(ModelBuilder modelBuilder)
    {
        var tags = new List<Tag>
        {
            new Tag { Id = 1, Name = "Work", IsCustom = false },
            new Tag { Id = 2, Name = "Career", IsCustom = false },
            new Tag { Id = 3, Name = "Studies", IsCustom = false },
            new Tag { Id = 4, Name = "Family", IsCustom = false },
            new Tag { Id = 5, Name = "Friends", IsCustom = false },
            new Tag { Id = 6, Name = "Relationships", IsCustom = false },
            new Tag { Id = 7, Name = "Health", IsCustom = false },
            new Tag { Id = 8, Name = "Fitness", IsCustom = false },
            new Tag { Id = 9, Name = "Personal Growth", IsCustom = false },
            new Tag { Id = 10, Name = "Self-care", IsCustom = false },
            new Tag { Id = 11, Name = "Hobbies", IsCustom = false },
            new Tag { Id = 12, Name = "Travel", IsCustom = false },
            new Tag { Id = 13, Name = "Nature", IsCustom = false },
            new Tag { Id = 14, Name = "Finance", IsCustom = false },
            new Tag { Id = 15, Name = "Spirituality", IsCustom = false },
            new Tag { Id = 16, Name = "Birthday", IsCustom = false },
            new Tag { Id = 17, Name = "Holiday", IsCustom = false },
            new Tag { Id = 18, Name = "Vacation", IsCustom = false },
            new Tag { Id = 19, Name = "Celebration", IsCustom = false },
            new Tag { Id = 20, Name = "Exercise", IsCustom = false },
            new Tag { Id = 21, Name = "Reading", IsCustom = false },
            new Tag { Id = 22, Name = "Writing", IsCustom = false },
            new Tag { Id = 23, Name = "Cooking", IsCustom = false },
            new Tag { Id = 24, Name = "Meditation", IsCustom = false },
            new Tag { Id = 25, Name = "Yoga", IsCustom = false },
            new Tag { Id = 26, Name = "Music", IsCustom = false },
            new Tag { Id = 27, Name = "Shopping", IsCustom = false },
            new Tag { Id = 28, Name = "Parenting", IsCustom = false },
            new Tag { Id = 29, Name = "Projects", IsCustom = false },
            new Tag { Id = 30, Name = "Planning", IsCustom = false },
            new Tag { Id = 31, Name = "Reflection", IsCustom = false }
        };

        modelBuilder.Entity<Tag>().HasData(tags);
    }

    private void SeedCategories(ModelBuilder modelBuilder)
    {
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Personal", Description = "Personal thoughts and reflections", Color = "#3498DB" },
            new Category { Id = 2, Name = "Professional", Description = "Work and career related", Color = "#2ECC71" },
            new Category { Id = 3, Name = "Health & Wellness", Description = "Physical and mental health", Color = "#E74C3C" },
            new Category { Id = 4, Name = "Relationships", Description = "Family, friends, and relationships", Color = "#F39C12" },
            new Category { Id = 5, Name = "Goals & Dreams", Description = "Aspirations and achievements", Color = "#9B59B6" }
        };

        modelBuilder.Entity<Category>().HasData(categories);
    }
}
