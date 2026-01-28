namespace JournalApp.Core.Entities;


// Represents a user in the journal application.
// Supports multi-user functionality with password-based authentication.

public class User
{
    public int Id { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public string Salt { get; set; } = string.Empty;
    
    public string? PinHash { get; set; }
    public string? PinSalt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
    
    public Streak? Streak { get; set; }
    
    public ICollection<Tag> CustomTags { get; set; } = new List<Tag>();
}
