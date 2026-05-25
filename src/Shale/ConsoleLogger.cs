using Microsoft.Extensions.Logging;

namespace Shale;

internal sealed class ConsoleLogger : ILogger {
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Console.WriteLine($"[PluginLoader:{logLevel}] {formatter(state, exception)}");

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}