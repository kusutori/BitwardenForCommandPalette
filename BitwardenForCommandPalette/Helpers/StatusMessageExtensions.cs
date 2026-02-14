// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Helpers;

/// <summary>
/// Extension methods for StatusMessage to simplify status banner management
/// </summary>
internal static class StatusMessageExtensions
{
    /// <summary>
    /// Shows the status message in the extension context
    /// </summary>
    public static void ShowStatus(this StatusMessage message)
    {
        ExtensionHostHelper.Instance?.ShowStatus(message, StatusContext.Extension);
    }

    /// <summary>
    /// Hides the status message
    /// </summary>
    public static void Hide(this StatusMessage message)
    {
        ExtensionHostHelper.Instance?.HideStatus(message);
    }

    /// <summary>
    /// Clears the status message content
    /// </summary>
    public static void Clear(this StatusMessage message)
    {
        message.Message = string.Empty;
        message.State = new MessageState();
        message.Progress = null;
    }
}
