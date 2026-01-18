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

namespace BitwardenForCommandPalette;

/// <summary>
/// Handles Details panel generation for vault items.
/// </summary>
internal static class ItemDetailsGenerator
{
    /// <summary>
    /// Creates the appropriate Details panel based on item type.
    /// </summary>
    public static Details CreateItemDetails(BitwardenItem item)
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
    /// Creates details for Login type items.
    /// </summary>
    public static Details CreateLoginDetails(BitwardenItem item)
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
                Data = new DetailsLink { Text = VaultPageHelpers.GetTruncatedNotes(item.Notes) }
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
    /// Creates details for Card type items.
    /// </summary>
    public static Details CreateCardDetails(BitwardenItem item)
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
                Data = new DetailsLink { Text = VaultPageHelpers.GetTruncatedNotes(item.Notes) }
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
    /// Creates details for Identity type items.
    /// </summary>
    public static Details CreateIdentityDetails(BitwardenItem item)
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
        var fullName = VaultPageHelpers.GetFullName(identity);
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
        var address = VaultPageHelpers.GetFormattedAddress(identity);
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
        if (VaultPageHelpers.HasIdentificationInfo(identity))
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
                Data = new DetailsLink { Text = VaultPageHelpers.GetTruncatedNotes(item.Notes) }
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
    /// Creates details for SecureNote type items.
    /// </summary>
    public static Details CreateSecureNoteDetails(BitwardenItem item)
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
    /// Creates default details for unknown item types.
    /// </summary>
    public static Details CreateDefaultDetails(BitwardenItem item)
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
    /// Adds custom fields to metadata.
    /// </summary>
    public static void AddCustomFieldsToMetadata(BitwardenItem item, List<IDetailsElement> metadata)
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
    /// Formats the body text for login items.
    /// </summary>
    public static string FormatLoginBody(BitwardenItem item)
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
    /// Formats the body text for card items.
    /// </summary>
    public static string FormatCardBody(BitwardenItem item)
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
    /// Formats the body text for identity items.
    /// </summary>
    public static string FormatIdentityBody(BitwardenItem item)
    {
        var parts = new List<string>();

        var fullName = VaultPageHelpers.GetFullName(item.Identity);
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
}
