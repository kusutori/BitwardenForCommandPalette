// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Pages;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Vault;

/// <summary>
/// Handles asynchronous data loading operations for the vault page.
/// </summary>
internal static class VaultItemsLoader
{
    /// <summary>
    /// Checks the Bitwarden status and loads items if unlocked.
    /// </summary>
    public static async Task CheckStatusAndLoadAsync(
        Action<BitwardenStatus?> setLastStatus,
        Action<bool> setIsLoading,
        Action<string?> setErrorMessage,
        Action raiseItemsChanged,
        Action updateTitle)
    {
        setIsLoading(true);
        raiseItemsChanged();

        try
        {
            var lastStatus = await BitwardenCliService.GetStatusAsync();
            setLastStatus(lastStatus);

            if (lastStatus == null)
            {
                setErrorMessage(ResourceHelper.StatusCliNotInstalled);
            }
            else if (lastStatus.IsUnlocked || BitwardenCliService.Instance.IsUnlocked)
            {
                // Items will be loaded via the normal flow
            }

            updateTitle();
        }
        catch (Exception ex)
        {
            setErrorMessage($"Error: {ex.Message}");
        }
        finally
        {
            setIsLoading(false);
            raiseItemsChanged();
        }
    }

    /// <summary>
    /// Loads vault items asynchronously.
    /// </summary>
    public static async Task LoadItemsAsync(
        Func<bool> isUnlocked,
        Action<bool> setIsLoading,
        Action<string?> setErrorMessage,
        Action<BitwardenItem[]> setItems,
        Action<BitwardenFolder[]> updateFolders,
        Action raiseItemsChanged)
    {
        if (!isUnlocked())
            return;

        setIsLoading(true);
        raiseItemsChanged();

        try
        {
            var service = BitwardenCliService.Instance;

            // Load items and folders in parallel
            var itemsTask = service.GetItemsAsync();
            var foldersTask = service.GetFoldersAsync();

            await Task.WhenAll(itemsTask, foldersTask);

            setItems(await itemsTask!);
            setErrorMessage(null);

            // Update filters with folder information
            var folders = await foldersTask;
            updateFolders(folders!);
        }
        catch (Exception ex)
        {
            setErrorMessage(ResourceHelper.StatusLoadItemsFailed(ex.Message));
        }
        finally
        {
            setIsLoading(false);
            raiseItemsChanged();
        }
    }

    /// <summary>
    /// Loads trash items asynchronously.
    /// </summary>
    public static async Task LoadTrashItemsAsync(
        Func<bool> isUnlocked,
        Action<bool> setIsLoading,
        Action<string?> setErrorMessage,
        Action<BitwardenItem[]> setTrashItems,
        Action raiseItemsChanged)
    {
        if (!isUnlocked())
            return;

        setIsLoading(true);
        raiseItemsChanged();

        try
        {
            var service = BitwardenCliService.Instance;
            setTrashItems(await service.GetTrashItemsAsync()!);
            setErrorMessage(null);
        }
        catch (Exception ex)
        {
            setErrorMessage(ResourceHelper.StatusLoadItemsFailed(ex.Message));
        }
        finally
        {
            setIsLoading(false);
            raiseItemsChanged();
        }
    }

    /// <summary>
    /// Refreshes the status and updates the title.
    /// </summary>
    public static async Task RefreshStatusAndTitleAsync(
        Func<BitwardenStatus?> getLastStatus,
        Action<BitwardenStatus?> setLastStatus,
        Action updateTitle,
        Func<bool> isUnlocked,
        Action<bool> setIsLoading,
        Action<string?> setErrorMessage,
        Action<BitwardenItem[]> setItems,
        Action<BitwardenFolder[]> updateFolders,
        Action raiseItemsChanged)
    {
        var lastStatus = await BitwardenCliService.GetStatusAsync();
        setLastStatus(lastStatus);
        updateTitle();

        if (lastStatus != null && (lastStatus.IsUnlocked || isUnlocked()))
        {
            await LoadItemsAsync(isUnlocked, setIsLoading, setErrorMessage, setItems, updateFolders, raiseItemsChanged);
        }
    }

    /// <summary>
    /// Handles filter property changed event.
    /// </summary>
    public static void OnVaultFiltersPropChanged(
        VaultFilter currentFilter,
        BitwardenItem[]? trashItems,
        Func<Task> loadTrashItems,
        Action raiseItemsChanged)
    {
        if (currentFilter.IsTrash && trashItems == null)
        {
            // Load trash items when switching to trash view
            _ = loadTrashItems();
        }
        else
        {
            // Refresh items when filter changes
            raiseItemsChanged();
        }
    }
}
