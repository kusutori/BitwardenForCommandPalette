// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Pages;

/// <summary>
/// Page for displaying and managing TOTP codes
/// </summary>
internal sealed partial class TotpPage : DynamicListPage, IDisposable
{
    private readonly BitwardenItem[] _itemsWithTotp;
    private readonly Dictionary<string, string> _totpCodes = new();
    private readonly Dictionary<string, TotpListItem> _totpListItems = new();
    private Timer? _refreshTimer;
    private bool _isLoadingCodes;
    private int _lastRemainingSeconds = -1;
    private bool _disposed;

    public TotpPage(BitwardenItem[] allItems)
    {
        // Filter items that have TOTP
        _itemsWithTotp = allItems
            .Where(item => item.ItemType == BitwardenItemType.Login && !string.IsNullOrEmpty(item.Login?.Totp))
            .OrderBy(item => item.Name)
            .ToArray();

        Icon = new IconInfo("\uE8D7"); // Shield icon for security
        Title = ResourceHelper.TotpPageTitle;
        Name = ResourceHelper.ActionOpen;
        PlaceholderText = ResourceHelper.TotpPagePlaceholder;

        // Start loading TOTP codes
        IsLoading = true;
        _ = LoadTotpCodesAsync();

        // Start timer to refresh remaining time every second
        _refreshTimer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private async Task LoadTotpCodesAsync()
    {
        _isLoadingCodes = true;
        var service = BitwardenCliService.Instance;

        // Load TOTP codes in parallel with limited concurrency
        var semaphore = new SemaphoreSlim(3); // Limit to 3 concurrent requests
        var tasks = _itemsWithTotp.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                var totp = await service.GetTotpAsync(item.Id!);
                if (!string.IsNullOrEmpty(totp))
                {
                    lock (_totpCodes)
                    {
                        _totpCodes[item.Id!] = totp;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _isLoadingCodes = false;
        IsLoading = false;
        RaiseItemsChanged();
    }

    private void OnTimerTick(object? state)
    {
        if (_disposed) return;

        var remaining = CalculateRemainingSeconds();

        // Only update if remaining time changed
        if (remaining != _lastRemainingSeconds)
        {
            _lastRemainingSeconds = remaining;

            // Update all list items with new remaining time
            foreach (var kvp in _totpListItems)
            {
                kvp.Value.UpdateRemainingTime(remaining);
            }

            // When timer reaches 0, refresh TOTP codes
            if (remaining <= 0 || remaining == 30)
            {
                _ = RefreshTotpCodesAsync();
            }
        }
    }

    private async Task RefreshTotpCodesAsync()
    {
        if (_isLoadingCodes) return;

        _isLoadingCodes = true;
        var service = BitwardenCliService.Instance;

        foreach (var item in _itemsWithTotp)
        {
            if (item.Id == null) continue;

            var totp = await service.GetTotpAsync(item.Id);
            if (!string.IsNullOrEmpty(totp))
            {
                lock (_totpCodes)
                {
                    _totpCodes[item.Id] = totp;
                }

                // Update the list item if it exists
                if (_totpListItems.TryGetValue(item.Id, out var listItem))
                {
                    listItem.UpdateTotpCode(totp);
                }
            }
        }

        _isLoadingCodes = false;
    }

    /// <summary>
    /// Calculate remaining seconds until TOTP expires (based on 30-second period)
    /// </summary>
    private static int CalculateRemainingSeconds()
    {
        var now = DateTimeOffset.UtcNow;
        var seconds = (int)(now.ToUnixTimeSeconds() % 30);
        return 30 - seconds;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        if (_isLoadingCodes && _totpCodes.Count == 0)
        {
            return [CreateLoadingItem()];
        }

        if (_itemsWithTotp.Length == 0)
        {
            return [CreateEmptyItem()];
        }

        var remaining = CalculateRemainingSeconds();
        var searchText = SearchText ?? string.Empty;

        var items = _itemsWithTotp
            .Where(item => string.IsNullOrEmpty(searchText) ||
                           (item.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (item.Login?.Username?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true))
            .Select(item => GetOrCreateListItem(item, remaining))
            .ToArray();

        return items.Length > 0 ? items : [CreateNoMatchItem()];
    }

    private TotpListItem GetOrCreateListItem(BitwardenItem item, int remainingSeconds)
    {
        var itemId = item.Id!;

        if (!_totpListItems.TryGetValue(itemId, out var listItem))
        {
            string? totpCode = null;
            lock (_totpCodes)
            {
                _totpCodes.TryGetValue(itemId, out totpCode);
            }

            listItem = new TotpListItem(item, totpCode, remainingSeconds);
            _totpListItems[itemId] = listItem;
        }

        return listItem;
    }

    private static ListItem CreateLoadingItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.TotpLoading,
            Subtitle = ResourceHelper.TotpLoadingSubtitle,
            Icon = new IconInfo("\uE895"), // Sync icon
        };
    }

    private static ListItem CreateEmptyItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.TotpNoItems,
            Subtitle = ResourceHelper.TotpNoItemsSubtitle,
            Icon = new IconInfo("\uE8D7"),
        };
    }

