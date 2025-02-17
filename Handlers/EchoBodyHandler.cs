// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class EchoBodyHandler
{
    public static async Task InvokeAsync(HttpContext context)
    {
        // Report back original request method verb.
        context.Response.Headers["X-HttpRequest-Method"] = context.Request.Method;

        // Report back original entity-body related request headers.
        string contentLength = context.Request.Headers["Content-Length"];
        if (!string.IsNullOrEmpty(contentLength))
        {
            context.Response.Headers["X-HttpRequest-Headers-ContentLength"] = contentLength;
        }

        string transferEncoding = context.Request.Headers.TransferEncoding;
        if (!string.IsNullOrEmpty(transferEncoding))
        {
            context.Response.Headers["X-HttpRequest-Headers-TransferEncoding"] = transferEncoding;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = context.Request.ContentType;
        await context.Request.Body.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
