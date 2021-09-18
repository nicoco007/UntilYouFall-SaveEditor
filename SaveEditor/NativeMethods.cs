// <copyright file="NativeMethods.cs" company="Nicolas Gnyra">
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
using System.Runtime.InteropServices;

namespace SaveEditor
{
    internal static class NativeMethods
    {
        public static readonly Guid LocalAppDataLow = Guid.Parse("A520A1A4-1780-4FF6-BD18-167343C5AF16");

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHGetKnownFolderPath(Guid folderId, uint flags, IntPtr token, [MarshalAs(UnmanagedType.LPWStr)] out string pszPath);
    }
}
