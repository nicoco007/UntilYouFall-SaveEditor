// <copyright file="SaveFileEncryption.cs" company="Nicolas Gnyra">
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
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using SaveEditor.Converters;
using SaveEditor.Models;

namespace SaveEditor
{
    internal class SaveFileEncryption
    {
        private readonly byte[] key;
        private readonly byte[] iv;

        public SaveFileEncryption(string key, string iv)
        {
            this.key = Convert.FromHexString(key);
            this.iv = Convert.FromHexString(iv);
        }

        public async Task DecryptFile(SaveFile saveFile, string outputFilePath, bool deserializeValues)
        {
            byte[] data = Convert.FromBase64String(await File.ReadAllTextAsync(saveFile.FullName));
            object? deserialized;
            Type type;
            JsonSerializerOptions? options;

            if (deserializeValues)
            {
                type = typeof(SaveData);
                options = new JsonSerializerOptions { Converters = { new SaveDataItemConverter() } };
            }
            else
            {
                type = typeof(object);
                options = null;
            }

            using (MemoryStream inputStream = new(data))
            using (Aes aes = this.GetAes())
            using (ICryptoTransform dec = aes.CreateDecryptor())
            using (CryptoStream cryptoStream = new(inputStream, dec, CryptoStreamMode.Read))
            {
                deserialized = await JsonSerializer.DeserializeAsync(cryptoStream, type, options);
            }

            using FileStream fileStream = new(outputFilePath, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(fileStream, deserialized, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        }

        public async Task EncryptFile(string inputFilePath, SaveFile saveFile)
        {
            SaveData? saveData;

            using (FileStream inputStream = new(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                saveData = await JsonSerializer.DeserializeAsync<SaveData>(inputStream);
            }

            using MemoryStream memoryStream = new();

            using (Aes aes = this.GetAes())
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                await JsonSerializer.SerializeAsync(cryptoStream, saveData, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Converters = { new SaveDataItemConverter() } });
            }

            await File.WriteAllTextAsync(saveFile.FullName, Convert.ToBase64String(memoryStream.ToArray()));
        }

        private Aes GetAes()
        {
            Aes aes = Aes.Create();

            aes.Key = this.key;
            aes.IV = this.iv;

            return aes;
        }
    }
}
