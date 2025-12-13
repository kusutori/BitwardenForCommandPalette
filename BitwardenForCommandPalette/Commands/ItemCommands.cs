// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BitwardenForCommandPalette.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Commands;

/// <summary>
/// Command to copy password to clipboard
/// </summary>
internal sealed partial class CopyPasswordCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyPasswordCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Password";
        Icon = new IconInfo("\uE8C8"); // Copy icon
    }

    public override CommandResult Invoke()
    {
        var password = _item.Login?.Password;
        if (!string.IsNullOrEmpty(password))
        {
            ClipboardHelper.SetText(password);
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy username to clipboard
/// </summary>
internal sealed partial class CopyUsernameCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyUsernameCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Username";
        Icon = new IconInfo("\uE77B"); // Contact icon
    }

    public override CommandResult Invoke()
    {
        var username = _item.Login?.Username;
        if (!string.IsNullOrEmpty(username))
        {
            ClipboardHelper.SetText(username);
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy URL to clipboard
/// </summary>
internal sealed partial class CopyUrlCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyUrlCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy URL";
        Icon = new IconInfo("\uE71B"); // Link icon
    }

    public override CommandResult Invoke()
    {
        var url = _item.Login?.Uris?.Length > 0 ? _item.Login.Uris[0].Uri : null;
        if (!string.IsNullOrEmpty(url))
        {
            ClipboardHelper.SetText(url);
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to open URL in browser
/// </summary>
internal sealed partial class OpenUrlCommand : InvokableCommand
{
    private readonly string? _url;

    public OpenUrlCommand(BitwardenItem item)
    {
        _url = item.Login?.Uris?.Length > 0 ? item.Login.Uris[0].Uri : null;
        Name = "Open URL";
        Icon = new IconInfo("\uE774"); // Globe icon
    }

    public override CommandResult Invoke()
    {
        if (!string.IsNullOrEmpty(_url))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors opening URL
            }
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy TOTP code to clipboard
/// </summary>
internal sealed partial class CopyTotpCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyTotpCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy TOTP";
        Icon = new IconInfo("\uE121"); // Time icon
    }

    public override CommandResult Invoke()
    {
        var totp = _item.Login?.Totp;
        if (!string.IsNullOrEmpty(totp))
        {
            // TODO: Generate TOTP code from secret
            ClipboardHelper.SetText(totp);
        }
        return CommandResult.Dismiss();
    }
}
