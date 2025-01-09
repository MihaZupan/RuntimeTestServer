// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class LargeResponseHandler
{
    public static async Task InvokeAsync(HttpContext context)
    {
        RequestHelper.AddResponseCookies(context);

        if (!AuthenticationHelper.HandleAuthentication(context))
        {
            return;
        }

        // Add original request method verb as a custom response header.
        context.Response.Headers["X-HttpRequest-Method"] = context.Request.Method;

        int size = 1024;
        if (context.Request.Query.TryGetValue("size", out var value))
        {
            size = int.Parse(value);
        }

        context.Response.ContentType = "application/octet-stream";
        context.Response.ContentLength = size;

        byte[] buffer = new byte[10 * 1024];
        Random.Shared.NextBytes(buffer);

        while (size > 0)
        {
            int toSend = Math.Min(size, buffer.Length);
            size -= toSend;
            await context.Response.Body.WriteAsync(buffer.AsMemory(0, toSend), context.RequestAborted);
        }
    }
}
