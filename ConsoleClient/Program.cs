using Common;
using ConsoleClient;

Console.WriteLine("Welcome to the official YAC Chat console client");
//Console.WriteLine("Please connect to a server");
//Console.Write("IP: ");
//string? ip = Console.ReadLine();


/*
if (ip == null || !Uri.CheckSchemeName(ip))
{
    Console.WriteLine("That not a valid ip!");
    return;
}*/

Client client = new("http://localhost:5221");

Console.WriteLine("Login");
Console.Write("Username: ");
string username = Console.ReadLine()!;
Console.Write("Password: ");
string password = Console.ReadLine()!;

try
{
    await client.Login(username, password);
}
catch (YaccException ye)
{
    Console.WriteLine(ye.Message);
    return;
}

Console.WriteLine("You're now logged in!");

Console.WriteLine("Open a chat to another user: ");
string? name = Console.ReadLine();

// Load messages
Message[]? messages = await client.GetAllMessagesWithUser(name!);
if (messages == null)
{
    Console.WriteLine($"Unable to load messages with {name}");
    return;
}

string inputField = "";
Console.Clear();
Console.WriteLine(name + "\n");
foreach (Message m in messages)
{
    Console.WriteLine($"{m.SenderName}: {m.Content}");
}

client.StartLongPollingConnection(m =>
{
    if (m.SenderName != name) return;
    
    AddLineToHistory($"{m.SenderName}: {m.Content}");
}, CancellationToken.None);

while (true)
{

    inputField = Console.ReadLine()!;
    Console.CursorTop--;
    Console.CursorLeft = 0;
    Console.Write(new string(' ', Console.WindowWidth));
    Console.CursorTop--;
    await client.SendMessage(name!, inputField);
    AddLineToHistory($"{username}: {inputField}");
    /*
    ConsoleKeyInfo key = Console.ReadKey();
    
    if (key.Key == ConsoleKey.Backspace)
        inputField = inputField[..^1];
    else if (key.Key == ConsoleKey.Enter)
    {
        await client.SendMessage(name!, inputField);
        AddLineToHistory($"{username}: {inputField}");
        inputField = "";
    }
    else
    {
        inputField += key.KeyChar;
    }*/


    
    //Console.Write(new string('\b', Console.CursorLeft));
    //Console.Write("> " + inputField);
}

void AddLineToHistory(string line)
{
    Console.CursorLeft = 0;
    Console.WriteLine();
    Console.CursorTop--;
    Console.WriteLine(line);
    Console.Write("> " + inputField);
}
