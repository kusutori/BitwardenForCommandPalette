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

    public BitwardenForCommandPalettePage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
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
        var filteredItems = FilterItems(_items, SearchText);
        return filteredItems.Select(CreateListItem).ToArray();
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

    private IEnumerable<BitwardenItem> FilterItems(BitwardenItem[] items, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return items;

        var search = searchText.ToLowerInvariant();
        return items.Where(item =>
            (item.Name?.ToLowerInvariant().Contains(search) ?? false) ||
            (item.Login?.Username?.ToLowerInvariant().Contains(search) ?? false) ||
            (item.Login?.Uris?.Any(u => u.Uri?.ToLowerInvariant().Contains(search) ?? false) ?? false)
        );
    }

    private ListItem CreateListItem(BitwardenItem item)
    {
        // Determine the primary command based on item type
        ICommand primaryCommand = item.ItemType switch
        {
            BitwardenItemType.Login => new CopyPasswordCommand(item),
            _ => new CopyPasswordCommand(item)
        };

        var listItem = new ListItem(primaryCommand)
        {
            Title = item.Name ?? "Unnamed Item",
            Subtitle = item.Subtitle,
            Icon = GetItemIcon(item),
            MoreCommands = GetContextCommands(item)
        };

        return listItem;
    }

    private ICommandContextItem[] GetContextCommands(BitwardenItem item)
    {
        var commands = new List<ICommandContextItem>();

        if (item.ItemType == BitwardenItemType.Login)
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

        return commands.ToArray();
    }

    private IconInfo GetItemIcon(BitwardenItem item)
    {
        return item.ItemType switch
        {
            BitwardenItemType.Login => new IconInfo("\uE77B"), // Contact icon
            BitwardenItemType.Card => new IconInfo("\uE8C7"), // Credit card icon
            BitwardenItemType.Identity => new IconInfo("\uE779"), // Contact2 icon
            BitwardenItemType.SecureNote => new IconInfo("\uE70B"), // Note icon
            _ => new IconInfo("\uE72E") // Lock icon
        };
    }

    private ListItem CreateLoadingItem()
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

    private ListItem CreateNotLoggedInItem()
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
