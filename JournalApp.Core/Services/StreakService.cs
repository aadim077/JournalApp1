using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;

namespace JournalApp.Core.Services;

/// <summary>
/// Service for managing and calculating user streaks.
/// </summary>
public class StreakService
{
    private readonly IRepository<Streak> _streakRepository;
    private readonly IRepository<JournalEntry> _entryRepository;

    public StreakService(IRepository<Streak> streakRepository, IRepository<JournalEntry> entryRepository)
    {
        _streakRepository = streakRepository;
        _entryRepository = entryRepository;
    }

    /// <summary>
    /// Updates the streak after a new entry is created.
    /// </summary>
    public async Task UpdateStreakAsync(int userId, DateTime entryDate)
    {
        var streaks = await _streakRepository.FindAsync(s => s.UserId == userId);
        var streak = streaks.FirstOrDefault();

        if (streak == null)
        {
            // Create new streak
            streak = new Streak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastEntryDate = entryDate,
                UpdatedAt = DateTime.UtcNow
            };
            await _streakRepository.AddAsync(streak);
        }
        else
        {
            if (streak.LastEntryDate.HasValue)
            {
                var daysDifference = (entryDate.Date - streak.LastEntryDate.Value.Date).Days;

                if (daysDifference == 1)
                {
                    // Consecutive day - increment streak
                    streak.CurrentStreak++;
                    if (streak.CurrentStreak > streak.LongestStreak)
                    {
                        streak.LongestStreak = streak.CurrentStreak;
                    }
                }
                else if (daysDifference > 1)
                {
                    // Streak broken - reset to 1
                    streak.CurrentStreak = 1;
                }
                // If daysDifference == 0, it's the same day (shouldn't happen due to one-entry-per-day rule)
            }
            else
            {
                // First entry
                streak.CurrentStreak = 1;
                streak.LongestStreak = 1;
            }

            streak.LastEntryDate = entryDate;
            streak.UpdatedAt = DateTime.UtcNow;
            await _streakRepository.UpdateAsync(streak);
        }

        await _streakRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Recalculates the streak from scratch (used after deletion).
    /// </summary>
    public async Task RecalculateStreakAsync(int userId)
    {
        var entries = (await _entryRepository.FindAsync(e => e.UserId == userId))
            .OrderBy(e => e.Date)
            .ToList();

        var streaks = await _streakRepository.FindAsync(s => s.UserId == userId);
        var streak = streaks.FirstOrDefault();

        if (streak == null)
        {
            streak = new Streak { UserId = userId };
            await _streakRepository.AddAsync(streak);
        }

        if (!entries.Any())
        {
            streak.CurrentStreak = 0;
            streak.LongestStreak = 0;
            streak.LastEntryDate = null;
        }
        else
        {
            int currentStreak = 1;
            int longestStreak = 1;
            DateTime lastDate = entries[0].Date;

            for (int i = 1; i < entries.Count; i++)
            {
                var daysDiff = (entries[i].Date.Date - lastDate.Date).Days;

                if (daysDiff == 1)
                {
                    currentStreak++;
                    if (currentStreak > longestStreak)
                    {
                        longestStreak = currentStreak;
                    }
                }
                else if (daysDiff > 1)
                {
                    currentStreak = 1;
                }

                lastDate = entries[i].Date;
            }

            // Check if streak is still active (last entry was yesterday or today)
            var today = DateTime.UtcNow.Date;
            var daysSinceLastEntry = (today - lastDate.Date).Days;

            if (daysSinceLastEntry > 1)
            {
                currentStreak = 0; // Streak broken
            }

            streak.CurrentStreak = currentStreak;
            streak.LongestStreak = longestStreak;
            streak.LastEntryDate = lastDate;
        }

        streak.UpdatedAt = DateTime.UtcNow;
        await _streakRepository.UpdateAsync(streak);
        await _streakRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the streak for a user.
    /// </summary>
    public async Task<Streak?> GetStreakAsync(int userId)
    {
        var streaks = await _streakRepository.FindAsync(s => s.UserId == userId);
        return streaks.FirstOrDefault();
    }

    /// <summary>
    /// Gets missed days (dates without entries) in a date range.
    /// </summary>
    public async Task<List<DateTime>> GetMissedDaysAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var entries = await _entryRepository.FindAsync(
            e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate);

        var entryDates = entries.Select(e => e.Date.Date).ToHashSet();
        var missedDays = new List<DateTime>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (!entryDates.Contains(date))
            {
                missedDays.Add(date);
            }
        }

        return missedDays;
    }
}
