namespace PlantTracker.Services;

/// <summary>
/// A delegating handler that intercepts 401 Unauthorized responses.
/// When the server rejects an expired JWT, this clears the local session
/// and redirects the user to the login page automatically.
/// </summary>
public class SessionExpiredHandler : DelegatingHandler
{
    private readonly AuthService _auth;
    private bool _isHandling; // prevent re-entrant redirects

    public SessionExpiredHandler(AuthService auth)
    {
        _auth = auth;
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
                await _auth.LogoutAsync();

                // CS4014 suppressed: async void lambda is safe here because all exceptions are caught inside
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
                    catch { /* Shell may not be ready; navigation will fall through to login on next action */ }
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



