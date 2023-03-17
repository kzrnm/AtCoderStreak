using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AtCoderStreak.TestUtil
{
    public class Logger : ILogger
    {
        private readonly List<(LogLevel level, string msg, EventId eventId, Exception exception)> list = new();
        public ReadOnlyCollection<(LogLevel level, string msg, EventId eventId, Exception exception)> Logs { get; }

        public Logger()
        {
            Logs = new ReadOnlyCollection<(LogLevel level, string msg, EventId eventId, Exception exception)>(list);
        }

        private class Disposable : IDisposable { public void Dispose() { } }
        IDisposable ILogger.BeginScope<TState>(TState state) => new Disposable();
        bool ILogger.IsEnabled(LogLevel logLevel) => true;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            list.Add((logLevel, msg, eventId, exception));
        }
    }
}
