using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();

app.MapPost("/register", Register)
    .WithName("Register")
    .WithOpenApi();

app.MapPost("/sendMessage", SendMessage)
    .WithName("sendMessage")
    .WithOpenApi();

app.MapGet("/getAllMessages", LoadMessages);
app.MapGet("/getAllMessagesWithUser", LoadMessagesWithUser);

app.Run();


int Register([FromHeader] string username, [FromHeader] string password)
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

    int id = (int) cmd.ExecuteScalar()!;
    return 200;
}

int SendMessage([FromHeader] string username, [FromHeader] string password, [FromHeader] string targetUser, [FromHeader] string text)
{
    using var con = GetDbConnection();

    if (!VerifyLogin(username, password, con)) return 301;

    using var cmd = new NpgsqlCommand("INSERT INTO users.messages (sender, recipient, time, content) VALUES" +
                                      " (" +
                                      "(select id from users.users where name = $1)," +
                                      "(select id from users.users where name = $2)," +
                                      "$3, $4)", con);
    cmd.Parameters.Add(new() {Value = username});
    cmd.Parameters.Add(new() {Value = targetUser});
    cmd.Parameters.Add(new() {Value = DateTime.Now});
    cmd.Parameters.Add(new() {Value = text});
    cmd.ExecuteNonQuery();

    return 201;
}

Message[] LoadMessages([FromHeader] string username, [FromHeader] string password)
{
    using var con = GetDbConnection();
    if (!VerifyLogin(username, password, con)) return null!; // TODO

    using var cmd = new NpgsqlCommand("SELECT t.time,t.content, sender.name, recipient.name FROM users.messages t " +
                                      "JOIN users.users sender ON t.sender=sender.id " +
                                      "JOIN users.users recipient ON t.recipient=recipient.id " +
                                      "WHERE recipient=(SELECT id FROM users.users WHERE name=$1)", con);
    cmd.Parameters.Add(new() {Value = username});

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

    return messages.ToArray();
}

Message[] LoadMessagesWithUser([FromHeader] string username, [FromHeader] string password, [FromHeader] string othersUsername)
{
    using var con = GetDbConnection();
    if (!VerifyLogin(username, password, con)) return null!; // TODO

    using var cmd = new NpgsqlCommand(
        "SELECT t.time, t.content, sender.name, recipient.name " +
        "FROM users.messages t " +
        "JOIN users.users sender ON t.sender = sender.id " +
        "JOIN users.users recipient ON t.recipient = recipient.id " +
        "WHERE " +
        "(recipient = (SELECT id FROM users.users WHERE name = $2) AND sender = (SELECT id FROM users.users WHERE name = $1)) " +
        "OR (recipient = (SELECT id FROM users.users WHERE name = $1) AND sender = (SELECT id FROM users.users WHERE name = $2))",
        con
    );
    cmd.Parameters.Add(new() {Value = username});
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

    return messages.ToArray();
}

bool VerifyLogin(string username, string password, NpgsqlConnection con)
{
    using var cmd = new NpgsqlCommand("SELECT password FROM users.users WHERE name = $1", con);
    cmd.Parameters.Add(new() {Value = username});
    string savedPasswordHash = (string) cmd.ExecuteScalar()!;

    byte[] savedHashBytes = Convert.FromBase64String(savedPasswordHash);
    byte[] salt = new byte[16];
    Array.Copy(savedHashBytes, 0, salt, 0, 16);
    var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 50_000, HashAlgorithmName.SHA256);
    byte[] hashBytes = pbkdf2.GetBytes(20);

    byte[] savedHashWithoutSalt = new byte[20];
    Array.Copy(savedHashBytes, 16, savedHashWithoutSalt, 0, 20);

    return CryptographicOperations.FixedTimeEquals(hashBytes, savedHashWithoutSalt);
}


NpgsqlConnection GetDbConnection()
{
    NpgsqlConnection c = new(configuration.GetConnectionString("app"));
    c.Open();
    return c;
}

class Message
{
    public required string SenderName { get; set; }
    public required string RecipientName { get; set; }
    public required string Content { get; set; }
    public required DateTime Timestamp { get; set; }
}