    private static ListItem CreateNoMatchItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.TotpNoMatch,
            Subtitle = ResourceHelper.TotpNoMatchSubtitle,
            Icon = new IconInfo("\uE721"), // Search icon
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _refreshTimer?.Dispose();
        _refreshTimer = null;

        foreach (var listItem in _totpListItems.Values)
        {
            listItem.Dispose();
        }
        _totpListItems.Clear();
    }
}

/// <summary>
/// List item for displaying a TOTP code with remaining time
/// </summary>
internal sealed partial class TotpListItem : ListItem, IDisposable
{
    private readonly BitwardenItem _item;
    private string? _totpCode;
    private int _remainingSeconds;
    private bool _disposed;

    public TotpListItem(BitwardenItem item, string? totpCode, int remainingSeconds)
        : base(new CopyTotpDirectCommand(item))
    {
        _item = item;
        _totpCode = totpCode;
        _remainingSeconds = remainingSeconds;

        Title = item.Name ?? "Unknown";
        Icon = IconService.GetItemIcon(item);

        UpdateDisplay();
    }

    public void UpdateTotpCode(string totpCode)
    {
        _totpCode = totpCode;
        UpdateDisplay();
    }

    public void UpdateRemainingTime(int remainingSeconds)
    {
        if (_remainingSeconds == remainingSeconds) return;
        _remainingSeconds = remainingSeconds;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_disposed) return;

        var username = _item.Login?.Username ?? string.Empty;
        var codeDisplay = !string.IsNullOrEmpty(_totpCode)
            ? FormatTotpCode(_totpCode)
            : ResourceHelper.TotpCodeLoading;

        // Format: "123 456 • 25s" or username if no code
        var timeIndicator = GetTimeIndicator(_remainingSeconds);

        Subtitle = !string.IsNullOrEmpty(username)
            ? $"{username} • {codeDisplay} • {timeIndicator}"
            : $"{codeDisplay} • {timeIndicator}";

        // Update tags with remaining time
        Tags = [CreateTimeTag(_remainingSeconds)];
    }

    /// <summary>
    /// Format TOTP code with space for readability (e.g., "123 456")
    /// </summary>
    private static string FormatTotpCode(string code)
    {
        if (code.Length == 6)
        {
            return $"{code[..3]} {code[3..]}";
        }
        return code;
    }

    /// <summary>
    /// Get time indicator string with emoji based on remaining time
    /// </summary>
    private static string GetTimeIndicator(int seconds)
    {
        return seconds switch
        {
            <= 5 => $"⚠️ {seconds}s",
            <= 10 => $"🟡 {seconds}s",
            _ => $"🟢 {seconds}s"
        };
    }

    /// <summary>
    /// Create a tag showing remaining time with emoji indicator
    /// </summary>
    private static Tag CreateTimeTag(int seconds)
    {
        var text = seconds switch
        {
            <= 5 => $"⚠️ {seconds}s",
            <= 10 => $"🟡 {seconds}s",
            _ => $"🟢 {seconds}s"
        };

        return new Tag { Text = text };
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

/// <summary>
/// Command to directly copy TOTP code (used as primary action on Enter key)
/// </summary>
internal sealed partial class CopyTotpDirectCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyTotpDirectCommand(BitwardenItem item)
    {
        _item = item;
        Name = ResourceHelper.CommandCopyTotp;
        Icon = new IconInfo("\uE8C8"); // Copy icon
    }

    public override CommandResult Invoke()
    {
        var itemId = _item.Id;
        if (string.IsNullOrEmpty(itemId))
        {
            return CommandResult.ShowToast(ResourceHelper.ToastTotpFailed);
        }

        // Get fresh TOTP code
        var totp = BitwardenCliService.Instance.GetTotpAsync(itemId).GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(totp))
        {
            ClipboardHelper.SetText(totp);
            return CommandResult.ShowToast(ResourceHelper.ToastTotpCopied(totp));
        }

        return CommandResult.ShowToast(ResourceHelper.ToastTotpFailed);
    }
}

/// <summary>
/// No-op command for informational list items
/// </summary>
internal sealed partial class NoOpCommand : InvokableCommand
{
    public override CommandResult Invoke() => CommandResult.KeepOpen();
}
