// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;

namespace BitwardenForCommandPalette.Helpers;

/// <summary>
/// Static singleton to store and access the IExtensionHost instance.
/// This allows commands and services to show status messages without passing the host around.
/// </summary>
internal static class ExtensionHostHelper
{
    /// <summary>
    /// The singleton instance of IExtensionHost
    /// </summary>
    public static IExtensionHost? Instance { get; set; }
}
