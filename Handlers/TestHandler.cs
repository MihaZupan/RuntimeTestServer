// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class TestHandler
{
    public static async Task InvokeAsync(HttpContext context)
    {
        RequestInformation info = await RequestInformation.CreateAsync(context.Request);
        string echoJson = info.SerializeToJson();

        context.Response.Headers.ContentMD5 = Convert.ToBase64String(ContentHelper.ComputeMD5Hash(echoJson));
        context.Response.ContentType = "text/plain";

        await context.Response.WriteAsync(echoJson);
    }
}
