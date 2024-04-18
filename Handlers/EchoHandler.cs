// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NetCoreServer
{
    public class EchoHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            RequestHelper.AddResponseCookies(context);

            if (context.Request.Method == "TRACE" &&
                context.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (!AuthenticationHelper.HandleAuthentication(context))
            {
                return;
            }

            // Add original request method verb as a custom response header.
            context.Response.Headers["X-HttpRequest-Method"] = context.Request.Method;

            // Echo back JSON encoded payload.
            RequestInformation info = await RequestInformation.CreateAsync(context.Request);
            string echoJson = info.SerializeToJson();

            byte[] bytes = Encoding.UTF8.GetBytes(echoJson);

            var delay = 0;
            if (context.Request.QueryString.HasValue)
            {
                if (context.Request.QueryString.Value.Contains("delay1sec"))
                {
                    delay = 1000;
                }
                else if (context.Request.QueryString.Value.Contains("delay10sec"))
                {
                    delay = 10000;
                }
            }

            if (delay > 0)
            {
                context.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();
            }

            context.Response.Headers.ContentMD5 = Convert.ToBase64String(MD5.HashData(bytes));
            context.Response.ContentType = "application/json";
            context.Response.ContentLength = bytes.Length;

            if (delay > 0)
            {
                await context.Response.StartAsync(context.RequestAborted);
                await context.Response.Body.WriteAsync(bytes.AsMemory(0, 10), context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
                await Task.Delay(delay, context.RequestAborted);
                await context.Response.Body.WriteAsync(bytes.AsMemory(10, bytes.Length - 10), context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
            else
            {
                await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
            }
        }
    }
}
