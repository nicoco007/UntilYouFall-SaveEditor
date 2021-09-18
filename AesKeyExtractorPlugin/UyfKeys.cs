using System;

namespace SaveEditor.Models
{
    internal class UyfKeys
    {
        public UyfKeys(int fileVersion, string key, string iv)
        {
            this.FileVersion = fileVersion;
            this.Key = key;
            this.IV = iv;
        }

        public int FileVersion { get; set; }

        public string Key { get; set; }

        public string IV { get; set; }
    }
}
