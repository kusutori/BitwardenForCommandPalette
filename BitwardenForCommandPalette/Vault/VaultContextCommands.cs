// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BitwardenForCommandPalette.Commands;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Pages;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace BitwardenForCommandPalette.Vault;

/// <summary>
/// Builds context menu commands for vault items.
/// </summary>
internal static class VaultContextCommands
{
    /// <summary>
    /// Gets context commands for a vault item.
    /// </summary>
    public static IContextItem[] GetContextCommands(
        BitwardenItem item,
        Func<Action, EditItemPage> createEditPage,
        Func<BitwardenItem, Action, Commands.DeleteItemCommand> createDeleteCommand,
        Action refreshItems)
    {
        var commands = new List<IContextItem>();

        switch (item.ItemType)
        {
            case BitwardenItemType.Login:
                AddLoginCommands(commands, item);
                break;
            case BitwardenItemType.Card:
                AddCardCommands(commands, item);
                break;
            case BitwardenItemType.Identity:
                AddIdentityCommands(commands, item);
                break;
            case BitwardenItemType.SecureNote:
                AddSecureNoteCommands(commands, item);
                break;
        }

        // Add notes command for all item types if notes exist
        if (!string.IsNullOrEmpty(item.Notes) && item.ItemType != BitwardenItemType.SecureNote)
        {
            commands.Add(new CommandContextItem(new CopyNotesCommand(item)));
        }

        // Add custom field commands
        if (item.Fields != null && item.Fields.Length > 0)
        {
            foreach (var field in item.Fields)
            {
                if (!string.IsNullOrEmpty(field.Value))
                {
                    commands.Add(new CommandContextItem(new CopyFieldCommand(field)));
                }
            }
        }

        // Add separator, edit command, and delete command at the bottom
        commands.Add(new Separator());
        commands.Add(new CommandContextItem(createEditPage(refreshItems))
        {
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.E)
        });
        commands.Add(new CommandContextItem(createDeleteCommand(item, refreshItems))
        {
            IsCritical = true,
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.Delete)
        });

        // Add common utility commands with separator
        commands.Add(new Separator());
        commands.Add(new CommandContextItem(new SyncVaultCommand())
        {
            Icon = new IconInfo("\uE895"),
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.R)
        });
        commands.Add(new CommandContextItem(new LockVaultCommand())
        {
            Icon = new IconInfo("\uE72E"),
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.L)
        });
        commands.Add(new CommandContextItem(new CreateItemTypeSelectorPage(null))
        {
            Icon = new IconInfo("\uE710"),
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, shift: true, vkey: VirtualKey.A)
        });

        return commands.ToArray();
    }

    /// <summary>
    /// Gets context commands for a trash item.
    /// </summary>
    public static IContextItem[] GetTrashContextCommands(
        BitwardenItem item,
        Func<Action, Commands.RestoreItemCommand> createRestoreCommand,
        Func<Action, Commands.PermanentDeleteCommand> createPermanentDeleteCommand,
        Action refreshTrashItems,
        Action refreshItems)
    {
        // NOTE: The first item in MoreCommands becomes the "secondary command" (Ctrl+Enter)
        // Since primaryCommand (Enter) is RestoreItemCommand, we put PermanentDeleteCommand first
        // so Ctrl+Enter triggers permanent delete (dangerous action requires explicit intent)
        var commands = new List<IContextItem>
        {
            // Permanent delete command first (Ctrl+Enter) - critical/red
            new CommandContextItem(createPermanentDeleteCommand(refreshTrashItems))
            {
                IsCritical = true,
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, shift: true, vkey: VirtualKey.Delete)
            },

            new Separator(),

            // Restore command (also available in menu, but Enter key is primary)
            new CommandContextItem(createRestoreCommand(() =>
            {
                refreshItems();
                refreshTrashItems();
            }))
        };

        return commands.ToArray();
    }

    private static void AddLoginCommands(List<IContextItem> commands, BitwardenItem item)
    {
        // NOTE: The first item in MoreCommands becomes the "secondary command" (Ctrl+Enter)
        // Since primaryCommand (Enter) is CopyPassword, we put CopyUsername first here for Ctrl+Enter

        if (!string.IsNullOrEmpty(item.Login?.Username))
        {
            commands.Add(new CommandContextItem(new CopyUsernameCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.U)
            });
        }

        if (item.Login?.Uris?.Length > 0 && !string.IsNullOrEmpty(item.Login.Uris[0].Uri))
        {
            commands.Add(new CommandContextItem(new CopyUrlCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, shift: true, vkey: VirtualKey.C)
            });
            commands.Add(new CommandContextItem(new Commands.OpenUrlCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.O)
            });
        }

        if (!string.IsNullOrEmpty(item.Login?.Totp))
        {
            commands.Add(new CommandContextItem(new CopyTotpCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.T)
            });
        }

        // Password is already the primary command (Enter key), but add it to More menu with shortcut
        if (!string.IsNullOrEmpty(item.Login?.Password))
        {
            commands.Add(new CommandContextItem(new CopyPasswordCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.P)
            });
        }
    }

    private static void AddCardCommands(List<IContextItem> commands, BitwardenItem item)
    {
        // CVV first for Ctrl+Enter (card number is primary command)
        if (!string.IsNullOrEmpty(item.Card?.Code))
        {
            commands.Add(new CommandContextItem(new CopyCardCvvCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.V)
            });
        }

        if (!string.IsNullOrEmpty(item.Card?.ExpMonth) && !string.IsNullOrEmpty(item.Card?.ExpYear))
        {
            commands.Add(new CommandContextItem(new CopyCardExpirationCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.X)
            });
        }

        if (!string.IsNullOrEmpty(item.Card?.CardholderName))
        {
            commands.Add(new CommandContextItem(new CopyCardholderNameCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.N)
            });
        }

        // Card number is already primary command, but add to More menu
        if (!string.IsNullOrEmpty(item.Card?.Number))
        {
            commands.Add(new CommandContextItem(new CopyCardNumberCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, shift: true, vkey: VirtualKey.N)
            });
        }
    }

    private static void AddIdentityCommands(List<IContextItem> commands, BitwardenItem item)
    {
        var identity = item.Identity;
        if (identity == null) return;

        // Email first for Ctrl+Enter (full name is primary command)
        if (!string.IsNullOrEmpty(identity.Email))
        {
            commands.Add(new CommandContextItem(new CopyEmailCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.M)
            });
        }

        if (!string.IsNullOrEmpty(identity.Phone))
        {
            commands.Add(new CommandContextItem(new CopyPhoneCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.P)
            });
        }

        // Check if has any address parts
        if (!string.IsNullOrWhiteSpace(identity.Address1) || !string.IsNullOrWhiteSpace(identity.City))
        {
            commands.Add(new CommandContextItem(new CopyAddressCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.A)
            });
        }

        if (!string.IsNullOrEmpty(identity.Company))
        {
            commands.Add(new CommandContextItem(new CopyCompanyCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.Ssn))
        {
            commands.Add(new CommandContextItem(new CopySsnCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.S)
            });
        }

        if (!string.IsNullOrEmpty(identity.PassportNumber))
        {
            commands.Add(new CommandContextItem(new CopyPassportCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.LicenseNumber))
        {
            commands.Add(new CommandContextItem(new CopyLicenseCommand(item)));
        }

        // Full name is already primary command, but add to More menu
        if (!string.IsNullOrWhiteSpace(identity.FirstName) || !string.IsNullOrWhiteSpace(identity.LastName))
        {
            commands.Add(new CommandContextItem(new CopyFullNameCommand(item))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.N)
            });
        }

        // Add username if different from name
        if (!string.IsNullOrEmpty(identity.Username))
        {
            commands.Add(new CommandContextItem(new InlineCommand(() =>
            {
                ClipboardHelper.SetText(identity.Username);
                return CommandResult.ShowToast(new ToastArgs { Message = ResourceHelper.ToastUsernameCopied });
            }))
            {
                Icon = new IconInfo("\uE77B")
            });
        }
    }

    private static void AddSecureNoteCommands(List<IContextItem> commands, BitwardenItem item)
    {
        if (!string.IsNullOrEmpty(item.Notes))
        {
            commands.Add(new CommandContextItem(new CopyNotesCommand(item)));
        }
    }
}
