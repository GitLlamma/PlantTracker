using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
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
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Services ─────────────────────────────────────────────────────────
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<GardenService>();
        builder.Services.AddSingleton<PlantService>();
        builder.Services.AddSingleton<SessionExpiredHandler>();
        builder.Services.AddSingleton<NotificationService>();

        // ── HTTP client ──────────────────────────────────────────────────────
        builder.Services.AddSingleton(sp =>
        {
            var handler = sp.GetRequiredService<SessionExpiredHandler>();
            handler.InnerHandler = new HttpClientHandler();
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(Constants.ApiBaseUrl)
            };
        });

        // ── ViewModels ───────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<SearchViewModel>();
        builder.Services.AddSingleton<PlantDetailViewModel>();
        builder.Services.AddTransient<MyGardenViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AddCustomPlantViewModel>();
        builder.Services.AddTransient<EditPlantViewModel>();
        builder.Services.AddTransient<PlantDiseasesViewModel>();
        builder.Services.AddTransient<PlantGalleryViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<SearchPage>();
        builder.Services.AddSingleton<PlantDetailPage>();
        builder.Services.AddTransient<MyGardenPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AddCustomPlantPage>();
        builder.Services.AddTransient<EditPlantPage>();
        builder.Services.AddTransient<PlantDiseasesPage>();
        builder.Services.AddTransient<PlantGalleryPage>();

        // ── Shell & App ──────────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}