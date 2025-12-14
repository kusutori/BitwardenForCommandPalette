// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Nodes;
using BitwardenForCommandPalette.Helpers;
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
        Name = ResourceHelper.ActionUnlock;
        Title = ResourceHelper.UnlockPageTitle;
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

        TemplateJson = $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "size": "medium",
            "weight": "bolder",
            "text": "{{ResourceHelper.UnlockCardTitle}}",
            "horizontalAlignment": "center",
            "wrap": true,
            "style": "heading"
        },
        {
            "type": "TextBlock",
            "text": "{{ResourceHelper.UnlockCardDescription}}",
            "wrap": true,
            "spacing": "medium"
        },
        {
            "type": "Input.Text",
            "id": "masterPassword",
            "label": "{{ResourceHelper.UnlockMasterPasswordLabel}}",
            "style": "password",
            "isRequired": true,
            "errorMessage": "{{ResourceHelper.UnlockMasterPasswordRequired}}",
            "placeholder": "{{ResourceHelper.UnlockMasterPasswordPlaceholder}}"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "{{ResourceHelper.UnlockButtonText}}",
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
            return CommandResult.GoBack();
        }

        var masterPassword = formInput["masterPassword"]?.GetValue<string>();
        if (string.IsNullOrEmpty(masterPassword))
        {
            return CommandResult.GoBack();
        }

        // Perform unlock synchronously to get the result
        var service = BitwardenCliService.Instance;
        var (success, message) = service.UnlockAsync(masterPassword).GetAwaiter().GetResult();

        if (success)
        {
            _onUnlocked?.Invoke();
            return CommandResult.GoBack();
        }
        else
        {
            // Show error toast and keep form open for retry
            return CommandResult.ShowToast(ResourceHelper.UnlockFailed);
        }
    }
}
