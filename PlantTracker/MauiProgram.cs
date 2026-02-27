using Microsoft.Extensions.Logging;
using PlantTracker.Services;
using PlantTracker.ViewModels;
using PlantTracker.Views;

namespace PlantTracker;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── HTTP client ──────────────────────────────────────────────────────
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(Constants.ApiBaseUrl)
        });

        // ── Services ─────────────────────────────────────────────────────────
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<GardenService>();
        builder.Services.AddSingleton<PlantService>();

        // ── ViewModels ───────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<SearchViewModel>();
        builder.Services.AddTransient<PlantDetailViewModel>();
        builder.Services.AddTransient<MyGardenViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<SearchPage>();
        builder.Services.AddTransient<PlantDetailPage>();
        builder.Services.AddTransient<MyGardenPage>();
        builder.Services.AddTransient<RemindersPage>();
        builder.Services.AddTransient<SettingsPage>();

        // ── Shell & App ──────────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}