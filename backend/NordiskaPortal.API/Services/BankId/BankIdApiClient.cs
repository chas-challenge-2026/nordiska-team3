using System.Net.Http.Json;
using System.Text.Json;

namespace NordiskaPortal.API.Services.BankId;

public sealed class BankIdApiClient : IBankIdApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public BankIdApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BankIdAuthResponse> AuthAsync(string endUserIp, CancellationToken cancellationToken)
    {
        using var response = await SendAsync("auth", new { endUserIp }, cancellationToken);
        return await ReadAsync<BankIdAuthResponse>(response, cancellationToken);
    }

    public async Task<BankIdCollectResponse> CollectAsync(string orderRef, CancellationToken cancellationToken)
    {
        using var response = await SendAsync("collect", new { orderRef }, cancellationToken);
        return await ReadAsync<BankIdCollectResponse>(response, cancellationToken);
    }

    public async Task CancelAsync(string orderRef, CancellationToken cancellationToken)
    {
        using var response = await SendAsync("cancel", new { orderRef }, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, object body, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new BankIdApiException("unreachable", ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BankIdApiException("timeout", "BankID did not answer in time.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var exception = await ToExceptionAsync(response, cancellationToken);
            response.Dispose();
            throw exception;
        }

        return response;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new BankIdApiException("emptyResponse", "BankID gave an empty answer.");
    }

    private static async Task<BankIdApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorCode = ((int)response.StatusCode).ToString();

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("errorCode", out var code) && code.GetString() is { } text)
            {
                errorCode = text;
            }
        }
        catch (JsonException)
        {
        }

        return new BankIdApiException(errorCode, $"BankID answered with {(int)response.StatusCode} ({errorCode}).");
    }
}