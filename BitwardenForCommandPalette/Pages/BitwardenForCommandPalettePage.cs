// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Commands;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Pages;
using BitwardenForCommandPalette.Services;
using BitwardenForCommandPalette.Vault;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace BitwardenForCommandPalette;

internal sealed partial class BitwardenForCommandPalettePage : DynamicListPage
{
    private BitwardenItem[]? _items;
    private BitwardenItem[]? _trashItems;
    private bool _isLoading;
    private string? _errorMessage;
    private BitwardenStatus? _lastStatus;
    private readonly VaultFilters _vaultFilters;
    private readonly Settings _settings;

    public BitwardenForCommandPalettePage(Settings settings)
    {
        _settings = settings;

        Icon = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.targetsize-24_altform-unplated.png");
        VaultPageHelpers.UpdateTitle(_lastStatus, out var title);
        Title = title;
        Name = ResourceHelper.ActionOpen;
        PlaceholderText = ResourceHelper.MainPagePlaceholder;
        ShowDetails = true; // Enable dual-column layout with details panel

        // Setup search bar filters dropdown
        _vaultFilters = new VaultFilters();
        _vaultFilters.PropChanged += VaultFilters_PropChanged;
        Filters = _vaultFilters;

        // TODO: Setup empty content - This is currently not used because the extension
        // always shows unlock items when locked, so the page is never truly empty.
        // Keeping this for potential future use.
        SetupEmptyContent();

        // Subscribe to service events for dynamic title updates
        var service = BitwardenCliService.Instance;
        service.TitleUpdated += OnTitleUpdated;
        service.StatusChanged += OnStatusChanged;

        // Initial load
        _ = CheckStatusAndLoadAsync();
    }

    private void SetupEmptyContent()
    {
        var emptyContentCommands = new List<IContextItem>
        {
            new Separator(),
            new CommandContextItem(_settings.SettingsPage)
            {
                Title = ResourceHelper.SettingsTitle,
                Subtitle = ResourceHelper.SettingsSubtitle,
                Icon = new IconInfo("\uE713") // Settings icon
            },
            new Separator(),
            new CommandContextItem(new SyncVaultCommand())
            {
                Icon = new IconInfo("\uE895") // Sync icon
            },
            new CommandContextItem(new CreateItemTypeSelectorPage(null))
            {
                Icon = new IconInfo("\uE710") // Add icon
            }
        };

        EmptyContent = new CommandItem(new Microsoft.CommandPalette.Extensions.Toolkit.NoOpCommand())
        {
            Icon = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.targetsize-24_altform-unplated.png"),
            Title = ResourceHelper.AppDisplayName,
            Subtitle = ResourceHelper.EmptyContentSubtitle,
            MoreCommands = emptyContentCommands.ToArray()
        };
    }

    private void OnTitleUpdated(string title)
    {
        Title = title;
    }

    private void OnStatusChanged()
    {
        _ = CheckStatusAndLoadAsync();
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
            return [VaultListBuilder.CreateLoadingItem()];
        }

        // Show loading state
        if (_isLoading)
        {
            return [VaultListBuilder.CreateLoadingItem()];
        }

