using System.Collections.Generic;

namespace NetCoreServer.Helpers;

public static class RuntimeEventCounters
{
    private static readonly string[] _systemRuntime = new[]
    {
        "cpu-usage",
        "working-set",
        "gc-heap-size",
        "gen-0-gc-count",
        "gen-1-gc-count",
        "gen-2-gc-count",
        "threadpool-thread-count",
        "monitor-lock-contention-count",
        "threadpool-queue-length",
        "threadpool-completed-items-count",
        "alloc-rate",
        "active-timer-count",
        "gc-fragmentation",
        "gc-committed",
        "exception-count",
        "time-in-gc",
        "gen-0-size",
        "gen-1-size",
        "gen-2-size",
        "loh-size",
        "poh-size",
    };

    private static readonly string[] _aspNetCoreHttpConnections = new[]
    {
        "connections-started",
        "connections-stopped",
        "connections-timed-out",
        "current-connections",
        "connections-duration",
    };

    private static readonly string[] _aspNetCoreKestrel = new[]
    {
        "connections-per-second",
        "total-connections",
        "tls-handshakes-per-second",
        "total-tls-handshakes",
        "current-tls-handshakes",
        "failed-tls-handshakes",
        "current-connections",
        "connection-queue-length",
        "request-queue-length",
        "current-upgraded-requests",
    };

    private static readonly string[] _systemNetSecurity = new[]
    {
        "total-tls-handshakes",
        "current-tls-handshakes",
        "failed-tls-handshakes",
        "all-tls-sessions-open",
    };

    private static readonly string[] _systemNetSockets = new[]
    {
        "outgoing-connections-established",
        "incoming-connections-established",
        "bytes-received",
        "bytes-sent",
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> EventCounters { get; } = new Dictionary<string, IReadOnlyList<string>>
    {
        { "System.Runtime",                         _systemRuntime.AsReadOnly() },
        { "Microsoft.AspNetCore.Http.Connections",  _aspNetCoreHttpConnections.AsReadOnly() },
        { "Microsoft-AspNetCore-Server-Kestrel",    _aspNetCoreKestrel.AsReadOnly() },
        { "System.Net.Security",                    _systemNetSecurity.AsReadOnly() },
        { "System.Net.Sockets",                     _systemNetSockets.AsReadOnly() },
    }.AsReadOnly();
}
