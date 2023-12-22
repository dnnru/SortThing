#region

using System;
using System.Collections.Concurrent;
using System.Timers;

#endregion

namespace SortThing.Utilities;

public static class Debouncer
{
    private static readonly ConcurrentDictionary<object, Timer> Timers = new ConcurrentDictionary<object, Timer>();

    public static void Debounce(object key, TimeSpan wait, Action action)
    {
        if (Timers.TryRemove(key, out var timer))
        {
            timer.Stop();
            timer.Dispose();
        }

        timer = new Timer(wait.TotalMilliseconds)
        {
            AutoReset = false
        };

        timer.Elapsed += (s, e) => action();
        Timers.TryAdd(key, timer);
        timer.Start();
    }
}