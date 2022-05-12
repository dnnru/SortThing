#region

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

#endregion

namespace SortThing.Services
{
    public class FileLogger : ILogger
    {
        private static readonly ConcurrentQueue<string> LogQueue = new ConcurrentQueue<string>();
        private static readonly ConcurrentStack<string> ScopeStack = new ConcurrentStack<string>();
        private static readonly SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
        private readonly string _categoryName;
        private readonly Timer _sinkTimer = new Timer(5000) { AutoReset = false };

        public FileLogger(string categoryName)
        {
            _categoryName = categoryName;
            _sinkTimer.Elapsed += SinkTimer_Elapsed;
        }

        private string LogPath => Path.Combine(Path.GetTempPath(), "SortThing", $"LogFile_{DateTime.Now:yyyy-MM-dd}.log");

        public IDisposable BeginScope<TState>(TState state)
        {
            ScopeStack.Push(state.ToString());
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
#if DEBUG
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return true;
#endif
                case LogLevel.Information:
                case LogLevel.Warning:
                case LogLevel.Error:
                case LogLevel.Critical:
                    return true;
                case LogLevel.None:
                    break;
                default:
                    break;
            }

            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                var scopeStack = ScopeStack.Any() ? new[] { ScopeStack.FirstOrDefault(), ScopeStack.LastOrDefault() } : Array.Empty<string>();

                var message = FormatLogEntry(logLevel, _categoryName, state?.ToString(), exception, scopeStack);
                LogQueue.Enqueue(message);
                _sinkTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error queueing log entry: {ex.Message}");
            }
        }

        private async Task CheckLogFileExists()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? throw new InvalidOperationException());
            if (!File.Exists(LogPath))
            {
                File.Create(LogPath).Close();
                if (OperatingSystem.IsLinux())
                {
                    await (Process.Start("sudo", $"chmod 775 {LogPath}")?.WaitForExitAsync()!).ConfigureAwait(false);
                }
            }
        }

        private string FormatLogEntry(LogLevel logLevel, string categoryName, string state, Exception exception, string[] scopeStack)
        {
            var ex = exception;
            var exMessage = exception?.Message;

            while (ex?.InnerException is not null)
            {
                exMessage += $" | {ex.InnerException.Message}";
                ex = ex.InnerException;
            }

            return $"[{logLevel}]\t"
                 + $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}\t"
                 + (scopeStack.Any() ? $"[{string.Join(" - ", scopeStack)} - {categoryName}]\t" : $"[{categoryName}]\t")
                 + $"Message: {state}\t"
                 + $"Exception: {exMessage}{Environment.NewLine}";
        }

        private async void SinkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await WriteLock.WaitAsync().ConfigureAwait(false);

                await CheckLogFileExists().ConfigureAwait(false);

                var message = string.Empty;

                while (LogQueue.TryDequeue(out var entry))
                {
                    message += entry;
                }

                File.AppendAllText(LogPath, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log entry: {ex.Message}");
            }
            finally
            {
                WriteLock.Release();
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
                ScopeStack.TryPop(out _);
            }
        }
    }
}