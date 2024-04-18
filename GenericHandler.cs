// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NetCoreServer
{
    public class GenericHandler
    {
        // Must have constructor with this signature, otherwise exception at run time.
        public GenericHandler(RequestDelegate next)
        {
            // This catch all HTTP Handler, so no need to store next.
        }

        public async Task Invoke(HttpContext context)
        {
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
