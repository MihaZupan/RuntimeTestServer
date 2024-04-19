// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using LettuceEncrypt;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using NetCoreServer.Helpers;
using System;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);

    options.ListenAnyIP(443, portOptions =>
    {
        portOptions.Use(async (connectionContext, next) =>
        {
            await TlsFilter.ProcessAsync(connectionContext, next);
        });

        portOptions.UseHttps(options =>
        {
            options.OnAuthenticate = (connectionContext, sslOptions) =>
            {
                if (connectionContext.Items.TryGetValue("TlsFilter.TargetHost", out object targetHostObj) &&
                    targetHostObj is string targetHost &&
                    targetHost.Contains("http11", StringComparison.OrdinalIgnoreCase))
                {
                    sslOptions.ApplicationProtocols.Remove(SslApplicationProtocol.Http2);
                }
            };

            options.UseLettuceEncrypt(portOptions.ApplicationServices);
        });
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin()));

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    DirectoryInfo certDir = new("/home/certs");
    certDir.Create();

    builder.Services.AddLettuceEncrypt()
        .PersistDataToDirectory(certDir, "certpass123");

    if (builder.Configuration["ApplicationInsights:ConnectionString"] is { Length: > 0 } appInsightsConnectionString)
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        });

        builder.Services.ConfigureTelemetryModule<EventCounterCollectionModule>((module, options) =>
        {
            foreach (var (eventSource, counters) in RuntimeEventCounters.EventCounters)
            {
                foreach (string counter in counters)
                {
                    module.Counters.Add(new EventCounterCollectionRequest(eventSource, counter));
                }
            }
        });
    }
}

var app = builder.Build();

app.UseCors();
app.UseWebSockets();
app.UseGenericHandler();

app.Run();