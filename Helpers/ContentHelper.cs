﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace NetCoreServer;

public sealed class ContentHelper
{
    public static byte[] GetDeflateBytes(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        var compressedStream = new MemoryStream();

        using (var compressor = new DeflateStream(compressedStream, CompressionMode.Compress, true))
        {
            compressor.Write(bytes, 0, bytes.Length);
        }

        return compressedStream.ToArray();
    }

    public static byte[] GetGZipBytes(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        var compressedStream = new MemoryStream();

        using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress, true))
        {
            compressor.Write(bytes, 0, bytes.Length);
        }

        return compressedStream.ToArray();

    }

    public static byte[] ComputeMD5Hash(string data)
    {
        return ComputeMD5Hash(Encoding.UTF8.GetBytes(data));
    }

    public static byte[] ComputeMD5Hash(byte[] data)
    {
        return MD5.HashData(data);
    }
}
