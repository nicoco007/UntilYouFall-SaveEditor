// <copyright file="SaveFileEncryption.cs" company="Nicolas Gnyra">
// Until You Fall Save Editor - AES Key Extractor Plugin
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
using Il2CppSystem.Security.Cryptography;
using SG.Claymore.SaveSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using SaveEditor.Models;
using Newtonsoft.Json;

#if BEPINEX
using BepInEx;
using BepInEx.IL2CPP;
#endif

#if MELONLOADER
using MelonLoader;
#endif

#if MELONLOADER
[assembly: MelonGame("Schell Games", "UntilYouFall")]
[assembly: MelonInfo(typeof(AesKeyExtractorPlugin.Plugin), AesKeyExtractorPlugin.Plugin.kName, AesKeyExtractorPlugin.Plugin.kVersion, AesKeyExtractorPlugin.Plugin.kAuthor)]
#endif
namespace AesKeyExtractorPlugin
{
#if BEPINEX
    [BepInPlugin(kGuid, kName, kVersion)]
#endif
    public class Plugin
#if BEPINEX
        : BasePlugin
#elif MELONLOADER
        : MelonMod
#endif
    {
        public const string kName = "AES Key Extractor";
        public const string kGuid = "com.nicoco007.until-you-fall.aes-key-extractor";
        public const string kVersion = "1.0.0";
        public const string kAuthor = "nicoco007";

        private const byte kFileVersion = 1;
        private const string kFileName = "SaveEncryptionKeys.json";
        private const string kHexCharacters = "0123456789abcdef";

        private readonly UnityAction<Scene, LoadSceneMode> _onSceneLoadedHandler;

        private bool _exported = false;

        public Plugin()
        {
            _onSceneLoadedHandler = new Action<Scene, LoadSceneMode>(OnSceneLoaded);
        }

#if BEPINEX
        public override void Load()
#elif MELONLOADER
        public override void OnApplicationStart()
#endif
        {
            SceneManager.add_sceneLoaded(_onSceneLoadedHandler);
        }
#if BEPINEX
        public override bool Unload()
#elif MELONLOADER
        public override void OnApplicationQuit()
#endif
        {
            SceneManager.remove_sceneLoaded(_onSceneLoadedHandler);
#if BEPINEX
            return base.Unload();
#endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_exported)
            {
                return;
            }

            SaveManager saveManager = FindObjectOfType<SaveManager>();

            if (saveManager == null)
            {
                LogWarning($"{nameof(SaveManager)} not found!");
                return;
            }

            Aes aes = saveManager.saveEncryption.aes;
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), kFileName);
            UyfKeys keys = new UyfKeys(kFileVersion, ConvertToHexString(aes.Key), ConvertToHexString(aes.IV));

            File.WriteAllText(filePath, JsonConvert.SerializeObject(keys, Formatting.Indented));

            LogInfo($"Exported keys to '{filePath}'");

            _exported = true;
        }

        private T FindObjectOfType<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> objects = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());

            if (objects.Length == 0)
            {
                return null;
            }

            return objects[0].Cast<T>();
        }

        private string ConvertToHexString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];

                chars[i * 2] = kHexCharacters[(b >> 4) & 0xF];
                chars[i * 2 + 1] = kHexCharacters[b & 0xF];
            }

            return new string(chars);
        }

        private void LogInfo(object message)
        {
#if BEPINEX
            Log.LogInfo(message);
#elif MELONLOADER
            MelonLogger.Msg(message);
#endif
        }

        private void LogWarning(object message)
        {
#if BEPINEX
            Log.LogWarning(message);
#elif MELONLOADER
            MelonLogger.Warning(message);
#endif
        }
    }
}
