namespace JournalApp.Core.Entities;

/// <summary>
/// Represents mood categories for journal entries.
/// </summary>
public enum MoodCategory
{
    Positive,
    Neutral,
    Negative
}

/// <summary>
/// Represents a mood that can be associated with journal entries.
/// Each entry has one primary mood and up to two secondary moods.
/// </summary>
public class Mood
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public MoodCategory Category { get; set; }
    
    public string Color { get; set; } = string.Empty; // Hex color for UI display
    
    public string Icon { get; set; } = string.Empty; // Icon identifier
    
    // Navigation properties
    public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();
}
