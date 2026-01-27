namespace JournalApp.Core.Entities;

/// <summary>
/// Junction table linking journal entries to tags.
/// </summary>
public class EntryTag
{
    public int Id { get; set; }
    
    public int JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
