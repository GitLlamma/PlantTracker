namespace PlantTracker.Services;

/// <summary>
/// A delegating handler that intercepts 401 Unauthorized responses.
/// When the server rejects an expired JWT, this clears the local session
/// and redirects the user to the login page automatically.
/// AuthService is resolved lazily to avoid a circular dependency:
///   AuthService → HttpClient → SessionExpiredHandler → AuthService
/// </summary>
public class SessionExpiredHandler : DelegatingHandler
{
    private readonly IServiceProvider _services;
    private bool _isHandling;

    public SessionExpiredHandler(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !_isHandling)
        {
            _isHandling = true;
            try
            {
                var auth = _services.GetRequiredService<AuthService>();
                await auth.LogoutAsync();

#pragma warning disable CS4014
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.DisplayAlertAsync(
                            "Session Expired",
                            "Your session has expired. Please sign in again.",
                            "OK");
                        await Shell.Current.GoToAsync("//Login");
                    }
                    catch { /* Shell may not be ready; user will be redirected on next action */ }
                });
#pragma warning restore CS4014
            }
            finally
            {
                _isHandling = false;
            }
        }

        return response;
    }
}



