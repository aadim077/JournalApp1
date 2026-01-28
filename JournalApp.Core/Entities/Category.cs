namespace JournalApp.Core.Entities;


/// Represents a category for organizing journal entries.

public class Category
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Color { get; set; } = string.Empty; 
    
    // Navigation properties
    public ICollection<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
}
