// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class VersionHandler
{
    public static async Task InvokeAsync(HttpContext context)
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(GetVersionInfo(), context.RequestAborted);
    }

    private static string GetVersionInfo()
    {
        string path = typeof(VersionHandler).Assembly.Location;

        return
            $"""
            Framework: {RuntimeInformation.FrameworkDescription}
            Creation Date: {File.GetCreationTime(path)}
            Last Modified: {File.GetLastWriteTime(path)}
            """;
    }
}
