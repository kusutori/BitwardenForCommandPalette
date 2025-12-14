// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Pages;

/// <summary>
/// Filter options for the vault
/// </summary>
internal sealed class VaultFilter
{
    public bool FavoritesOnly { get; set; }
    public string? FolderId { get; set; }
    public string? FolderName { get; set; }
    public BitwardenItemType? ItemType { get; set; }
}

/// <summary>
/// Page for selecting vault filters
/// </summary>
internal sealed partial class FilterPage : DynamicListPage
{
    private readonly Action<VaultFilter> _onFilterSelected;
    private readonly VaultFilter _currentFilter;
    private BitwardenFolder[]? _folders;
    private bool _isLoading;

    public FilterPage(VaultFilter currentFilter, Action<VaultFilter> onFilterSelected)
    {
        _currentFilter = currentFilter;
        _onFilterSelected = onFilterSelected;
        Icon = new IconInfo("\uE71C"); // Filter icon
        Name = ResourceHelper.ActionFilter;
        Title = ResourceHelper.FilterPageTitle;
        PlaceholderText = ResourceHelper.FilterPagePlaceholder;

        _ = LoadFoldersAsync();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    private async System.Threading.Tasks.Task LoadFoldersAsync()
    {
        _isLoading = true;
        RaiseItemsChanged();

        try
        {
            _folders = await BitwardenCliService.Instance.GetFoldersAsync();
        }
        finally
        {
            _isLoading = false;
            RaiseItemsChanged();
        }
    }

    public override IListItem[] GetItems()
    {
        if (_isLoading)
        {
            return [new ListItem(new NoOpFilterCommand()) { Title = ResourceHelper.FilterLoadingFolders, Icon = new IconInfo("\uE117") }];
        }

        var items = new List<IListItem>
        {
            // Clear all filters
            new ListItem(new ApplyFilterCommand(new VaultFilter(), _onFilterSelected))
            {
                Title = ResourceHelper.FilterAllItems,
                Subtitle = ResourceHelper.FilterAllItemsSubtitle,
                Icon = new IconInfo("\uE8A5"),
                Tags = _currentFilter.FolderId == null && !_currentFilter.FavoritesOnly && _currentFilter.ItemType == null
                    ? [new Tag { Text = ResourceHelper.FilterTagActive }]
                    : []
            },

            // Favorites only
            new ListItem(new ApplyFilterCommand(new VaultFilter { FavoritesOnly = true }, _onFilterSelected))
            {
                Title = ResourceHelper.FilterFavoritesOnly,
                Subtitle = ResourceHelper.FilterFavoritesSubtitle,
                Icon = new IconInfo("\uE734"),
                Tags = _currentFilter.FavoritesOnly ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
            },

            // By item type section
            new ListItem(new ApplyFilterCommand(new VaultFilter { ItemType = BitwardenItemType.Login }, _onFilterSelected))
            {
                Title = ResourceHelper.FilterLoginsOnly,
                Subtitle = ResourceHelper.FilterLoginsSubtitle,
                Icon = new IconInfo("\uE77B"),
                Tags = _currentFilter.ItemType == BitwardenItemType.Login ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
            },

            new ListItem(new ApplyFilterCommand(new VaultFilter { ItemType = BitwardenItemType.Card }, _onFilterSelected))
            {
                Title = ResourceHelper.FilterCardsOnly,
                Subtitle = ResourceHelper.FilterCardsSubtitle,
                Icon = new IconInfo("\uE8C7"),
                Tags = _currentFilter.ItemType == BitwardenItemType.Card ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
            },

            new ListItem(new ApplyFilterCommand(new VaultFilter { ItemType = BitwardenItemType.Identity }, _onFilterSelected))
            {
                Title = ResourceHelper.FilterIdentitiesOnly,
                Subtitle = ResourceHelper.FilterIdentitiesSubtitle,
                Icon = new IconInfo("\uE77B"),
                Tags = _currentFilter.ItemType == BitwardenItemType.Identity ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
            },

            new ListItem(new ApplyFilterCommand(new VaultFilter { ItemType = BitwardenItemType.SecureNote }, _onFilterSelected))
            {
                Title = ResourceHelper.FilterNotesOnly,
                Subtitle = ResourceHelper.FilterNotesSubtitle,
                Icon = new IconInfo("\uE8A0"),
                Tags = _currentFilter.ItemType == BitwardenItemType.SecureNote ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
            }
        };

        // Add folder filters
        if (_folders != null && _folders.Length > 0)
        {
            items.Add(new SectionHeaderItem(ResourceHelper.FilterByFolder));

            // "No Folder" option
            items.Add(new ListItem(new ApplyFilterCommand(new VaultFilter { FolderId = "null", FolderName = "No Folder" }, _onFilterSelected))
            {
                Title = ResourceHelper.FilterNoFolder,
                Subtitle = ResourceHelper.FilterNoFolderSubtitle,
                Icon = new IconInfo("\uE8B7"),
                Tags = _currentFilter.FolderId == "null" ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
            });

            foreach (var folder in _folders)
            {
                if (folder.Id == null) continue;
                var filter = new VaultFilter { FolderId = folder.Id, FolderName = folder.Name };
                items.Add(new ListItem(new ApplyFilterCommand(filter, _onFilterSelected))
                {
                    Title = ResourceHelper.FilterFolderItem(folder.Name ?? string.Empty),
                    Subtitle = ResourceHelper.FilterFolderSubtitle,
                    Icon = new IconInfo("\uE8B7"),
                    Tags = _currentFilter.FolderId == folder.Id ? [new Tag { Text = ResourceHelper.FilterTagActive }] : []
                });
            }
        }

        return items.ToArray();
    }
}

/// <summary>
/// Command to apply a filter
/// </summary>
internal sealed partial class ApplyFilterCommand : InvokableCommand
{
    private readonly VaultFilter _filter;
    private readonly Action<VaultFilter> _onApply;

    public ApplyFilterCommand(VaultFilter filter, Action<VaultFilter> onApply)
    {
        _filter = filter;
        _onApply = onApply;
        Name = ResourceHelper.ActionApplyFilter;
    }

    public override CommandResult Invoke()
    {
        _onApply(_filter);
        return CommandResult.GoBack();
    }
}

/// <summary>
/// No-op command for filter page headers
/// </summary>
internal sealed partial class NoOpFilterCommand : InvokableCommand
{
    public override CommandResult Invoke() => CommandResult.KeepOpen();
}

/// <summary>
/// Separator list item for visual grouping - just a section header
/// </summary>
internal sealed partial class SectionHeaderItem : ListItem
{
    public SectionHeaderItem(string title) : base(new NoOpFilterCommand())
    {
        Title = title;
        Icon = new IconInfo("\uE8B7"); // Folder icon
    }
}
