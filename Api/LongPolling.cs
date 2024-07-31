using System.Collections.Concurrent;
using System.Xml.Linq;
using Common;

namespace Api;

public class LongPolling
{
    public static ConcurrentDictionary<int, CancellationTokenSource> Registrations = new();
    public static ConcurrentDictionary<int, Message?> MessagesToDeliver = new();

    public static void Register(int userId, CancellationTokenSource cancellationTokenRegistration)
    {
        Registrations[userId] = cancellationTokenRegistration;
    }

    public static async Task<bool> Wait(int id)
    {
        CancellationTokenSource source = new();
        Register(id, source);
        try
        {
            await Task.Delay(3_600_000, source.Token);
        }
        catch (TaskCanceledException e)
        {
            return true;
        }

        return false;
    }

    public static void OnMessageReceived(Message? message)
    {
        if (Registrations.TryGetValue(message.RecipientId, out CancellationTokenSource? cancellationTokenSource))
        {
            MessagesToDeliver[message.RecipientId] = message;
            cancellationTokenSource.Cancel();
            Registrations.Remove(message.RecipientId, out cancellationTokenSource);
        }
    }
}