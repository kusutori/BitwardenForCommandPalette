// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;

namespace BitwardenForCommandPalette.Vault;

/// <summary>
/// Shared helper methods for the vault page.
/// </summary>
internal static class VaultPageHelpers
{
    /// <summary>
    /// Gets the full name from identity.
    /// </summary>
    public static string GetFullName(BitwardenIdentity? identity)
    {
        if (identity == null) return string.Empty;
        var parts = new[] { identity.Title, identity.FirstName, identity.MiddleName, identity.LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Gets formatted address from identity.
    /// </summary>
    public static string GetFormattedAddress(BitwardenIdentity? identity)
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
    /// Checks if identity has any identification info.
    /// </summary>
    public static bool HasIdentificationInfo(BitwardenIdentity? identity)
    {
        if (identity == null) return false;
        return !string.IsNullOrEmpty(identity.Ssn) ||
               !string.IsNullOrEmpty(identity.PassportNumber) ||
               !string.IsNullOrEmpty(identity.LicenseNumber);
    }

    /// <summary>
    /// Truncates notes for display.
    /// </summary>
    public static string GetTruncatedNotes(string notes)
    {
        const int maxLength = 200;
        if (notes.Length <= maxLength)
            return notes;
        return notes[..maxLength] + "...";
    }

    /// <summary>
    /// Updates the title to show last sync time.
    /// </summary>
    public static void UpdateTitle(BitwardenStatus? lastStatus, out string title)
    {
        if (lastStatus?.LastSync != null)
        {
            var lastSync = lastStatus.LastSync.Value.ToLocalTime();
            var syncTimeStr = lastSync.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.CurrentCulture);
            title = $"{ResourceHelper.MainPageTitle} ({ResourceHelper.MainPageLastSync}: {syncTimeStr})";
        }
        else
        {
            title = ResourceHelper.MainPageTitle;
        }
    }
}
