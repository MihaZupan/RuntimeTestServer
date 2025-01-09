// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class DeflateHandler
{
    private const string ResponseBody = "Sending DEFLATE compressed";
    private static readonly byte[] s_deflateBytes = ContentHelper.GetDeflateBytes(ResponseBody);
    private static readonly string s_contentMD5 = Convert.ToBase64String(ContentHelper.ComputeMD5Hash(ResponseBody));

    public static async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.ContentMD5 = s_contentMD5;
        context.Response.Headers.ContentEncoding = "deflate";
        context.Response.ContentType = "text/plain";

        await context.Response.Body.WriteAsync(s_deflateBytes, context.RequestAborted);
    }
}
