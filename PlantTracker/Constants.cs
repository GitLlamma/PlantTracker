namespace PlantTracker;

public static class Constants
{
#if DEBUG
    // Android emulator uses 10.0.2.2 to reach the host machine's localhost
    public const string ApiBaseUrl = "https://10.0.2.2:7036";
    // Windows/desktop debug — use this instead if running on Windows:
    // public const string ApiBaseUrl = "https://localhost:7036";
#else
    public const string ApiBaseUrl = "https://your-app.azurewebsites.net";
#endif

    public const string AuthTokenKey = "auth_token";
    public const string UserIdKey = "user_id";
    public const string UserEmailKey = "user_email";
    public const string UserDisplayNameKey = "user_display_name";
    public const string UserZipCodeKey = "user_zip_code";
}

