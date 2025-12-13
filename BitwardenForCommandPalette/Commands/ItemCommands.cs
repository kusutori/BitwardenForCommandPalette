// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Commands;

/// <summary>
/// Command to copy password to clipboard
/// </summary>
internal sealed partial class CopyPasswordCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyPasswordCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Password";
        Icon = new IconInfo("\uE8C8"); // Copy icon
    }

    public override CommandResult Invoke()
    {
        var password = _item.Login?.Password;
        if (!string.IsNullOrEmpty(password))
        {
            ClipboardHelper.SetText(password);
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy username to clipboard
/// </summary>
internal sealed partial class CopyUsernameCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyUsernameCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Username";
        Icon = new IconInfo("\uE77B"); // Contact icon
    }

    public override CommandResult Invoke()
    {
        var username = _item.Login?.Username;
        if (!string.IsNullOrEmpty(username))
        {
            ClipboardHelper.SetText(username);
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy URL to clipboard
/// </summary>
internal sealed partial class CopyUrlCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyUrlCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy URL";
        Icon = new IconInfo("\uE71B"); // Link icon
    }

    public override CommandResult Invoke()
    {
        var url = _item.Login?.Uris?.Length > 0 ? _item.Login.Uris[0].Uri : null;
        if (!string.IsNullOrEmpty(url))
        {
            ClipboardHelper.SetText(url);
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to open URL in browser
/// </summary>
internal sealed partial class OpenUrlCommand : InvokableCommand
{
    private readonly string? _url;

    public OpenUrlCommand(BitwardenItem item)
    {
        _url = item.Login?.Uris?.Length > 0 ? item.Login.Uris[0].Uri : null;
        Name = "Open URL";
        Icon = new IconInfo("\uE774"); // Globe icon
    }

    public override CommandResult Invoke()
    {
        if (!string.IsNullOrEmpty(_url))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors opening URL
            }
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy TOTP code to clipboard
/// </summary>
internal sealed partial class CopyTotpCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyTotpCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy TOTP";
        Icon = new IconInfo("\uE121"); // Time icon
    }

    public override CommandResult Invoke()
    {
        // Use bw get totp to generate the actual TOTP code
        var itemId = _item.Id;
        if (string.IsNullOrEmpty(itemId))
        {
            return CommandResult.Dismiss();
        }

        var totp = BitwardenCliService.Instance.GetTotpAsync(itemId).GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(totp))
        {
            ClipboardHelper.SetText(totp);
            return CommandResult.ShowToast($"TOTP copied: {totp}");
        }

        return CommandResult.ShowToast("❌ Failed to generate TOTP");
    }
}

#region Card Commands

/// <summary>
/// Command to copy card number to clipboard
/// </summary>
internal sealed partial class CopyCardNumberCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyCardNumberCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Card Number";
        Icon = new IconInfo("\uE8C7"); // Card icon
    }

    public override CommandResult Invoke()
    {
        var number = _item.Card?.Number;
        if (!string.IsNullOrEmpty(number))
        {
            ClipboardHelper.SetText(number);
            // Show only last 4 digits in toast for security
            var lastFour = number.Length > 4 ? number[^4..] : number;
            return CommandResult.ShowToast($"Card number copied (****{lastFour})");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy card CVV to clipboard
/// </summary>
internal sealed partial class CopyCardCvvCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyCardCvvCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy CVV";
        Icon = new IconInfo("\uE8D7"); // Shield icon
    }

    public override CommandResult Invoke()
    {
        var code = _item.Card?.Code;
        if (!string.IsNullOrEmpty(code))
        {
            ClipboardHelper.SetText(code);
            return CommandResult.ShowToast("CVV copied");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy card expiration date to clipboard
/// </summary>
internal sealed partial class CopyCardExpirationCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyCardExpirationCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Expiration";
        Icon = new IconInfo("\uE787"); // Calendar icon
    }

    public override CommandResult Invoke()
    {
        var card = _item.Card;
        if (card != null && !string.IsNullOrEmpty(card.ExpMonth) && !string.IsNullOrEmpty(card.ExpYear))
        {
            var expiration = $"{card.ExpMonth}/{card.ExpYear}";
            ClipboardHelper.SetText(expiration);
            return CommandResult.ShowToast($"Expiration copied: {expiration}");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy cardholder name to clipboard
/// </summary>
internal sealed partial class CopyCardholderNameCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyCardholderNameCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Cardholder Name";
        Icon = new IconInfo("\uE77B"); // Contact icon
    }

    public override CommandResult Invoke()
    {
        var name = _item.Card?.CardholderName;
        if (!string.IsNullOrEmpty(name))
        {
            ClipboardHelper.SetText(name);
            return CommandResult.ShowToast($"Cardholder name copied");
        }
        return CommandResult.Dismiss();
    }
}

#endregion

#region Identity Commands

/// <summary>
/// Command to copy full name to clipboard
/// </summary>
internal sealed partial class CopyFullNameCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyFullNameCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Full Name";
        Icon = new IconInfo("\uE77B"); // Contact icon
    }

    public override CommandResult Invoke()
    {
        var identity = _item.Identity;
        if (identity != null)
        {
            var parts = new[] { identity.Title, identity.FirstName, identity.MiddleName, identity.LastName }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var fullName = string.Join(" ", parts);
            if (!string.IsNullOrEmpty(fullName))
            {
                ClipboardHelper.SetText(fullName);
                return CommandResult.ShowToast($"Name copied");
            }
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy email to clipboard
/// </summary>
internal sealed partial class CopyEmailCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyEmailCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Email";
        Icon = new IconInfo("\uE715"); // Mail icon
    }

    public override CommandResult Invoke()
    {
        var email = _item.Identity?.Email;
        if (!string.IsNullOrEmpty(email))
        {
            ClipboardHelper.SetText(email);
            return CommandResult.ShowToast($"Email copied");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy phone number to clipboard
/// </summary>
internal sealed partial class CopyPhoneCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyPhoneCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Phone";
        Icon = new IconInfo("\uE717"); // Phone icon
    }

    public override CommandResult Invoke()
    {
        var phone = _item.Identity?.Phone;
        if (!string.IsNullOrEmpty(phone))
        {
            ClipboardHelper.SetText(phone);
            return CommandResult.ShowToast($"Phone copied");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy address to clipboard
/// </summary>
internal sealed partial class CopyAddressCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyAddressCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Address";
        Icon = new IconInfo("\uE81D"); // Home icon
    }

    public override CommandResult Invoke()
    {
        var identity = _item.Identity;
        if (identity != null)
        {
            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(identity.Address1)) addressParts.Add(identity.Address1);
            if (!string.IsNullOrWhiteSpace(identity.Address2)) addressParts.Add(identity.Address2);
            if (!string.IsNullOrWhiteSpace(identity.Address3)) addressParts.Add(identity.Address3);

            var cityStateZip = new[] { identity.City, identity.State, identity.PostalCode }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            if (cityStateZip.Any()) addressParts.Add(string.Join(", ", cityStateZip));

            if (!string.IsNullOrWhiteSpace(identity.Country)) addressParts.Add(identity.Country);

            var fullAddress = string.Join("\n", addressParts);
            if (!string.IsNullOrEmpty(fullAddress))
            {
                ClipboardHelper.SetText(fullAddress);
                return CommandResult.ShowToast("Address copied");
            }
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy company to clipboard
/// </summary>
internal sealed partial class CopyCompanyCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyCompanyCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Company";
        Icon = new IconInfo("\uE731"); // Building icon
    }

    public override CommandResult Invoke()
    {
        var company = _item.Identity?.Company;
        if (!string.IsNullOrEmpty(company))
        {
            ClipboardHelper.SetText(company);
            return CommandResult.ShowToast($"Company copied");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy SSN to clipboard
/// </summary>
internal sealed partial class CopySsnCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopySsnCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy SSN";
        Icon = new IconInfo("\uE8D7"); // Shield icon
    }

    public override CommandResult Invoke()
    {
        var ssn = _item.Identity?.Ssn;
        if (!string.IsNullOrEmpty(ssn))
        {
            ClipboardHelper.SetText(ssn);
            return CommandResult.ShowToast("SSN copied");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy passport number to clipboard
/// </summary>
internal sealed partial class CopyPassportCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyPassportCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Passport Number";
        Icon = new IconInfo("\uE8F1"); // Airplane icon
    }

    public override CommandResult Invoke()
    {
        var passport = _item.Identity?.PassportNumber;
        if (!string.IsNullOrEmpty(passport))
        {
            ClipboardHelper.SetText(passport);
            return CommandResult.ShowToast("Passport number copied");
        }
        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Command to copy license number to clipboard
/// </summary>
internal sealed partial class CopyLicenseCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyLicenseCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy License Number";
        Icon = new IconInfo("\uE7EF"); // Car icon
    }

    public override CommandResult Invoke()
    {
        var license = _item.Identity?.LicenseNumber;
        if (!string.IsNullOrEmpty(license))
        {
            ClipboardHelper.SetText(license);
            return CommandResult.ShowToast("License number copied");
        }
        return CommandResult.Dismiss();
    }
}

#endregion

#region Secure Note Commands

/// <summary>
/// Command to copy secure note content to clipboard
/// </summary>
internal sealed partial class CopyNotesCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyNotesCommand(BitwardenItem item)
    {
        _item = item;
        Name = "Copy Notes";
        Icon = new IconInfo("\uE8A0"); // Note icon
    }

    public override CommandResult Invoke()
    {
        var notes = _item.Notes;
        if (!string.IsNullOrEmpty(notes))
        {
            ClipboardHelper.SetText(notes);
            return CommandResult.ShowToast("Notes copied");
        }
        return CommandResult.Dismiss();
    }
}

#endregion

#region Custom Field Commands

/// <summary>
/// Command to copy a custom field value to clipboard
/// </summary>
internal sealed partial class CopyFieldCommand : InvokableCommand
{
    private readonly BitwardenField _field;

    public CopyFieldCommand(BitwardenField field)
    {
        _field = field;
        Name = $"Copy {field.Name}";
        Icon = new IconInfo("\uE8C8"); // Copy icon
    }

    public override CommandResult Invoke()
    {
        var value = _field.Value;
        if (!string.IsNullOrEmpty(value))
        {
            ClipboardHelper.SetText(value);
            return CommandResult.ShowToast($"{_field.Name} copied");
        }
        return CommandResult.Dismiss();
    }
}

#endregion

#region Vault Commands

/// <summary>
/// Command to sync the vault
/// </summary>
internal sealed partial class SyncVaultCommand : InvokableCommand
{
    public SyncVaultCommand()
    {
        Name = "Sync Vault";
        Icon = new IconInfo("\uE895"); // Sync icon
    }

    public override CommandResult Invoke()
    {
        var success = BitwardenCliService.Instance.SyncAsync().GetAwaiter().GetResult();
        if (success)
        {
            return CommandResult.ShowToast("Vault synced successfully");
        }
        return CommandResult.ShowToast("❌ Failed to sync vault");
    }
}

/// <summary>
/// Command to lock the vault
/// </summary>
internal sealed partial class LockVaultCommand : InvokableCommand
{
    public LockVaultCommand()
    {
        Name = "Lock Vault";
        Icon = new IconInfo("\uE72E"); // Lock icon
    }

    public override CommandResult Invoke()
    {
        var success = BitwardenCliService.Instance.LockAsync().GetAwaiter().GetResult();
        if (success)
        {
            return CommandResult.ShowToast("Vault locked");
        }
        return CommandResult.ShowToast("❌ Failed to lock vault");
    }
}

#endregion
