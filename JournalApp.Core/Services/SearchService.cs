using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;

namespace JournalApp.Core.Services;

/// <summary>
/// Service for searching and filtering journal entries.
/// </summary>
public class SearchService
{
    private readonly IRepository<JournalEntry> _entryRepository;
    private readonly IRepository<EntryMood> _entryMoodRepository;
    private readonly IRepository<EntryTag> _entryTagRepository;
    private readonly AuthService _authService;

    public SearchService(
        IRepository<JournalEntry> entryRepository,
        IRepository<EntryMood> entryMoodRepository,
        IRepository<EntryTag> entryTagRepository,
        AuthService authService)
    {
        _entryRepository = entryRepository;
        _entryMoodRepository = entryMoodRepository;
        _entryTagRepository = entryTagRepository;
        _authService = authService;
    }

    /// <summary>
    /// Searches entries by title or content.
    /// </summary>
    public async Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm)
    {
        if (_authService.CurrentUser == null || string.IsNullOrWhiteSpace(searchTerm))
            return new List<JournalEntry>();

        var userId = _authService.CurrentUser.Id;
        var lowerSearchTerm = searchTerm.ToLower();

        var entries = await _entryRepository.FindAsync(
            e => e.UserId == userId &&
                 (e.Title.ToLower().Contains(lowerSearchTerm) ||
                  e.Content.ToLower().Contains(lowerSearchTerm)));

        return entries.OrderByDescending(e => e.Date).ToList();
    }

    /// <summary>
    /// Filters entries by date range.
    /// </summary>
    public async Task<List<JournalEntry>> FilterByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (_authService.CurrentUser == null)
            return new List<JournalEntry>();

        var userId = _authService.CurrentUser.Id;

        var entries = await _entryRepository.FindAsync(
            e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate);

        return entries.OrderByDescending(e => e.Date).ToList();
    }

    /// <summary>
    /// Filters entries by moods.
    /// </summary>
    public async Task<List<JournalEntry>> FilterByMoodsAsync(List<int> moodIds)
    {
        if (_authService.CurrentUser == null || !moodIds.Any())
            return new List<JournalEntry>();

        var userId = _authService.CurrentUser.Id;

        // Get entry IDs that have any of the specified moods
        var entryMoods = await _entryMoodRepository.FindAsync(
            em => moodIds.Contains(em.MoodId));

        var entryIds = entryMoods.Select(em => em.JournalEntryId).Distinct().ToList();

        if (!entryIds.Any())
            return new List<JournalEntry>();

        var entries = await _entryRepository.FindAsync(
            e => e.UserId == userId && entryIds.Contains(e.Id));

        return entries.OrderByDescending(e => e.Date).ToList();
    }

    /// <summary>
    /// Filters entries by tags.
    /// </summary>
    public async Task<List<JournalEntry>> FilterByTagsAsync(List<int> tagIds)
    {
        if (_authService.CurrentUser == null || !tagIds.Any())
            return new List<JournalEntry>();

        var userId = _authService.CurrentUser.Id;

        // Get entry IDs that have any of the specified tags
        var entryTags = await _entryTagRepository.FindAsync(
            et => tagIds.Contains(et.TagId));

        var entryIds = entryTags.Select(et => et.JournalEntryId).Distinct().ToList();

        if (!entryIds.Any())
            return new List<JournalEntry>();

        var entries = await _entryRepository.FindAsync(
            e => e.UserId == userId && entryIds.Contains(e.Id));

        return entries.OrderByDescending(e => e.Date).ToList();
    }

    /// <summary>
    /// Advanced search with multiple filters.
    /// </summary>
    public async Task<List<JournalEntry>> AdvancedSearchAsync(
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        List<int>? moodIds = null,
        List<int>? tagIds = null)
    {
        if (_authService.CurrentUser == null)
            return new List<JournalEntry>();

        var userId = _authService.CurrentUser.Id;
        var allEntries = (await _entryRepository.FindAsync(e => e.UserId == userId)).ToList();

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            allEntries = allEntries.Where(e =>
                e.Title.ToLower().Contains(lowerSearchTerm) ||
                e.Content.ToLower().Contains(lowerSearchTerm)).ToList();
        }

        // Apply date range filter
        if (startDate.HasValue)
        {
            allEntries = allEntries.Where(e => e.Date >= startDate.Value).ToList();
        }

        if (endDate.HasValue)
        {
            allEntries = allEntries.Where(e => e.Date <= endDate.Value).ToList();
        }

        // Apply mood filter
        if (moodIds != null && moodIds.Any())
        {
            var entryMoods = await _entryMoodRepository.FindAsync(
                em => moodIds.Contains(em.MoodId));
            var moodEntryIds = entryMoods.Select(em => em.JournalEntryId).Distinct().ToHashSet();

            allEntries = allEntries.Where(e => moodEntryIds.Contains(e.Id)).ToList();
        }

        // Apply tag filter
        if (tagIds != null && tagIds.Any())
        {
            var entryTags = await _entryTagRepository.FindAsync(
                et => tagIds.Contains(et.TagId));
            var tagEntryIds = entryTags.Select(et => et.JournalEntryId).Distinct().ToHashSet();

            allEntries = allEntries.Where(e => tagEntryIds.Contains(e.Id)).ToList();
        }

        return allEntries.OrderByDescending(e => e.Date).ToList();
    }
}
