using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using System.Text;

namespace JournalApp.Core.Services;

/// <summary>
/// Service for exporting journal entries.
/// </summary>
public class ExportService
{
    private readonly IRepository<JournalEntry> _entryRepository;
    private readonly IRepository<EntryMood> _entryMoodRepository;
    private readonly IRepository<EntryTag> _entryTagRepository;
    private readonly IRepository<Mood> _moodRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly AuthService _authService;

    public ExportService(
        IRepository<JournalEntry> entryRepository,
        IRepository<EntryMood> entryMoodRepository,
        IRepository<EntryTag> entryTagRepository,
        IRepository<Mood> moodRepository,
        IRepository<Tag> tagRepository,
        IRepository<Category> categoryRepository,
        AuthService authService)
    {
        _entryRepository = entryRepository;
        _entryMoodRepository = entryMoodRepository;
        _entryTagRepository = entryTagRepository;
        _moodRepository = moodRepository;
        _tagRepository = tagRepository;
        _categoryRepository = categoryRepository;
        _authService = authService;
    }

    /// <summary>
    /// Exports journal entries to a text file for a given date range.
    /// TODO: Implement PDF export using QuestPDF
    /// </summary>
    public async Task<(bool Success, string Message, string? FilePath)> ExportToFileAsync(
        DateTime startDate,
        DateTime endDate,
        string outputPath)
    {
        try
        {
            if (_authService.CurrentUser == null)
                return (false, "User not authenticated.", null);

            var userId = _authService.CurrentUser.Id;

            // Get entries in date range
            var entries = (await _entryRepository.FindAsync(
                e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate))
                .OrderBy(e => e.Date)
                .ToList();

            if (!entries.Any())
                return (false, "No entries found in the specified date range.", null);

            // Get related data
            var entryIds = entries.Select(e => e.Id).ToList();
            var entryMoods = (await _entryMoodRepository.FindAsync(em => entryIds.Contains(em.JournalEntryId))).ToList();
            var entryTags = (await _entryTagRepository.FindAsync(et => entryIds.Contains(et.JournalEntryId))).ToList();
            var allMoods = (await _moodRepository.GetAllAsync()).ToList();
            var allTags = (await _tagRepository.GetAllAsync()).ToList();
            var allCategories = (await _categoryRepository.GetAllAsync()).ToList();

            // Generate text content
            var sb = new StringBuilder();
            sb.AppendLine($"Journal Entries - {_authService.CurrentUser.Username}");
            sb.AppendLine($"Date Range: {startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            foreach (var entry in entries)
            {
                sb.AppendLine($"Date: {entry.Date:dddd, MMMM dd, yyyy}");
                
                if (!string.IsNullOrEmpty(entry.Title))
                {
                    sb.AppendLine($"Title: {entry.Title}");
                }

                // Moods
                var moods = entryMoods.Where(em => em.JournalEntryId == entry.Id).ToList();
                if (moods.Any())
                {
                    var moodNames = moods.Select(em =>
                    {
                        var mood = allMoods.FirstOrDefault(m => m.Id == em.MoodId);
                        return mood != null ? $"{mood.Icon} {mood.Name}" + (em.IsPrimary ? " (Primary)" : "") : "";
                    }).Where(s => !string.IsNullOrEmpty(s));

                    sb.AppendLine($"Moods: {string.Join(", ", moodNames)}");
                }

                // Category
                if (entry.CategoryId.HasValue)
                {
                    var category = allCategories.FirstOrDefault(c => c.Id == entry.CategoryId);
                    if (category != null)
                    {
                        sb.AppendLine($"Category: {category.Name}");
                    }
                }

                // Tags
                var tags = entryTags.Where(et => et.JournalEntryId == entry.Id).ToList();
                if (tags.Any())
                {
                    var tagNames = tags.Select(et =>
                    {
                        var tag = allTags.FirstOrDefault(t => t.Id == et.TagId);
                        return tag?.Name ?? "";
                    }).Where(s => !string.IsNullOrEmpty(s));

                    sb.AppendLine($"Tags: {string.Join(", ", tagNames)}");
                }

                sb.AppendLine();
                sb.AppendLine(entry.Content);
                sb.AppendLine();
                sb.AppendLine($"Word Count: {entry.WordCount} | Created: {entry.CreatedAt:g} | Updated: {entry.UpdatedAt:g}");
                sb.AppendLine(new string('-', 80));
                sb.AppendLine();
            }

            // Write to file
            await File.WriteAllTextAsync(outputPath, sb.ToString());

            return (true, "Export successful!", outputPath);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to export: {ex.Message}", null);
        }
    }
}
