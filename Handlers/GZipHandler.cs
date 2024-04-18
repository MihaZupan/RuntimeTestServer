// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class GZipHandler
    {
        private const string ResponseBody = "Sending GZIP compressed";
        private static readonly byte[] GZipBytes = ContentHelper.GetGZipBytes(ResponseBody);
        private static readonly string ContentMD5 = Convert.ToBase64String(ContentHelper.ComputeMD5Hash(ResponseBody));

        public static async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers.ContentMD5 = ContentMD5;
            context.Response.Headers.ContentEncoding = "gzip";
            context.Response.ContentType = "text/plain";

            await context.Response.Body.WriteAsync(GZipBytes);
        }
    }
}
