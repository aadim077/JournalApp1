namespace JournalApp.Core.Entities;

/// <summary>
/// Represents a category for organizing journal entries.
/// </summary>
public class Category
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Color { get; set; } = string.Empty; // Hex color for UI
    
    // Navigation properties
    public ICollection<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
}
