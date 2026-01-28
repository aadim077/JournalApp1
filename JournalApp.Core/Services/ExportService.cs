using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JournalApp.Core.Services;

public class ExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
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

   
    // Exports journal entries to a text file for a given date range.
    
    
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

    public async Task<(bool Success, string Message, string? FilePath)> ExportToPdfAsync(
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

            // Create Document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Personal Journal").FontSize(24).Bold().FontColor(Colors.DeepPurple.Medium);
                            col.Item().Text($"User: {_authService.CurrentUser.Username}").FontSize(12).Italic();
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"{startDate:MMM dd} - {endDate:MMM dd, yyyy}").FontSize(10);
                            col.Item().Text($"Total Entries: {entries.Count}").FontSize(10);
                        });
                    });

                    // Content
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        foreach (var entry in entries)
                        {
                            col.Item().PaddingBottom(1, Unit.Centimetre).Column(entryCol =>
                            {
                                // Entry Header
                                entryCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(entry.Date.ToString("dddd, MMMM dd, yyyy")).Bold().FontSize(14);
                                });

                                if (!string.IsNullOrEmpty(entry.Title))
                                {
                                    entryCol.Item().PaddingTop(2).Text(entry.Title).FontSize(16).Bold().FontColor(Colors.Grey.Darken3);
                                }

                                // Metadata (Moods, Category)
                                entryCol.Item().PaddingVertical(5).Row(row =>
                                {
                                    var moods = entryMoods.Where(em => em.JournalEntryId == entry.Id).ToList();
                                    if (moods.Any())
                                    {
                                        var moodNames = moods.Select(em =>
                                        {
                                            var mood = allMoods.FirstOrDefault(m => m.Id == em.MoodId);
                                            return mood != null ? $"{mood.Icon} {mood.Name}" : "";
                                        }).Where(s => !string.IsNullOrEmpty(s));

                                        row.RelativeItem().Text($"Mood: {string.Join(", ", moodNames)}").FontSize(9).Italic();
                                    }

                                    if (entry.CategoryId.HasValue)
                                    {
                                        var category = allCategories.FirstOrDefault(c => c.Id == entry.CategoryId);
                                        if (category != null)
                                        {
                                            row.RelativeItem().AlignRight().Text($"Category: {category.Name}").FontSize(9);
                                        }
                                    }
                                });

                                // Content 
                                
                                var plainText = System.Text.RegularExpressions.Regex.Replace(entry.Content, "<.*?>", string.Empty);
                                entryCol.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Text(plainText).FontSize(11);

                                // Tags
                                var tags = entryTags.Where(et => et.JournalEntryId == entry.Id).ToList();
                                if (tags.Any())
                                {
                                    var tagNames = tags.Select(et =>
                                    {
                                        var tag = allTags.FirstOrDefault(t => t.Id == et.TagId);
                                        return tag?.Name ?? "";
                                    }).Where(s => !string.IsNullOrEmpty(s));
                                    
                                    entryCol.Item().PaddingTop(5).Text($"Tags: {string.Join(", ", tagNames)}").FontSize(8).FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                    });

                    // Footer
                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            // Generate the PDF
            document.GeneratePdf(outputPath);

            return (true, "Export successful!", outputPath);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to export PDF: {ex.Message}", null);
        }
    }
}
