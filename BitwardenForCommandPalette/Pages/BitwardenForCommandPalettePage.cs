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
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace BitwardenForCommandPalette;

internal sealed partial class BitwardenForCommandPalettePage : DynamicListPage
{
    private BitwardenItem[]? _items;
    private bool _isLoading;
    private string? _errorMessage;
    private BitwardenStatus? _lastStatus;
    private readonly VaultFilters _vaultFilters;

    public BitwardenForCommandPalettePage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.targetsize-24_altform-unplated.png");
        Title = ResourceHelper.MainPageTitle;
        Name = ResourceHelper.ActionOpen;
        PlaceholderText = ResourceHelper.MainPagePlaceholder;
        ShowDetails = true; // Enable dual-column layout with details panel

        // Setup search bar filters dropdown
        _vaultFilters = new VaultFilters();
        _vaultFilters.PropChanged += VaultFilters_PropChanged;
        Filters = _vaultFilters;

        // Initial load
        _ = LoadItemsAsync();
    }

    private void VaultFilters_PropChanged(object sender, IPropChangedEventArgs args)
    {
        // Refresh items when filter changes
        RaiseItemsChanged();
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

        // Filter items based on search text and dropdown filter
        var currentFilter = _vaultFilters.ToVaultFilter();
        var filteredItems = FilterItems(_items, SearchText, currentFilter);

        // Create list with utility commands at the end
        var listItems = new List<IListItem>();

        // Add TOTP entry at the top if no search text
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            listItems.Add(CreateTotpItem());
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

    private static ListItem CreateSyncItem()
    {
        return new ListItem(new SyncVaultCommand())
        {
            Title = ResourceHelper.MainSyncButton,
            Subtitle = ResourceHelper.MainSyncSubtitle,
            Icon = new IconInfo("\uE895") // Sync icon
        };
    }

    private static ListItem CreateLockItem()
    {
        return new ListItem(new LockVaultCommand())
        {
            Title = ResourceHelper.MainLockButton,
            Subtitle = ResourceHelper.MainLockSubtitle,
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    private ListItem CreateTotpItem()
    {
        // Count items with TOTP configured
        var totpCount = _items?.Count(i => !string.IsNullOrEmpty(i.Login?.Totp)) ?? 0;
        var totpPage = new TotpPage(_items ?? []);
        return new ListItem(totpPage)
        {
            Title = ResourceHelper.TotpPageTitle,
            Subtitle = totpCount > 0
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, ResourceHelper.TotpItemCount, totpCount)
                : ResourceHelper.TotpNoItems,
            Icon = new IconInfo("\uE8D7") // Stopwatch/Timer icon
        };
    }

    private async Task CheckStatusAndLoadAsync()
    {
        _isLoading = true;
        RaiseItemsChanged();

        try
        {
            var service = BitwardenCliService.Instance;
            _lastStatus = await BitwardenCliService.GetStatusAsync();

            if (_lastStatus == null)
            {
                _errorMessage = ResourceHelper.StatusCliNotInstalled;
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
            // Load items and folders in parallel
            var itemsTask = service.GetItemsAsync();
            var foldersTask = service.GetFoldersAsync();

            await Task.WhenAll(itemsTask, foldersTask);

            _items = await itemsTask;
            _errorMessage = null;

            // Update filters with folder information
            var folders = await foldersTask;
            _vaultFilters.UpdateFolders(folders);
        }
        catch (Exception ex)
        {
            _errorMessage = ResourceHelper.StatusLoadItemsFailed(ex.Message);
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

        // Sort: favorites first, then by name
        result = result.OrderByDescending(item => item.Favorite)
                       .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase);

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
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Subtitle = GetItemSubtitle(item),
            Icon = IconService.GetItemIcon(item),
            MoreCommands = GetContextCommands(item),
            Tags = item.Favorite ? [new Tag { Text = ResourceHelper.ItemTagFavorite }] : [],
            Details = CreateItemDetails(item)
        };

        return listItem;
    }

    /// <summary>
    /// Creates details panel for a vault item
    /// </summary>
    private static Details CreateItemDetails(BitwardenItem item)
    {
        return item.ItemType switch
        {
            BitwardenItemType.Login => CreateLoginDetails(item),
            BitwardenItemType.Card => CreateCardDetails(item),
            BitwardenItemType.Identity => CreateIdentityDetails(item),
            BitwardenItemType.SecureNote => CreateSecureNoteDetails(item),
            _ => CreateDefaultDetails(item)
        };
    }

    /// <summary>
    /// Creates details for Login type items
    /// </summary>
    private static Details CreateLoginDetails(BitwardenItem item)
    {
        var login = item.Login;
        var metadata = new List<IDetailsElement>();

        // Username
        if (!string.IsNullOrEmpty(login?.Username))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsUsername,
                Data = new DetailsLink { Text = login.Username }
            });
        }

        // Password (masked)
        if (!string.IsNullOrEmpty(login?.Password))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsPassword,
                Data = new DetailsLink { Text = "••••••••••••" }
            });
        }

        // TOTP indicator
        if (!string.IsNullOrEmpty(login?.Totp))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsTotp,
                Data = new DetailsTags { Tags = [new Tag(ResourceHelper.DetailsEnabled) { Icon = new IconInfo("\uE73E") }] }
            });
        }

        // Separator before URLs
        if (login?.Uris?.Length > 0)
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsUrls,
                Data = new DetailsSeparator()
            });

            foreach (var uri in login.Uris.Where(u => !string.IsNullOrEmpty(u.Uri)))
            {
                Uri.TryCreate(uri.Uri, UriKind.Absolute, out var parsedUri);
                metadata.Add(new DetailsElement
                {
                    Key = string.Empty,
                    Data = new DetailsLink { Text = uri.Uri ?? string.Empty, Link = parsedUri }
                });
            }
        }

        // Custom fields
        AddCustomFieldsToMetadata(item, metadata);

        // Notes indicator
        if (!string.IsNullOrEmpty(item.Notes))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsNotes,
                Data = new DetailsSeparator()
            });
            metadata.Add(new DetailsElement
            {
                Key = string.Empty,
                Data = new DetailsLink { Text = GetTruncatedNotes(item.Notes) }
            });
        }

        return new Details
        {
            HeroImage = IconService.GetItemIcon(item),
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Body = FormatLoginBody(item),
            Metadata = metadata.ToArray()
        };
    }

    /// <summary>
    /// Creates details for Card type items
    /// </summary>
    private static Details CreateCardDetails(BitwardenItem item)
    {
        var card = item.Card;
        var metadata = new List<IDetailsElement>();

        // Card brand and type
        if (!string.IsNullOrEmpty(card?.Brand))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsBrand,
                Data = new DetailsLink { Text = card.Brand }
            });
        }

        // Card number (masked)
        if (!string.IsNullOrEmpty(card?.Number))
        {
            var maskedNumber = card.Number.Length > 4
                ? $"•••• •••• •••• {card.Number[^4..]}"
                : "•••• •••• •••• ••••";
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsCardNumber,
                Data = new DetailsLink { Text = maskedNumber }
            });
        }

        // Expiration
        if (!string.IsNullOrEmpty(card?.ExpMonth) && !string.IsNullOrEmpty(card?.ExpYear))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsExpiration,
                Data = new DetailsLink { Text = $"{card.ExpMonth}/{card.ExpYear}" }
            });
        }

        // CVV (masked)
        if (!string.IsNullOrEmpty(card?.Code))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsCvv,
                Data = new DetailsLink { Text = "•••" }
            });
        }

        // Cardholder name
        if (!string.IsNullOrEmpty(card?.CardholderName))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsCardholderName,
                Data = new DetailsLink { Text = card.CardholderName }
            });
        }

        // Custom fields
        AddCustomFieldsToMetadata(item, metadata);

        // Notes
        if (!string.IsNullOrEmpty(item.Notes))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsNotes,
                Data = new DetailsSeparator()
            });
            metadata.Add(new DetailsElement
            {
                Key = string.Empty,
                Data = new DetailsLink { Text = GetTruncatedNotes(item.Notes) }
            });
        }

        return new Details
        {
            HeroImage = IconService.GetItemIcon(item),
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Body = FormatCardBody(item),
            Metadata = metadata.ToArray()
        };
    }

    /// <summary>
    /// Creates details for Identity type items
    /// </summary>
    private static Details CreateIdentityDetails(BitwardenItem item)
    {
        var identity = item.Identity;
        var metadata = new List<IDetailsElement>();

        // Personal info section
        metadata.Add(new DetailsElement
        {
            Key = ResourceHelper.DetailsPersonalInfo,
            Data = new DetailsSeparator()
        });

        // Full name
        var fullName = GetFullName(identity);
        if (!string.IsNullOrEmpty(fullName))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsFullName,
                Data = new DetailsLink { Text = fullName }
            });
        }

        // Email
        if (!string.IsNullOrEmpty(identity?.Email))
        {
            Uri.TryCreate($"mailto:{identity.Email}", UriKind.Absolute, out var mailtoUri);
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsEmail,
                Data = new DetailsLink { Text = identity.Email, Link = mailtoUri }
            });
        }

        // Phone
        if (!string.IsNullOrEmpty(identity?.Phone))
        {
            Uri.TryCreate($"tel:{identity.Phone}", UriKind.Absolute, out var telUri);
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsPhone,
                Data = new DetailsLink { Text = identity.Phone, Link = telUri }
            });
        }

        // Company
        if (!string.IsNullOrEmpty(identity?.Company))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsCompany,
                Data = new DetailsLink { Text = identity.Company }
            });
        }

        // Address section
        var address = GetFormattedAddress(identity);
        if (!string.IsNullOrEmpty(address))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsAddress,
                Data = new DetailsSeparator()
            });
            metadata.Add(new DetailsElement
            {
                Key = string.Empty,
                Data = new DetailsLink { Text = address }
            });
        }

        // ID section (if any IDs exist)
        if (HasIdentificationInfo(identity))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsIdentification,
                Data = new DetailsSeparator()
            });

            if (!string.IsNullOrEmpty(identity?.Ssn))
            {
                metadata.Add(new DetailsElement
                {
                    Key = ResourceHelper.DetailsSsn,
                    Data = new DetailsLink { Text = "•••-••-" + (identity.Ssn.Length >= 4 ? identity.Ssn[^4..] : "••••") }
                });
            }

            if (!string.IsNullOrEmpty(identity?.PassportNumber))
            {
                metadata.Add(new DetailsElement
                {
                    Key = ResourceHelper.DetailsPassport,
                    Data = new DetailsLink { Text = identity.PassportNumber }
                });
            }

            if (!string.IsNullOrEmpty(identity?.LicenseNumber))
            {
                metadata.Add(new DetailsElement
                {
                    Key = ResourceHelper.DetailsLicense,
                    Data = new DetailsLink { Text = identity.LicenseNumber }
                });
            }
        }

        // Custom fields
        AddCustomFieldsToMetadata(item, metadata);

        // Notes
        if (!string.IsNullOrEmpty(item.Notes))
        {
            metadata.Add(new DetailsElement
            {
                Key = ResourceHelper.DetailsNotes,
                Data = new DetailsSeparator()
            });
            metadata.Add(new DetailsElement
            {
                Key = string.Empty,
                Data = new DetailsLink { Text = GetTruncatedNotes(item.Notes) }
            });
        }

        return new Details
        {
            HeroImage = IconService.GetItemIcon(item),
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Body = FormatIdentityBody(item),
            Metadata = metadata.ToArray()
        };
    }

    /// <summary>
    /// Creates details for SecureNote type items
    /// </summary>
    private static Details CreateSecureNoteDetails(BitwardenItem item)
    {
        var metadata = new List<IDetailsElement>();

        // Custom fields
        AddCustomFieldsToMetadata(item, metadata);

        // For secure notes, show the full note content in the body with Markdown
        var body = string.Empty;
        if (!string.IsNullOrEmpty(item.Notes))
        {
            body = $"```\n{item.Notes}\n```";
        }

        return new Details
        {
            HeroImage = IconService.GetItemIcon(item),
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Body = body,
            Metadata = metadata.ToArray()
        };
    }

    /// <summary>
    /// Creates default details for unknown item types
    /// </summary>
    private static Details CreateDefaultDetails(BitwardenItem item)
    {
        var metadata = new List<IDetailsElement>();
        AddCustomFieldsToMetadata(item, metadata);

        return new Details
        {
            HeroImage = IconService.GetItemIcon(item),
            Title = item.Name ?? ResourceHelper.ItemSubtitleUnnamed,
            Metadata = metadata.ToArray()
        };
    }

    /// <summary>
    /// Adds custom fields to metadata
    /// </summary>
    private static void AddCustomFieldsToMetadata(BitwardenItem item, List<IDetailsElement> metadata)
    {
        if (item.Fields == null || item.Fields.Length == 0)
            return;

        metadata.Add(new DetailsElement
        {
            Key = ResourceHelper.DetailsCustomFields,
            Data = new DetailsSeparator()
        });

        foreach (var field in item.Fields)
        {
            if (string.IsNullOrEmpty(field.Name))
                continue;

            var displayValue = field.Type == (int)BitwardenFieldType.Hidden
                ? "••••••••"
                : field.Value ?? string.Empty;

            metadata.Add(new DetailsElement
            {
                Key = field.Name,
                Data = new DetailsLink { Text = displayValue }
            });
        }
    }

    /// <summary>
    /// Formats the body text for login items
    /// </summary>
    private static string FormatLoginBody(BitwardenItem item)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(item.Login?.Username))
        {
            parts.Add($"**{ResourceHelper.DetailsUsername}:** {item.Login.Username}");
        }

        var primaryUri = item.Login?.Uris?.FirstOrDefault(u => !string.IsNullOrEmpty(u.Uri))?.Uri;
        if (!string.IsNullOrEmpty(primaryUri))
        {
            parts.Add($"**{ResourceHelper.DetailsWebsite}:** {primaryUri}");
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// Formats the body text for card items
    /// </summary>
    private static string FormatCardBody(BitwardenItem item)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(item.Card?.Brand))
        {
            parts.Add($"**{ResourceHelper.DetailsBrand}:** {item.Card.Brand}");
        }

        if (!string.IsNullOrEmpty(item.Card?.CardholderName))
        {
            parts.Add($"**{ResourceHelper.DetailsCardholderName}:** {item.Card.CardholderName}");
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// Formats the body text for identity items
    /// </summary>
    private static string FormatIdentityBody(BitwardenItem item)
    {
        var parts = new List<string>();

        var fullName = GetFullName(item.Identity);
        if (!string.IsNullOrEmpty(fullName))
        {
            parts.Add($"**{ResourceHelper.DetailsFullName}:** {fullName}");
        }

        if (!string.IsNullOrEmpty(item.Identity?.Email))
        {
            parts.Add($"**{ResourceHelper.DetailsEmail}:** {item.Identity.Email}");
        }

        if (!string.IsNullOrEmpty(item.Identity?.Company))
        {
            parts.Add($"**{ResourceHelper.DetailsCompany}:** {item.Identity.Company}");
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// Gets the full name from identity
    /// </summary>
    private static string GetFullName(BitwardenIdentity? identity)
    {
        if (identity == null) return string.Empty;
        var parts = new[] { identity.Title, identity.FirstName, identity.MiddleName, identity.LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Gets formatted address from identity
    /// </summary>
    private static string GetFormattedAddress(BitwardenIdentity? identity)
    {
        if (identity == null) return string.Empty;

        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(identity.Address1))
            lines.Add(identity.Address1);
        if (!string.IsNullOrWhiteSpace(identity.Address2))
            lines.Add(identity.Address2);
        if (!string.IsNullOrWhiteSpace(identity.Address3))
            lines.Add(identity.Address3);

        var cityStateParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(identity.City))
            cityStateParts.Add(identity.City);
        if (!string.IsNullOrWhiteSpace(identity.State))
            cityStateParts.Add(identity.State);
        if (!string.IsNullOrWhiteSpace(identity.PostalCode))
            cityStateParts.Add(identity.PostalCode);
        if (cityStateParts.Count > 0)
            lines.Add(string.Join(", ", cityStateParts));

        if (!string.IsNullOrWhiteSpace(identity.Country))
            lines.Add(identity.Country);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Checks if identity has any identification info
    /// </summary>
    private static bool HasIdentificationInfo(BitwardenIdentity? identity)
    {
        if (identity == null) return false;
        return !string.IsNullOrEmpty(identity.Ssn) ||
               !string.IsNullOrEmpty(identity.PassportNumber) ||
               !string.IsNullOrEmpty(identity.LicenseNumber);
    }

    /// <summary>
    /// Truncates notes for display
    /// </summary>
    private static string GetTruncatedNotes(string notes)
    {
        const int maxLength = 200;
        if (notes.Length <= maxLength)
            return notes;
        return notes[..maxLength] + "...";
    }

    private static string GetItemSubtitle(BitwardenItem item)
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

    private IContextItem[] GetContextCommands(BitwardenItem item)
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

        // Add separator and edit command at the bottom
        commands.Add(new Separator());
        commands.Add(new CommandContextItem(new EditItemPage(item, () =>
        {
            // Refresh the page after editing
            _ = LoadItemsAsync();
        }))
        {
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.E)
        });

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
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.E)
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
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.E)
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
            })
            { Name = ResourceHelper.CommandCopyUsername, Icon = new IconInfo("\uE77B") })
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.U)
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

    private static ListItem CreateLoadingItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.StatusLoading,
            Subtitle = ResourceHelper.StatusLoadingSubtitle,
            Icon = new IconInfo("\uE895") // Sync icon
        };
    }

    private ListItem CreateErrorItem(string message)
    {
        return new ListItem(new RefreshCommand(this))
        {
            Title = ResourceHelper.StatusError,
            Subtitle = message,
            Icon = new IconInfo("\uE783") // Error icon
        };
    }

    private static ListItem CreateNotLoggedInItem()
    {
        return new ListItem(new NoOpCommand())
        {
            Title = ResourceHelper.StatusNotLoggedIn,
            Subtitle = ResourceHelper.StatusNotLoggedInSubtitle,
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
            Title = ResourceHelper.MainUnlockButton,
            Subtitle = _lastStatus?.UserEmail ?? ResourceHelper.MainUnlockSubtitle,
            Icon = new IconInfo("\uE72E") // Lock icon
        };
    }

    private ListItem CreateEmptyItem()
    {
        return new ListItem(new RefreshCommand(this))
        {
            Title = ResourceHelper.StatusNoItems,
            Subtitle = ResourceHelper.StatusNoItemsSubtitle,
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
        Name = ResourceHelper.ActionNoAction;
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
        Name = ResourceHelper.ActionRefresh;
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
