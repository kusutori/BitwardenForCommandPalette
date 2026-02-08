// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Vault;

/// <summary>
/// Command that does nothing (for display-only items).
/// </summary>
internal sealed partial class NoOpCommand : InvokableCommand
{
    public NoOpCommand()
    {
        Name = ResourceHelper.ActionNoAction;
    }

    public override CommandResult Invoke()
    {
        return CommandResult.KeepOpen();
    }
}

/// <summary>
/// Command to refresh the vault.
/// </summary>
internal sealed partial class RefreshCommand : InvokableCommand
{
    private readonly Action _refresh;

    public RefreshCommand(Action refresh)
    {
        _refresh = refresh;
        Name = ResourceHelper.ActionRefresh;
        Icon = new IconInfo("\uE72C"); // Refresh icon
    }

    public override CommandResult Invoke()
    {
        _refresh();
        return CommandResult.KeepOpen();
    }
}

/// <summary>
/// Command to unlock the vault using Windows Hello biometric authentication via bwbio CLI.
/// </summary>
internal sealed partial class BiometricUnlockCommand : InvokableCommand
{
    private readonly Action _onUnlocked;

    public BiometricUnlockCommand(Action onUnlocked)
    {
        _onUnlocked = onUnlocked;
        Name = ResourceHelper.UnlockBiometricButton;
        Icon = new IconInfo("\uE8D7"); // Shield icon for biometric security
    }

    public override CommandResult Invoke()
    {
        var service = BitwardenCliService.Instance;
        var (success, message) = service.UnlockWithBiometricAsync().GetAwaiter().GetResult();

        if (success)
        {
            _onUnlocked();

            if (message.Contains("network", StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.ShowToast(ResourceHelper.GetString("UnlockNetworkWarning"));
            }

            return CommandResult.KeepOpen();
        }
        else
        {
            return CommandResult.ShowToast(message);
        }
    }
}

/// <summary>
/// Inline command helper for simple operations.
/// </summary>
internal sealed partial class InlineCommand : InvokableCommand
{
    private readonly Func<CommandResult> _action;

    public InlineCommand(Func<CommandResult> action)
    {
        _action = action;
    }

    public override CommandResult Invoke() => _action();
}
