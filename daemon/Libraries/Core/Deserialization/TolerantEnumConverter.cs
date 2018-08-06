/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System;
using System.Linq;
using Newtonsoft.Json;

namespace Spectero.daemon.Libraries.Core.Deserialization
{
    class TolerantEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var type = IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType;
            return type.IsEnum;
        }
    
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var isNullable = IsNullableType(objectType);
            var enumType = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
    
            var names = Enum.GetNames(enumType);
    
            if (reader.TokenType == JsonToken.String)
            {
                var enumText = reader.Value.ToString();
    
                if (!string.IsNullOrEmpty(enumText))
                {
                    var match = names
                        .Where(n => string.Equals(n, enumText, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
    
                    if (match != null)
                    {
                        return Enum.Parse(enumType, match);
                    }
                }
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                var enumVal = Convert.ToInt32(reader.Value);
                var values = (int[])Enum.GetValues(enumType);
                if (values.Contains(enumVal))
                {
                    return Enum.Parse(enumType, enumVal.ToString());
                }
            }
    
            if (!isNullable)
            {
                var defaultName = names
                    .Where(n => string.Equals(n, "Unknown", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
    
                if (defaultName == null)
                {
                    defaultName = names.First();
                }
    
                return Enum.Parse(enumType, defaultName);
            }
    
            return null;
        }
    
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    
        private bool IsNullableType(Type t)
        {
            return (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
    }
}