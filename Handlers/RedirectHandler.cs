// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class RedirectHandler
    {
        public static Task InvokeAsync(HttpContext context)
        {
            int statusCode = 302;
            string statusCodeString = context.Request.Query["statuscode"];
            if (!string.IsNullOrEmpty(statusCodeString))
            {
                try
                {
                    statusCode = int.Parse(statusCodeString);
                    if (statusCode < 300 || statusCode > 308)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.SetStatusDescription("Invalid redirect statuscode: " + statusCodeString);
                        return Task.CompletedTask;
                    }
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 400;
                    context.Response.SetStatusDescription("Error parsing statuscode: " + statusCodeString);
                    return Task.CompletedTask;
                }
            }

            string redirectUri = context.Request.Query["uri"];
            if (string.IsNullOrEmpty(redirectUri))
            {
                context.Response.StatusCode = 400;
                context.Response.SetStatusDescription("Missing redirection uri");
                return Task.CompletedTask;
            }

            string hopsString = context.Request.Query["hops"];
            int hops = 1;
            if (!string.IsNullOrEmpty(hopsString))
            {
                try
                {
                    hops = int.Parse(hopsString);
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 400;
                    context.Response.SetStatusDescription("Error parsing hops: " + hopsString);
                    return Task.CompletedTask;
                }
            }

            RequestHelper.AddResponseCookies(context);

            if (hops <= 1)
            {
                context.Response.Headers["Location"] = redirectUri;
            }
            else
            {
                context.Response.Headers["Location"] = string.Format("/Redirect.ashx?uri={0}&hops={1}",
                    redirectUri,
                    hops - 1);
            }

            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }
    }
}
