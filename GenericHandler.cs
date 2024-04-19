// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NetCoreServer
{
    public class GenericHandler
    {
        private long _requestCount = 0;

        public GenericHandler(RequestDelegate next)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    Console.WriteLine($"Request count: {_requestCount}");
                    await Task.Delay(2000);
                }
            });
        }

        public async Task Invoke(HttpContext context)
        {
            Interlocked.Increment(ref _requestCount);

            string path = (context.Request.Path.Value ?? "").ToLowerInvariant();

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
        public static IApplicationBuilder UseGenericHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GenericHandler>();
        }

        public static void SetStatusDescription(this HttpResponse response, string description)
        {
            response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = description;
        }
    }
}
