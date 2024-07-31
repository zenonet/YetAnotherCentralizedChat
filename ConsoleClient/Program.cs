
using Common;
using ConsoleClient;

Console.WriteLine("Welcome to the official YAC Chat console client");
Console.WriteLine("Please connect to a server");
Console.Write("IP: ");
string? ip = Console.ReadLine();


/*
if (ip == null || !Uri.CheckSchemeName(ip))
{
    Console.WriteLine("That not a valid ip!");
    return;
}*/

Client client = new(ip);

Console.WriteLine("Login");
Console.Write("Username: ");
string username = Console.ReadLine()!;
Console.Write("Password: ");
string password = Console.ReadLine()!;

bool isLoggedIn = await client.Login(username, password);
if(isLoggedIn)
    Console.WriteLine("You're now logged in!");
else
{
    Console.WriteLine("Uh, that didn't work...");
    return;
}

Console.WriteLine("Open a chat to another user: ");
string? name = Console.ReadLine();

// Load messages
Message[]? messages = await client.GetAllMessagesWithUser(name!);
if (messages == null)
{
    Console.WriteLine($"Unable to load messages with {name}");
    return;
}
Console.Clear();
Console.WriteLine(name + "\n");
foreach (Message m in messages)
{
    Console.WriteLine($"{m.SenderName}: {m.Content}");
}
