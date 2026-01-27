namespace JournalApp.Core.Entities;

/// <summary>
/// Represents a tag that can be applied to journal entries.
/// Supports both pre-built and custom user-created tags.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public bool IsCustom { get; set; }
    
    // For custom tags, links to the user who created it
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    // Navigation properties
    public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
}
