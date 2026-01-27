using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;

namespace JournalApp.Core.Services;

/// <summary>
/// Service for analytics and insights on journal data.
/// </summary>
public class AnalyticsService
{
    private readonly IRepository<JournalEntry> _entryRepository;
    private readonly IRepository<EntryMood> _entryMoodRepository;
    private readonly IRepository<EntryTag> _entryTagRepository;
    private readonly IRepository<Mood> _moodRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly AuthService _authService;

    public AnalyticsService(
        IRepository<JournalEntry> entryRepository,
        IRepository<EntryMood> entryMoodRepository,
        IRepository<EntryTag> entryTagRepository,
        IRepository<Mood> moodRepository,
        IRepository<Tag> tagRepository,
        AuthService authService)
    {
        _entryRepository = entryRepository;
        _entryMoodRepository = entryMoodRepository;
        _entryTagRepository = entryTagRepository;
        _moodRepository = moodRepository;
        _tagRepository = tagRepository;
        _authService = authService;
    }

    /// <summary>
    /// Gets mood distribution (percentage of positive, neutral, negative moods).
    /// </summary>
    public async Task<Dictionary<MoodCategory, double>> GetMoodDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return new Dictionary<MoodCategory, double>();

        var entries = await GetEntriesInRangeAsync(startDate, endDate);
        var entryIds = entries.Select(e => e.Id).ToList();

        if (!entryIds.Any())
            return new Dictionary<MoodCategory, double>();

        // Get all primary moods for these entries
        var entryMoods = (await _entryMoodRepository.FindAsync(
            em => entryIds.Contains(em.JournalEntryId) && em.IsPrimary))
            .ToList();

        var allMoods = (await _moodRepository.GetAllAsync()).ToList();

        var moodCounts = new Dictionary<MoodCategory, int>
        {
            { MoodCategory.Positive, 0 },
            { MoodCategory.Neutral, 0 },
            { MoodCategory.Negative, 0 }
        };

        foreach (var entryMood in entryMoods)
        {
            var mood = allMoods.FirstOrDefault(m => m.Id == entryMood.MoodId);
            if (mood != null)
            {
                moodCounts[mood.Category]++;
            }
        }

        var total = moodCounts.Values.Sum();
        var distribution = new Dictionary<MoodCategory, double>();

        foreach (var category in moodCounts.Keys)
        {
            distribution[category] = total > 0 ? (double)moodCounts[category] / total * 100 : 0;
        }

        return distribution;
    }

    /// <summary>
    /// Gets the most frequent mood.
    /// </summary>
    public async Task<(Mood? Mood, int Count)> GetMostFrequentMoodAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return (null, 0);

        var entries = await GetEntriesInRangeAsync(startDate, endDate);
        var entryIds = entries.Select(e => e.Id).ToList();

        if (!entryIds.Any())
            return (null, 0);

        var entryMoods = (await _entryMoodRepository.FindAsync(
            em => entryIds.Contains(em.JournalEntryId) && em.IsPrimary))
            .ToList();

        var moodCounts = entryMoods
            .GroupBy(em => em.MoodId)
            .Select(g => new { MoodId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        if (moodCounts == null)
            return (null, 0);

        var mood = await _moodRepository.GetByIdAsync(moodCounts.MoodId);
        return (mood, moodCounts.Count);
    }

    /// <summary>
    /// Gets the most used tags.
    /// </summary>
    public async Task<List<(Tag Tag, int Count)>> GetMostUsedTagsAsync(int topN = 10, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return new List<(Tag, int)>();

        var entries = await GetEntriesInRangeAsync(startDate, endDate);
        var entryIds = entries.Select(e => e.Id).ToList();

        if (!entryIds.Any())
            return new List<(Tag, int)>();

        var entryTags = (await _entryTagRepository.FindAsync(
            et => entryIds.Contains(et.JournalEntryId)))
            .ToList();

        var tagCounts = entryTags
            .GroupBy(et => et.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToList();

        var result = new List<(Tag, int)>();
        foreach (var tagCount in tagCounts)
        {
            var tag = await _tagRepository.GetByIdAsync(tagCount.TagId);
            if (tag != null)
            {
                result.Add((tag, tagCount.Count));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets tag breakdown (percentage of entries per tag).
    /// </summary>
    public async Task<Dictionary<string, double>> GetTagBreakdownAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return new Dictionary<string, double>();

        var entries = await GetEntriesInRangeAsync(startDate, endDate);
        var entryIds = entries.Select(e => e.Id).ToList();

        if (!entryIds.Any())
            return new Dictionary<string, double>();

        var entryTags = (await _entryTagRepository.FindAsync(
            et => entryIds.Contains(et.JournalEntryId)))
            .ToList();

        var tagCounts = entryTags
            .GroupBy(et => et.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToList();

        var totalEntries = entries.Count();
        var breakdown = new Dictionary<string, double>();

        foreach (var tagCount in tagCounts)
        {
            var tag = await _tagRepository.GetByIdAsync(tagCount.TagId);
            if (tag != null)
            {
                breakdown[tag.Name] = (double)tagCount.Count / totalEntries * 100;
            }
        }

        return breakdown;
    }

    /// <summary>
    /// Gets word count trends over time.
    /// </summary>
    public async Task<Dictionary<DateTime, double>> GetWordCountTrendsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return new Dictionary<DateTime, double>();

        var entries = (await GetEntriesInRangeAsync(startDate, endDate))
            .OrderBy(e => e.Date)
            .ToList();

        var trends = new Dictionary<DateTime, double>();

        foreach (var entry in entries)
        {
            trends[entry.Date] = entry.WordCount;
        }

        return trends;
    }

    /// <summary>
    /// Gets average word count.
    /// </summary>
    public async Task<double> GetAverageWordCountAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return 0;

        var entries = await GetEntriesInRangeAsync(startDate, endDate);

        if (!entries.Any())
            return 0;

        return entries.Average(e => e.WordCount);
    }

    /// <summary>
    /// Helper method to get entries in a date range.
    /// </summary>
    private async Task<List<JournalEntry>> GetEntriesInRangeAsync(DateTime? startDate, DateTime? endDate)
    {
        if (_authService.CurrentUser == null)
            return new List<JournalEntry>();

        var userId = _authService.CurrentUser.Id;

        if (startDate.HasValue && endDate.HasValue)
        {
            return (await _entryRepository.FindAsync(
                e => e.UserId == userId && e.Date >= startDate.Value && e.Date <= endDate.Value))
                .ToList();
        }
        else if (startDate.HasValue)
        {
            return (await _entryRepository.FindAsync(
                e => e.UserId == userId && e.Date >= startDate.Value))
                .ToList();
        }
        else if (endDate.HasValue)
        {
            return (await _entryRepository.FindAsync(
                e => e.UserId == userId && e.Date <= endDate.Value))
                .ToList();
        }
        else
        {
            return (await _entryRepository.FindAsync(e => e.UserId == userId))
                .ToList();
        }
    }
}
