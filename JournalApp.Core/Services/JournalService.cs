using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JournalApp.Core.Services;


// Service for managing journal entries.
// Handles CRUD operations and enforces one-entry-per-day rule.

public class JournalService
{
    private readonly IRepository<JournalEntry> _entryRepository;
    private readonly IRepository<EntryMood> _entryMoodRepository;
    private readonly IRepository<EntryTag> _entryTagRepository;
    private readonly StreakService _streakService;
    private readonly AuthService _authService;

    public JournalService(
        IRepository<JournalEntry> entryRepository,
        IRepository<EntryMood> entryMoodRepository,
        IRepository<EntryTag> entryTagRepository,
        StreakService streakService,
        AuthService authService)
    {
        _entryRepository = entryRepository;
        _entryMoodRepository = entryMoodRepository;
        _entryTagRepository = entryTagRepository;
        _streakService = streakService;
        _authService = authService;
    }

   
    // Creates a new journal entry for the current user.
    
    public async Task<(bool Success, string Message, JournalEntry? Entry)> CreateEntryAsync(
        DateTime date,
        string title,
        string content,
        int? categoryId,
        int primaryMoodId,
        List<int>? secondaryMoodIds,
        List<int>? tagIds)
    {
        try
        {
            if (_authService.CurrentUser == null)
                return (false, "User not authenticated.", null);

            var userId = _authService.CurrentUser.Id;
            var entryDate = date.Date; // Normalize to date only

            // Check if entry already exists for this date
            var existingEntries = await _entryRepository.FindAsync(
                e => e.UserId == userId && e.Date.Date == entryDate);

            if (existingEntries.Any())
                return (false, "An entry already exists for this date.", null);

            // Calculate word count
            var wordCount = CalculateWordCount(content);

            // Create entry
            var entry = new JournalEntry
            {
                UserId = userId,
                Date = entryDate,
                Title = title,
                Content = content,
                CategoryId = categoryId,
                WordCount = wordCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _entryRepository.AddAsync(entry);
            await _entryRepository.SaveChangesAsync();

            // Add primary mood
            var primaryMood = new EntryMood
            {
                JournalEntryId = entry.Id,
                MoodId = primaryMoodId,
                IsPrimary = true
            };
            await _entryMoodRepository.AddAsync(primaryMood);

            // Add secondary moods (max 2)
            if (secondaryMoodIds != null && secondaryMoodIds.Any())
            {
                var moodsToAdd = secondaryMoodIds.Take(2);
                foreach (var moodId in moodsToAdd)
                {
                    var secondaryMood = new EntryMood
                    {
                        JournalEntryId = entry.Id,
                        MoodId = moodId,
                        IsPrimary = false
                    };
                    await _entryMoodRepository.AddAsync(secondaryMood);
                }
            }

            // Add tags
            if (tagIds != null && tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    var entryTag = new EntryTag
                    {
                        JournalEntryId = entry.Id,
                        TagId = tagId
                    };
                    await _entryTagRepository.AddAsync(entryTag);
                }
            }

            await _entryRepository.SaveChangesAsync();

            // Update streak
            await _streakService.UpdateStreakAsync(userId, entryDate);

            return (true, "Entry created successfully!", entry);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to create entry: {ex.Message}", null);
        }
    }

    // Updates an existing journal entry.
    
    public async Task<(bool Success, string Message)> UpdateEntryAsync(
        int entryId,
        string title,
        string content,
        int? categoryId,
        int primaryMoodId,
        List<int>? secondaryMoodIds,
        List<int>? tagIds)
    {
        try
        {
            if (_authService.CurrentUser == null)
                return (false, "User not authenticated.");

            var entry = await _entryRepository.GetByIdAsync(entryId);
            if (entry == null)
                return (false, "Entry not found.");

            if (entry.UserId != _authService.CurrentUser.Id)
                return (false, "Unauthorized access.");

            // Update entry
            entry.Title = title;
            entry.Content = content;
            entry.CategoryId = categoryId;
            entry.WordCount = CalculateWordCount(content);
            entry.UpdatedAt = DateTime.UtcNow;

            await _entryRepository.UpdateAsync(entry);

            // Update moods - remove existing and add new
            var existingMoods = await _entryMoodRepository.FindAsync(em => em.JournalEntryId == entryId);
            foreach (var mood in existingMoods)
            {
                await _entryMoodRepository.DeleteAsync(mood);
            }

            // Add primary mood
            var primaryMood = new EntryMood
            {
                JournalEntryId = entryId,
                MoodId = primaryMoodId,
                IsPrimary = true
            };
            await _entryMoodRepository.AddAsync(primaryMood);

            // Add secondary moods
            if (secondaryMoodIds != null && secondaryMoodIds.Any())
            {
                var moodsToAdd = secondaryMoodIds.Take(2);
                foreach (var moodId in moodsToAdd)
                {
                    var secondaryMood = new EntryMood
                    {
                        JournalEntryId = entryId,
                        MoodId = moodId,
                        IsPrimary = false
                    };
                    await _entryMoodRepository.AddAsync(secondaryMood);
                }
            }

            // Update tags - remove existing and add new
            var existingTags = await _entryTagRepository.FindAsync(et => et.JournalEntryId == entryId);
            foreach (var tag in existingTags)
            {
                await _entryTagRepository.DeleteAsync(tag);
            }

            if (tagIds != null && tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    var entryTag = new EntryTag
                    {
                        JournalEntryId = entryId,
                        TagId = tagId
                    };
                    await _entryTagRepository.AddAsync(entryTag);
                }
            }

            await _entryRepository.SaveChangesAsync();

            return (true, "Entry updated successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to update entry: {ex.Message}");
        }
    }

  
    // Deletes a journal entry.
    
    public async Task<(bool Success, string Message)> DeleteEntryAsync(int entryId)
    {
        try
        {
            if (_authService.CurrentUser == null)
                return (false, "User not authenticated.");

            var entry = await _entryRepository.GetByIdAsync(entryId);
            if (entry == null)
                return (false, "Entry not found.");

            if (entry.UserId != _authService.CurrentUser.Id)
                return (false, "Unauthorized access.");

            var entryDate = entry.Date;

            await _entryRepository.DeleteAsync(entry);
            await _entryRepository.SaveChangesAsync();

            // Update streak after deletion
            await _streakService.RecalculateStreakAsync(_authService.CurrentUser.Id);

            return (true, "Entry deleted successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete entry: {ex.Message}");
        }
    }

    
    // Gets an entry by date for the current user.
    
    public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
    {
        if (_authService.CurrentUser == null)
            return null;

        var entries = await _entryRepository.FindAsync(
            e => e.UserId == _authService.CurrentUser.Id && e.Date.Date == date.Date);

        return entries.FirstOrDefault();
    }

    
    // Gets all entries for the current user.
    
    public async Task<IEnumerable<JournalEntry>> GetAllEntriesAsync()
    {
        if (_authService.CurrentUser == null)
            return Enumerable.Empty<JournalEntry>();

        return await _entryRepository.FindAsync(e => e.UserId == _authService.CurrentUser.Id);
    }

    
    // Calculates word count from content.
   
    private int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }
}
