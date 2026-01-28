using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JournalApp.Core.Services;


// Service for analytics and insights on journal data.

public partial class AnalyticsService
{
    private readonly IRepository<JournalEntry> _entryRepository;
    private readonly IRepository<EntryMood> _entryMoodRepository;
    private readonly IRepository<EntryTag> _entryTagRepository;
    private readonly IRepository<Mood> _moodRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly AuthService _authService;
    private readonly StreakService _streakService;

    public AnalyticsService(
        IRepository<JournalEntry> entryRepository,
        IRepository<EntryMood> entryMoodRepository,
        IRepository<EntryTag> entryTagRepository,
        IRepository<Mood> moodRepository,
        IRepository<Tag> tagRepository,
        AuthService authService,
        StreakService streakService)
    {
        _entryRepository = entryRepository;
        _entryMoodRepository = entryMoodRepository;
        _entryTagRepository = entryTagRepository;
        _moodRepository = moodRepository;
        _tagRepository = tagRepository;
        _authService = authService;
        _streakService = streakService;
    }

   
    // Gets mood distribution 
   
    public async Task<Dictionary<MoodCategory, double>> GetMoodDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return new Dictionary<MoodCategory, double>();

        var entries = await GetEntriesInRangeAsync(startDate, endDate);
        var entryIds = entries.Select(e => e.Id).ToList();

        if (!entryIds.Any())
            return new Dictionary<MoodCategory, double>();

        var moodCounts = new Dictionary<MoodCategory, int>
        {
            { MoodCategory.Positive, 0 },
            { MoodCategory.Neutral, 0 },
            { MoodCategory.Negative, 0 }
        };

        foreach (var entry in entries)
        {
            moodCounts[entry.PrimaryMood]++;
        }

        var total = moodCounts.Values.Sum();
        var distribution = new Dictionary<MoodCategory, double>();

        foreach (var category in moodCounts.Keys)
        {
            distribution[category] = total > 0 ? (double)moodCounts[category] / total * 100 : 0;
        }

        return distribution;
    }

    
    // Gets the most frequent mood.
   
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

    
    // Gets the most used tags.
   
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

    
    // Gets tag breakdown
   
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

   
    // Gets word count trends for the last N entries.
   
    public async Task<Dictionary<DateTime, int>> GetWordCountTrendsAsync(int userId, int lastN)
    {
        var entries = (await _entryRepository.FindAsync(e => e.UserId == userId))
            .OrderByDescending(e => e.Date)
            .Take(lastN)
            .OrderBy(e => e.Date)
            .ToList();

        return entries.ToDictionary(e => e.Date, e => e.WordCount);
    }

    
    //Gets word count trends over time.
   
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

    
    // Gets average word count.
    
    public async Task<double> GetAverageWordCountAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_authService.CurrentUser == null)
            return 0;

        var entries = await GetEntriesInRangeAsync(startDate, endDate);

        if (!entries.Any())
            return 0;

        return entries.Average(e => e.WordCount);
    }

   
    // Gets all statistics for the dashboard.
  
    public async Task<JournalStats> GetStatsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var stats = new JournalStats
        {
            TotalEntries = (await _entryRepository.FindAsync(e => e.UserId == userId)).Count(),
            MoodDistribution = (await GetMoodDistributionAsync(startDate, endDate)).ToDictionary(k => k.Key.ToString(), v => v.Value),
            MostUsedTags = (await GetMostUsedTagsAsync(5, startDate, endDate)).ToDictionary(k => k.Tag.Name, v => v.Count)
        };

        var moodInfo = await GetMostFrequentMoodAsync(startDate, endDate);
        stats.MostFrequentMood = moodInfo.Mood?.Name;

        // Populate streak info
        var streak = await _streakService.GetStreakAsync(userId);
        if (streak != null)
        {
            stats.CurrentStreak = streak.CurrentStreak;
            stats.LongestStreak = streak.LongestStreak;
        }

        return stats;
    }

  
    // Helper method to get entries in a date range.
   
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

public class JournalStats
{
    public int TotalEntries { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public string? MostFrequentMood { get; set; }
    public Dictionary<string, double> MoodDistribution { get; set; } = new();
    public Dictionary<string, int> MostUsedTags { get; set; } = new();
}
