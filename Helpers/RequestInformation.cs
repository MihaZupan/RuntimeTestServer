// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace NetCoreServer;

public sealed class RequestInformation
{
    public string Method { get; private set; }

    public string Url { get; private set; }

    public NameValueCollection Headers { get; private set; }

    public NameValueCollection Cookies { get; private set; }

    public string BodyContent { get; private set; }

    public int BodyLength { get; private set; }

    public bool SecureConnection { get; private set; }

    public bool ClientCertificatePresent { get; private set; }

    public X509Certificate2 ClientCertificate { get; private set; }

    public static async Task<RequestInformation> CreateAsync(HttpRequest request)
    {
        var info = new RequestInformation
        {
            Method = request.Method,
            Url = request.Path + request.QueryString,
            Headers = []
        };

        foreach (KeyValuePair<string, StringValues> header in request.Headers)
        {
            info.Headers.Add(header.Key, header.Value.ToString());
        }

        var cookies = new NameValueCollection();
        CookieCollection cookieCollection = RequestHelper.GetRequestCookies(request);
        foreach (Cookie cookie in cookieCollection)
        {
            cookies.Add(cookie.Name, cookie.Value);
        }
        info.Cookies = cookies;

        string body = string.Empty;
        try
        {
            request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 64 * 1024 * 1024; // 64 MB

            Stream stream = request.Body;
            using (var reader = new StreamReader(stream))
            {
                body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);
            }
        }
        catch (Exception ex)
        {
            // We might want to log these exceptions also.
            body = ex.ToString();
        }
        finally
        {
            info.BodyContent = body;
            info.BodyLength = body.Length;
        }

        if (HttpProtocol.IsHttp2(request.Protocol) &&
            !request.Headers.ContainsKey(HeaderNames.ContentLength) &&
            !request.Headers.ContainsKey(HeaderNames.TransferEncoding))
        {
            info.Headers.Add(HeaderNames.TransferEncoding, "chunked");
        }

        info.SecureConnection = request.IsHttps;

        // FixMe: https://github.com/dotnet/runtime/issues/52693
        // info.ClientCertificate = request.ClientCertificate;

        return info;
    }

    private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
    {
        Converters = { new NameValueCollectionConverter() }
    };

    public string SerializeToJson()
    {
        return JsonConvert.SerializeObject(this, _jsonSerializerSettings);
    }

    private RequestInformation()
    {
    }
}
