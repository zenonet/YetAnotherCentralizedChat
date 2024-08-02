using System.Buffers.Text;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Api;
using Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
var crypto = new RSACryptoServiceProvider();
crypto.ImportFromPem(File.ReadAllText("privateKey.pem"));

app.UseHttpsRedirection();

app.MapPost("/register", Register)
    .WithName("Register")
    .WithOpenApi();

app.MapPost("/login", Login)
    .WithName("Login")
    .Produces<TokenResult>()
    .WithOpenApi();

app.MapPost("/sendMessage", SendMessage)
    .WithName("sendMessage")
    .WithOpenApi();

app.MapGet("/getAllMessages", LoadMessages)
    .Produces<Message[]>();
app.MapGet("/getConversationPartners", GetConversationPartnerUsernames)
    .Produces<string[]>();
app.MapGet("/getAllMessagesWithUser", LoadMessagesWithUser)
    .Produces<Message[]>();

app.MapGet("/longPolling", LongPollForMessages)
    .Produces<Message>();

app.Run();
return;

IResult Register([FromHeader] string username, [FromHeader] string password)
{
    using var con = GetDbConnection();

    byte[] salt = new byte[16];
    RandomNumberGenerator.Fill(salt);

    var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 50_000, HashAlgorithmName.SHA256);
    byte[] hash = pbkdf2.GetBytes(20);
    byte[] hashBytes = new byte[36];
    Array.Copy(salt, 0, hashBytes, 0, 16);
    Array.Copy(hash, 0, hashBytes, 16, 20);
    string passwordHash = Convert.ToBase64String(hashBytes);
    
    using var cmd = new NpgsqlCommand("INSERT INTO users.users (name, password) VALUES ($1, $2) RETURNING id", con);
    cmd.Parameters.Add(new() {Value = username});
    cmd.Parameters.Add(new() {Value = passwordHash});

    try
    {
        int id = (int) cmd.ExecuteScalar()!;
    }
    catch (PostgresException)
    {
        return Results.Conflict();
    }

    return Results.Created();
}

IResult SendMessage([FromHeader] string token, [FromHeader] string targetUser, [FromHeader] string text)
{
    using var con = GetDbConnection();

    User? user = VerifyToken(token);
    if (user == null) return Results.Unauthorized();
    DateTime time = DateTime.Now;

    using var cmd = new NpgsqlCommand("INSERT INTO users.messages (sender, recipient, time, content) VALUES " +
                                      "(" +
                                      "$1," +
                                      "(select id from users.users where name = $2)," +
                                      "$3, $4) RETURNING recipient", con);
    cmd.Parameters.Add(new() {Value = user.Id});
    cmd.Parameters.Add(new() {Value = targetUser});
    cmd.Parameters.Add(new() {Value = time});
    cmd.Parameters.Add(new() {Value = text});
    int recipientId = (int) cmd.ExecuteScalar()!;

    LongPolling.OnMessageReceived(new()
    {
        Content = text,
        RecipientId = recipientId,
        Timestamp = time,
        SenderId = user.Id,
        SenderName = user.Name,
        RecipientName = targetUser,
    });
    return Results.Created();
}

IResult LoadMessages([FromHeader] string token)
{
    using var con = GetDbConnection();

    User? user = VerifyToken(token);
    if (user == null) return Results.Unauthorized();

    using var cmd = new NpgsqlCommand("SELECT t.time,t.content, t.recipient, t.sender, sender.name, recipient.name FROM users.messages t " +
                                      "JOIN users.users sender ON t.sender=sender.id " +
                                      "JOIN users.users recipient ON t.recipient=recipient.id " +
                                      "WHERE recipient=$1", con);
    cmd.Parameters.Add(new() {Value = user.Id});

    List<Message> messages = new();
    var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        messages.Add(new()
        {
            Content = (string) reader["content"],
            Timestamp = (DateTime) reader["time"],
            SenderName = reader.GetString(2),
            RecipientName = reader.GetString(3),
            RecipientId = (int) reader["recipient"],
            SenderId = (int) reader["sender"],
        });
    }

    return Results.Ok(messages.ToArray());
}

IResult LoadMessagesWithUser([FromHeader] string token, [FromHeader] string othersUsername)
{
    using var con = GetDbConnection();
    User? user = VerifyToken(token);
    if (user == null) return Results.Unauthorized();

    using var cmd = new NpgsqlCommand(
        "SELECT t.time, t.content, sender.name, recipient.name " +
        "FROM users.messages t " +
        "JOIN users.users sender ON t.sender = sender.id " +
        "JOIN users.users recipient ON t.recipient = recipient.id " +
        "WHERE " +
        "(recipient = (SELECT id FROM users.users WHERE name = $2) AND sender = $1 " +
        "OR (recipient = $1 AND sender = (SELECT id FROM users.users WHERE name = $2)))",
        con
    );
    cmd.Parameters.Add(new() {Value = user.Id});
    cmd.Parameters.Add(new() {Value = othersUsername});

    List<Message> messages = new();
    var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        messages.Add(new()
        {
            Content = (string) reader["content"],
            Timestamp = (DateTime) reader["time"],
            SenderName = reader.GetString(2),
            RecipientName = reader.GetString(3),
        });
    }

    return Results.Ok(messages.ToArray());
}

