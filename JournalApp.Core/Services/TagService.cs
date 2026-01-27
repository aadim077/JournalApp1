using JournalApp.Core.Entities;
using JournalApp.Core.Interfaces;

namespace JournalApp.Core.Services;

/// <summary>
/// Service for managing tags.
/// </summary>
public class TagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly AuthService _authService;

    public TagService(IRepository<Tag> tagRepository, AuthService authService)
    {
        _tagRepository = tagRepository;
        _authService = authService;
    }

    /// <summary>
    /// Gets all tags (pre-built and user's custom tags).
    /// </summary>
    public async Task<List<Tag>> GetAllTagsAsync()
    {
        if (_authService.CurrentUser == null)
            return new List<Tag>();

        var userId = _authService.CurrentUser.Id;

        // Get pre-built tags and user's custom tags
        var tags = await _tagRepository.FindAsync(
            t => !t.IsCustom || (t.IsCustom && t.UserId == userId));

        return tags.OrderBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Gets only pre-built tags.
    /// </summary>
    public async Task<List<Tag>> GetPreBuiltTagsAsync()
    {
        var tags = await _tagRepository.FindAsync(t => !t.IsCustom);
        return tags.OrderBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Gets user's custom tags.
    /// </summary>
    public async Task<List<Tag>> GetCustomTagsAsync()
    {
        if (_authService.CurrentUser == null)
            return new List<Tag>();

        var userId = _authService.CurrentUser.Id;
        var tags = await _tagRepository.FindAsync(t => t.IsCustom && t.UserId == userId);
        return tags.OrderBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Creates a custom tag for the current user.
    /// </summary>
    public async Task<(bool Success, string Message, Tag? Tag)> CreateCustomTagAsync(string tagName)
    {
        try
        {
            if (_authService.CurrentUser == null)
                return (false, "User not authenticated.", null);

            if (string.IsNullOrWhiteSpace(tagName))
                return (false, "Tag name cannot be empty.", null);

            var userId = _authService.CurrentUser.Id;

            // Check if tag already exists
            var existingTags = await _tagRepository.FindAsync(
                t => t.Name.ToLower() == tagName.ToLower() &&
                     (!t.IsCustom || t.UserId == userId));

            if (existingTags.Any())
                return (false, "A tag with this name already exists.", null);

            var tag = new Tag
            {
                Name = tagName,
                IsCustom = true,
                UserId = userId
            };

            await _tagRepository.AddAsync(tag);
            await _tagRepository.SaveChangesAsync();

            return (true, "Tag created successfully!", tag);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to create tag: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Deletes a custom tag.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteCustomTagAsync(int tagId)
    {
        try
        {
            if (_authService.CurrentUser == null)
                return (false, "User not authenticated.");

            var tag = await _tagRepository.GetByIdAsync(tagId);

            if (tag == null)
                return (false, "Tag not found.");

            if (!tag.IsCustom)
                return (false, "Cannot delete pre-built tags.");

            if (tag.UserId != _authService.CurrentUser.Id)
                return (false, "Unauthorized access.");

            await _tagRepository.DeleteAsync(tag);
            await _tagRepository.SaveChangesAsync();

            return (true, "Tag deleted successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete tag: {ex.Message}");
        }
    }
}
