// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Pages;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette;

/// <summary>
/// Handles list item creation and filtering for the vault page.
/// </summary>
internal static class VaultListBuilder
{
    /// <summary>
    /// Creates a list item from a vault item.
    /// </summary>
    public static ListItem CreateListItem(
        BitwardenItem item,
        Func<BitwardenItem, ICommand> getPrimaryCommand,
        Func<BitwardenItem, IContextItem[]> getContextCommands,
        Func<BitwardenItem, Details> getDetails)
    {
        var primaryCommand = getPrimaryCommand(item);

        return new ListItem(primaryCommand)
        {
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Subtitle = GetItemSubtitle(item),
            Icon = IconService.GetItemIcon(item),
            MoreCommands = getContextCommands(item),
            Tags = item.Favorite ? [new Tag { Text = ResourceHelper.ItemTagFavorite }] : [],
            Details = getDetails(item)
        };
    }

    /// <summary>
    /// Creates a TOTP list item.
    /// </summary>
    public static ListItem CreateTotpItem(BitwardenItem[] items)
    {
        var totpCount = items.Count(i => !string.IsNullOrEmpty(i.Login?.Totp));
        var totpPage = new TotpPage(items);
        return new ListItem(totpPage)
        {
            Title = ResourceHelper.TotpPageTitle,
            Subtitle = totpCount > 0
                ? ResourceHelper.GetString("TotpItemCount", totpCount)
                : ResourceHelper.TotpNoItems,
            Icon = new IconInfo("\uE8D7") // Stopwatch/Timer icon
        };
    }

    /// <summary>
    /// Creates an empty state list item.
    /// </summary>
    public static ListItem CreateEmptyItem(ICommand refreshCommand)
    {
        return new ListItem(refreshCommand)
        {
            Title = ResourceHelper.StatusNoItems,
            Subtitle = ResourceHelper.StatusNoItemsSubtitle,
            Icon = new IconInfo("\uE7C3") // Empty icon
        };
    }

    /// <summary>
    /// Creates a loading state list item.
    /// </summary>
    public static ListItem CreateLoadingItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.StatusLoading,
            Subtitle = ResourceHelper.StatusLoadingSubtitle,
            Icon = new IconInfo("\uE895") // Sync icon
        };
    }

    /// <summary>
    /// Creates a not logged in list item.
    /// </summary>
    public static ListItem CreateNotLoggedInItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.StatusNotLoggedIn,
            Subtitle = ResourceHelper.StatusNotLoggedInSubtitle,
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    /// <summary>
    /// Creates an error state list item.
    /// </summary>
    public static ListItem CreateErrorItem(string message, ICommand refreshCommand)
    {
        return new ListItem(refreshCommand)
        {
            Title = ResourceHelper.StatusError,
            Subtitle = message,
            Icon = new IconInfo("\uE783") // Error icon
        };
    }

    /// <summary>
    /// Creates an unlock list item.
    /// </summary>
    public static ListItem CreateUnlockItem(BitwardenStatus? lastStatus, Action onUnlocked)
    {
        var unlockPage = new UnlockPage(onUnlocked);
        return new ListItem(unlockPage)
        {
            Title = ResourceHelper.MainUnlockButton,
            Subtitle = lastStatus?.UserEmail ?? ResourceHelper.MainUnlockSubtitle,
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    /// <summary>
    /// Creates a trash list item.
    /// </summary>
    public static ListItem CreateTrashListItem(
        BitwardenItem item,
        Func<BitwardenItem, ICommand> getPrimaryCommand,
        Func<BitwardenItem, IContextItem[]> getContextCommands,
        Func<BitwardenItem, Details> getDetails)
    {
        var primaryCommand = getPrimaryCommand(item);

        return new ListItem(primaryCommand)
        {
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Subtitle = GetItemSubtitle(item),
            Icon = IconService.GetItemIcon(item),
            MoreCommands = getContextCommands(item),
            Details = getDetails(item)
        };
    }

    /// <summary>
    /// Creates an empty trash list item.
    /// </summary>
    public static ListItem CreateTrashEmptyItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.TrashEmpty,
            Subtitle = ResourceHelper.TrashEmptySubtitle,
            Icon = new IconInfo("\uE74D") // Delete/Trash icon
        };
    }

    /// <summary>
    /// Filters vault items based on search text and filter criteria.
    /// </summary>
    public static IEnumerable<BitwardenItem> FilterItems(BitwardenItem[] items, string? searchText, VaultFilter filter)
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

        // Sort: favorites first, then by name
        result = result.OrderByDescending(item => item.Favorite)
                       .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase);

        return result;
    }

    /// <summary>
    /// Filters trash items based on search text.
    /// </summary>
    public static IEnumerable<BitwardenItem> FilterTrashItems(BitwardenItem[] items, string? searchText)
    {
        IEnumerable<BitwardenItem> result = items;

        // Apply search text filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            result = result.Where(item =>
                (item.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Login?.Username?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        // Sort by name
        result = result.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase);

        return result;
    }

    /// <summary>
    /// Gets the subtitle for a vault item based on its type.
    /// </summary>
    public static string GetItemSubtitle(BitwardenItem item)
    {
        return item.ItemType switch
        {
            BitwardenItemType.Login => item.Login?.Username ?? string.Empty,
            BitwardenItemType.Card => GetCardSubtitle(item.Card),
            BitwardenItemType.Identity => GetIdentitySubtitle(item.Identity),
            BitwardenItemType.SecureNote => ResourceHelper.ItemSubtitleSecureNote,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the subtitle for a card item.
    /// </summary>
    public static string GetCardSubtitle(BitwardenCard? card)
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

    /// <summary>
    /// Gets the subtitle for an identity item.
    /// </summary>
    public static string GetIdentitySubtitle(BitwardenIdentity? identity)
    {
        if (identity == null) return string.Empty;
        var nameParts = new[] { identity.FirstName, identity.LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", nameParts);
    }
}
