// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NetCoreServer;

public sealed class GenericHandler
{
    public static long RequestCounter;

    public GenericHandler(RequestDelegate next) { }

    public async Task Invoke(HttpContext context)
    {
        Interlocked.Increment(ref RequestCounter);

        context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 8 * 1024 * 1024; // 8 MB

        string path = context.Request.Path.Value?.ToLowerInvariant();

        await (path switch
        {
            "/deflate.ashx" => DeflateHandler.InvokeAsync(context),
            "/emptycontent.ashx" => Task.CompletedTask,
            "/gzip.ashx" => GZipHandler.InvokeAsync(context),
            "/redirect.ashx" => RedirectHandler.InvokeAsync(context),
            "/statuscode.ashx" => StatusCodeHandler.InvokeAsync(context),
            "/verifyupload.ashx" => VerifyUploadHandler.InvokeAsync(context),
            "/version" => VersionHandler.InvokeAsync(context),
            "/websocket/echowebsocket.ashx" => EchoWebSocketHandler.InvokeAsync(context),
            "/websocket/echowebsocketheaders.ashx" => EchoWebSocketHeadersHandler.InvokeAsync(context),
            "/test.ashx" => TestHandler.InvokeAsync(context),
            "/large.ashx" => LargeResponseHandler.InvokeAsync(context),
            "/echobody.ashx" => EchoBodyHandler.InvokeAsync(context),
            _ => EchoHandler.InvokeAsync(context)
        });
    }
}

public static class GenericHandlerExtensions
{
    public static void SetStatusDescription(this HttpContext context, string description)
    {
        context.Features.Get<IHttpResponseFeature>().ReasonPhrase = description;
    }
}
