namespace JournalApp.Core.Entities;

/// <summary>
/// Represents a journal entry. Each user can have only one entry per day.
/// Supports rich text/Markdown content, mood tracking, and tagging.
/// </summary>
public class JournalEntry
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime Date { get; set; } // Date of the entry (date only, no time)
    
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty; // Rich text or Markdown
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int WordCount { get; set; } // Calculated field
    
    // Category (optional)
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // Navigation properties for moods and tags
    public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();
    
    public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
}
