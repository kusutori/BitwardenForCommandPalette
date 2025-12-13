// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace BitwardenForCommandPalette.Models;

/// <summary>
/// Represents the status response from 'bw status' command
/// </summary>
public class BitwardenStatus
{
    [JsonPropertyName("serverUrl")]
    public string? ServerUrl { get; set; }

    [JsonPropertyName("lastSync")]
    public DateTime? LastSync { get; set; }

    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Returns true if the vault is unlocked
    /// </summary>
    [JsonIgnore]
    public bool IsUnlocked => Status?.ToLowerInvariant() == "unlocked";

    /// <summary>
    /// Returns true if the vault is locked
    /// </summary>
    [JsonIgnore]
    public bool IsLocked => Status?.ToLowerInvariant() == "locked";

    /// <summary>
    /// Returns true if the user is logged out
    /// </summary>
    [JsonIgnore]
    public bool IsLoggedOut => Status?.ToLowerInvariant() == "unauthenticated";
}
