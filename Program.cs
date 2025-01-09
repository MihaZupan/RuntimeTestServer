// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using LettuceEncrypt;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using NetCoreServer.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using Yarp.Telemetry.Consumption;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);

    if (OperatingSystem.IsLinux())
    {
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
    }
});

if (OperatingSystem.IsLinux())
{
    builder.Services.AddLettuceEncrypt();
}

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddRequestTimeouts(options =>
    options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromMinutes(1) });

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

builder.Services.AddMetricsConsumer<MetricsConsumer>();

var app = builder.Build();

app.UseRequestTimeouts();

app.UseCors();
app.UseWebSockets();
app.UseMiddleware<GenericHandler>();

app.Run();

sealed class MetricsConsumer : IMetricsConsumer<SocketsMetrics>
{
    private static readonly Stopwatch s_uptime = Stopwatch.StartNew();

    public static string LastStatus { get; private set; } = string.Empty;

    public void OnMetrics(SocketsMetrics previous, SocketsMetrics current)
    {
        long requests = GenericHandler.RequestCounter;
        long connections = current.IncomingConnectionsEstablished;

        string requestsString =
            requests > 1_000_000 ? $"{requests / 1_000_000d:N2} M" :
            requests > 1_000 ? $"{requests / 1_000d:N2} k" :
            requests.ToString();

        string connectionsString =
            connections > 1_000_000 ? $"{connections / 1_000_000d:N2} M" :
            connections > 1_000 ? $"{connections / 1_000d:N2} k" :
            connections.ToString();

        LastStatus =
            $"""
            [{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}]
            Uptime: {s_uptime.Elapsed}
            Requests: {requestsString}
            Received: {current.BytesReceived >> 20} MB
            Sent: {current.BytesSent >> 20} MB
            Incoming connections: {connectionsString}
            """;

        Console.WriteLine(LastStatus.ReplaceLineEndings(" "));
    }
}
