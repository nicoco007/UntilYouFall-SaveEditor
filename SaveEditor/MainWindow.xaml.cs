// <copyright file="MainWindow.xaml.cs" company="Nicolas Gnyra">
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ookii.Dialogs.Wpf;
using SaveEditor.Models;
using SaveEditor.ValidationRules;

namespace SaveEditor
{
    public partial class MainWindow : Window, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private static readonly string SettingsFilePath = Path.Join(Directory.GetCurrentDirectory(), "settings.json");

        private readonly ObservableCollection<SaveFile> saveFiles = new();
        private readonly Dictionary<string, object> errors = new();
        private readonly Dictionary<string, List<ValidationRule>> validationRules = new()
        {
            {
                nameof(PersistentDataPath),
                new List<ValidationRule>
                {
                    new RequiredValidationRule(),
                    new DirectoryExistenceValidationRule(),
                    new SaveDataDirectoryValidation(),
                }
            },
            {
                nameof(EncryptionKey),
                new List<ValidationRule>
                {
                    new StringLengthValidationRule(64),
                    new HexStringValidationRule(),
                }
            },
            {
                nameof(EncryptionIV),
                new List<ValidationRule>
                {
                    new StringLengthValidationRule(32),
                    new HexStringValidationRule(),
                }
            },
        };

        private readonly VistaSaveFileDialog exportFileDialog = new()
        {
            Filter = "JSON File|*.json",
            AddExtension = true,
            OverwritePrompt = true,
        };

        private readonly VistaOpenFileDialog importFileDialog = new()
        {
            Filter = "JSON File|*.json",
        };

        private Settings settings = new();

