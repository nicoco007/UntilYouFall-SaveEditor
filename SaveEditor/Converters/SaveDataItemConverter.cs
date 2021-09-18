// <copyright file="SaveDataItemConverter.cs" company="Nicolas Gnyra">
// Until You Fall Save Editor
// Copyright © 2021  Nicolas Gnyra
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see &lt;https://www.gnu.org/licenses/&gt;.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SaveEditor.Models;

namespace SaveEditor.Converters
{
    internal class SaveDataItemConverter : JsonConverter<SaveDataItem>
    {
        public override SaveDataItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonElement element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            SaveDataItem item = new();

            foreach (JsonProperty property in element.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "Key":
                        item.Key = property.Value.GetString();
                        break;

                    case "Name":
                        item.Name = property.Value.GetString();
                        break;

                    case "Value":
                        if (property.Value.ValueKind != JsonValueKind.String)
                        {
                            throw new InvalidOperationException($"Unexpected value kind '{property.Value.ValueKind}'");
                        }

                        try
                        {
                            item.Value = JsonSerializer.Deserialize<JsonElement>(property.Value.GetString()!);
                        }
                        catch (JsonException)
                        {
                            item.Value = property.Value;
                        }

                        break;

                    default:
                        if (item.ExtraProperties == null)
                        {
                            item.ExtraProperties = new Dictionary<string, JsonElement>();
                        }

                        item.ExtraProperties.Add(property.Name, property.Value);

                        break;
                }
            }

            return item;
        }

        public override void Write(Utf8JsonWriter writer, SaveDataItem value, JsonSerializerOptions options)
        {
            Dictionary<string, object?> dict = new();

            dict.Add("Key", value.Key);

            if (value.Value.ValueKind == JsonValueKind.String)
            {
                dict.Add("Value", value.Value);
            }
            else
            {
                dict.Add("Value", JsonSerializer.Serialize(value.Value));
            }

            dict.Add("Name", value.Name);

            if (value.ExtraProperties != null)
            {
                foreach ((string key, JsonElement element) in value.ExtraProperties)
                {
                    dict.Add(key, element);
                }
            }

            JsonSerializer.Serialize(writer, dict, options);
        }
    }
}
