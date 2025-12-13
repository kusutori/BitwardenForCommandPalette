// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Commands;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Pages;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette;

internal sealed partial class BitwardenForCommandPalettePage : DynamicListPage
{
    private BitwardenItem[]? _items;
    private bool _isLoading;
    private string? _errorMessage;
    private BitwardenStatus? _lastStatus;
    private VaultFilter _currentFilter = new();

    public BitwardenForCommandPalettePage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.targetsize-24_altform-unplated.png");
        Title = "Bitwarden For Command Palette";
        Name = "Open";
        PlaceholderText = "Search vault items...";

        // Initial load
        _ = LoadItemsAsync();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        var service = BitwardenCliService.Instance;

        // Check if we have a status check pending
        if (_lastStatus == null && !_isLoading)
        {
            _ = CheckStatusAndLoadAsync();
            return [CreateLoadingItem()];
        }

        // Show loading state
        if (_isLoading)
        {
            return [CreateLoadingItem()];
        }

        // Show error if any
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            return [CreateErrorItem(_errorMessage)];
        }

        // Check vault status
        if (_lastStatus != null)
        {
            if (_lastStatus.IsLoggedOut)
            {
                return [CreateNotLoggedInItem()];
            }

            if (_lastStatus.IsLocked || !service.IsUnlocked)
            {
                return [CreateUnlockItem()];
            }
        }

        // Show vault items
        if (_items == null || _items.Length == 0)
        {
            return [CreateEmptyItem()];
        }

        // Filter items based on search text
        var filteredItems = FilterItems(_items, SearchText, _currentFilter);

        // Create list with utility commands at the end
        var listItems = new List<IListItem>();

        // Add filter button at the top if no search text
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            listItems.Add(CreateFilterItem());
        }

        listItems.AddRange(filteredItems.Select(CreateListItem));

        // Add utility commands if no search text
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            listItems.Add(CreateSyncItem());
            listItems.Add(CreateLockItem());
        }

        return listItems.ToArray();
    }

    private ListItem CreateFilterItem()
    {
        var filterPage = new FilterPage(_currentFilter, OnFilterApplied);
        var subtitle = GetFilterDescription();
        return new ListItem(filterPage)
        {
            Title = "🔍 Filter",
            Subtitle = subtitle,
            Icon = new IconInfo("\uE71C") // Filter icon
        };
    }

    private string GetFilterDescription()
    {
        var parts = new List<string>();

        if (_currentFilter.FavoritesOnly)
            parts.Add("Favorites");

        if (_currentFilter.ItemType.HasValue)
        {
            parts.Add(_currentFilter.ItemType.Value switch
            {
                BitwardenItemType.Login => "Logins",
                BitwardenItemType.Card => "Cards",
                BitwardenItemType.Identity => "Identities",
                BitwardenItemType.SecureNote => "Notes",
                _ => "All Types"
            });
        }

        if (!string.IsNullOrEmpty(_currentFilter.FolderName))
            parts.Add($"Folder: {_currentFilter.FolderName}");

        return parts.Count > 0 ? string.Join(" | ", parts) : "No filter applied";
    }

    private void OnFilterApplied(VaultFilter filter)
    {
        _currentFilter = filter;
        RaiseItemsChanged();
    }

    private static ListItem CreateSyncItem()
    {
        return new ListItem(new SyncVaultCommand())
        {
            Title = "🔄 Sync Vault",
            Subtitle = "Sync your vault with the Bitwarden server",
            Icon = new IconInfo("\uE895") // Sync icon
        };
    }

    private static ListItem CreateLockItem()
    {
        return new ListItem(new LockVaultCommand())
        {
            Title = "🔒 Lock Vault",
            Subtitle = "Lock your vault and clear the session",
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    private async Task CheckStatusAndLoadAsync()
    {
        _isLoading = true;
        RaiseItemsChanged();

        try
        {
            var service = BitwardenCliService.Instance;
            _lastStatus = await service.GetStatusAsync();

            if (_lastStatus == null)
            {
                _errorMessage = "Failed to get Bitwarden status. Is the CLI installed?";
            }
            else if (_lastStatus.IsUnlocked || service.IsUnlocked)
            {
                await LoadItemsAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            RaiseItemsChanged();
        }
    }

    private async Task LoadItemsAsync()
    {
        var service = BitwardenCliService.Instance;

        if (!service.IsUnlocked)
            return;

        _isLoading = true;
        RaiseItemsChanged();

        try
        {
            _items = await service.GetItemsAsync();
            _errorMessage = null;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load items: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            RaiseItemsChanged();
        }
    }

    private static IEnumerable<BitwardenItem> FilterItems(BitwardenItem[] items, string? searchText, VaultFilter filter)
    {
        IEnumerable<BitwardenItem> result = items;

        // Apply filter options
        if (filter.FavoritesOnly)
        {
            result = result.Where(item => item.Favorite);
        }

        if (filter.ItemType.HasValue)
        {
            result = result.Where(item => item.ItemType == filter.ItemType.Value);
        }

        if (filter.FolderId != null)
        {
            if (filter.FolderId == "null")
            {
                result = result.Where(item => item.FolderId == null);
            }
            else
            {
                result = result.Where(item => item.FolderId == filter.FolderId);
            }
        }

        // Apply search text filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            result = result.Where(item =>
                (item.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Login?.Username?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Login?.Uris?.Any(u => u.Uri?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ?? false)
            );
        }

        return result;
    }

    private ListItem CreateListItem(BitwardenItem item)
    {
        // Determine the primary command based on item type
        ICommand primaryCommand = item.ItemType switch
        {
            BitwardenItemType.Login => new CopyPasswordCommand(item),
            BitwardenItemType.Card => new CopyCardNumberCommand(item),
            BitwardenItemType.Identity => new CopyFullNameCommand(item),
            BitwardenItemType.SecureNote => new CopyNotesCommand(item),
            _ => new CopyPasswordCommand(item)
        };

        var listItem = new ListItem(primaryCommand)
        {
            Title = item.Name ?? "Unnamed Item",
            Subtitle = GetItemSubtitle(item),
            Icon = IconService.GetItemIcon(item),
            MoreCommands = GetContextCommands(item),
            Tags = item.Favorite ? [new Tag { Text = "⭐" }] : []
        };

        return listItem;
    }

    private static string GetItemSubtitle(BitwardenItem item)
    {
        return item.ItemType switch
        {
            BitwardenItemType.Login => item.Login?.Username ?? string.Empty,
            BitwardenItemType.Card => GetCardSubtitle(item.Card),
            BitwardenItemType.Identity => GetIdentitySubtitle(item.Identity),
            BitwardenItemType.SecureNote => "Secure Note",
            _ => string.Empty
        };
    }

    private static string GetCardSubtitle(BitwardenCard? card)
    {
        if (card == null) return string.Empty;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(card.Brand)) parts.Add(card.Brand);
        if (!string.IsNullOrEmpty(card.Number) && card.Number.Length >= 4)
        {
            parts.Add($"****{card.Number[^4..]}");
        }
        return string.Join(" ", parts);
    }

    private static string GetIdentitySubtitle(BitwardenIdentity? identity)
    {
        if (identity == null) return string.Empty;
        var nameParts = new[] { identity.FirstName, identity.LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", nameParts);
    }

    private static ICommandContextItem[] GetContextCommands(BitwardenItem item)
    {
        var commands = new List<ICommandContextItem>();

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

        return commands.ToArray();
    }

    private static void AddLoginCommands(List<ICommandContextItem> commands, BitwardenItem item)
    {
        if (!string.IsNullOrEmpty(item.Login?.Password))
        {
            commands.Add(new CommandContextItem(new CopyPasswordCommand(item)));
        }

        if (!string.IsNullOrEmpty(item.Login?.Username))
        {
            commands.Add(new CommandContextItem(new CopyUsernameCommand(item)));
        }

        if (item.Login?.Uris?.Length > 0 && !string.IsNullOrEmpty(item.Login.Uris[0].Uri))
        {
            commands.Add(new CommandContextItem(new CopyUrlCommand(item)));
            commands.Add(new CommandContextItem(new Commands.OpenUrlCommand(item)));
        }

        if (!string.IsNullOrEmpty(item.Login?.Totp))
        {
            commands.Add(new CommandContextItem(new CopyTotpCommand(item)));
        }
    }

    private static void AddCardCommands(List<ICommandContextItem> commands, BitwardenItem item)
    {
        if (!string.IsNullOrEmpty(item.Card?.Number))
        {
            commands.Add(new CommandContextItem(new CopyCardNumberCommand(item)));
        }

        if (!string.IsNullOrEmpty(item.Card?.Code))
        {
            commands.Add(new CommandContextItem(new CopyCardCvvCommand(item)));
        }

        if (!string.IsNullOrEmpty(item.Card?.ExpMonth) && !string.IsNullOrEmpty(item.Card?.ExpYear))
        {
            commands.Add(new CommandContextItem(new CopyCardExpirationCommand(item)));
        }

        if (!string.IsNullOrEmpty(item.Card?.CardholderName))
        {
            commands.Add(new CommandContextItem(new CopyCardholderNameCommand(item)));
        }
    }

    private static void AddIdentityCommands(List<ICommandContextItem> commands, BitwardenItem item)
    {
        var identity = item.Identity;
        if (identity == null) return;

        // Check if has any name parts
        if (!string.IsNullOrWhiteSpace(identity.FirstName) || !string.IsNullOrWhiteSpace(identity.LastName))
        {
            commands.Add(new CommandContextItem(new CopyFullNameCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.Email))
        {
            commands.Add(new CommandContextItem(new CopyEmailCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.Phone))
        {
            commands.Add(new CommandContextItem(new CopyPhoneCommand(item)));
        }

        // Check if has any address parts
        if (!string.IsNullOrWhiteSpace(identity.Address1) || !string.IsNullOrWhiteSpace(identity.City))
        {
            commands.Add(new CommandContextItem(new CopyAddressCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.Company))
        {
            commands.Add(new CommandContextItem(new CopyCompanyCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.Ssn))
        {
            commands.Add(new CommandContextItem(new CopySsnCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.PassportNumber))
        {
            commands.Add(new CommandContextItem(new CopyPassportCommand(item)));
        }

        if (!string.IsNullOrEmpty(identity.LicenseNumber))
        {
            commands.Add(new CommandContextItem(new CopyLicenseCommand(item)));
        }

        // Add username if different from name
        if (!string.IsNullOrEmpty(identity.Username))
        {
            commands.Add(new CommandContextItem(new InlineCommand(() =>
            {
                ClipboardHelper.SetText(identity.Username);
                return CommandResult.ShowToast(new ToastArgs { Message = "Username copied" });
            })
            { Name = "Copy Username", Icon = new IconInfo("\uE77B") }));
        }
    }

    private static void AddSecureNoteCommands(List<ICommandContextItem> commands, BitwardenItem item)
    {
        if (!string.IsNullOrEmpty(item.Notes))
        {
            commands.Add(new CommandContextItem(new CopyNotesCommand(item)));
        }
    }

    private static ListItem CreateLoadingItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = "Loading...",
            Subtitle = "Please wait while loading vault data",
            Icon = new IconInfo("\uE117") // Sync icon
        };
    }

    private ListItem CreateErrorItem(string message)
    {
        return new ListItem(new RefreshCommand(this))
        {
            Title = "Error",
            Subtitle = message,
            Icon = new IconInfo("\uE783") // Error icon
        };
    }

    private static ListItem CreateNotLoggedInItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = "Not Logged In",
            Subtitle = "Please login using 'bw login' command first",
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    private ListItem CreateUnlockItem()
    {
        // Use UnlockPage directly as the command - it inherits from ContentPage which implements ICommand
        // When user presses Enter, Command Palette will navigate to this page
        var unlockPage = new UnlockPage(() => OnUnlocked());
        return new ListItem(unlockPage)
        {
            Title = "🔐 Unlock Vault",
            Subtitle = _lastStatus?.UserEmail ?? "Enter your master password to unlock",
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    private ListItem CreateEmptyItem()
    {
        return new ListItem(new RefreshCommand(this))
        {
            Title = "No Items",
            Subtitle = "Your vault is empty or no items match the search",
            Icon = new IconInfo("\uE7C3") // Empty icon
        };
    }

    /// <summary>
    /// Refresh the vault items
    /// </summary>
    public void Refresh()
    {
        _lastStatus = null;
        _items = null;
        _errorMessage = null;
        _ = CheckStatusAndLoadAsync();
    }

    /// <summary>
    /// Called after successful unlock
    /// </summary>
    public void OnUnlocked()
    {
        _lastStatus = new BitwardenStatus { Status = "unlocked" };
        _ = LoadItemsAsync();
    }
}

/// <summary>
/// Command that does nothing (for display-only items)
/// </summary>
internal sealed partial class NoOpCommand : InvokableCommand
{
    public NoOpCommand()
    {
        Name = "No Action";
    }

    public override CommandResult Invoke()
    {
        return CommandResult.KeepOpen();
    }
}

/// <summary>
/// Command to refresh the vault
/// </summary>
internal sealed partial class RefreshCommand : InvokableCommand
{
    private readonly BitwardenForCommandPalettePage _page;

    public RefreshCommand(BitwardenForCommandPalettePage page)
    {
        _page = page;
        Name = "Refresh";
        Icon = new IconInfo("\uE72C"); // Refresh icon
    }

    public override CommandResult Invoke()
    {
        _page.Refresh();
        return CommandResult.KeepOpen();
    }
}

/// <summary>
/// Inline command helper for simple operations
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
