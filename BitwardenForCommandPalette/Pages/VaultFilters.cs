// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Pages;

/// <summary>
/// Vault filters that appear in the search bar dropdown
/// </summary>
internal sealed partial class VaultFilters : Filters
{
    // Filter IDs
    public const string AllItemsFilterId = "all";
    public const string FavoritesFilterId = "favorites";
    public const string LoginsFilterId = "logins";
    public const string CardsFilterId = "cards";
    public const string IdentitiesFilterId = "identities";
    public const string NotesFilterId = "notes";
    public const string TrashFilterId = "trash";
    public const string NoFolderFilterId = "folder_none";
    public const string FolderFilterIdPrefix = "folder_";

    private BitwardenFolder[]? _folders;

    public VaultFilters()
    {
        CurrentFilterId = AllItemsFilterId;
    }

    /// <summary>
    /// Update the folders list and notify the UI to refresh
    /// </summary>
    public void UpdateFolders(BitwardenFolder[]? folders)
    {
        _folders = folders;
        // Trigger UI refresh by raising property changed
        OnPropertyChanged(nameof(Filters));
    }

    public override IFilterItem[] GetFilters()
    {
        var filters = new List<IFilterItem>
        {
            // All items
            new Filter
            {
                Id = AllItemsFilterId,
                Name = ResourceHelper.FilterAllItems,
                Icon = new IconInfo("\U0001F4CB") // 📋
            },

            new Separator(),

            // Favorites only
            new Filter
            {
                Id = FavoritesFilterId,
                Name = ResourceHelper.FilterFavoritesOnly,
                Icon = new IconInfo("\u2B50") // ⭐
            },

            new Separator(),

            // By item type
            new Filter
            {
                Id = LoginsFilterId,
                Name = ResourceHelper.FilterLoginsOnly,
                Icon = new IconInfo("\U0001F511") // 🔑
            },

            new Filter
            {
                Id = CardsFilterId,
                Name = ResourceHelper.FilterCardsOnly,
                Icon = new IconInfo("\U0001F4B3") // 💳
            },

            new Filter
            {
                Id = IdentitiesFilterId,
                Name = ResourceHelper.FilterIdentitiesOnly,
                Icon = new IconInfo("\U0001F464") // 👤
            },

            new Filter
            {
                Id = NotesFilterId,
                Name = ResourceHelper.FilterNotesOnly,
                Icon = new IconInfo("\U0001F4DD") // 📝
            },

            new Separator(),

            // Trash
            new Filter
            {
                Id = TrashFilterId,
                Name = ResourceHelper.FilterTrash,
                Icon = new IconInfo("\U0001F5D1") // 🗑️
            }
        };

        // Add folder filters if available
        if (_folders != null && _folders.Length > 0)
        {
            filters.Add(new Separator());

            // "No Folder" option
            filters.Add(new Filter
            {
                Id = NoFolderFilterId,
                Name = ResourceHelper.FilterNoFolder,
                Icon = new IconInfo("\U0001F4C2") // 📂
            });

            // Individual folders
            foreach (var folder in _folders)
            {
                if (folder.Id == null) continue;
                filters.Add(new Filter
                {
                    Id = FolderFilterIdPrefix + folder.Id,
                    Name = ResourceHelper.FilterFolderItem(folder.Name ?? string.Empty),
                    Icon = new IconInfo("\U0001F4C1") // 📁
                });
            }
        }

        return [.. filters];
    }

    /// <summary>
    /// Convert the current filter ID to a VaultFilter object for filtering items
    /// </summary>
    public VaultFilter ToVaultFilter()
    {
        // Check for folder filters
        if (CurrentFilterId == NoFolderFilterId)
        {
            return new VaultFilter { FolderId = "null", FolderName = "No Folder" };
        }

        if (CurrentFilterId.StartsWith(FolderFilterIdPrefix, System.StringComparison.Ordinal))
        {
            var folderId = CurrentFilterId[FolderFilterIdPrefix.Length..];
            var folderName = _folders?.FirstOrDefault(f => f.Id == folderId)?.Name;
            return new VaultFilter { FolderId = folderId, FolderName = folderName };
        }

        return CurrentFilterId switch
        {
            FavoritesFilterId => new VaultFilter { FavoritesOnly = true },
            LoginsFilterId => new VaultFilter { ItemType = BitwardenItemType.Login },
            CardsFilterId => new VaultFilter { ItemType = BitwardenItemType.Card },
            IdentitiesFilterId => new VaultFilter { ItemType = BitwardenItemType.Identity },
            NotesFilterId => new VaultFilter { ItemType = BitwardenItemType.SecureNote },
            TrashFilterId => new VaultFilter { IsTrash = true },
            _ => new VaultFilter() // AllItemsFilterId or any other
        };
    }
}
