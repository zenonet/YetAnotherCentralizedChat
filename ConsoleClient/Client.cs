﻿using System.Text.Json;
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
}