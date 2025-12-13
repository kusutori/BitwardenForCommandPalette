// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace BitwardenForCommandPalette.Models;

/// <summary>
/// Represents a Bitwarden vault item
/// </summary>
public class BitwardenItem
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("organizationId")]
    public string? OrganizationId { get; set; }

    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("reprompt")]
    public int Reprompt { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("favorite")]
    public bool Favorite { get; set; }

    [JsonPropertyName("login")]
    public BitwardenLogin? Login { get; set; }

    [JsonPropertyName("card")]
    public BitwardenCard? Card { get; set; }

    [JsonPropertyName("identity")]
    public BitwardenIdentity? Identity { get; set; }

    [JsonPropertyName("secureNote")]
    public BitwardenSecureNote? SecureNote { get; set; }

    [JsonPropertyName("fields")]
    public BitwardenField[]? Fields { get; set; }

    [JsonPropertyName("collectionIds")]
    public string[]? CollectionIds { get; set; }

    [JsonPropertyName("revisionDate")]
    public DateTime? RevisionDate { get; set; }

    [JsonPropertyName("creationDate")]
    public DateTime? CreationDate { get; set; }

    [JsonPropertyName("deletedDate")]
    public DateTime? DeletedDate { get; set; }

    /// <summary>
    /// Gets the item type as an enum
    /// </summary>
    [JsonIgnore]
    public BitwardenItemType ItemType => (BitwardenItemType)Type;

    /// <summary>
    /// Gets the display subtitle for the item
    /// </summary>
    [JsonIgnore]
    public string Subtitle
    {
        get
        {
            return ItemType switch
            {
                BitwardenItemType.Login => Login?.Username ?? string.Empty,
                BitwardenItemType.Card => Card?.Brand ?? string.Empty,
                BitwardenItemType.Identity => Identity?.FirstName ?? string.Empty,
                BitwardenItemType.SecureNote => "Secure Note",
                _ => string.Empty
            };
        }
    }
}

/// <summary>
/// Bitwarden item types
/// </summary>
public enum BitwardenItemType
{
    Login = 1,
    SecureNote = 2,
    Card = 3,
    Identity = 4
}

/// <summary>
/// Represents login credentials
/// </summary>
public class BitwardenLogin
{
    [JsonPropertyName("uris")]
    public BitwardenUri[]? Uris { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("totp")]
    public string? Totp { get; set; }

    [JsonPropertyName("passwordRevisionDate")]
    public DateTime? PasswordRevisionDate { get; set; }

    [JsonPropertyName("fido2Credentials")]
    public object[]? Fido2Credentials { get; set; }
}

/// <summary>
/// Represents a URI associated with a login
/// </summary>
public class BitwardenUri
{
    [JsonPropertyName("match")]
    public int? Match { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}

/// <summary>
/// Represents a credit card
/// </summary>
public class BitwardenCard
{
    [JsonPropertyName("cardholderName")]
    public string? CardholderName { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("expMonth")]
    public string? ExpMonth { get; set; }

    [JsonPropertyName("expYear")]
    public string? ExpYear { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// Represents identity information
/// </summary>
public class BitwardenIdentity
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("address3")]
    public string? Address3 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("ssn")]
    public string? Ssn { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("passportNumber")]
    public string? PassportNumber { get; set; }

    [JsonPropertyName("licenseNumber")]
    public string? LicenseNumber { get; set; }
}

/// <summary>
/// Represents a secure note
/// </summary>
public class BitwardenSecureNote
{
    [JsonPropertyName("type")]
    public int Type { get; set; }
}

/// <summary>
/// Represents a custom field
/// </summary>
public class BitwardenField
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("linkedId")]
    public int? LinkedId { get; set; }
}
