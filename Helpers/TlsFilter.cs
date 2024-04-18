using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Threading.Tasks;
using System;
using Yarp.ReverseProxy.Utilities.Tls;
using System.Threading;

namespace NetCoreServer.Helpers;

internal static class TlsFilter
{
    public static async Task ProcessAsync(ConnectionContext connectionContext, Func<Task> next)
    {
        var input = connectionContext.Transport.Input;
        var minBytesExamined = 0L;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(connectionContext.ConnectionClosed);
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        while (true)
        {
            var result = await input.ReadAsync(cts.Token);
            var buffer = result.Buffer;

            if (result.IsCompleted)
            {
                return;
            }

            if (buffer.Length == 0)
            {
                continue;
            }

            if (!TryReadHello(buffer, out var targetHost))
            {
                minBytesExamined = buffer.Length;
                input.AdvanceTo(buffer.Start, buffer.End);
                continue;
            }

            connectionContext.Items["TlsFilter.TargetHost"] = targetHost;

            var examined = buffer.Slice(buffer.Start, minBytesExamined).End;
            input.AdvanceTo(buffer.Start, examined);
            break;
        }

        await next();
    }

    private static bool TryReadHello(ReadOnlySequence<byte> buffer, out string targetHost)
    {
        targetHost = null;

        if (buffer.Length > 2048)
        {
            throw new InvalidOperationException("Too much data");
        }

        ReadOnlySpan<byte> data = buffer.IsSingleSegment
            ? buffer.First.Span
            : buffer.ToArray();

        TlsFrameHelper.TlsFrameInfo info = default;
        if (!TlsFrameHelper.TryGetFrameInfo(data, ref info))
        {
            return false;
        }

        targetHost = info.TargetName;
        return true;
    }
}