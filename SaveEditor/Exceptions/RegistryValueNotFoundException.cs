﻿// <copyright file="RegistryValueNotFoundException.cs" company="Nicolas Gnyra">
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

namespace SaveEditor.Exceptions
{
    internal class RegistryValueNotFoundException : Exception
    {
        public RegistryValueNotFoundException(string valueName)
            : base($"Could not find registry value '{valueName}'")
        {
        }

        public RegistryValueNotFoundException(string valueName, string keyName)
            : base($"Could not find registry value '{valueName}' in key '{keyName}'")
        {
        }
    }
}
