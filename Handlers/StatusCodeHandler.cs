// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer;

public sealed class StatusCodeHandler
{
    public static Task InvokeAsync(HttpContext context)
    {
        string statusCodeString = context.Request.Query["statuscode"];
        string statusDescription = context.Request.Query["statusdescription"];

        if (!int.TryParse(statusCodeString, out int statusCode))
        {
            context.Response.StatusCode = 400;
            context.SetStatusDescription("Error parsing statuscode: " + statusCodeString);
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;
        context.SetStatusDescription(string.IsNullOrWhiteSpace(statusDescription) ? " " : statusDescription);
        return Task.CompletedTask;
    }
}
