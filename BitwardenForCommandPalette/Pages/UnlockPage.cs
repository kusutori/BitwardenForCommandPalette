// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Pages;

/// <summary>
/// Page for unlocking the Bitwarden vault
/// </summary>
internal sealed partial class UnlockPage : ContentPage
{
    private readonly UnlockForm _unlockForm;

    public UnlockPage(Action? onUnlocked = null)
    {
        _unlockForm = new UnlockForm(onUnlocked);
        Icon = new IconInfo("\uE72E"); // Lock icon
        Name = "Unlock";
        Title = "Unlock Bitwarden Vault";
    }

    public override IContent[] GetContent() => [_unlockForm];
}

/// <summary>
/// Form content for entering the master password
/// </summary>
internal sealed partial class UnlockForm : FormContent
{
    private readonly Action? _onUnlocked;

    public UnlockForm(Action? onUnlocked = null)
    {
        _onUnlocked = onUnlocked;

        TemplateJson = """
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "size": "medium",
            "weight": "bolder",
            "text": "🔐 Unlock Bitwarden Vault",
            "horizontalAlignment": "center",
            "wrap": true,
            "style": "heading"
        },
        {
            "type": "TextBlock",
            "text": "Enter your master password to unlock the vault.",
            "wrap": true,
            "spacing": "medium"
        },
        {
            "type": "Input.Text",
            "id": "masterPassword",
            "label": "Master Password",
            "style": "password",
            "isRequired": true,
            "errorMessage": "Master password is required",
            "placeholder": "Enter your master password"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Unlock",
            "style": "positive",
            "data": {
                "action": "unlock"
            }
        }
    ]
}
""";
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();
        if (formInput == null)
        {
            return CommandResult.GoHome();
        }

        var masterPassword = formInput["masterPassword"]?.GetValue<string>();
        if (string.IsNullOrEmpty(masterPassword))
        {
            return CommandResult.GoHome();
        }

        // Perform unlock asynchronously
        _ = UnlockAsync(masterPassword);

        return CommandResult.KeepOpen();
    }

    private async Task UnlockAsync(string masterPassword)
    {
        var service = BitwardenCliService.Instance;
        var (success, message) = await service.UnlockAsync(masterPassword);

        if (success)
        {
            _onUnlocked?.Invoke();
        }
        else
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Unlock failed: {message}");
        }
    }
}
