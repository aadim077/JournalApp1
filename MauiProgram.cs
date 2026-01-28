using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using JournalApp.Data.Context;
using JournalApp.Data.Repositories;
using JournalApp.Core.Interfaces;
using JournalApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace JournalApp.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Configure SQLite Database
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "journal.db");
        builder.Services.AddDbContext<JournalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Register MudBlazor
        builder.Services.AddMudServices();

        // Register Repositories
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register Services
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<StreakService>();
        builder.Services.AddScoped<JournalService>();
        builder.Services.AddScoped<AnalyticsService>();
        builder.Services.AddScoped<SearchService>();
        builder.Services.AddScoped<TagService>();
        builder.Services.AddScoped<ExportService>();

        var app = builder.Build();

        // Ensure database is created and matches model
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<JournalDbContext>();
                // For development: If you get a "no such column" error, 
                // you can uncomment the next line to reset the database.
                dbContext.Database.EnsureDeleted(); 
                
               
                
                dbContext.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database creation failed: {ex.Message}");
        }

        return app;
    }
}
