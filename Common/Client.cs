using System.Net;
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

    public async Task Register(string username, string password)
    {
        HttpRequestMessage message = new(HttpMethod.Post, "/register");
        message.Headers.Add("username", username);
        message.Headers.Add("password", password);
        await httpClient.SendAsync(message);
    }
    
    public async Task<bool> Login(string username, string password)
    {
        HttpRequestMessage message = new(HttpMethod.Post, "/login");
        message.Headers.Add("username", username);
        message.Headers.Add("password", password);
        var response = await httpClient.SendAsync(message);
        
        if (!response.IsSuccessStatusCode) return false;
        token = await response.Content.ReadAsStringAsync();
        return true;
    }

    public async Task<Message[]?> GetAllMessagesWithUser(string username)
    {
        HttpRequestMessage message = new(HttpMethod.Get, "/getAllMessagesWithUser");
        message.Headers.Add("token", token);
        message.Headers.Add("othersUsername", username);
        var response = await httpClient.SendAsync(message);
        if (!response.IsSuccessStatusCode) return null;
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Message[]>(content, jsonSerializerOptions);
    }
    
    public async Task<bool> SendMessage(string recipientName, string text)
    {
        HttpRequestMessage message = new(HttpMethod.Post, "/sendMessage");
        message.Headers.Add("token", token);
        message.Headers.Add("targetUser", recipientName);
        message.Headers.Add("text", text);
        var response = await httpClient.SendAsync(message);
        return response.IsSuccessStatusCode;
    }

    public void StartLongPollingConnection(Action<Message> messageReceived, CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpRequestMessage message = new(HttpMethod.Get, "/longPolling");
                message.Headers.Add("token", token);
                var response = await httpClient.SendAsync(message, cancellationToken);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    messageReceived.Invoke(JsonSerializer.Deserialize<Message>(content, jsonSerializerOptions)!);
                }
            }
        }, cancellationToken);
    }
}