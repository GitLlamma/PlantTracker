namespace PlantTracker;

public static class Constants
{
#if DEBUG
    // Windows/desktop debug
    public const string ApiBaseUrl = "https://localhost:7036";
    // Android emulator — swap to this when running on Android:
    // public const string ApiBaseUrl = "https://10.0.2.2:7036";
#else
    // ⚠️ Replace this with your actual Azure App Service URL before building for release
    public const string ApiBaseUrl = "https://planttrackerapi-d8haeza3dve2e0cz.westcentralus-01.azurewebsites.net";
#endif

    public const string AuthTokenKey = "auth_token";
    public const string UserIdKey = "user_id";
    public const string UserEmailKey = "user_email";
    public const string UserDisplayNameKey = "user_display_name";
    public const string UserZipCodeKey = "user_zip_code";
}

