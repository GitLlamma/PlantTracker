namespace PlantTracker;

public static class Constants
{
#if DEBUG && WINDOWS
    // Windows desktop debug — points to local API
    public const string ApiBaseUrl = "https://localhost:7036";
#else
    // Android (debug or release) and all release builds use Azure
    public const string ApiBaseUrl = "https://planttrackerapi-d8haeza3dve2e0cz.westcentralus-01.azurewebsites.net";
#endif

    public const string AuthTokenKey = "auth_token";
    public const string UserIdKey = "user_id";
    public const string UserEmailKey = "user_email";
    public const string UserDisplayNameKey = "user_display_name";
    public const string UserZipCodeKey = "user_zip_code";
}