        // Show error if any
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            var refreshCommand = new RefreshCommand(Refresh);
            return [VaultListBuilder.CreateErrorItem(_errorMessage, refreshCommand)];
        }

        // Check vault status
        if (_lastStatus != null)
        {
            if (_lastStatus.IsLoggedOut)
            {
                return [VaultListBuilder.CreateNotLoggedInItem()];
            }

            if (_lastStatus.IsLocked || !service.IsUnlocked)
            {
                return VaultListBuilder.CreateUnlockItems(_lastStatus, OnUnlocked);
            }
        }

        // Show vault items
        var currentFilter = _vaultFilters.ToVaultFilter();

        // Check if we're in trash view
        if (currentFilter.IsTrash)
        {
            return GetTrashViewItems(currentFilter);
        }

        if (_items == null || _items.Length == 0)
        {
            var refreshCommand = new RefreshCommand(Refresh);
            return [VaultListBuilder.CreateEmptyItem(refreshCommand)];
        }

        // Filter items based on search text and dropdown filter
        var filteredItems = VaultListBuilder.FilterItems(_items, SearchText, currentFilter);

        // Create list with utility commands at the end
        var listItems = new List<IListItem>();

        // Add TOTP entry at the top if no search text
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            listItems.Add(VaultListBuilder.CreateTotpItem(_items));
        }

        listItems.AddRange(filteredItems.Select(item => VaultListBuilder.CreateListItem(
            item,
            GetPrimaryCommand,
            GetContextCommands,
            ItemDetailsGenerator.CreateItemDetails)));

        return listItems.ToArray();
    }

    private IListItem[] GetTrashViewItems(VaultFilter currentFilter)
    {
        if (_trashItems == null || _trashItems.Length == 0)
        {
            return [VaultListBuilder.CreateTrashEmptyItem()];
        }

        // Filter trash items based on search text
        var filteredItems = VaultListBuilder.FilterTrashItems(_trashItems, SearchText);

        var listItems = new List<IListItem>();
        listItems.AddRange(filteredItems.Select(item => VaultListBuilder.CreateTrashListItem(
            item,
            GetTrashPrimaryCommand,
            GetTrashContextCommands,
            ItemDetailsGenerator.CreateItemDetails)));

        return listItems.ToArray();
    }

    private ICommand GetPrimaryCommand(BitwardenItem item)
    {
        return item.ItemType switch
        {
            BitwardenItemType.Login => new CopyPasswordCommand(item),
            BitwardenItemType.Card => new CopyCardNumberCommand(item),
            BitwardenItemType.Identity => new CopyFullNameCommand(item),
            BitwardenItemType.SecureNote => new CopyNotesCommand(item),
            _ => new CopyPasswordCommand(item)
        };
    }

    private ICommand GetTrashPrimaryCommand(BitwardenItem item)
    {
        return new RestoreItemCommand(item, async () =>
        {
            await LoadItemsAsync();
            await LoadTrashItemsAsync();
        });
    }

    private IContextItem[] GetContextCommands(BitwardenItem item)
    {
        return VaultContextCommands.GetContextCommands(
            item,
            onRefresh => new EditItemPage(item, onRefresh),
            (itemToDelete, onRefresh) => new DeleteItemCommand(itemToDelete, onRefresh),
            Refresh);
    }

    private IContextItem[] GetTrashContextCommands(BitwardenItem item)
    {
        return VaultContextCommands.GetTrashContextCommands(
            item,
            onComplete => new RestoreItemCommand(item, onComplete),
            onComplete => new PermanentDeleteCommand(item, onComplete),
            () => _ = LoadTrashItemsAsync(),
            () => _ = LoadItemsAsync());
    }

    private async Task CheckStatusAndLoadAsync()
    {
        await VaultItemsLoader.CheckStatusAndLoadAsync(
            status => _lastStatus = status,
            loading => _isLoading = loading,
            error => _errorMessage = error,
            () => RaiseItemsChanged(),
            () => { VaultPageHelpers.UpdateTitle(_lastStatus, out var title); Title = title; });
    }

    private async Task LoadItemsAsync()
    {
        await VaultItemsLoader.LoadItemsAsync(
            () => BitwardenCliService.Instance.IsUnlocked,
            loading => _isLoading = loading,
            error => _errorMessage = error,
            items => _items = items,
            folders => _vaultFilters.UpdateFolders(folders),
            () => RaiseItemsChanged());
    }

    private async Task LoadTrashItemsAsync()
    {
        await VaultItemsLoader.LoadTrashItemsAsync(
            () => BitwardenCliService.Instance.IsUnlocked,
            loading => _isLoading = loading,
            error => _errorMessage = error,
            items => _trashItems = items,
            () => RaiseItemsChanged());
    }

    private void VaultFilters_PropChanged(object sender, IPropChangedEventArgs args)
    {
        VaultItemsLoader.OnVaultFiltersPropChanged(
            _vaultFilters.ToVaultFilter(),
            _trashItems,
            LoadTrashItemsAsync,
            () => RaiseItemsChanged());
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
