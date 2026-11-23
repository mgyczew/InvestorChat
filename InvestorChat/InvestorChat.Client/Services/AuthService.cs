using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace InvestorChat.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "investorchat.auth";
    private bool _initialized;

    public AuthService(HttpClient http, IJSRuntime jsRuntime)
    {
        _http = http;
        _jsRuntime = jsRuntime;
    }

    public string? Token { get; private set; }
    public string? UserName { get; private set; }
    public DateTimeOffset? TokenExpiresAtUtc { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && TokenExpiresAtUtc > DateTimeOffset.UtcNow;

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UserName = username,
            Password = password
        });

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        if (result is null)
        {
            throw new InvalidOperationException("Brak odpowiedzi z serwera.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }

        await ApplyTokenAsync(result.Token!, result.UserName!, result.ExpiresAtUtc ?? throw new InvalidOperationException("Brak czasu wygaśnięcia tokenu."));
        return result;
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = username,
            Email = email,
            Password = password
        });

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        if (result is null)
        {
            throw new InvalidOperationException("Brak odpowiedzi z serwera.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }

        await ApplyTokenAsync(result.Token!, result.UserName!, result.ExpiresAtUtc ?? throw new InvalidOperationException("Brak czasu wygaśnięcia tokenu."));
        return result;
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        SetToken(null, null, null);
    }

    public event Action? AuthStateChanged;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            var storedValue = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            var stored = string.IsNullOrWhiteSpace(storedValue)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<StoredAuth>(storedValue);
            if (stored is not null && !string.IsNullOrWhiteSpace(stored.Token) && !string.IsNullOrWhiteSpace(stored.UserName) && stored.ExpiresAtUtc > DateTimeOffset.UtcNow)
            {
                SetToken(stored.Token, stored.UserName, stored.ExpiresAtUtc);
            }
            else if (stored is not null)
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
            }
        }
        catch (System.Text.Json.JsonException)
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        }
        catch (JSException)
        {
        }
        finally
        {
            _initialized = true;
        }
    }

    private async Task ApplyTokenAsync(string token, string userName, DateTimeOffset expiresAtUtc)
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, System.Text.Json.JsonSerializer.Serialize(new StoredAuth(token, userName, expiresAtUtc)));
        SetToken(token, userName, expiresAtUtc);
    }

    private void SetToken(string? token, string? userName, DateTimeOffset? expiresAtUtc)
    {
        Token = token;
        UserName = userName;
        TokenExpiresAtUtc = expiresAtUtc;

        if (string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Notify subscribers (UI) that auth state changed
        // Wrap invocation to avoid exceptions in subscriber handlers bubbling out to JS runtime
        try
        {
            AuthStateChanged?.Invoke();
        }
        catch
        {
            // Swallow subscriber exceptions to keep client runtime stable.
            // If you need to log these, inject ILogger<AuthService> and log here.
        }
    }

    private sealed record StoredAuth(string Token, string UserName, DateTimeOffset ExpiresAtUtc);
}


public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? UserName { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
