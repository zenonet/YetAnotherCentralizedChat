using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Common;

namespace ConsoleClient;

public class Client(string ip)
{
    private HttpClient httpClient = new()
    {
        BaseAddress = new(ip),
    };

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private string? token;
    public string Username { get; private set; }

    public async Task Register(string username, string password)
    {
        HttpRequestMessage message = new(HttpMethod.Post, "register");
        message.Headers.Add("username", username);
        message.Headers.Add("password", password);
        Username = username;
        HttpResponseMessage response = await httpClient.SendAsync(message);
        await MaybeThrowExceptionFromResponse(response);
    }

    public async Task Login(string username, string password)
    {
        HttpRequestMessage message = new(HttpMethod.Post, "login");
        message.Headers.Add("username", username);
        message.Headers.Add("password", password);
        var response = await httpClient.SendAsync(message);
        await MaybeThrowExceptionFromResponse(response);

        token = (await response.Content.ReadFromJsonAsync<TokenResult>(jsonSerializerOptions))!.Token;
        Username = username;
    }

    public async Task<Message[]?> GetAllMessagesWithUser(string username)
    {
        HttpRequestMessage message = new(HttpMethod.Get, "getAllMessagesWithUser");
        message.Headers.Add("token", token);
        message.Headers.Add("othersUsername", username);
        var response = await httpClient.SendAsync(message);
        await MaybeThrowExceptionFromResponse(response);

        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<Message[]>(jsonSerializerOptions);
    }

    public async Task SendMessage(string recipientName, string text)
    {
        HttpRequestMessage message = new(HttpMethod.Post, "sendMessage");
        message.Headers.Add("token", token);
        message.Headers.Add("targetUser", recipientName);
        message.Headers.Add("text", text);
        var response = await httpClient.SendAsync(message);
        await MaybeThrowExceptionFromResponse(response);
    }

    public async Task<string[]?> GetConversationPartners()
    {
        HttpRequestMessage message = new(HttpMethod.Get, "getConversationPartners");
        message.Headers.Add("token", token);
        var response = await httpClient.SendAsync(message);

        await MaybeThrowExceptionFromResponse(response);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<string[]>(jsonSerializerOptions);
    }

    public void StartLongPollingConnection(Action<Message> messageReceived, CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpRequestMessage message = new(HttpMethod.Get, "longPolling");
                message.Headers.Add("token", token);
                var response = await httpClient.SendAsync(message, cancellationToken);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Message? msg = await response.Content.ReadFromJsonAsync<Message>(jsonSerializerOptions, cancellationToken);
                    if (msg == null) continue;
                    messageReceived.Invoke(msg);
                }
            }
        }, cancellationToken);
    }

    private async Task MaybeThrowExceptionFromResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized) throw new YaccException("Username or password is incorrect.");
        throw new YaccException((await response.Content.ReadAsStringAsync()).Trim('"'));
    }
}

public class YaccException(string msg) : Exception(msg);