// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class VersionHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(GetVersionInfo());
        }

        private static string GetVersionInfo()
        {
            Type t = typeof(VersionHandler);
            string path = t.Assembly.Location;

            var buffer = new StringBuilder();
            buffer.AppendLine("Framework: " + RuntimeInformation.FrameworkDescription);
            buffer.AppendLine("Creation Date: " + File.GetCreationTime(path));
            buffer.AppendLine("Last Modified: " + File.GetLastWriteTime(path));

            return buffer.ToString();
        }        
    }
}
