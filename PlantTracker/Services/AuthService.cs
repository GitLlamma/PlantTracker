using System.Net.Http.Json;
using PlantTracker.Shared.DTOs.Auth;

namespace PlantTracker.Services;

public class AuthService
{
    private readonly HttpClient _http;

    public AuthService(HttpClient http)
    {
        _http = http;
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(
        SecureStorage.Default.GetAsync(Constants.AuthTokenKey).GetAwaiter().GetResult());

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var token = await SecureStorage.Default.GetAsync(Constants.AuthTokenKey);
        if (string.IsNullOrEmpty(token)) return null;

        return new UserDto
        {
            Id = await SecureStorage.Default.GetAsync(Constants.UserIdKey) ?? string.Empty,
            Email = await SecureStorage.Default.GetAsync(Constants.UserEmailKey) ?? string.Empty,
            DisplayName = await SecureStorage.Default.GetAsync(Constants.UserDisplayNameKey) ?? string.Empty,
            ZipCode = await SecureStorage.Default.GetAsync(Constants.UserZipCodeKey) ?? string.Empty
        };
    }

    public async Task<string?> GetTokenAsync() =>
        await SecureStorage.Default.GetAsync(Constants.AuthTokenKey);

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequestDto
            {
                Email = dto.Email.Trim().ToLowerInvariant(),
                Password = dto.Password,
                DisplayName = dto.DisplayName,
                ZipCode = dto.ZipCode
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result is null) return (false, "Invalid response from server.");

            await SaveSessionAsync(result);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequestDto
            {
                Email = dto.Email.Trim().ToLowerInvariant(),
                Password = dto.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (false, $"Login failed ({(int)response.StatusCode}): {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result is null) return (false, "Invalid response from server.");

            await SaveSessionAsync(result);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        SecureStorage.Default.Remove(Constants.AuthTokenKey);
        SecureStorage.Default.Remove(Constants.UserIdKey);
        SecureStorage.Default.Remove(Constants.UserEmailKey);
        SecureStorage.Default.Remove(Constants.UserDisplayNameKey);
        SecureStorage.Default.Remove(Constants.UserZipCodeKey);
        await Task.CompletedTask;
    }

    private static async Task SaveSessionAsync(AuthResponseDto result)
    {
        await SecureStorage.Default.SetAsync(Constants.AuthTokenKey, result.Token);
        await SecureStorage.Default.SetAsync(Constants.UserIdKey, result.User.Id);
        await SecureStorage.Default.SetAsync(Constants.UserEmailKey, result.User.Email);
        await SecureStorage.Default.SetAsync(Constants.UserDisplayNameKey, result.User.DisplayName);
        await SecureStorage.Default.SetAsync(Constants.UserZipCodeKey, result.User.ZipCode);
    }
}