        public MainWindow()
        {
            this.SaveFiles = new ListCollectionView(this.saveFiles)
            {
                GroupDescriptions =
                {
                    new PropertyGroupDescription("ProfileName"),
                },
            };

            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public string? PersistentDataPath
        {
            get => this.settings.PersistentDataPath;
            set
            {
                this.settings.PersistentDataPath = value;

                if (this.ValidateProperty(value))
                {
                    this.LoadProfiles();
                }

                this.NotifyPropertyChanged();
            }
        }

        public string? EncryptionKey
        {
            get => this.settings.EncryptionKey;
            set
            {
                this.settings.EncryptionKey = value;
                this.ValidateProperty(value);
                this.NotifyPropertyChanged();
            }
        }

        public string? EncryptionIV
        {
            get => this.settings.EncryptionIV;
            set
            {
                this.settings.EncryptionIV = value;
                this.ValidateProperty(value);
                this.NotifyPropertyChanged();
            }
        }

        public bool DeserializeValues
        {
            get => this.settings.DeserializeValues;
            set
            {
                this.settings.DeserializeValues = value;
                this.NotifyPropertyChanged();
            }
        }

        public ICollectionView SaveFiles { get; }

        public SaveFile? SelectedFile { get; set; }

        public bool HasErrors => this.errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                if (this.errors.TryGetValue(propertyName, out object? error))
                {
                    return new[] { error };
                }
                else
                {
                    return Array.Empty<object>();
                }
            }
            else
            {
                return this.errors.Values;
            }
        }

        private static string GetPersistentDataPath()
        {
            if (NativeMethods.SHGetKnownFolderPath(NativeMethods.LocalAppDataLow, 0, IntPtr.Zero, out string path) == 0)
            {
                return Path.Join(path, "Schell Games", "UntilYouFall");
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get location of LocalLow AppData");
            }
        }

        private static Settings GetDefaultSettings()
        {
            string? persistentDataPath = null;

            try
            {
                persistentDataPath = GetPersistentDataPath();
            }
            catch (Exception)
            {
                MessageBox.Show($"Could not get data folder path. Please enter it manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return new Settings
            {
                PersistentDataPath = persistentDataPath,
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await this.LoadSettings();
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            await this.SaveSettings();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new();

            if (!string.IsNullOrWhiteSpace(this.PersistentDataPath) && Directory.Exists(this.PersistentDataPath))
            {
                folderBrowserDialog.SelectedPath = this.PersistentDataPath;
            }
            else
            {
                try
                {
                    folderBrowserDialog.SelectedPath = GetPersistentDataPath();
                }
                catch (Exception)
                {
                }
            }

            if (folderBrowserDialog.ShowDialog(this) != true)
            {
                return;
            }

            this.PersistentDataPath = folderBrowserDialog.SelectedPath;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.EncryptionKey) || string.IsNullOrEmpty(this.EncryptionIV) || this.SelectedFile == null)
            {
                return;
            }

            this.exportFileDialog.InitialDirectory = Path.GetDirectoryName(this.importFileDialog.FileName);
            this.exportFileDialog.FileName = Path.ChangeExtension(this.SelectedFile.Name, "json");

            if (this.exportFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                SaveFileEncryption encryption = new(this.EncryptionKey, this.EncryptionIV);
                await encryption.DecryptFile(this.SelectedFile, this.exportFileDialog.FileName, this.DeserializeValues);
                MessageBox.Show($"Exported successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (CryptographicException)
            {
                MessageBox.Show($"Failed to decrypt data. Are the encryption key and IV correct?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.EncryptionKey) || string.IsNullOrEmpty(this.EncryptionIV) || this.SelectedFile == null)
            {
                return;
            }

            this.importFileDialog.InitialDirectory = Path.GetDirectoryName(this.exportFileDialog.FileName);

            if (this.importFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                SaveFileEncryption encryption = new(this.EncryptionKey, this.EncryptionIV);
                await encryption.EncryptFile(this.importFileDialog.FileName, this.SelectedFile);
                MessageBox.Show($"Imported successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (CryptographicException)
            {
                MessageBox.Show($"Failed to encrypt data. Are the encryption key and IV correct?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Failed to parse JSON: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            this.PersistentDataPath = GetPersistentDataPath();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            this.LoadProfiles();
        }

        private async Task LoadSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                this.settings = GetDefaultSettings();
                this.NotifySettingsChanged();
                return;
            }

            try
            {
                using (FileStream stream = new(SettingsFilePath, FileMode.Open, FileAccess.Read))
                {
                    this.settings = await JsonSerializer.DeserializeAsync<Settings>(stream) ?? GetDefaultSettings();
                }

                this.NotifySettingsChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NotifySettingsChanged()
        {
            if (this.ValidateProperty(this.PersistentDataPath, nameof(this.PersistentDataPath)))
            {
                this.LoadProfiles();
            }

            this.ValidateProperty(this.EncryptionIV, nameof(this.EncryptionIV));
            this.ValidateProperty(this.EncryptionKey, nameof(this.EncryptionKey));

            this.NotifyPropertyChanged(null);
        }

        private async Task SaveSettings()
        {
            try
            {
                using FileStream stream = new(SettingsFilePath, FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(stream, this.settings, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProfiles()
        {
            if (string.IsNullOrWhiteSpace(this.PersistentDataPath))
            {
                return;
            }

            try
            {
                this.saveFiles.Clear();

                foreach (string directory in Directory.EnumerateDirectories(this.PersistentDataPath, "Profile?", SearchOption.TopDirectoryOnly))
                {
                    foreach (string file in Directory.EnumerateFiles(directory, "*.cls", SearchOption.TopDirectoryOnly))
                    {
                        this.saveFiles.Add(new SaveFile(file, Path.GetFileName(directory), Path.GetFileNameWithoutExtension(file)));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load profiles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnErrorsChanged([CallerMemberName] string? propertyName = "")
        {
            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private bool ValidateProperty<T>(T value, [CallerMemberName] string propertyName = "")
        {
            this.errors.Remove(propertyName);

            if (this.validationRules.TryGetValue(propertyName, out List<ValidationRule>? propertyValidationRules))
            {
                foreach (ValidationRule rule in propertyValidationRules)
                {
                    ValidationResult result = rule.Validate(value, CultureInfo.CurrentCulture);

                    if (!result.IsValid)
                    {
                        this.errors[propertyName] = result.ErrorContent;

                        this.OnErrorsChanged(propertyName);
                        return false;
                    }
                }
            }

            this.OnErrorsChanged(propertyName);
            return true;
        }

        private async void ImportKeys_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new()
            {
                Filter = "UYF Save Encryption Keys|SaveEncryptionKeys.json",
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            UyfKeys? uyfKeys = null;

            try
            {
                using FileStream stream = new(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                uyfKeys = await JsonSerializer.DeserializeAsync<UyfKeys>(stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load keys: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (uyfKeys == null)
            {
                return;
            }

            if (uyfKeys.FileVersion != 1)
            {
                MessageBox.Show($"Unexpected file version {uyfKeys.FileVersion}. Was the file exported with the latest version of AES Key Extractor?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(uyfKeys.Key))
            {
                MessageBox.Show($"Missing key. Was the file exported with the latest version of AES Key Extractor?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(uyfKeys.IV))
            {
                MessageBox.Show($"Missing IV. Was the file exported with the latest version of AES Key Extractor?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.EncryptionKey = uyfKeys.Key;
            this.EncryptionIV = uyfKeys.IV;
        }
    }
}
