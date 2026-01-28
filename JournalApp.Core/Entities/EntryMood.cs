namespace JournalApp.Core.Entities;


// Junction table linking journal entries to moods.
// Supports one primary mood and up to two secondary moods per entry.

public class EntryMood
{
    public int Id { get; set; }
    
    public int JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    
    public int MoodId { get; set; }
    public Mood Mood { get; set; } = null!;
    
    public bool IsPrimary { get; set; } // True for primary mood, false for secondary
}
