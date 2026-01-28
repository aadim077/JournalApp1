using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JournalApp.Core.Services;


// Service for managing tags, moods, and categories.
// Provides initialization logic for pre-built metadata.

public class TagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<Mood> _moodRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly AuthService _authService;

    public TagService(
        IRepository<Tag> tagRepository, 
        IRepository<Mood> moodRepository,
        IRepository<Category> categoryRepository,
        AuthService authService)
    {
        _tagRepository = tagRepository;
        _moodRepository = moodRepository;
        _categoryRepository = categoryRepository;
        _authService = authService;
    }

    public async Task<List<Mood>> GetMoodsAsync()
    {
        var moods = (await _moodRepository.GetAllAsync()).ToList();
        if (!moods.Any())
        {
            await SeedMoodsAsync();
            moods = (await _moodRepository.GetAllAsync()).ToList();
        }
        return moods;
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var categories = (await _categoryRepository.GetAllAsync()).ToList();
        if (!categories.Any())
        {
            await SeedCategoriesAsync();
            categories = (await _categoryRepository.GetAllAsync()).ToList();
        }
        return categories;
    }

    public async Task<List<Tag>> GetPrebuiltTagsAsync()
    {
        var tags = (await _tagRepository.FindAsync(t => !t.IsCustom)).ToList();
        if (!tags.Any())
        {
            await SeedTagsAsync();
            tags = (await _tagRepository.FindAsync(t => !t.IsCustom)).ToList();
        }
        return tags;
    }

    private async Task SeedMoodsAsync()
    {
        var moods = new List<Mood>
        {
            new() { Name = "Happy", Category = MoodCategory.Positive },
            new() { Name = "Excited", Category = MoodCategory.Positive },
            new() { Name = "Relaxed", Category = MoodCategory.Positive },
            new() { Name = "Grateful", Category = MoodCategory.Positive },
            new() { Name = "Confident", Category = MoodCategory.Positive },
            new() { Name = "Calm", Category = MoodCategory.Neutral },
            new() { Name = "Thoughtful", Category = MoodCategory.Neutral },
            new() { Name = "Curious", Category = MoodCategory.Neutral },
            new() { Name = "Nostalgic", Category = MoodCategory.Neutral },
            new() { Name = "Bored", Category = MoodCategory.Neutral },
            new() { Name = "Sad", Category = MoodCategory.Negative },
            new() { Name = "Angry", Category = MoodCategory.Negative },
            new() { Name = "Stressed", Category = MoodCategory.Negative },
            new() { Name = "Lonely", Category = MoodCategory.Negative },
            new() { Name = "Anxious", Category = MoodCategory.Negative }
        };
        foreach (var m in moods) await _moodRepository.AddAsync(m);
        await _moodRepository.SaveChangesAsync();
    }

    private async Task SeedCategoriesAsync()
    {
        var categories = new List<Category>
        {
            new() { Name = "Work", Description = "Professional tasks and environment" },
            new() { Name = "Personal", Description = "Private life and thoughts" },
            new() { Name = "Health", Description = "Physical and mental well-being" },
            new() { Name = "Travel", Description = "Trips and exploration" },
            new() { Name = "Finance", Description = "Money management and goals" }
        };
        foreach (var c in categories) await _categoryRepository.AddAsync(c);
        await _categoryRepository.SaveChangesAsync();
    }

    private async Task SeedTagsAsync()
    {
        string[] tagNames = { "Work", "Career", "Studies", "Family", "Friends", "Relationships", "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies", "Travel", "Nature", "Finance", "Spirituality", "Birthday", "Holiday", "Vacation", "Celebration", "Exercise", "Reading", "Writing", "Cooking", "Meditation", "Yoga", "Music", "Shopping", "Parenting", "Projects", "Planning", "Reflection" };
        foreach (var name in tagNames) await _tagRepository.AddAsync(new Tag { Name = name, IsCustom = false });
        await _tagRepository.SaveChangesAsync();
    }
}
