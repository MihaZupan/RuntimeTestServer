// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace NetCoreServer;

public sealed class NameValueCollectionConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is not NameValueCollection collection)
        {
            return;
        }

        writer.Formatting = Formatting.Indented;
        writer.WriteStartObject();
        foreach (string key in collection.AllKeys)
        {
            writer.WritePropertyName(key);
            writer.WriteValue(collection.Get(key));
        }
        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var nameValueCollection = new NameValueCollection();
        var key = "";
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                nameValueCollection = new NameValueCollection();
            }
            if (reader.TokenType == JsonToken.EndObject)
            {
                return nameValueCollection;
            }
            if (reader.TokenType == JsonToken.PropertyName)
            {
                key = reader.Value.ToString();
            }
            if (reader.TokenType == JsonToken.String)
                nameValueCollection.Add(key, reader.Value.ToString());
        }
        return nameValueCollection;
    }

    public override bool CanConvert(Type objectType)
    {
        while (objectType is not null)
        {
            if (objectType == typeof(NameValueCollection))
            {
                return true;
            }

            objectType = objectType.BaseType;
        }

        return false;
    }
}
