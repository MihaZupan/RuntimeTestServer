// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class EchoWebSocketHandler
{
    private const int MaxDataLength = 16 * 1024 * 1024; // 16 MB
    private const int MaxBufferSize = 128 * 1024;

    public static async Task InvokeAsync(HttpContext context)
    {
        QueryString queryString = context.Request.QueryString;
        bool replyWithPartialMessages = queryString.HasValue && queryString.Value.Contains("replyWithPartialMessages");
        bool replyWithEnhancedCloseMessage = queryString.HasValue && queryString.Value.Contains("replyWithEnhancedCloseMessage");

        string subProtocol = context.Request.Query["subprotocol"];

        if (context.Request.QueryString.HasValue && context.Request.QueryString.Value.Contains("delay10sec"))
        {
            await Task.Delay(10000, context.RequestAborted);
        }
        else if (context.Request.QueryString.HasValue && context.Request.QueryString.Value.Contains("delay20sec"))
        {
            await Task.Delay(20000, context.RequestAborted);
        }

        try
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Not a websocket request");

                return;
            }

            WebSocket socket;
            if (!string.IsNullOrEmpty(subProtocol))
            {
                socket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
            }
            else
            {
                socket = await context.WebSockets.AcceptWebSocketAsync();
            }

            using (socket)
            {
                await ProcessWebSocketRequest(socket, replyWithPartialMessages, replyWithEnhancedCloseMessage, context.RequestAborted);
            }
        }
        catch (Exception)
        {
            // We might want to log these exceptions. But for now we ignore them.
        }
    }

    private static async Task ProcessWebSocketRequest(
        WebSocket socket,
        bool replyWithPartialMessages,
        bool replyWithEnhancedCloseMessage,
        CancellationToken cancellationToken)
    {
        var receiveBuffer = new byte[MaxBufferSize];
        var throwAwayBuffer = new byte[MaxBufferSize];

        int totalBytesRead = 0;

        // Stay in loop while websocket is open
        while (socket.State is WebSocketState.Open or WebSocketState.CloseSent)
        {
            WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
            totalBytesRead += receiveResult.Count;

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                if (receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.Empty, null, cancellationToken);
                }
                else
                {
                    WebSocketCloseStatus closeStatus = receiveResult.CloseStatus.GetValueOrDefault();
                    await socket.CloseAsync(
                        closeStatus,
                        replyWithEnhancedCloseMessage ?
                            ("Server received: " + (int)closeStatus + " " + receiveResult.CloseStatusDescription) :
                            receiveResult.CloseStatusDescription,
                        cancellationToken);
                }

                continue;
            }

            // Keep reading until we get an entire message.
            int offset = receiveResult.Count;
            while (totalBytesRead <= MaxDataLength && receiveResult.EndOfMessage == false)
            {
                if (offset < MaxBufferSize)
                {
                    receiveResult = await socket.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer, offset, MaxBufferSize - offset),
                        cancellationToken);
                }
                else
                {
                    receiveResult = await socket.ReceiveAsync(
                        new ArraySegment<byte>(throwAwayBuffer),
                        cancellationToken);
                }

                offset += receiveResult.Count;
                totalBytesRead += receiveResult.Count;
            }

            if (totalBytesRead > MaxDataLength)
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Exceeded length limit.", cancellationToken);
                return;
            }

            // Close socket if the message was too big.
            if (offset > MaxBufferSize)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.MessageTooBig,
                    $"{WebSocketCloseStatus.MessageTooBig}: {offset} > {MaxBufferSize}",
                    cancellationToken);

                continue;
            }

            bool sendMessage = false;
            string receivedMessage = null;
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, offset);
                if (receivedMessage == ".close")
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                }
                else if (receivedMessage == ".shutdown")
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                }
                else if (receivedMessage == ".abort")
                {
                    socket.Abort();
                }
                else if (receivedMessage == ".delay5sec")
                {
                    await Task.Delay(5000, cancellationToken);
                }
                else if (receivedMessage == ".receiveMessageAfterClose")
                {
                    byte[] buffer = Encoding.UTF8.GetBytes($"{receivedMessage} {DateTime.UtcNow:HH:mm:ss}");

                    await socket.SendAsync(
                        buffer.AsMemory(0, buffer.Length),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                }
                else if (socket.State == WebSocketState.Open)
                {
                    sendMessage = true;
                }
            }
            else
            {
                sendMessage = true;
            }

            if (sendMessage)
            {
                await socket.SendAsync(
                    receiveBuffer.AsMemory(0, offset),
                    receiveResult.MessageType,
                    !replyWithPartialMessages,
                    cancellationToken);
            }
            if (receivedMessage == ".closeafter")
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
            }
            else if (receivedMessage == ".shutdownafter")
            {
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
            }
        }
    }
}
