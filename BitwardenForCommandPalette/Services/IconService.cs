// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BitwardenForCommandPalette.Models;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Services;

/// <summary>
/// Helper class for generating icons for Bitwarden items
/// </summary>
public static partial class IconService
{
    /// <summary>
    /// Bitwarden icon service base URL
    /// </summary>
    private const string IconServiceBaseUrl = "https://icons.bitwarden.net";

    /// <summary>
    /// Cache for icon URLs - Key: domain, Value: IconInfo
    /// </summary>
    private static readonly Dictionary<string, IconInfo> _iconCache = new();

    /// <summary>
    /// Set of domains where icon is known to be unavailable
    /// Populated manually based on actual testing
    /// </summary>
    private static readonly HashSet<string> _unavailableIconDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Known domains where Bitwarden icon service doesn't have icons
        "adobe.com",
        "account.adobe.com"
    };

    /// <summary>
    /// Maximum cached icons before eviction
    /// </summary>
    private const int MaxCacheSize = 200;

    /// <summary>
    /// Default icons for different item types
    /// </summary>
    private static readonly IconInfo DefaultLoginIcon = new("\uE77B"); // Contact icon
    private static readonly IconInfo DefaultWebIcon = new("\uE774"); // Web/Globe icon for websites
    private static readonly IconInfo DefaultCardIcon = new("\uE8C7"); // Credit card icon
    private static readonly IconInfo DefaultIdentityIcon = new("\uE779"); // Contact2 icon
    private static readonly IconInfo DefaultSecureNoteIcon = new("\uE70B"); // Note icon
    private static readonly IconInfo DefaultIcon = new("\uE72E"); // Lock icon

    /// <summary>
    /// Gets the icon for a Bitwarden item
    /// </summary>
    public static IconInfo GetItemIcon(BitwardenItem item)
    {
        // For login items, try to get website icon
        if (item.ItemType == BitwardenItemType.Login)
        {
            var domain = ExtractDomainFromItem(item);
            if (!string.IsNullOrEmpty(domain))
            {
                // Check cache first
                if (_iconCache.TryGetValue(domain, out var cachedIcon))
                {
                    return cachedIcon;
                }

                IconInfo iconInfo;

                // Check if domain is in unavailable list
                if (_unavailableIconDomains.Contains(domain))
                {
                    // Use default web icon for known unavailable domains
                    iconInfo = DefaultWebIcon;
                }
                else
                {
                    // Use icon service URL
                    // UI layer will handle fallback if image fails to load
                    var iconUrl = $"{IconServiceBaseUrl}/{domain}/icon.png";
                    iconInfo = new IconInfo(iconUrl);
                }

                // Manage cache size
                if (_iconCache.Count >= MaxCacheSize)
                {
                    // Remove oldest half when cache is full
                    var toRemove = MaxCacheSize / 2;
                    var keys = new List<string>(_iconCache.Keys);
                    for (int i = 0; i < toRemove && i < keys.Count; i++)
                    {
                        _iconCache.Remove(keys[i]);
                    }
                }

                _iconCache[domain] = iconInfo;
                return iconInfo;
            }
            return DefaultWebIcon;
        }

        // For other item types, return default icons
        return item.ItemType switch
        {
            BitwardenItemType.Card => DefaultCardIcon,
            BitwardenItemType.Identity => DefaultIdentityIcon,
            BitwardenItemType.SecureNote => DefaultSecureNoteIcon,
            _ => DefaultIcon
        };
    }

    /// <summary>
    /// Extracts domain from a login item for icon lookup
    /// </summary>
    private static string? ExtractDomainFromItem(BitwardenItem item)
    {
        // Only process login items with URIs
        if (item.Login?.Uris == null || item.Login.Uris.Length == 0)
            return null;

        // Get the first URI
        var uri = item.Login.Uris[0].Uri;
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        // Extract and return the domain
        return ExtractHostname(uri);
    }

    /// <summary>
    /// Extracts the hostname from a URI string
    /// </summary>
    private static string? ExtractHostname(string uriString)
    {
        // Skip non-HTTP URIs (android://, ios://, etc.)
        if (!uriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !uriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Check if it's a plain domain without protocol
            if (!uriString.Contains("://") && !uriString.StartsWith("android", StringComparison.OrdinalIgnoreCase))
            {
                // Might be a plain domain like "google.com"
                var plainHostname = ExtractDomainFromHostname(uriString.Split('/')[0]);
                if (!string.IsNullOrEmpty(plainHostname))
                    return plainHostname;
            }
            return null;
        }

        try
        {
            var uri = new Uri(uriString);
            var host = uri.Host;

            // Extract the registrable domain (e.g., accounts.google.com -> google.com)
            return ExtractDomainFromHostname(host);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the registrable domain from a hostname
    /// For example: accounts.google.com -> google.com, www.example.co.uk -> example.co.uk
    /// </summary>
    private static string? ExtractDomainFromHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return null;

        hostname = hostname.ToLowerInvariant().Trim();

        // Handle IP addresses - return as-is
        if (IpAddressRegex().IsMatch(hostname))
            return hostname;

        // Handle localhost
        if (hostname == "localhost")
            return null;

        var parts = hostname.Split('.');

        // Need at least 2 parts for a valid domain
        if (parts.Length < 2)
            return null;

        // Handle common two-part TLDs (co.uk, com.au, etc.)
        var twoPartTlds = new[]
        {
            "co.uk", "co.jp", "co.kr", "co.nz", "co.za", "co.in",
            "com.au", "com.br", "com.cn", "com.hk", "com.mx", "com.sg", "com.tw",
            "org.uk", "net.au", "gov.uk", "ac.uk", "edu.au"
        };

        if (parts.Length >= 3)
        {
            var lastTwo = $"{parts[^2]}.{parts[^1]}";
            foreach (var tld in twoPartTlds)
            {
                if (lastTwo.Equals(tld, StringComparison.OrdinalIgnoreCase))
                {
                    // Return last 3 parts (e.g., example.co.uk)
                    return parts.Length >= 3 ? $"{parts[^3]}.{parts[^2]}.{parts[^1]}" : hostname;
                }
            }
        }

        // Default: return last 2 parts (e.g., google.com)
        return $"{parts[^2]}.{parts[^1]}";
    }

    [GeneratedRegex(@"^(\d{1,3}\.){3}\d{1,3}$")]
    private static partial Regex IpAddressRegex();
}
