namespace JournalApp.Core.Entities;


// Tracks journaling streaks for a user.
// Maintains current streak, longest streak, and last entry date.

public class Streak
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int CurrentStreak { get; set; }
    
    public int LongestStreak { get; set; }
    
    public DateTime? LastEntryDate { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
