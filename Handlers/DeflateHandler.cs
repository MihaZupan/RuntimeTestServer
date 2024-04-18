// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class DeflateHandler
    {
        private const string ResponseBody = "Sending DEFLATE compressed";
        private static readonly byte[] DeflateBytes = ContentHelper.GetDeflateBytes(ResponseBody);
        private static readonly string ContentMD5 = Convert.ToBase64String(ContentHelper.ComputeMD5Hash(ResponseBody));

        public static async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers.ContentMD5 = ContentMD5;
            context.Response.Headers.ContentEncoding = "deflate";
            context.Response.ContentType = "text/plain";

            await context.Response.Body.WriteAsync(DeflateBytes);
        }
    }
}