IResult GetConversationPartnerUsernames([FromHeader] string token)
{
    using var con = GetDbConnection();
    User? user = VerifyToken(token);
    if (user == null) return Results.Unauthorized();

    using var cmd = new NpgsqlCommand(
        "SELECT " +
        "CASE " +
        "WHEN sender=$1 THEN recipient.name " +
        "ELSE sender.name " +
        "END AS username, " +
        "CASE " +
        "WHEN sender=$1 THEN recipient.id " +
        "ELSE sender.id " +
        "END AS oid " +
        "FROM " +
        "users.messages " +
        "JOIN " +
        "users.users sender ON messages.sender = sender.id " +
        "JOIN " +
        "users.users recipient ON messages.recipient = recipient.id " +
        "WHERE " +
        "sender=$1 OR " +
        "recipient=$1 " +
        "GROUP BY " +
        "username, " +
        "oid;",
        con
    );
    cmd.Parameters.Add(new() {Value = user.Id});

    List<string> conversationPartners = new();
    var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        conversationPartners.Add((string) reader["username"]);
    }

    return Results.Ok(conversationPartners.ToArray());
}

IResult Login([FromHeader] string username, [FromHeader] string password)
{
    TokenResult token = VerifyUserAndCreateToken(username, password);
    if(token != null)
        return Results.Ok(token);
    
    return Results.Unauthorized();
}

async Task<IResult> LongPollForMessages([FromHeader] string token)
{
    User? user = VerifyToken(token);
    if (user == null) return Results.Unauthorized();

    bool hasNewMessage = await LongPolling.Wait(user.Id);
    if (hasNewMessage)
    {
        LongPolling.MessagesToDeliver.Remove(user.Id, out Message? msg);
        return Results.Json(msg);
    }
    return Results.NoContent();
}

User? VerifyToken(string token)
{
    string unencryptedToken = Encoding.UTF8.GetString(
        crypto.Decrypt(
            Convert.FromBase64String(token),
            RSAEncryptionPadding.Pkcs1
        )
    );

    // Check if the token is made by this private key by checking a constant string (I should probably use cryptographic signing for this)
    string[] sections = unencryptedToken.Split(';');
    if (sections[0] != "iwouldnottrustmyencryption") return null;

    // Check if the token is expired
    var expirationDate = DateTime.FromBinary(long.Parse(sections[3]));
    if (expirationDate < DateTime.Now) return null;

    return new()
    {
        Name = sections[1],
        Id = int.Parse(sections[2]),
    };
}

TokenResult? VerifyUserAndCreateToken(string username, string password)
{
    // Auth
    using var con = GetDbConnection();
    using var cmd = new NpgsqlCommand("SELECT password, id FROM users.users WHERE name = $1", con);
    cmd.Parameters.Add(new() {Value = username});

    using NpgsqlDataReader reader = cmd.ExecuteReader();

    if (!reader.Read()) return null;

    int userId = reader.GetInt32(1);

    string savedPasswordHash = reader.GetString(0);
    byte[] savedHashBytes = Convert.FromBase64String(savedPasswordHash);
    byte[] salt = new byte[16];
    Array.Copy(savedHashBytes, 0, salt, 0, 16);
    var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 50_000, HashAlgorithmName.SHA256);
    byte[] hashBytes = pbkdf2.GetBytes(20);

    byte[] savedHashWithoutSalt = new byte[20];
    Array.Copy(savedHashBytes, 16, savedHashWithoutSalt, 0, 20);

    if (!CryptographicOperations.FixedTimeEquals(hashBytes, savedHashWithoutSalt))
        return null;

    // Token creation
    DateTime expirationDate = DateTime.Now.AddHours(4);
    
    // Also add some randomness so that tokens aren't determined only by changing data, clients have as well (does that make sense?)
    string unencryptedToken = $"iwouldnottrustmyencryption;{username};{userId};{expirationDate.ToBinary()};{Random.Shared.NextSingle()}";

    byte[] token = crypto.Encrypt(Encoding.UTF8.GetBytes(unencryptedToken), RSAEncryptionPadding.Pkcs1);
    return new()
    {
        Token = Convert.ToBase64String(token),
        ExpirationDate = expirationDate,
    };
}

NpgsqlConnection GetDbConnection()
{
    NpgsqlConnection c = new(configuration.GetConnectionString("app"));
    c.Open();
    return c;
}

class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}