using System.Collections.Concurrent;

namespace Api;

public class LongPolling
{
    public static ConcurrentDictionary<int, CancellationTokenSource> Registrations = new();

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

    public static void OnMessageReceived(int recipientId)
    {
        if (Registrations.TryGetValue(recipientId, out CancellationTokenSource? cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }
    }
}