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

    using var cmd = new NpgsqlCommand("INSERT INTO users (name, password) VALUES ($1, $2) RETURNING id", con);
    cmd.Parameters.Add(new() {Value = username});
    cmd.Parameters.Add(new() {Value = passwordHash});

    int id = (int)cmd.ExecuteScalar()!;
    return 200;
}

int SendMessage([FromHeader] string username, [FromHeader] string password, [FromHeader] string targetUser, [FromHeader] string text)
{
    using var con = GetDbConnection();

    if (!VerifyLogin(username, password, con)) return 301;
    
    using var cmd = new NpgsqlCommand("INSERT INTO messages (sender, recipient, time, content)", con);
    cmd.Parameters.Add(new() {Value = username});
    cmd.Parameters.Add(new() {Value = username});
    cmd.Parameters.Add(new() {Value = DateTime.Now});
    cmd.Parameters.Add(new() {Value = text});
    cmd.ExecuteNonQuery();

    return 201;
}

bool VerifyLogin(string username, string password, NpgsqlConnection con)
{
    using var cmd = new NpgsqlCommand("SELECT password FROM users WHERE name = $1", con);
    cmd.Parameters.Add(new() {Value = username});
    string savedPasswordHash = (string) cmd.ExecuteScalar()!;
    
    byte[] savedHashBytes = Convert.FromBase64String(savedPasswordHash);
    byte[] salt = new byte[16];
    Array.Copy(savedHashBytes, 0, salt, 0, 16);
    var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 50_000, HashAlgorithmName.MD5);
    byte[] hashBytes = pbkdf2.GetBytes(20);

    return CryptographicOperations.FixedTimeEquals(hashBytes, savedHashBytes);
}


NpgsqlConnection GetDbConnection()
{
    NpgsqlConnection c = new(configuration.GetConnectionString("app"));
    c.Open();
    return c;
}